using System;
using TopSpeed.Core.Updates;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private const int AboutCopyResultId = 1001;

        private void ShowAboutDialog()
        {
            var gameVersionLine = LocalizationService.Format(
                LocalizationService.Mark("Game version: {0}"),
                UpdateConfig.CurrentVersion.ToMachineString());
            var protocolVersionLine = LocalizationService.Format(
                LocalizationService.Mark("Protocol version: {0}"),
                ProtocolProfile.Current.ToMachineString());

            var copyText = string.Join(Environment.NewLine, gameVersionLine, protocolVersionLine);
            var dialog = new Dialog(
                LocalizationService.Mark("About"),
                null,
                QuestionId.Close,
                new[]
                {
                    new DialogItem(gameVersionLine),
                    new DialogItem(protocolVersionLine)
                },
                onResult: null,
                new DialogButton(
                    AboutCopyResultId,
                    LocalizationService.Mark("Copy"),
                    onClick: () => CopyAboutText(copyText)),
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));
            _dialogs.Show(dialog);
        }

        private void CopyAboutText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (_clipboard.TrySetText(text))
            {
                _speech.Speak(LocalizationService.Mark("Copied to clipboard."));
                return;
            }

            _speech.Speak(LocalizationService.Mark("Unable to copy to clipboard."));
        }
    }
}
