# iOS Bootstrap Notes

This document tracks the first dependency-prep steps for `net10.0-ios`.

## Staged Native Dependencies

These files are expected in-tree:

- `TopSpeed/ThirdParty/ios/SDL3.xcframework/ios-arm64/SDL3.framework/*`
- `TopSpeed/ThirdParty/ios/libphonon.a`
- `TopSpeed/ThirdParty/ios/libprism.a` (static link for iOS)

SoundFlow iOS binaries are already available in-tree:

- `SoundFlow/Src/Backends/MiniAudio/runtimes/ios-arm64/native/miniaudio.framework/*`
- `SoundFlow/Codecs/SoundFlow.Codecs.FFMpeg/runtimes/ios-arm64/native/soundflow-ffmpeg.framework/*`

## Manual Dependency Prep

1. Extract `SDL3/SDL3.xcframework/Info.plist` and `SDL3/SDL3.xcframework/ios-arm64/*` from:
   `C:\Users\diamond_star\Downloads\SDL3-3.4.8.dmg`
2. Copy them to:
   `TopSpeed/ThirdParty/ios/SDL3.xcframework/`
3. Copy:
   `D:\steamaudio\lib\ios\libphonon.a`
   to:
   `TopSpeed/ThirdParty/ios/libphonon.a`
4. Extract:
   `C:\Users\diamond_star\Downloads\ios-arm64.zip`
   then extract nested `prism-ios-arm64.zip`, and copy:
   `static/release/lib/libprism.a`
   to:
   `TopSpeed/ThirdParty/ios/libprism.a`

## Runtime Resolution Change

`TopSpeed/Runtime/NativeLibraryBootstrap.cs` now includes iOS candidates for:

- `phonon` -> `phonon`, then `__Internal` (for static-link fallback)
- `prism` -> `__Internal`, then `prism` (static-link first)

This keeps current desktop/mobile behavior unchanged while preparing for iOS static linking.

## Host Project Bootstrap

`TopSpeed.iOS/TopSpeed.iOS.csproj` is now added with:

- `net10.0-ios` app host bootstrap (`Program.cs` + `AppDelegate.cs`).
- Native references for:
  - `TopSpeed/ThirdParty/ios/SDL3.xcframework`
  - `TopSpeed/ThirdParty/ios/libprism.a`
  - `TopSpeed/ThirdParty/ios/libphonon.a`
  - `SoundFlow` MiniAudio iOS framework
  - `SoundFlow` FFmpeg iOS framework
- Bundled game assets (`Sounds`, `Tracks`, `Vehicles`, `languages`, docs HTML).
- Runtime wiring to `TopSpeed.IOSLauncher` and iOS platform adapters:
  - `IosMotionSteeringSource`
  - `IosDocumentOpener`
  - `IosUpdatePackageInstaller`

The project is kept outside `TopSpeed.sln` so existing desktop/Android CI builds are unaffected on machines without the iOS workload.

## Next Bring-Up Steps

1. Build `TopSpeed.iOS` on macOS with Xcode + .NET iOS workload:
   `dotnet build top_speed_net/TopSpeed.iOS/TopSpeed.iOS.csproj -c Debug -f net10.0-ios -r ios-arm64`
2. Publish signed IPA from macOS (Release):
   `dotnet publish top_speed_net/TopSpeed.iOS/TopSpeed.iOS.csproj -c Release -f net10.0-ios -r ios-arm64 -p:ArchiveOnBuild=true -p:BuildIpa=true -p:CodesignKey="Apple Distribution: ... " -p:CodesignProvision="..."`
3. Verify startup/native load order and touch/gesture/control parity on device.

## CI Signing Secrets

The release workflow uses these repository secrets:

- `IOS_BUILD_CERTIFICATE_BASE64`: Base64-encoded `.p12` signing certificate.
- `IOS_P12_PASSWORD`: password for the `.p12` file.
- `IOS_BUILD_PROVISION_PROFILE_BASE64`: Base64-encoded `.mobileprovision` file.
- `IOS_KEYCHAIN_PASSWORD`: temporary keychain password used by the runner.
- `IOS_CODESIGN_KEY`: certificate name used by `CodesignKey` (for example `Apple Distribution: Example Corp (TEAMID)`).
- `IOS_CODESIGN_PROVISION`: provisioning profile name used by `CodesignProvision`.

How to prepare values:

1. Export your iOS distribution certificate as `.p12` from Keychain/Xcode.
2. Download the matching `.mobileprovision` profile from Apple Developer portal.
3. Base64-encode both files:
   - macOS:
     - `base64 -i BUILD_CERTIFICATE.p12 | pbcopy`
     - `base64 -i BUILD_PROFILE.mobileprovision | pbcopy`
   - PowerShell:
     - `[Convert]::ToBase64String([IO.File]::ReadAllBytes("BUILD_CERTIFICATE.p12"))`
     - `[Convert]::ToBase64String([IO.File]::ReadAllBytes("BUILD_PROFILE.mobileprovision"))`
4. Add the six secrets in GitHub: `Settings -> Secrets and variables -> Actions`.
5. Ensure `IOS_CODESIGN_KEY` and `IOS_CODESIGN_PROVISION` exactly match installed certificate/profile names.
