param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$ServerVersion,
    [Parameter(Mandatory = $true)][string]$GameZip,
    [Parameter(Mandatory = $true)][string]$ServerZip
)

$ErrorActionPreference = "Stop"

dotnet restore TopSpeed.Tests/TopSpeed.Tests.csproj
dotnet restore TopSpeed.Server/TopSpeed.Server.csproj
dotnet restore TopSpeed/TopSpeed.csproj

dotnet build TopSpeed.Tests/TopSpeed.Tests.csproj --configuration Release --framework net10.0-windows --no-restore
dotnet test TopSpeed.Tests/TopSpeed.Tests.csproj --configuration Release --framework net10.0-windows --no-build --verbosity normal

dotnet publish TopSpeed.Server/TopSpeed.Server.csproj --configuration Release -p:ServerPublishProfile=ReleaseWinX64
dotnet build TopSpeed/TopSpeed.csproj --configuration Release --framework net10.0-windows --runtime win-x64 --no-restore

if (Test-Path $GameZip) { Remove-Item $GameZip -Force }
if (Test-Path $ServerZip) { Remove-Item $ServerZip -Force }

Compress-Archive -Path "TopSpeed\bin\Release\net10.0\win-x64\*" -DestinationPath $GameZip -CompressionLevel Optimal
Compress-Archive -Path "TopSpeed.Server\bin\Publish\win-x64\*" -DestinationPath $ServerZip -CompressionLevel Optimal

python build/ci/verify_docs.py --archive $GameZip --prefix docs

Get-Item $GameZip, $ServerZip | Select-Object Name, Length

if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
    "artifact_paths<<EOF" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "top_speed_net/$GameZip" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "top_speed_net/$ServerZip" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "EOF" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
}
