#!/usr/bin/env bash
set -euo pipefail

version="${1:?missing version}"
android_version_code="${2:?missing android version code}"
apk_arm64="${3:?missing arm64 apk name}"
apk_arm32="${4:?missing arm32 apk name}"

dotnet restore TopSpeed.Android/TopSpeed.Android.csproj

dotnet publish TopSpeed.Android/TopSpeed.Android.csproj \
  --configuration Release \
  --framework net10.0-android \
  --runtime android-arm64 \
  -p:AndroidPackageFormat=apk \
  -p:ApplicationDisplayVersion="$version" \
  -p:ApplicationVersion="$android_version_code"

dotnet publish TopSpeed.Android/TopSpeed.Android.csproj \
  --configuration Release \
  --framework net10.0-android \
  --runtime android-arm \
  -p:AndroidPackageFormat=apk \
  -p:ApplicationDisplayVersion="$version" \
  -p:ApplicationVersion="$android_version_code"

stage_apk() {
  local rid="$1"
  local out_name="$2"
  local rid_root="TopSpeed.Android/bin/Release/net10.0-android/$rid"
  local apk_path

  apk_path="$(find "$rid_root" -type f -name '*Signed.apk' | sort | head -n 1)"
  if [[ -z "$apk_path" ]]; then
    apk_path="$(find "$rid_root" -type f -name '*.apk' ! -name '*unaligned*' | sort | head -n 1)"
  fi
  if [[ -z "$apk_path" ]]; then
    echo "No APK was produced for runtime '$rid'."
    find "$rid_root" -type f | sort || true
    exit 1
  fi

  cp "$apk_path" "$out_name"
  python build/ci/verify_docs.py --archive "$out_name" --prefix assets/docs
}

stage_apk "android-arm64" "$apk_arm64"
stage_apk "android-arm" "$apk_arm32"

ls -lh "$apk_arm64" "$apk_arm32"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    echo "artifact_paths<<EOF"
    echo "top_speed_net/$apk_arm64"
    echo "top_speed_net/$apk_arm32"
    echo "EOF"
  } >> "$GITHUB_OUTPUT"
fi
