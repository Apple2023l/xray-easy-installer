#!/usr/bin/env python3
"""Install Xray VLESS + REALITY or Hysteria 2 on a Linux server.

Run on a new server as root, or stream it over SSH:
    ssh root@NEW_SERVER_IP 'python3 -' < setup_xray.py

The script uses the official project installers, generates new credentials,
tests a local client connection, and prints an import link. It only removes an
existing installation when --replace is explicitly passed.
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
HYSTERIA_INSTALLER_URL = "https://get.hy2.sh/"
XRAY = pathlib.Path("/usr/local/bin/xray")
CONFIG = pathlib.Path("/usr/local/etc/xray/config.json")
LINK_FILE = pathlib.Path("/root/xray-link.txt")
HYSTERIA = pathlib.Path("/usr/local/bin/hysteria")
HYSTERIA_CONFIG = pathlib.Path("/etc/hysteria/config.yaml")
HYSTERIA_LINK_FILE = pathlib.Path("/root/hysteria2-link.txt")
DEFAULT_SITE = "www.xbox.com"
DEFAULT_PORT = 8435
DEFAULT_HYSTERIA_PORT = 40460
PROFILES = ("xhttp", "vision", "hysteria2")


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



def check_udp_port(port: int) -> None:
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        try:
            sock.bind(("0.0.0.0", port))
        except OSError as exc:
            raise RuntimeError(f"UDP port {port} is already in use") from exc


def check_domain(domain: str, ip: str) -> None:
    addresses = {
        item[4][0] for item in socket.getaddrinfo(domain, 443, socket.AF_INET, socket.SOCK_STREAM)
    }
    if ip not in addresses:
        found = ", ".join(sorted(addresses)) or "no IPv4 records"
        raise RuntimeError(f"Domain {domain} resolves to {found}, expected {ip}")
    say(f"Domain DNS check passed: {domain} -> {ip}")


def official_hysteria_install(replace: bool) -> None:
    existing = HYSTERIA.exists() or HYSTERIA_CONFIG.exists()
    if existing and not replace:
        raise RuntimeError("Hysteria 2 already exists. Use --replace to reinstall it.")
    install_dependencies()
    with tempfile.TemporaryDirectory(prefix="hysteria-installer-") as directory:
        script = pathlib.Path(directory) / "install.sh"
        request = urllib.request.Request(HYSTERIA_INSTALLER_URL, headers={"User-Agent": "hy2-bootstrap/1.0"})
        with urllib.request.urlopen(request, timeout=30) as response:
            script.write_bytes(response.read())
        content = script.read_bytes()
        first_line = content.splitlines()[0] if content else b""
        if not (first_line.startswith(b"#!") and b"bash" in first_line):
            raise RuntimeError("The official Hysteria installer did not download correctly")
        environment = os.environ.copy()
        environment["HYSTERIA_USER"] = "root"
        if existing:
            say("Removing the existing Hysteria 2 installation")
            result = subprocess.run(["bash", str(script), "--remove"], text=True,
                                    stdout=sys.stderr, stderr=sys.stderr, env=environment)
            if result.returncode:
                raise RuntimeError("Could not remove the existing Hysteria 2 installation")
        say("Installing the latest stable Hysteria 2 with the official installer")
        result = subprocess.run(["bash", str(script)], text=True,
                                stdout=sys.stderr, stderr=sys.stderr, env=environment)
        if result.returncode:
            raise RuntimeError("The official Hysteria 2 installer failed")


def install_caddy_decoy(domain: str, replace: bool) -> tuple[pathlib.Path, pathlib.Path]:
    caddyfile = pathlib.Path("/etc/caddy/Caddyfile")
    marker = pathlib.Path("/etc/caddy/.xray-easy-installer")
    if caddyfile.exists() and not marker.exists() and not replace:
        raise RuntimeError("Caddy already has a configuration. Use a fresh server or --replace.")
    if shutil.which("apt-get") is None:
        raise RuntimeError("Caddy automatic setup requires Ubuntu or Debian with apt-get")
    if shutil.which("caddy") is None:
        say("Installing Caddy from the official repository")
        run("apt-get", "update", "-qq")
        run("apt-get", "install", "-y", "-qq", "debian-keyring", "debian-archive-keyring",
            "apt-transport-https", "curl", "gnupg")
        keyring = pathlib.Path("/usr/share/keyrings/caddy-stable-archive-keyring.gpg")
        repository = pathlib.Path("/etc/apt/sources.list.d/caddy-stable.list")
        with tempfile.TemporaryDirectory(prefix="caddy-repository-") as directory:
            source_key = pathlib.Path(directory) / "gpg.key"
            request = urllib.request.Request(
                "https://dl.cloudsmith.io/public/caddy/stable/gpg.key",
                headers={"User-Agent": "xray-easy-installer/1.6"},
            )
            with urllib.request.urlopen(request, timeout=30) as response:
                source_key.write_bytes(response.read())
            run("gpg", "--batch", "--yes", "--dearmor", "--output", str(keyring), str(source_key))
            request = urllib.request.Request(
                "https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt",
                headers={"User-Agent": "xray-easy-installer/1.6"},
            )
            with urllib.request.urlopen(request, timeout=30) as response:
                repository.write_bytes(response.read())
        keyring.chmod(0o644)
        repository.chmod(0o644)
        run("apt-get", "update", "-qq")
        run("apt-get", "install", "-y", "-qq", "caddy")
    web_root = pathlib.Path("/var/www/hysteria-decoy")
    web_root.mkdir(parents=True, exist_ok=True)
    (web_root / "index.html").write_text("""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Welcome</title><style>body{font:16px system-ui;margin:0;background:#f5f7fb;color:#20242c;display:grid;place-items:center;min-height:100vh}.card{background:white;padding:42px 48px;border-radius:18px;box-shadow:0 12px 40px #18203318;text-align:center}h1{margin:0 0 10px}p{color:#687080}</style></head>
<body><main class="card"><h1>Welcome</h1><p>This service is online.</p></main></body></html>\n""")
    caddyfile.parent.mkdir(parents=True, exist_ok=True)
    caddyfile.write_text(f"""{domain} {{
    root * {web_root}
    encode zstd gzip
    file_server
}}
""")
    marker.write_text("Managed by Xray Easy Installer\n")
    run("caddy", "validate", "--config", str(caddyfile), "--adapter", "caddyfile", capture=True)
    open_ufw(80)
    open_ufw(443)
    run("systemctl", "enable", "caddy", capture=True)
    run("systemctl", "restart", "caddy", capture=True)
    deadline = time.monotonic() + 90
    storage = pathlib.Path("/var/lib/caddy/.local/share/caddy/certificates")
    while time.monotonic() < deadline:
        certs = list(storage.glob(f"*/{domain}/{domain}.crt"))
        for cert in certs:
            key = cert.with_suffix(".key")
            if cert.stat().st_size > 0 and key.exists() and key.stat().st_size > 0:
                say(f"Caddy HTTPS decoy is ready: https://{domain}/")
                return cert, key
        time.sleep(2)
    logs = subprocess.run(["journalctl", "--no-pager", "-n", "100", "-u", "caddy"],
                          capture_output=True, text=True).stdout
    raise RuntimeError("Caddy could not obtain a TLS certificate:\n" + logs[-2400:])


def write_hysteria_config(port: int, domain: str, password: str, obfs_password: str,
                          masquerade: str, cert: pathlib.Path, key: pathlib.Path) -> None:
    HYSTERIA_CONFIG.parent.mkdir(parents=True, exist_ok=True)
    content = f"""listen: :{port}

tls:
  cert: {cert}
  key: {key}

auth:
  type: password
  password: {password}

obfs:
  type: salamander
  salamander:
    password: {obfs_password}

masquerade:
  type: proxy
  proxy:
    url: https://{masquerade}/
    rewriteHost: true
"""
    with tempfile.NamedTemporaryFile("w", dir=HYSTERIA_CONFIG.parent,
                                     prefix=".new-hysteria-", suffix=".yaml", delete=False) as file:
        file.write(content)
        temporary = pathlib.Path(file.name)
    try:
        os.chmod(temporary, 0o600)
        os.replace(temporary, HYSTERIA_CONFIG)
    finally:
        temporary.unlink(missing_ok=True)


def open_ufw_udp(port: int) -> None:
    if shutil.which("ufw") is None:
        return
    status = run("ufw", "status", capture=True).stdout
    if re.search(r"^Status: active\s*$", status, re.MULTILINE):
        run("ufw", "allow", f"{port}/udp", capture=True)


def hysteria_client_config(ip: str, port: int, domain: str, password: str,
                           obfs_password: str, socks_port: int) -> str:
    return f"""server: {ip}:{port}
auth: {password}
tls:
  sni: {domain}
obfs:
  type: salamander
  salamander:
    password: {obfs_password}
socks5:
  listen: 127.0.0.1:{socks_port}
"""


def test_hysteria_connection(ip: str, port: int, domain: str,
                             password: str, obfs_password: str) -> None:
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        socks_port = sock.getsockname()[1]
    with tempfile.TemporaryDirectory(prefix="hysteria-client-test-") as directory:
        config_file = pathlib.Path(directory) / "client.yaml"
        config_file.write_text(hysteria_client_config(
            ip, port, domain, password, obfs_password, socks_port
        ))
        with (pathlib.Path(directory) / "client.log").open("w+") as log:
            client = subprocess.Popen([str(HYSTERIA), "client", "-c", str(config_file)],
                                      stdout=log, stderr=log)
            try:
                wait_for_port(socks_port, seconds=12)
                result = subprocess.run(
                    ["curl", "--noproxy", "", "--proxy", f"socks5h://127.0.0.1:{socks_port}",
                     "-sS", "-o", "/dev/null", "-w", "%{http_code}", "--max-time", "20",
                     "https://example.com/"], capture_output=True, text=True,
                )
                if result.returncode or result.stdout.strip() != "200":
                    log.flush()
                    log.seek(0)
                    raise RuntimeError(f"Hysteria proxy test failed:\n{(result.stderr + log.read())[-1600:]}")
            finally:
                client.terminate()
                try:
                    client.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    client.kill()
                    client.wait()


def make_hysteria_link(ip: str, port: int, domain: str,
                       password: str, obfs_password: str) -> str:
    query = urllib.parse.urlencode({
        "obfs": "salamander", "obfs-password": obfs_password, "sni": domain
    })
    return (f"hysteria2://{urllib.parse.quote(password, safe='')}@{ip}:{port}/?{query}"
            f"#Hysteria2-{urllib.parse.quote(domain)}")


def install_hysteria(ip: str, port: int, domain: str, masquerade: str, replace: bool) -> None:
    check_domain(domain, ip)
    check_site_tls(masquerade)
    if not replace:
        check_udp_port(port)
        check_port(80)
        check_port(443)
    official_hysteria_install(replace)
    if replace:
        check_udp_port(port)
    cert, key = install_caddy_decoy(domain, replace)
    password = secrets.token_hex(12)
    obfs_password = secrets.token_hex(12)
    write_hysteria_config(port, domain, password, obfs_password, masquerade, cert, key)
    open_ufw_udp(port)
    run("systemctl", "enable", "hysteria-server.service", capture=True)
    run("systemctl", "restart", "hysteria-server.service", capture=True)
    time.sleep(3)
    if run("systemctl", "is-active", "hysteria-server.service", capture=True).stdout.strip() != "active":
        logs = subprocess.run(["journalctl", "--no-pager", "-n", "80", "-u",
                               "hysteria-server.service"], capture_output=True, text=True).stdout
        raise RuntimeError("Hysteria 2 service is not active:\n" + logs[-2000:])
    say("Testing a Hysteria 2 connection")
    test_hysteria_connection("127.0.0.1", port, domain, password, obfs_password)
    link = make_hysteria_link(ip, port, domain, password, obfs_password)
    with tempfile.NamedTemporaryFile("w", dir=HYSTERIA_LINK_FILE.parent,
                                     prefix=".hysteria2-link-", delete=False) as file:
        file.write(link + "\n")
        temporary = pathlib.Path(file.name)
    temporary.chmod(0o600)
    os.replace(temporary, HYSTERIA_LINK_FILE)
    print(link, flush=True)
    say(f"Verified. A root-only copy is saved at {HYSTERIA_LINK_FILE}")

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
    parser.add_argument("--port", type=int, help="Listening port (profile default if omitted)")
    parser.add_argument("--site", default=DEFAULT_SITE, help="REALITY site or Hysteria 2 domain")
    parser.add_argument("--masquerade", default="www.microsoft.com",
                        help="Hysteria 2 reverse-proxy masquerade hostname")
    parser.add_argument("--profile", choices=PROFILES, default="xhttp",
                        help="Profile: XHTTP, RAW/TCP Vision, or Hysteria 2")
    parser.add_argument("--replace", action="store_true", help="Purge an existing Xray installation")
    args = parser.parse_args()
    if os.geteuid() != 0:
        raise RuntimeError("Run this script as root")
    port = args.port or (DEFAULT_HYSTERIA_PORT if args.profile == "hysteria2" else DEFAULT_PORT)
    if not 1 <= port <= 65535:
        raise ValueError("Port must be between 1 and 65535")
    if not re.fullmatch(r"(?=.{1,253}$)[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?)+", args.site):
        raise ValueError("Site must be a valid lowercase DNS hostname")
    if not re.fullmatch(r"(?=.{1,253}$)[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?)+", args.masquerade):
        raise ValueError("Masquerade must be a valid lowercase DNS hostname")
    if shutil.which("systemctl") is None:
        raise RuntimeError("This script requires a systemd Linux server")
    ip = public_ipv4(args.ip)
    say(f"Public IP: {ip}")
    if args.profile == "hysteria2":
        say(f"Selected Hysteria 2 domain: {args.site}")
        say(f"Selected Hysteria 2 masquerade: {args.masquerade}")
        install_hysteria(ip, port, args.site, args.masquerade, args.replace)
        return
    if (XRAY.exists() or CONFIG.exists()) and not args.replace:
        raise RuntimeError("Xray already exists. Use --replace to remove its old configuration.")
    if not args.replace:
        check_port(port)
    say(f"Selected REALITY site: {args.site}")
    say("Selected profile: " + ("XHTTP + REALITY" if args.profile == "xhttp"
                                else "RAW + REALITY + Vision"))
    check_site_tls(args.site)
    official_install(args.replace)
    if args.replace:
        check_port(port)
    check_target(args.site)
    private_key, password = new_keypair()
    user_id = str(uuid.uuid4())
    short_id = secrets.token_hex(8)
    path = "/x/" + secrets.token_urlsafe(18)
    write_config(server_config(
        port, user_id, short_id, path, private_key, args.site, args.profile
    ))
    open_ufw(port)
    run("systemctl", "enable", "xray", capture=True)
    run("systemctl", "restart", "xray", capture=True)
    wait_for_port(port)
    if run("systemctl", "is-active", "xray", capture=True).stdout.strip() != "active":
        raise RuntimeError("Xray service is not active")
    profile_name = "XHTTP + REALITY" if args.profile == "xhttp" else "RAW + REALITY + Vision"
    say(f"Testing a {profile_name} connection")
    test_connection(port, user_id, short_id, path, password, args.site, args.profile)
    link = make_link(ip, port, user_id, short_id, path, password, args.site, args.profile)
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
