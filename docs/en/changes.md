# Changes

This file tracks new changes to the game for both client and server to make it easier to find previous changes.

The game versioning follows a specific pattern by using year.month.day.revision, where revision is an incremental number if there is more than one release in a single day.


## 2026.5.11.1
### Game Changes
- Added a full voice chat system to the game. Any player who is connected to a server can enable their communicator by pressing ctrl+shift+c to listen to other players, and either holding v or ctrl+shift+v to talk.
- The communicator has a frequency, between 0.0 and 1000.0. The default public frequency is 1.0 which is by default all players are tuned to. You can read the current frequency by pressing f, and change it by pressing ctrl+f.
- There are new settings in the audio to choose the default voice input device and Microphone gain.
- Added a new category in the volume settings for communicator. This controls the loudness of communicator sounds as well as other players. This does not affect the radio.


### Server Changes
- Added voice chat support.
- Added a new flag to control voice chat on the server level.


## 2026.5.9.2
### Game Changes
- Fixed multiplayer voice chat: remote players could not hear each other at all. The communicator now works in the multiplayer lobby in addition to inside rooms. Anyone tuned to the same communicator frequency hears the transmission regardless of which room (or no room) they are in.
- Removed the leftover `TOPSPEED_VOICE_DEBUG` opt-in voice-chat tracing introduced while diagnosing the regression above.

### Server Changes
- Voice chat is now relayed to every connected player on the server (filtered client-side by communicator frequency) instead of being scoped to a single room, so voice works in the lobby and across rooms.


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

