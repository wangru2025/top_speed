using System.Collections.Generic;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsAudioSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new CheckBox(LocalizationService.Mark("Enable HRTF audio"),
                    () => _settings.HrtfAudio,
                    value => _settingsActions.UpdateSetting(() => _settings.HrtfAudio = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, Three-D audio uses HRTF spatialization for more realistic positioning."))),
                new CheckBox(LocalizationService.Mark("Stereo widening for own car"),
                    () => _settings.StereoWidening,
                    value => _settingsActions.UpdateSetting(() => _settings.StereoWidening = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("Accessibility option for clearer left-right cues with HRTF. It attenuates the opposite ear for your own car sounds only."))),
                new CheckBox(LocalizationService.Mark("Automatic audio device format"),
                    () => _settings.AutoDetectAudioDeviceFormat,
                    value => _settingsActions.UpdateSetting(() => _settings.AutoDetectAudioDeviceFormat = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, the game uses the device channel count and sample rate. Restart required.")))
            };

            return BackMenu("options_audio", items);
        }
    }
}





