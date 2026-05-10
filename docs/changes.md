# Changes

This file tracks new changes to the game for both client and server to make it easier to find previous changes.

The game versioning follows a specific pattern by using year.month.day.revision, where revision is an incremental number if there is more than one release in a single day.


## 2026.5.9.2
### Game Changes
- Fixed multiplayer voice chat so that remote players actually hear push-to-talk and voice-activation transmissions:
  - The capture device is now brought up as soon as the communicator is armed (enabled with a non-zero frequency on a connected session), so the first VOX/PTT key press no longer loses the leading frames while MiniAudio cold-starts the microphone.
  - VOX (voice-activation) now actually gates on the RMS voice-activity detector — previously it transmitted continuously whenever VAD was on; now it transmits only while voice is detected (with a short hold window) and stops when you go quiet.
  - The local mic open/close cue is now tied to the real transmission state instead of the should-transmit intent, so the "you're now live" sound only plays after the server has accepted the start packet.
  - When the communicator is turned off (or the session disconnects), the capture device is torn down instead of leaking the OS mic indefinitely.
- Removed duplicated logic in the runtime: there is now a single transmission state machine (`Disarm` / `BeginTransmission` / `EndTransmission`) instead of four copies of the stop-and-clear pattern, and the client-side `LiveSend` / `VoiceSend` start/frame/stop logic is consolidated into a shared `OutboundStreamSend` base.


## 2026.5.9.1
### Game Changes
- Fixed the in-vehicle radio in multiplayer crashing when a track finishes and loops back to the start (notably with FLAC files). The fix is in the SoundFlow native FFmpeg wrapper: tail-of-stream codec/demuxer hiccups are now reported as graceful end-of-stream instead of as fatal decoder errors, so the radio source's `Seek(0)`+retry path recovers cleanly.


## 2026.5.5.1
### Game Changes
- Fixed many bugs with the multiplayer server.
- Added a new way of navigating through message history by using the comma to move to the previous item, period to move to the next item, and left/right brackets are used to navigate between buffers. The separate history screen is still available.
- Added an ability to copy the current buffer item to the clipboard by pressing ctrl+space, or by going to the history and pressing enter on any message there.
- Added the ability to reset menu shortcuts to their defaults.


### Server Changes
- Fixed many bugs related to server connection and room deadlocks where players were being stuck in a room after joining multiple times.


## 2026.5.4.3
### Game Changes
- Added the ability to choose which modifier keys are being used when you remap a key in the game. This allows you to either use both modifiers, or the left/right.
- Fixed a critical crash with ZDSR by disabling CET compatibility. The game should no longer crash again when ZDSR is installed.
- Fixed some critical crashes when discovering local servers on the network.
- Android version now runs in landscape mode.


### Server Changes
- Fixed a regression where protocol version mismatches did not trigger a hard fail.

## 2026.5.4.2
This is a hot fix for Android arm 32 and Mac.

## 2026.5.4.1

### Game Changes

* Fixed many crashes that could happen randomly due to audio processing for invalid audio buffers.
* Added Spanish translation for copilot and race announcements.
* Added support for Mac ARM-64 and Android arm-32 (ARM-v7) builds.
* Added support for uploading your custom tracks to the server.
* Android builds now use a permenant signature and no longer conflicts with existing versions.


### Server Changes

* Refactored server and made race finish events much more reliable.
* Added reconnect support, when a player loses connection suddenly, there is now a 20 seconds reconnect period before fully disposing the player.
* Fixed player randomization.
* Added moderation tools to prevent duplicate names on the server, prevent long names, prevent repeated letters in a name, and control text chat on the server level.
* Added initial support for custom tracks.
* You can now host your own custom tracks on the server, and other people can see them when they enable custom tracks.

