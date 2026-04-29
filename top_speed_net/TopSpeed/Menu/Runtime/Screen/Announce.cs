using System;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Speech;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        public bool TrySpeakCurrentHintOnDemand()
        {
            if (_index == NoSelection)
                return false;

            var item = _items[_index];
            var hint = item.GetHintText();
            if (string.IsNullOrWhiteSpace(hint))
                return false;

            CancelHint();
            _speech.Speak(hint!, SpeechService.SpeakFlag.NoInterrupt);
            return true;
        }

        public void AnnounceSelection()
        {
            AnnounceCurrent(!_justEntered);
            _justEntered = false;
        }

        private void AnnounceCurrent(bool purge, SpeechService.SpeakFlag speakFlag = SpeechService.SpeakFlag.NoInterruptButStop)
        {
            if (_index == NoSelection)
                return;

            var item = _items[_index];
            var displayText = item.GetDisplayText();
            _speech.Speak(displayText, speakFlag);
            ScheduleHint(item, _index, displayText);
        }

        public void AnnounceTitle()
        {
            _justEntered = true;
            _ignoreHeldInput = true;
            _activeActionIndex = NoSelection;
            CancelHint();
            var opening = _openingAnnouncementOverride ?? Title;
            _openingAnnouncementOverride = null;
            _waitForTitleSpeechBeforeAutoFocus = !string.IsNullOrWhiteSpace(opening);
            if (!string.IsNullOrWhiteSpace(opening))
                _speech.Speak(opening, ResolveTitleSpeakFlag());

            _index = NoSelection;
            if (_suppressAutoFocus)
            {
                _suppressAutoFocus = false;
                ClearAutoFocusPending();
            }
            else
            {
                QueueAutoFocusFirstItem(force: string.IsNullOrWhiteSpace(opening));
            }
        }

        public void QueueTitleAnnouncement(string? openingAnnouncementOverride = null)
        {
            _openingAnnouncementOverride = openingAnnouncementOverride;
            _titlePending = true;
        }

        private void ScheduleHint(MenuItem item, int index, string displayText)
        {
            CancelHint();
            if (!_usageHintsEnabled())
                return;
            var hint = item.GetHintText();
            if (string.IsNullOrWhiteSpace(hint))
                return;

            var token = Volatile.Read(ref _hintToken);
            var delayMs = CalculateHintDelay(displayText);
            Task.Run(async () =>
            {
                await Task.Delay(delayMs).ConfigureAwait(false);
                if (token != Volatile.Read(ref _hintToken))
                    return;
                if (_disposed || _index != index)
                    return;
                var delayedHint = item.GetHintText();
                if (!_usageHintsEnabled() || string.IsNullOrWhiteSpace(delayedHint))
                    return;
                _speech.Speak(delayedHint!, SpeechService.SpeakFlag.NoInterrupt);
            });
        }

        private int CalculateHintDelay(string displayText)
        {
            var words = CountWords(displayText);
            var rateMs = _speech.ScreenReaderRateMs;
            var baseDelay = rateMs > 0f ? words * rateMs : 0f;
            var totalDelay = baseDelay + 1000f;
            return (int)Math.Max(0, Math.Ceiling(totalDelay));
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private void CancelHint()
        {
            Interlocked.Increment(ref _hintToken);
        }

        private SpeechService.SpeakFlag ResolveTitleSpeakFlag()
        {
            return ActiveView.TitleFlag == SpeechService.SpeakFlag.None
                ? SpeechService.SpeakFlag.NoInterrupt
                : ActiveView.TitleFlag;
        }
    }
}

