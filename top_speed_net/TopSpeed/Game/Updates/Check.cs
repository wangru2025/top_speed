using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Core.Updates;
using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void StartAutoUpdateCheck()
        {
            if (!_settings.AutoCheckUpdates)
                return;
            if (_updateCheckQueued || _updateCheckTask != null)
                return;

            _updateCheckQueued = true;
            _updateCheckTask = Task.Run(() => _updateService.CheckAsync(UpdateConfig.CurrentVersion, CancellationToken.None));
        }

        private void StartManualUpdateCheck()
        {
            if (_updateCheckTask != null)
            {
                _speech.Speak(LocalizationService.Mark("Update check is already in progress."));
                return;
            }

            if (_updateDownloadTask != null)
            {
                _speech.Speak(LocalizationService.Mark("An update download is already in progress."));
                return;
            }

            _manualUpdateRequest = true;
            _updatePromptShown = false;
            _pendingUpdateInfo = null;
            _updateCheckTask = Task.Run(() => _updateService.CheckAsync(UpdateConfig.CurrentVersion, CancellationToken.None));
            _speech.Speak(LocalizationService.Mark("Checking for updates."));
        }

        private void UpdateUpdateFlow()
        {
            HandleUpdateCheckCompletion();
            HandleLatestChangesCompletion();
            HandleUpdatePrompt();
            HandleUpdateDownload();
        }

        private void HandleUpdateCheckCompletion()
        {
            if (_updateCheckTask == null || !_updateCheckTask.IsCompleted)
                return;

            var wasManual = _manualUpdateRequest;
            _manualUpdateRequest = false;

            UpdateCheckResult result;
            if (_updateCheckTask.IsFaulted || _updateCheckTask.IsCanceled)
            {
                result = new UpdateCheckResult
                {
                    IsSuccess = false,
                    ErrorMessage = LocalizationService.Mark("Update check failed.")
                };
            }
            else
            {
                result = _updateCheckTask.GetAwaiter().GetResult();
            }

            _updateCheckTask = null;
            if (!result.IsSuccess)
            {
                if (wasManual)
                {
                    ShowMessageDialog(
                        LocalizationService.Mark("Update check failed"),
                        LocalizationService.Mark("The game could not check for updates."),
                        new[] { result.ErrorMessage });
                }

                return;
            }

            _pendingUpdateInfo = result.Update;
            if (_pendingUpdateInfo == null && wasManual)
            {
                ShowMessageDialog(
                    LocalizationService.Mark("No updates found"),
                    LocalizationService.Mark("You are already using the latest version."),
                    Array.Empty<string>());
            }
        }

        private void StartLatestChangesFetch()
        {
            if (_latestChangesTask != null)
            {
                _speech.Speak(LocalizationService.Mark("Latest changes are already being fetched."));
                return;
            }

            _latestChangesTask = Task.Run(() => _updateService.GetLatestChangesAsync(CancellationToken.None));
            _speech.Speak(LocalizationService.Mark("Fetching latest changes."));
        }

        private void HandleLatestChangesCompletion()
        {
            if (_latestChangesTask == null || !_latestChangesTask.IsCompleted)
                return;

            LatestChangesResult result;
            if (_latestChangesTask.IsFaulted || _latestChangesTask.IsCanceled)
            {
                result = new LatestChangesResult
                {
                    IsSuccess = false,
                    ErrorMessage = LocalizationService.Mark("Latest changes request failed.")
                };
            }
            else
            {
                result = _latestChangesTask.GetAwaiter().GetResult();
            }

            _latestChangesTask = null;
            if (!result.IsSuccess)
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Latest changes"),
                    string.Empty,
                    new[] { result.ErrorMessage });
                return;
            }

            var items = new List<DialogItem>();
            if (result.Changes.Count == 0)
            {
                items.Add(new DialogItem(LocalizationService.Mark("No changes are listed.")));
            }
            else
            {
                for (var i = 0; i < result.Changes.Count; i++)
                    items.Add(new DialogItem(result.Changes[i]));
            }

            var dialog = new Dialog(
                LocalizationService.Mark("Latest changes"),
                null,
                QuestionId.Close,
                items,
                onResult: null,
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));
            _dialogs.Show(dialog);
        }

        private void HandleUpdatePrompt()
        {
            if (_pendingUpdateInfo == null || _updatePromptShown)
                return;
            if (_updateDownloadTask != null)
                return;
            if (_dialogs.IsDialogMenu(_menu.CurrentId)
                || _multiplayerCoordinator.Questions.IsQuestionMenu(_menu.CurrentId)
                || _choices.IsChoiceMenu(_menu.CurrentId)
                || _textInputPromptActive
                || _inputMapping.IsActive
                || _shortcutMapping.IsActive)
                return;

            var update = _pendingUpdateInfo;
            _updatePromptShown = true;
            var caption = LocalizationService.Format(
                LocalizationService.Mark("A new version of Top Speed was detected. Your current version is {0}. The new version is {1}. Would you like to download the update?"),
                UpdateConfig.CurrentVersion,
                update.Version);
            var changeItems = new List<DialogItem>();
            var hasChanges = false;
            if (update.Changes != null)
            {
                for (var i = 0; i < update.Changes.Count; i++)
                {
                    var line = update.Changes[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    if (!hasChanges)
                    {
                        changeItems.Add(new DialogItem(LocalizationService.Mark("What's new in this version:")));
                        hasChanges = true;
                    }
                    changeItems.Add(new DialogItem(line.Trim()));
                }
            }

            var dialog = new Dialog(LocalizationService.Mark("New version detected."),
                caption,
                QuestionId.Cancel,
                changeItems,
                onResult: resultId =>
                {
                    if (resultId == QuestionId.Confirm)
                        BeginUpdateDownload(update);
                },
                new DialogButton(QuestionId.Confirm, LocalizationService.Mark("Download update")), new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));

            _dialogs.Show(dialog);
        }
    }
}





