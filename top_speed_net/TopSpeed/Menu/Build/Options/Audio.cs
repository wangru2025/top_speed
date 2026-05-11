using System.Collections.Generic;

using TopSpeed.Input;
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
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, the game uses the device channel count and sample rate. Restart required."))),
                new MenuItem(
                    () => LocalizationService.Format(
                        LocalizationService.Mark("Voice input device: {0}"),
                        _audio.GetVoiceInputDeviceLabel()),
                    MenuAction.None,
                    onActivate: _audio.ChooseVoiceInputDevice,
                    hint: LocalizationService.Mark("Select the microphone used for communicator voice chat in multiplayer.")),
                new Slider(
                    LocalizationService.Mark("Microphone input gain"),
                    $"{DriveSettings.MinVoiceInputGainPercent}-{DriveSettings.MaxVoiceInputGainPercent}",
                    () => _settings.VoiceInputGainPercent,
                    value => _settingsActions.UpdateSetting(() => _settings.VoiceInputGainPercent = value),
                    hintProvider: HintSliderProvider(LocalizationService.Mark("Amplifies the captured microphone signal before it is sent to other players. 100 is unity gain. Raise this if other players report you sound too quiet; lower it if your voice clips.")))
            };

            return BackMenu("options_audio", items);
        }
    }
}





