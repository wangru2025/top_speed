using System;
using System.Drawing;
using System.Windows.Forms;
using TopSpeed.Localization;
using TopSpeed.Runtime;

namespace TopSpeed.Windowing
{
    internal sealed class GameWindow : Form
    {
        private const int WmSysCommand = 0x0112;
        private const int WmSysChar = 0x0106;
        private const int ScKeyMenu = 0xF100;
        private readonly TextBox _inputBox;
        private readonly object _textInputLock = new object();
        private bool _submitPending;
        private bool _cancelPending;
        private string _submittedText = string.Empty;

        public GameWindow()
        {
            Text = LocalizationService.Translate(LocalizationService.Mark("Top Speed"));
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(640, 360);
            KeyPreview = true;
            IsMdiContainer = true;

            _inputBox = new TextBox
            {
                Visible = false,
                Enabled = false,
                AcceptsReturn = true,
                CausesValidation = false,
                ImeMode = ImeMode.NoControl,
                TabStop = false,
                BorderStyle = BorderStyle.FixedSingle,
                Width = 400,
                Left = 12,
                Top = ClientSize.Height - 48,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            _inputBox.KeyDown += OnInputKeyDown;
            Controls.Add(_inputBox);
        }

        public void ShowTextInput(string? initialText)
        {
            lock (_textInputLock)
            {
                _submittedText = string.Empty;
                _submitPending = false;
                _cancelPending = false;
            }
            RunOnUiThread(() =>
            {
                _inputBox.Text = initialText ?? string.Empty;
                _inputBox.Enabled = true;
                _inputBox.Visible = true;
                _inputBox.BringToFront();
                ActiveControl = _inputBox;
                _inputBox.SelectAll();
                _inputBox.Focus();
                Invalidate(_inputBox.Bounds);
                Update();
            });
        }

        public void HideTextInput()
        {
            RunOnUiThread(() =>
            {
                if (!_inputBox.Visible && !_inputBox.Enabled)
                    return;

                var bounds = _inputBox.Bounds;
                ActiveControl = null;
                _inputBox.Enabled = false;
                _inputBox.Visible = false;
                _inputBox.Clear();
                Focus();
                Invalidate(bounds);
                Update();
            });
        }

        public bool TryConsumeTextInput(out TextInputResult result)
        {
            lock (_textInputLock)
            {
                if (_submitPending)
                {
                    _submitPending = false;
                    result = TextInputResult.Submitted(_submittedText);
                    return true;
                }
                if (_cancelPending)
                {
                    _cancelPending = false;
                    result = TextInputResult.CreateCancelled();
                    return true;
                }
            }
            result = default;
            return false;
        }

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                lock (_textInputLock)
                {
                    _submittedText = _inputBox.Text;
                    _submitPending = true;
                }
                HideTextInput();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                lock (_textInputLock)
                    _cancelPending = true;
                HideTextInput();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void RunOnUiThread(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
                return;
            }

            action();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmSysCommand && ((int)m.WParam & 0xFFF0) == ScKeyMenu)
            {
                return;
            }

            // Swallow system-char events so Alt-based game shortcuts do not trigger the Windows bell.
            if (m.Msg == WmSysChar)
            {
                return;
            }

            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F10)
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}

