#!/usr/bin/env python3
"""Install a fresh Xray VLESS + REALITY server on Ubuntu.

Run on a new server as root, or stream it over SSH:
    ssh root@NEW_SERVER_IP 'python3 -' < setup_xray.py

The script uses the official XTLS installer, generates new credentials, tests a
local client connection, and prints a VLESS import link. It only removes an
existing Xray installation when --replace is explicitly passed.
"""

from __future__ import annotations

import argparse
import ipaddress
import json
import os
import pathlib
import pwd
import re
import secrets
import shutil
import socket
import ssl
import subprocess
import sys
import tempfile
import time
import urllib.parse
import urllib.request
import uuid


INSTALLER_URL = "https://github.com/XTLS/Xray-install/raw/main/install-release.sh"
XRAY = pathlib.Path("/usr/local/bin/xray")
CONFIG = pathlib.Path("/usr/local/etc/xray/config.json")
LINK_FILE = pathlib.Path("/root/xray-link.txt")
DEFAULT_SITE = "www.xbox.com"
DEFAULT_PORT = 8435
PROFILES = ("xhttp", "vision")


def say(message: str) -> None:
    print(message, file=sys.stderr, flush=True)


def run(*args: str, capture: bool = False) -> subprocess.CompletedProcess[str]:
    if not capture:
        result = subprocess.run(args, text=True, stdout=sys.stderr, stderr=sys.stderr)
        if result.returncode:
            raise RuntimeError(f"Command failed: {args[0]} (exit {result.returncode})")
        return result
    result = subprocess.run(args, text=True, capture_output=True)
    if result.returncode:
        detail = ((result.stdout or "") + "\n" + (result.stderr or "")).strip()
        raise RuntimeError(f"Command failed: {args[0]}\n{detail[-1600:]}")
    return result


def public_ipv4(value: str | None) -> str:
    if value is None:
        with urllib.request.urlopen("https://api.ipify.org", timeout=10) as response:
            value = response.read(80).decode().strip()
    address = ipaddress.ip_address(value)
    if not isinstance(address, ipaddress.IPv4Address) or not address.is_global:
        raise ValueError(f"Expected a public IPv4 address, got {value!r}; pass --ip")
    return str(address)


def install_dependencies() -> None:
    missing = [name for name in ("curl", "unzip") if shutil.which(name) is None]
    if missing:
        if shutil.which("apt-get") is None:
            raise RuntimeError(f"Missing {', '.join(missing)} and apt-get is unavailable")
        say("Installing dependencies: " + ", ".join(missing))
        run("apt-get", "update", "-qq")
        run("apt-get", "install", "-y", "-qq", "ca-certificates", *missing)


def official_install(replace: bool) -> None:
    existing = XRAY.exists() or CONFIG.exists()
    if existing and not replace:
        raise RuntimeError("Xray already exists. Use --replace to remove its old configuration.")
    install_dependencies()
    with tempfile.TemporaryDirectory(prefix="xray-installer-") as directory:
        script = pathlib.Path(directory) / "install-release.sh"
        request = urllib.request.Request(INSTALLER_URL, headers={"User-Agent": "xray-bootstrap/1.0"})
        with urllib.request.urlopen(request, timeout=30) as response:
            script.write_bytes(response.read())
        content = script.read_bytes()
        first_line = content.splitlines()[0] if content else b""
        if not (first_line.startswith(b"#!") and b"bash" in first_line):
            raise RuntimeError("The official installer did not download correctly")
        if existing:
            say("Removing the existing Xray installation and configuration")
            run("bash", str(script), "remove", "--purge")
        say("Installing the latest stable Xray with the official installer")
        run("bash", str(script), "install")


def check_port(port: int) -> None:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        try:
            sock.bind(("0.0.0.0", port))
        except OSError as exc:
            raise RuntimeError(f"TCP port {port} is already in use") from exc


def check_site_tls(site: str) -> None:
    context = ssl.create_default_context()
    context.minimum_version = ssl.TLSVersion.TLSv1_3
    context.set_alpn_protocols(["h2", "http/1.1"])
    with socket.create_connection((site, 443), timeout=8) as connection:
        connection.settimeout(8)
        with context.wrap_socket(connection, server_hostname=site) as tls:
            if tls.version() != "TLSv1.3":
                raise RuntimeError(f"{site} does not offer TLS 1.3")
    say(f"REALITY site TLS check passed: {site}")


