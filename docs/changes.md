# Changes

This file tracks new changes to the game for both client and server to make it easier to find previous changes.



The game versioning follows a specific pattern by using year.month.day.revision, where revision is an incremental number if there is more than one release in a single day.



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

