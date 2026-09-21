# Xray Easy Installer

Desktop installer for deploying Xray or Hysteria 2 over SSH without editing
configuration files or using the command line.

The macOS and Windows interfaces can be switched between Russian, English,
Persian (with right-to-left layout), and Simplified Chinese.

## Supported profiles

- VLESS + XHTTP + REALITY
- VLESS + RAW/TCP + REALITY + Vision
- Hysteria 2 + Salamander with a Caddy HTTPS decoy website

The VLESS profiles use TCP port `8435`; Hysteria 2 uses UDP port `40460`.
The installer downloads the latest
stable Xray release through the official XTLS installer, generates new
credentials, validates the server configuration and performs a local proxy
connection test before returning the profile.

Hysteria 2 uses the official installer from `get.hy2.sh`. Caddy is installed
from its official repository, obtains and renews the TLS certificate, and serves
a neutral HTTPS page on the domain. Hysteria uses the same certificate and its
built-in masquerade proxies a site selected in the app. Before installation,
point an IPv4 DNS record directly to the VPS. TCP ports `80` and `443` must be
free, and UDP port `40460` must be allowed by the VPS provider firewall. A
proxied CDN record cannot be used because the domain must resolve to the VPS.
The desktop apps show live installation progress for Caddy and automatically
resume an interrupted Hysteria installation managed by this installer.

## Applications

### macOS

The SwiftUI source is in `macos/`. Build with:

```bash
bash macos/build.sh
```

### Windows

The standalone Windows source is in [`windows-dotnet/`](windows-dotnet/).
GitHub Actions builds two separate release files automatically:

```text
Xray-Installer-macOS.zip
Xray-Installer-Windows.exe
```

## Generated output

- VLESS or Hysteria 2 import link
- QR code for Shadowrocket and compatible clients
- sing-box outbound JSON for Podkop

## Server requirements

- Ubuntu or Debian with systemd
- Public IPv4 address
- Root SSH access
- TCP port 8435 available for VLESS
- A domain, free TCP ports 80 and 443, plus UDP port 40460 for Hysteria 2

## Security

SSH passwords are used only during the active installation and are not written
to disk. The generated server link is saved on the VPS as `/root/xray-link.txt`
or `/root/hysteria2-link.txt` with root-only permissions.

## Third-party software

Xray-core and its logo belong to the XTLS project. See `macos/ASSETS.txt` for
the logo attribution. Xray is downloaded at installation time and is not
included in this repository.

## License

Application source code is available under the MIT License. Third-party assets
retain their original licenses.