def check_target(site: str) -> None:
    report = run(str(XRAY), "tls", "ping", site, capture=True).stdout
    with_sni = report.split("Pinging with SNI", 1)[-1]
    if "Handshake succeeded" not in with_sni or "TLS Version:" not in with_sni:
        raise RuntimeError(f"REALITY target {site} did not pass the TLS check")


def new_keypair() -> tuple[str, str]:
    output = run(str(XRAY), "x25519", capture=True).stdout
    fields = {}
    for line in output.splitlines():
        if ":" in line:
            name, value = line.split(":", 1)
            fields[name.strip().lower()] = value.strip()
    private_key = fields.get("privatekey")
    password = next(
        (value for name, value in fields.items() if name.startswith(("password", "public key"))),
        None,
    )
    if not private_key or not password:
        raise RuntimeError("Could not parse the Xray x25519 keypair")
    return private_key, password


def transport_settings(profile: str, path: str) -> dict:
    if profile == "xhttp":
        return {
            "network": "xhttp",
            "xhttpSettings": {"path": path, "mode": "auto"},
        }
    return {
        "network": "raw",
        "rawSettings": {"header": {"type": "none"}},
    }


def server_config(port: int, user_id: str, short_id: str, path: str,
                  private_key: str, site: str, profile: str) -> dict:
    client = {"id": user_id}
    if profile == "vision":
        client["flow"] = "xtls-rprx-vision"
    stream_settings = {
        **transport_settings(profile, path),
        "security": "reality",
        "realitySettings": {
            "target": f"{site}:443",
            "serverNames": [site],
            "privateKey": private_key,
            "shortIds": [short_id],
        },
    }
    return {
        "log": {"loglevel": "warning"},
        "inbounds": [
            {
                "listen": "0.0.0.0",
                "port": port,
                "protocol": "vless",
                "settings": {"clients": [client], "decryption": "none"},
                "streamSettings": stream_settings,
            }
        ],
        "outbounds": [{"protocol": "freedom", "tag": "direct"}],
    }


def write_config(config: dict) -> None:
    CONFIG.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(
        mode="w", dir=CONFIG.parent, prefix=".new-xray-", suffix=".json", delete=False
    ) as file:
        json.dump(config, file, indent=2)
        file.write("\n")
        temporary = pathlib.Path(file.name)
    try:
        nobody_group = pwd.getpwnam("nobody").pw_gid
        os.chown(temporary, 0, nobody_group)
        os.chmod(temporary, 0o640)
        run(str(XRAY), "run", "-test", "-config", str(temporary), capture=True)
        os.replace(temporary, CONFIG)
    finally:
        temporary.unlink(missing_ok=True)


def open_ufw(port: int) -> None:
    if shutil.which("ufw") is None:
        return
    status = run("ufw", "status", capture=True).stdout
    if re.search(r"^Status: active\s*$", status, re.MULTILINE):
        run("ufw", "allow", f"{port}/tcp", capture=True)


def wait_for_port(port: int, seconds: float = 8) -> None:
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        try:
            with socket.create_connection(("127.0.0.1", port), timeout=0.3):
                return
        except OSError:
            time.sleep(0.2)
    raise RuntimeError(f"Xray did not listen on TCP port {port}")


def client_config(port: int, socks_port: int, user_id: str, short_id: str,
                  path: str, password: str, site: str, profile: str) -> dict:
    user = {"id": user_id, "encryption": "none"}
    if profile == "vision":
        user["flow"] = "xtls-rprx-vision"
    stream_settings = {
        **transport_settings(profile, path),
        "security": "reality",
        "realitySettings": {
            "serverName": site,
            "fingerprint": "chrome",
            "password": password,
            "shortId": short_id,
        },
    }
    return {
        "log": {"loglevel": "warning"},
        "inbounds": [
            {"listen": "127.0.0.1", "port": socks_port, "protocol": "socks", "settings": {"udp": False}}
        ],
        "outbounds": [
            {
                "protocol": "vless",
                "settings": {
                    "vnext": [
                        {"address": "127.0.0.1", "port": port, "users": [user]}
                    ]
                },
                "streamSettings": stream_settings,
            }
        ],
    }


