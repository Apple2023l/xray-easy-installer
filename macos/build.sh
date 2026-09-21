#!/bin/bash
set -euo pipefail

project_dir="$(cd "$(dirname "$0")/.." && pwd)"
app_dir="$project_dir/dist/Xray Installer.app"
contents_dir="$app_dir/Contents"
sdk_path="${XRAY_MACOS_SDK:-/Library/Developer/CommandLineTools/SDKs/MacOSX14.5.sdk}"
module_cache="$project_dir/.build/module-cache"

if [[ ! -d "$sdk_path" ]]; then
  sdk_path="$(xcrun --sdk macosx --show-sdk-path)"
fi
mkdir -p "$contents_dir/MacOS" "$contents_dir/Resources" "$module_cache"
swiftc -parse-as-library -O -target arm64-apple-macosx13.0 -sdk "$sdk_path" \
  -module-cache-path "$module_cache" \
  -framework SwiftUI -framework AppKit \
  "$project_dir/macos/XrayInstallerApp.swift" \
  -o "$contents_dir/MacOS/XrayInstaller"
cp "$project_dir/macos/Info.plist" "$contents_dir/Info.plist"
cp "$project_dir/setup_xray.py" "$contents_dir/Resources/setup_xray.py"
cp "$project_dir/macos/askpass.sh" "$contents_dir/Resources/askpass.sh"
cp "$project_dir/macos/xray-logo.png" "$contents_dir/Resources/xray-logo.png"
cp "$project_dir/macos/ASSETS.txt" "$contents_dir/Resources/ASSETS.txt"
chmod 700 "$contents_dir/Resources/askpass.sh"
echo "$app_dir"
