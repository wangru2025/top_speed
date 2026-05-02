#!/usr/bin/env bash
set -euo pipefail

version="${1:?missing version}"
server_version="${2:?missing server version}"
game_zip="${3:?missing game zip name}"

dotnet restore TopSpeed/TopSpeed.csproj
dotnet restore TopSpeed.Server/TopSpeed.Server.csproj

dotnet build TopSpeed.Shared/TopSpeed.Shared.csproj --configuration Release

dotnet publish TopSpeed.Server/TopSpeed.Server.csproj --configuration Release -p:ServerPublishProfile=ReleaseLinuxX64 -p:UseTrimmedUpdater=true
dotnet publish TopSpeed.Server/TopSpeed.Server.csproj --configuration Release -p:ServerPublishProfile=ReleaseLinuxArm64 -p:UseTrimmedUpdater=true

profiles=(
  ReleaseLinuxArm32
  ReleaseLinuxMuslX64
  ReleaseLinuxMuslArm64
)

for profile in "${profiles[@]}"; do
  dotnet publish TopSpeed.Server/TopSpeed.Server.csproj --configuration Release -p:ServerPublishProfile="$profile" -p:UseTrimmedUpdater=true
done

dotnet publish TopSpeed.Server/TopSpeed.Server.csproj --configuration Release -p:ServerPublishProfile=ReleaseLinuxX86FrameworkDependent
dotnet build TopSpeed/TopSpeed.csproj --configuration Release -p:RuntimeIdentifier=linux-x64 -p:UseTrimmedUpdater=true

rm -f "$game_zip"
(
  cd TopSpeed/bin/Release/net10.0/linux-x64
  zip -rq "$GITHUB_WORKSPACE/top_speed_net/$game_zip" .
)
python build/ci/verify_docs.py --archive "$game_zip" --prefix docs

server_targets=(
  linux-x64
  linux-arm64
  linux-arm32
  linux-musl-x64
  linux-musl-arm64
  linux-x86-fdd
)

artifact_paths=("top_speed_net/$game_zip")

for rid in "${server_targets[@]}"; do
  destination="TopSpeed.Server-$rid-Release-v-$server_version.zip"
  rm -f "$destination"
  (
    cd "TopSpeed.Server/bin/Publish/$rid"
    zip -rq "$GITHUB_WORKSPACE/top_speed_net/$destination" .
  )
  artifact_paths+=("top_speed_net/$destination")
done

ls -lh "$game_zip"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    echo "artifact_paths<<EOF"
    printf '%s\n' "${artifact_paths[@]}"
    echo "EOF"
  } >> "$GITHUB_OUTPUT"
fi