def test_connection(port: int, user_id: str, short_id: str, path: str,
                    password: str, site: str, profile: str) -> None:
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        socks_port = sock.getsockname()[1]
    with tempfile.TemporaryDirectory(prefix="xray-client-test-") as directory:
        config_file = pathlib.Path(directory) / "client.json"
        config_file.write_text(json.dumps(
            client_config(port, socks_port, user_id, short_id, path, password, site, profile)
        ))
        run(str(XRAY), "run", "-test", "-config", str(config_file), capture=True)
        with (pathlib.Path(directory) / "client.log").open("w+") as log:
            client = subprocess.Popen([str(XRAY), "run", "-config", str(config_file)],
                                      stdout=log, stderr=log)
            try:
                wait_for_port(socks_port)
                result = subprocess.run(
                    ["curl", "--noproxy", "", "--proxy", f"socks5h://127.0.0.1:{socks_port}",
                     "-sS", "-o", "/dev/null", "-w", "%{http_code}", "--max-time", "15",
                     "https://example.com/"],
                    capture_output=True, text=True,
                )
                if result.returncode or result.stdout.strip() != "200":
                    log.flush()
                    log.seek(0)
                    detail = (result.stderr + "\n" + log.read())[-1600:]
                    raise RuntimeError(f"Proxy test failed (HTTP {result.stdout.strip()}):\n{detail}")
            finally:
                client.terminate()
                try:
                    client.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    client.kill()
                    client.wait()


def make_link(ip: str, port: int, user_id: str, short_id: str,
              path: str, password: str, site: str, profile: str) -> str:
    parameters = {
        "encryption": "none", "security": "reality", "sni": site,
        "fp": "chrome", "pbk": password, "sid": short_id,
    }
    if profile == "xhttp":
        parameters.update({"type": "xhttp", "path": path, "mode": "auto"})
        label = "Xray-XHTTP"
    else:
        # VLESS URI clients commonly call the RAW transport "tcp".
        parameters.update({"type": "tcp", "headerType": "none", "flow": "xtls-rprx-vision"})
        label = "Xray-Vision"
    query = urllib.parse.urlencode(parameters)
    return f"vless://{user_id}@{ip}:{port}?{query}#{label}-{urllib.parse.quote(site)}"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ip", help="Public IPv4 address; auto-detected if omitted")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--site", default=DEFAULT_SITE, help="REALITY site hostname")
    parser.add_argument("--profile", choices=PROFILES, default="xhttp",
                        help="Transport profile: xhttp or RAW/TCP Vision")
    parser.add_argument("--replace", action="store_true", help="Purge an existing Xray installation")
    args = parser.parse_args()
    if os.geteuid() != 0:
        raise RuntimeError("Run this script as root")
    if not 1 <= args.port <= 65535:
        raise ValueError("Port must be between 1 and 65535")
    if not re.fullmatch(r"(?=.{1,253}$)[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?)+", args.site):
        raise ValueError("Site must be a valid lowercase DNS hostname")
    if shutil.which("systemctl") is None:
        raise RuntimeError("This script requires a systemd Linux server")
    ip = public_ipv4(args.ip)
    say(f"Public IP: {ip}")
    if (XRAY.exists() or CONFIG.exists()) and not args.replace:
        raise RuntimeError("Xray already exists. Use --replace to remove its old configuration.")
    if not args.replace:
        check_port(args.port)
    say(f"Selected REALITY site: {args.site}")
    say("Selected profile: " + ("XHTTP + REALITY" if args.profile == "xhttp"
                                else "RAW + REALITY + Vision"))
    check_site_tls(args.site)
    official_install(args.replace)
    if args.replace:
        check_port(args.port)
    check_target(args.site)
    private_key, password = new_keypair()
    user_id = str(uuid.uuid4())
    short_id = secrets.token_hex(8)
    path = "/x/" + secrets.token_urlsafe(18)
    write_config(server_config(
        args.port, user_id, short_id, path, private_key, args.site, args.profile
    ))
    open_ufw(args.port)
    run("systemctl", "enable", "xray", capture=True)
    run("systemctl", "restart", "xray", capture=True)
    wait_for_port(args.port)
    if run("systemctl", "is-active", "xray", capture=True).stdout.strip() != "active":
        raise RuntimeError("Xray service is not active")
    profile_name = "XHTTP + REALITY" if args.profile == "xhttp" else "RAW + REALITY + Vision"
    say(f"Testing a {profile_name} connection")
    test_connection(args.port, user_id, short_id, path, password, args.site, args.profile)
    link = make_link(ip, args.port, user_id, short_id, path, password, args.site, args.profile)
    with tempfile.NamedTemporaryFile("w", dir=LINK_FILE.parent, prefix=".xray-link-", delete=False) as file:
        file.write(link + "\n")
        temporary = pathlib.Path(file.name)
    temporary.chmod(0o600)
    os.replace(temporary, LINK_FILE)
    print(link, flush=True)
    say(f"Verified. A root-only copy is saved at {LINK_FILE}")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, RuntimeError, subprocess.SubprocessError) as error:
        say(f"ERROR: {error}")
        sys.exit(1)
