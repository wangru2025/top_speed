using TopSpeed.Input;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private static readonly AudioVolumeCategory[] GlobalVolumeShortcutCategories =
        {
            AudioVolumeCategory.Master,
            AudioVolumeCategory.PlayerVehicleEngine,
            AudioVolumeCategory.PlayerVehicleEvents,
            AudioVolumeCategory.OtherVehicleEngine,
            AudioVolumeCategory.OtherVehicleEvents,
            AudioVolumeCategory.SurfaceLoops,
            AudioVolumeCategory.Radio,
            AudioVolumeCategory.AmbientsAndSources,
            AudioVolumeCategory.Music,
            AudioVolumeCategory.OnlineServerEvents,
            AudioVolumeCategory.Communicator
        };

        private int _globalVolumeShortcutCategoryIndex;

        private void HandleGlobalVolumeShortcuts()
        {
            if (_textInputPromptActive || _inputMapping.IsActive || _shortcutMapping.IsActive)
                return;

            var controlDown = _input.IsDown(InputKey.LeftControl) || _input.IsDown(InputKey.RightControl);
            var shiftDown = _input.IsDown(InputKey.LeftShift) || _input.IsDown(InputKey.RightShift);
            var altDown = _input.IsDown(InputKey.LeftAlt) || _input.IsDown(InputKey.RightAlt);

            if (_input.WasPressed(InputKey.F6) && !controlDown && !altDown)
            {
                CycleGlobalVolumeCategory(shiftDown ? -1 : 1);
                return;
            }

            if (_input.WasPressed(InputKey.F7) && !controlDown && !altDown)
            {
                AdjustSelectedGlobalVolume(shiftDown ? -10 : -1);
                return;
            }

            if (_input.WasPressed(InputKey.F8) && !controlDown && !altDown)
                AdjustSelectedGlobalVolume(shiftDown ? 10 : 1);
        }

        private void CycleGlobalVolumeCategory(int delta)
        {
            if (GlobalVolumeShortcutCategories.Length == 0)
                return;

            _globalVolumeShortcutCategoryIndex += delta;
            while (_globalVolumeShortcutCategoryIndex < 0)
                _globalVolumeShortcutCategoryIndex += GlobalVolumeShortcutCategories.Length;
            while (_globalVolumeShortcutCategoryIndex >= GlobalVolumeShortcutCategories.Length)
                _globalVolumeShortcutCategoryIndex -= GlobalVolumeShortcutCategories.Length;

            var category = GlobalVolumeShortcutCategories[_globalVolumeShortcutCategoryIndex];
            var percent = GetVolumePercent(category);
            _speech.Speak(LocalizationService.Format(
                LocalizationService.Mark("{0}: {1} percent"),
                GetVolumeCategoryName(category),
                percent));
        }

        private void AdjustSelectedGlobalVolume(int delta)
        {
            if (GlobalVolumeShortcutCategories.Length == 0)
                return;

            var category = GlobalVolumeShortcutCategories[_globalVolumeShortcutCategoryIndex];
            var current = GetVolumePercent(category);
            var next = current + delta;
            next = AudioVolumeSettings.ClampPercent(next);
            if (next == current)
                return;
            SetVolumePercent(category, next);
            _settings.SyncMusicVolumeFromAudioCategories();
            ApplyAudioSettings();
            SaveSettings();
            _speech.Speak(LocalizationService.Format(LocalizationService.Mark("{0} percent"), next));
        }

        private int GetVolumePercent(AudioVolumeCategory category)
        {
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            return _settings.AudioVolumes.GetPercent(category);
        }

        private void SetVolumePercent(AudioVolumeCategory category, int percent)
        {
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            switch (category)
            {
                case AudioVolumeCategory.Master:
                    _settings.AudioVolumes.MasterPercent = percent;
                    return;
                case AudioVolumeCategory.PlayerVehicleEngine:
                    _settings.AudioVolumes.PlayerVehicleEnginePercent = percent;
                    return;
                case AudioVolumeCategory.PlayerVehicleEvents:
                    _settings.AudioVolumes.PlayerVehicleEventsPercent = percent;
                    return;
                case AudioVolumeCategory.OtherVehicleEngine:
                    _settings.AudioVolumes.OtherVehicleEnginePercent = percent;
                    return;
                case AudioVolumeCategory.OtherVehicleEvents:
                    _settings.AudioVolumes.OtherVehicleEventsPercent = percent;
                    return;
                case AudioVolumeCategory.SurfaceLoops:
                    _settings.AudioVolumes.SurfaceLoopsPercent = percent;
                    return;
                case AudioVolumeCategory.Radio:
                    _settings.AudioVolumes.RadioPercent = percent;
                    return;
                case AudioVolumeCategory.AmbientsAndSources:
                    _settings.AudioVolumes.AmbientsAndSourcesPercent = percent;
                    return;
                case AudioVolumeCategory.Music:
                    _settings.AudioVolumes.MusicPercent = percent;
                    return;
                case AudioVolumeCategory.OnlineServerEvents:
                    _settings.AudioVolumes.OnlineServerEventsPercent = percent;
                    return;
                case AudioVolumeCategory.Communicator:
                    _settings.AudioVolumes.CommunicatorPercent = percent;
                    return;
            }
        }

        private string GetVolumeCategoryName(AudioVolumeCategory category)
        {
            return category switch
            {
                AudioVolumeCategory.Master => LocalizationService.Mark("Master audio"),
                AudioVolumeCategory.PlayerVehicleEngine => LocalizationService.Mark("Vehicle engine"),
                AudioVolumeCategory.PlayerVehicleEvents => LocalizationService.Mark("Vehicle events"),
                AudioVolumeCategory.OtherVehicleEngine => LocalizationService.Mark("Other vehicles engine"),
                AudioVolumeCategory.OtherVehicleEvents => LocalizationService.Mark("Other vehicles events"),
                AudioVolumeCategory.SurfaceLoops => LocalizationService.Mark("Surface loops"),
                AudioVolumeCategory.Radio => LocalizationService.Mark("Radio"),
                AudioVolumeCategory.AmbientsAndSources => LocalizationService.Mark("Ambients and sound sources"),
                AudioVolumeCategory.Music => LocalizationService.Mark("Music"),
                AudioVolumeCategory.OnlineServerEvents => LocalizationService.Mark("Online server events"),
                AudioVolumeCategory.Communicator => LocalizationService.Mark("Communicator"),
                _ => LocalizationService.Mark("Volume")
            };
        }

    }
}
