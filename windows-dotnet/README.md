# Xray Installer for Windows

Standalone Windows Forms application built with .NET 8.

The interface includes Russian, English, Persian, and Simplified Chinese.

## Build

```powershell
dotnet publish XrayInstaller.Windows.csproj -c Release -r win-x64 --self-contained true -o ..\dist\windows-x64
```

The resulting `Xray-Installer-Windows.exe` contains the .NET runtime, SSH
client, QR generator and server installation script. Python and OpenSSH are
not required on the Windows computer.

The target VPS must provide Python 3, which is included by default with current
Ubuntu and Debian server images.
