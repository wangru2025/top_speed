using System;
using Eto.Forms;
using TopSpeed.Localization;
using TopSpeed.Runtime;

namespace TopSpeed.Windowing.Eto
{
    internal sealed class FileDialogService : IFileDialogs
    {
        private readonly WindowHost _window;

        public FileDialogService(WindowHost window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public void PickAudioFile(Action<string?> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            Application.Instance.AsyncInvoke(() =>
            {
                string? selectedPath = null;
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = LocalizationService.Translate(LocalizationService.Mark("Select a media file"));
                    dialog.Filters.Add(new FileFilter("Audio files", ".wav", ".ogg", ".mp3", ".flac", ".aac", ".m4a"));
                    dialog.Filters.Add(new FileFilter("All files", ".*"));

                    if (dialog.ShowDialog(_window.MainForm) == DialogResult.Ok)
                        selectedPath = dialog.FileName;
                }

                onCompleted(selectedPath);
            });
        }

        public void PickFolder(string? initialFolder, Action<string?> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            Application.Instance.AsyncInvoke(() =>
            {
                string? selectedFolder = null;
                using (var dialog = new SelectFolderDialog())
                {
                    dialog.Title = LocalizationService.Translate(LocalizationService.Mark("Select a media folder"));
                    if (!string.IsNullOrWhiteSpace(initialFolder))
                    {
                        dialog.Directory = initialFolder!;
                    }

                    if (dialog.ShowDialog(_window.MainForm) == DialogResult.Ok)
                        selectedFolder = dialog.Directory;
                }

                onCompleted(selectedFolder);
            });
        }
    }
}
