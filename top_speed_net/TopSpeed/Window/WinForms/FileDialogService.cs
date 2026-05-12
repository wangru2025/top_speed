using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TopSpeed.Localization;
using TopSpeed.Runtime;

namespace TopSpeed.Windowing.WinForms
{
    internal sealed class FileDialogService : IFileDialogs
    {
        public void PickAudioFile(Action<string?> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            void ShowDialog()
            {
                string? selectedPath = null;
                using (var dialog = new OpenFileDialog())
                {
                    dialog.CheckFileExists = true;
                    dialog.CheckPathExists = true;
                    dialog.Multiselect = false;
                    dialog.Title = LocalizationService.Translate(LocalizationService.Mark("Select a media file"));
                    dialog.Filter = "Audio files|*.wav;*.ogg;*.mp3;*.flac;*.aac;*.m4a|All files|*.*";

                    var owner = GetDialogOwner();
                    var result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
                    if (result == DialogResult.OK)
                        selectedPath = dialog.FileName;
                }

                onCompleted(selectedPath);
            }

            InvokeDialog(ShowDialog, "RadioMediaPicker");
        }

        public void PickFolder(string? initialFolder, Action<string?> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            void ShowDialog()
            {
                string? selectedFolder = null;
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = LocalizationService.Translate(LocalizationService.Mark("Select a media folder"));
                    dialog.ShowNewFolderButton = false;
                    if (!string.IsNullOrWhiteSpace(initialFolder) && Directory.Exists(initialFolder))
                        dialog.SelectedPath = initialFolder;

                    var owner = GetDialogOwner();
                    var result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
                    if (result == DialogResult.OK)
                        selectedFolder = dialog.SelectedPath;
                }

                onCompleted(selectedFolder);
            }

            InvokeDialog(ShowDialog, "RadioFolderPicker");
        }

        private static void InvokeDialog(Action showDialog, string threadName)
        {
            var ownerWindow = GetDialogOwnerForm();
            if (ownerWindow != null && ownerWindow.IsHandleCreated && !ownerWindow.IsDisposed)
            {
                ownerWindow.BeginInvoke(showDialog);
                return;
            }

            var thread = new Thread(() => showDialog())
            {
                IsBackground = true,
                Name = threadName
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private static Form? GetDialogOwnerForm()
        {
            if (Application.OpenForms.Count == 0)
                return null;

            return Application.OpenForms[0];
        }

        private static IWin32Window? GetDialogOwner() => GetDialogOwnerForm();
    }
}


