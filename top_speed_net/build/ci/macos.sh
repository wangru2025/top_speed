#!/usr/bin/env bash
set -euo pipefail

version="${1:?missing version}"
server_version="${2:?missing server version}"
game_zip="${3:?missing game zip name}"
server_zip="${4:?missing server zip name}"

dotnet restore TopSpeed/TopSpeed.csproj
dotnet restore TopSpeed.Server/TopSpeed.Server.csproj

dotnet build TopSpeed.Shared/TopSpeed.Shared.csproj --configuration Release
dotnet publish TopSpeed/TopSpeed.csproj --configuration Release -p:RuntimeIdentifier=osx-arm64 -p:UseNativeAotUpdater=true
dotnet publish TopSpeed.Server/TopSpeed.Server.csproj --configuration Release -p:ServerPublishProfile=ReleaseMacArm64 -p:UseNativeAotUpdater=true

rm -f "$game_zip" "$server_zip"

(
  cd TopSpeed/bin/Publish/osx-arm64
  ditto -c -k --sequesterRsrc --keepParent TopSpeed.app "$GITHUB_WORKSPACE/top_speed_net/$game_zip"
)

(
  cd TopSpeed.Server/bin/Publish/mac-arm64
  zip -rq "$GITHUB_WORKSPACE/top_speed_net/$server_zip" .
)

python build/ci/verify_docs.py --archive "$game_zip" --prefix TopSpeed.app/Contents/MacOS/docs

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    echo "artifact_paths<<EOF"
    echo "top_speed_net/$game_zip"
    echo "top_speed_net/$server_zip"
    echo "EOF"
  } >> "$GITHUB_OUTPUT"
fi
