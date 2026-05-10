namespace TopSpeed.Menu
{
    internal interface IMenuAudioActions
    {
        void SaveMusicVolume(float volume);
        void ApplyAudioSettings();
        string GetVoiceInputDeviceLabel();
        void ChooseVoiceInputDevice();
    }
}

