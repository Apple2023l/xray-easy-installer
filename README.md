# Xray Easy Installer

Desktop installer for deploying a fresh Xray server over SSH without editing
JSON files or using the command line.

The macOS and Windows interfaces can be switched between Russian, English,
Persian (with right-to-left layout), and Simplified Chinese.

## Supported profiles

- VLESS + XHTTP + REALITY
- VLESS + RAW/TCP + REALITY + Vision

Both profiles use port `8435` by default. The installer downloads the latest
stable Xray release through the official XTLS installer, generates new
credentials, validates the server configuration and performs a local proxy
connection test before returning the profile.

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

- VLESS import link
- QR code for Shadowrocket and compatible clients
- sing-box outbound JSON for Podkop

## Server requirements

- Ubuntu or Debian with systemd
- Public IPv4 address
- Root SSH access
- TCP port 8435 available

## Security

SSH passwords are used only during the active installation and are not written
to disk. The generated server link is saved on the VPS as
`/root/xray-link.txt` with root-only permissions.

## Third-party software

Xray-core and its logo belong to the XTLS project. See `macos/ASSETS.txt` for
the logo attribution. Xray is downloaded at installation time and is not
included in this repository.

## License

Application source code is available under the MIT License. Third-party assets
retain their original licenses.
