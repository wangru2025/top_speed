using System;
using Eto.Drawing;
using Eto.Forms;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Runtime;

namespace TopSpeed.Windowing.Eto
{
    internal sealed class WindowHost : IWindowHost, IKeyboardEventSource
    {
        private readonly object _textInputLock = new object();
        private readonly Application _application;
        private readonly Form _window;
        private readonly Drawable _root;
        private TextBox? _inputBox;
        private bool _loadedRaised;
        private bool _submitPending;
        private bool _cancelPending;
        private bool _textInputActive;
        private string _submittedText = string.Empty;

        public event Action? Loaded;
        public event Action? Closed;
        public event Action<InputKey>? KeyDown;
        public event Action<InputKey>? KeyUp;

        public IntPtr NativeHandle { get; private set; }

        internal Form MainForm => _window;

        public WindowHost()
        {
            _application = ApplicationFactory.GetOrCreate();
            _window = new Form
            {
                Title = ResolveWindowTitle(),
                ClientSize = new Size(640, 360),
                Resizable = false,
                Maximizable = false,
                Minimizable = true
            };
            _window.Shown += OnShown;
            _window.Closed += OnClosed;
            _window.KeyDown += OnWindowKeyDown;
            _window.KeyUp += OnWindowKeyUp;
            _window.LostFocus += OnWindowLostFocus;
            _root = new Drawable
            {
                CanFocus = true
            };
            _root.KeyDown += OnWindowKeyDown;
            _root.KeyUp += OnWindowKeyUp;
            _window.Content = _root;
        }

        public void Run()
        {
            _application.Run(_window);
        }

        public void RequestClose()
        {
            InvokeOnUi(() =>
            {
                try
                {
                    _window.Close();
                }
                catch
                {
                }
            });
        }

        public void Dispose()
        {
            _window.Shown -= OnShown;
            _window.Closed -= OnClosed;
            _window.KeyDown -= OnWindowKeyDown;
            _window.KeyUp -= OnWindowKeyUp;
            _window.LostFocus -= OnWindowLostFocus;
            _root.KeyDown -= OnWindowKeyDown;
            _root.KeyUp -= OnWindowKeyUp;
            DisposeInputBoxControl();
            _window.Dispose();
        }

        internal void ShowTextInput(string? initialText)
        {
            lock (_textInputLock)
            {
                _submittedText = string.Empty;
                _submitPending = false;
                _cancelPending = false;
            }

            InvokeOnUi(() =>
            {
                EnsureInputBox();
                if (_inputBox == null)
                    return;

                _inputBox.Text = initialText ?? string.Empty;
                _inputBox.Visible = true;
                _inputBox.Enabled = true;
                _root.Content = _inputBox;
                _textInputActive = true;
                ReleaseAllModifiers();
                _inputBox.Focus();
            });
        }

        internal void HideTextInput()
        {
            InvokeOnUi(() =>
            {
                HideInputBox();
                _root.Focus();
            });
        }

        internal bool TryConsumeTextInput(out TextInputResult result)
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

        private void OnShown(object? sender, EventArgs e)
        {
            if (!_loadedRaised)
            {
                _loadedRaised = true;
                NativeHandle = ResolveNativeHandle(_window);
                _root.Focus();
                Loaded?.Invoke();
            }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            Closed?.Invoke();
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            if (_textInputActive)
                return;
            EmitKeyDown(e.KeyData);
        }

        private void OnWindowKeyUp(object? sender, KeyEventArgs e)
        {
            if (_textInputActive)
                return;
            EmitKeyUp(e.KeyData);
        }

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (_inputBox == null)
                return;

            if (EtoKeyMap.MatchesEnter(e.KeyData))
            {
                lock (_textInputLock)
                {
                    _submittedText = _inputBox.Text ?? string.Empty;
                    _submitPending = true;
                }

                HideTextInput();
                e.Handled = true;
                return;
            }

            if (EtoKeyMap.MatchesEscape(e.KeyData))
            {
                lock (_textInputLock)
                    _cancelPending = true;

                HideTextInput();
                e.Handled = true;
                return;
            }

            // Let the native text box handle all edit/navigation keys while active.
        }

        private void OnInputKeyUp(object? sender, KeyEventArgs e)
        {
            if (_inputBox == null)
                return;
        }

        private void OnWindowLostFocus(object? sender, EventArgs e)
        {
            ReleaseAllModifiers();
        }

        private void EmitKeyDown(Keys keyData)
        {
            if (EtoKeyMap.TryMap(keyData, out var key))
                KeyDown?.Invoke(key);
        }

        private void EmitKeyUp(Keys keyData)
        {
            if (EtoKeyMap.TryMap(keyData, out var key))
                KeyUp?.Invoke(key);
        }

        private void ReleaseAllModifiers()
        {
            KeyUp?.Invoke(InputKey.LeftShift);
            KeyUp?.Invoke(InputKey.RightShift);
            KeyUp?.Invoke(InputKey.LeftControl);
            KeyUp?.Invoke(InputKey.RightControl);
            KeyUp?.Invoke(InputKey.LeftAlt);
            KeyUp?.Invoke(InputKey.RightAlt);
        }

        private void InvokeOnUi(Action action)
        {
            var app = Application.Instance ?? _application;
            try
            {
                app.Invoke(action);
            }
            catch
            {
                app.AsyncInvoke(action);
            }
        }

        private void EnsureInputBox()
        {
            if (_inputBox != null)
                return;

            _inputBox = new TextBox
            {
                Visible = false
            };
            _inputBox.KeyDown += OnInputKeyDown;
            _inputBox.KeyUp += OnInputKeyUp;
        }

        private void HideInputBox()
        {
            if (_inputBox == null)
                return;

            _textInputActive = false;
            _inputBox.Visible = false;
            _inputBox.Enabled = false;
            _root.Content = null;
            ReleaseAllModifiers();
        }

        private void DisposeInputBoxControl()
        {
            if (_inputBox == null)
                return;

            HideInputBox();
            _inputBox.KeyDown -= OnInputKeyDown;
            _inputBox.KeyUp -= OnInputKeyUp;
            _inputBox.Dispose();
            _inputBox = null;
        }

        private static string ResolveWindowTitle()
        {
            var title = LocalizationService.Translate(LocalizationService.Mark("Top Speed"));
            return string.IsNullOrWhiteSpace(title) ? "Top Speed" : title;
        }

        private static IntPtr ResolveNativeHandle(Form window)
        {
            try
            {
                var controlObject = window.ControlObject;
                if (controlObject == null)
                    return IntPtr.Zero;

                var handleProperty = controlObject.GetType().GetProperty("Handle");
                if (handleProperty == null)
                    return IntPtr.Zero;

                var value = handleProperty.GetValue(controlObject);
                return value is IntPtr handle ? handle : IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }
    }
}
