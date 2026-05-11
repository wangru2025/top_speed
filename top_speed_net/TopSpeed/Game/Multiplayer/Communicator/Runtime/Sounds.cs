using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        // Drives the local mic open/close cue from the actual transmission state so
        // the user only hears the "you're now live" sound after the network actually
        // accepted the voice-start, not on key-down.
        private void UpdateMicCue(bool open)
        {
            if (_micCueOpen == open)
                return;

            _micCueOpen = open;
            PlayLocalCue(open ? GetMicOpenSound() : GetMicCloseSound());
        }

        private void PlayLocalCue(SoundAsset? sound)
        {
            if (sound == null)
                return;

            try
            {
                _audio.PlayOneShot(sound, AudioEngineOptions.UiBusName, configure: handle =>
                {
                    handle.SetVolumePercent(_settings, AudioVolumeCategory.Communicator, 100);
                });
            }
            catch
            {
            }
        }

        private SoundAsset? GetMicOpenSound()
        {
            return GetCachedSound(ref _micOpenSound, Path.Combine("network", "comm", "mic_open.wav"));
        }

        private SoundAsset? GetMicCloseSound()
        {
            var sound = GetCachedSound(ref _micCloseSound, Path.Combine("network", "mic_close.wav"));
            if (sound != null)
                return sound;

            return GetCachedSound(ref _micCloseSound, Path.Combine("network", "comm", "mic_close.wav"));
        }

        private void PlayRemotePttCue()
        {
            var cue = GetPttCue();
            if (cue == null)
                return;

            PlayLocalCue(cue);
        }

        private SoundAsset? GetPttCue()
        {
            var index = _random.Next(0, _pttCues.Length);
            if (_pttCues[index] != null)
                return _pttCues[index];

            var legacyPath = AssetPaths.ResolveLegacySoundPath($"unkey{index + 1}.wav");
            if (legacyPath == null)
                return null;

            try
            {
                _pttCues[index] = _audio.LoadAsset(legacyPath, streamFromDisk: false);
                return _pttCues[index];
            }
            catch
            {
                return null;
            }
        }

        private SoundAsset? GetCachedSound(ref SoundAsset? cache, string relativePath)
        {
            if (cache != null)
                return cache;

            var path = Path.Combine(AssetPaths.SoundsRoot, relativePath);
            if (!_audio.TryResolvePath(path, out var fullPath))
                return null;

            try
            {
                cache = _audio.LoadAsset(fullPath, streamFromDisk: false);
                return cache;
            }
            catch
            {
                return null;
            }
        }

        private void DisposeCachedSounds()
        {
            DisposeSound(ref _micOpenSound);
            DisposeSound(ref _micCloseSound);
            for (var i = 0; i < _pttCues.Length; i++)
            {
                var cue = _pttCues[i];
                if (cue == null)
                    continue;
                cue.Dispose();
                _pttCues[i] = null;
            }
        }

        private static void DisposeSound(ref SoundAsset? sound)
        {
            if (sound == null)
                return;

            try
            {
                sound.Dispose();
            }
            catch
            {
            }

            sound = null;
        }
    }
}
