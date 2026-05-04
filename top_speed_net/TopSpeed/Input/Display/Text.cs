using TopSpeed.Input.Devices.Controller;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal static class InputDisplayText
    {
        public static string Key(InputKey key)
        {
            if ((int)key <= 0)
                return LocalizationService.Translate(LocalizationService.Mark("none"));
            if (IsTechnicalScanCodeKey(key))
                return LocalizationService.Translate(LocalizationService.Mark("Unknown"));

            return key switch
            {
                InputKey.Unknown => LocalizationService.Translate(LocalizationService.Mark("Unknown")),
                InputKey.Escape => LocalizationService.Translate(LocalizationService.Mark("Escape")),
                InputKey.Minus => LocalizationService.Translate(LocalizationService.Mark("Minus")),
                InputKey.Equals => LocalizationService.Translate(LocalizationService.Mark("Equals")),
                InputKey.Back => LocalizationService.Translate(LocalizationService.Mark("Backspace")),
                InputKey.Tab => LocalizationService.Translate(LocalizationService.Mark("Tab")),
                InputKey.LeftBracket => LocalizationService.Translate(LocalizationService.Mark("Left bracket")),
                InputKey.RightBracket => LocalizationService.Translate(LocalizationService.Mark("Right bracket")),
                InputKey.Return => LocalizationService.Translate(LocalizationService.Mark("Enter")),
                InputKey.LeftControl => LocalizationService.Translate(LocalizationService.Mark("Left Control")),
                InputKey.Semicolon => LocalizationService.Translate(LocalizationService.Mark("Semicolon")),
                InputKey.Apostrophe => LocalizationService.Translate(LocalizationService.Mark("Apostrophe")),
                InputKey.Grave => LocalizationService.Translate(LocalizationService.Mark("Grave")),
                InputKey.LeftShift => LocalizationService.Translate(LocalizationService.Mark("Left Shift")),
                InputKey.BothShift => LocalizationService.Translate(LocalizationService.Mark("Both Shift keys")),
                InputKey.Backslash => LocalizationService.Translate(LocalizationService.Mark("Backslash")),
                InputKey.Slash => LocalizationService.Translate(LocalizationService.Mark("Slash")),
                InputKey.RightShift => LocalizationService.Translate(LocalizationService.Mark("Right Shift")),
                InputKey.Multiply => LocalizationService.Translate(LocalizationService.Mark("Multiply")),
                InputKey.LeftAlt => LocalizationService.Translate(LocalizationService.Mark("Left Alt")),
                InputKey.Space => LocalizationService.Translate(LocalizationService.Mark("Space")),
                InputKey.Capital => LocalizationService.Translate(LocalizationService.Mark("Caps Lock")),
                InputKey.NumberLock => LocalizationService.Translate(LocalizationService.Mark("Num Lock")),
                InputKey.ScrollLock => LocalizationService.Translate(LocalizationService.Mark("Scroll Lock")),
                InputKey.Subtract => LocalizationService.Translate(LocalizationService.Mark("Subtract")),
                InputKey.Add => LocalizationService.Translate(LocalizationService.Mark("Add")),
                InputKey.Decimal => LocalizationService.Translate(LocalizationService.Mark("Decimal")),
                InputKey.NumberPadEquals => LocalizationService.Translate(LocalizationService.Mark("Numpad Equals")),
                InputKey.PreviousTrack => "Previous Track",
                InputKey.AT => LocalizationService.Translate(LocalizationService.Mark("At")),
                InputKey.Colon => LocalizationService.Translate(LocalizationService.Mark("Colon")),
                InputKey.Underline => LocalizationService.Translate(LocalizationService.Mark("Underline")),
                InputKey.Stop => LocalizationService.Translate(LocalizationService.Mark("Stop")),
                InputKey.NextTrack => "Next Track",
                InputKey.NumberPadEnter => LocalizationService.Translate(LocalizationService.Mark("Numpad Enter")),
                InputKey.RightControl => LocalizationService.Translate(LocalizationService.Mark("Right Control")),
                InputKey.BothControl => LocalizationService.Translate(LocalizationService.Mark("Both Control keys")),
                InputKey.Mute => "Mute",
                InputKey.Calculator => "Calculator",
                InputKey.PlayPause => "Play/Pause",
                InputKey.MediaStop => "Media Stop",
                InputKey.VolumeDown => "Volume Down",
                InputKey.VolumeUp => "Volume Up",
                InputKey.WebHome => "Web Home",
                InputKey.NumberPadComma => LocalizationService.Translate(LocalizationService.Mark("Numpad Comma")),
                InputKey.Divide => LocalizationService.Translate(LocalizationService.Mark("Divide")),
                InputKey.PrintScreen => LocalizationService.Translate(LocalizationService.Mark("Print Screen")),
                InputKey.RightAlt => LocalizationService.Translate(LocalizationService.Mark("Right Alt")),
                InputKey.BothAlt => LocalizationService.Translate(LocalizationService.Mark("Both Alt keys")),
                InputKey.Pause => LocalizationService.Translate(LocalizationService.Mark("Pause")),
                InputKey.Home => LocalizationService.Translate(LocalizationService.Mark("Home")),
                InputKey.Up => LocalizationService.Translate(LocalizationService.Mark("Up")),
                InputKey.PageUp => LocalizationService.Translate(LocalizationService.Mark("Page Up")),
                InputKey.Left => LocalizationService.Translate(LocalizationService.Mark("Left")),
                InputKey.Right => LocalizationService.Translate(LocalizationService.Mark("Right")),
                InputKey.End => LocalizationService.Translate(LocalizationService.Mark("End")),
                InputKey.Down => LocalizationService.Translate(LocalizationService.Mark("Down")),
                InputKey.PageDown => LocalizationService.Translate(LocalizationService.Mark("Page Down")),
                InputKey.Insert => LocalizationService.Translate(LocalizationService.Mark("Insert")),
                InputKey.Delete => LocalizationService.Translate(LocalizationService.Mark("Delete")),
                InputKey.LeftWindowsKey => LocalizationService.Translate(LocalizationService.Mark("Left Windows")),
                InputKey.RightWindowsKey => LocalizationService.Translate(LocalizationService.Mark("Right Windows")),
                InputKey.Applications => LocalizationService.Translate(LocalizationService.Mark("Applications")),
                InputKey.Power => "Power",
                InputKey.Sleep => "Sleep",
                InputKey.Wake => "Wake",
                InputKey.WebSearch => "Web Search",
                InputKey.WebFavorites => "Web Favorites",
                InputKey.WebRefresh => "Web Refresh",
                InputKey.WebStop => "Web Stop",
                InputKey.WebForward => "Web Forward",
                InputKey.WebBack => "Web Back",
                InputKey.MyComputer => "My Computer",
                InputKey.Mail => "Mail",
                InputKey.MediaSelect => "Media Select",
                _ => key.ToString()
            };
        }

        private static bool IsTechnicalScanCodeKey(InputKey key)
        {
            return key == InputKey.Oem102
                || key == InputKey.Kana
                || key == InputKey.AbntC1
                || key == InputKey.Convert
                || key == InputKey.NoConvert
                || key == InputKey.Yen
                || key == InputKey.AbntC2
                || key == InputKey.Kanji
                || key == InputKey.AX
                || key == InputKey.Unlabeled;
        }

        public static string Axis(AxisOrButton axis)
        {
            return Axis(axis, ControllerDisplayProfile.Joystick);
        }

        public static string Axis(AxisOrButton axis, ControllerDisplayProfile profile)
        {
            if (profile.IsGamepad)
                return GamepadAxis(axis, profile.GamepadFamily);

            return GenericAxis(axis);
        }

        private static string GenericAxis(AxisOrButton axis)
        {
            return axis switch
            {
                AxisOrButton.AxisNone => LocalizationService.Translate(LocalizationService.Mark("none")),
                AxisOrButton.AxisXNeg => LocalizationService.Translate(LocalizationService.Mark("X minus")),
                AxisOrButton.AxisXPos => LocalizationService.Translate(LocalizationService.Mark("X plus")),
                AxisOrButton.AxisYNeg => LocalizationService.Translate(LocalizationService.Mark("Y minus")),
                AxisOrButton.AxisYPos => LocalizationService.Translate(LocalizationService.Mark("Y plus")),
                AxisOrButton.AxisZNeg => LocalizationService.Translate(LocalizationService.Mark("Z minus")),
                AxisOrButton.AxisZPos => LocalizationService.Translate(LocalizationService.Mark("Z plus")),
                AxisOrButton.AxisRxNeg => LocalizationService.Translate(LocalizationService.Mark("RX minus")),
                AxisOrButton.AxisRxPos => LocalizationService.Translate(LocalizationService.Mark("RX plus")),
                AxisOrButton.AxisRyNeg => LocalizationService.Translate(LocalizationService.Mark("RY minus")),
                AxisOrButton.AxisRyPos => LocalizationService.Translate(LocalizationService.Mark("RY plus")),
                AxisOrButton.AxisRzNeg => LocalizationService.Translate(LocalizationService.Mark("RZ minus")),
                AxisOrButton.AxisRzPos => LocalizationService.Translate(LocalizationService.Mark("RZ plus")),
                AxisOrButton.AxisSlider1Neg => LocalizationService.Translate(LocalizationService.Mark("Slider 1 minus")),
                AxisOrButton.AxisSlider1Pos => LocalizationService.Translate(LocalizationService.Mark("Slider 1 plus")),
                AxisOrButton.AxisSlider2Neg => LocalizationService.Translate(LocalizationService.Mark("Slider 2 minus")),
                AxisOrButton.AxisSlider2Pos => LocalizationService.Translate(LocalizationService.Mark("Slider 2 plus")),
                AxisOrButton.Button1 => LocalizationService.Translate(LocalizationService.Mark("Button 1")),
                AxisOrButton.Button2 => LocalizationService.Translate(LocalizationService.Mark("Button 2")),
                AxisOrButton.Button3 => LocalizationService.Translate(LocalizationService.Mark("Button 3")),
                AxisOrButton.Button4 => LocalizationService.Translate(LocalizationService.Mark("Button 4")),
                AxisOrButton.Button5 => LocalizationService.Translate(LocalizationService.Mark("Button 5")),
                AxisOrButton.Button6 => LocalizationService.Translate(LocalizationService.Mark("Button 6")),
                AxisOrButton.Button7 => LocalizationService.Translate(LocalizationService.Mark("Button 7")),
                AxisOrButton.Button8 => LocalizationService.Translate(LocalizationService.Mark("Button 8")),
                AxisOrButton.Button9 => LocalizationService.Translate(LocalizationService.Mark("Button 9")),
                AxisOrButton.Button10 => LocalizationService.Translate(LocalizationService.Mark("Button 10")),
                AxisOrButton.Button11 => LocalizationService.Translate(LocalizationService.Mark("Button 11")),
                AxisOrButton.Button12 => LocalizationService.Translate(LocalizationService.Mark("Button 12")),
                AxisOrButton.Button13 => LocalizationService.Translate(LocalizationService.Mark("Button 13")),
                AxisOrButton.Button14 => LocalizationService.Translate(LocalizationService.Mark("Button 14")),
                AxisOrButton.Button15 => LocalizationService.Translate(LocalizationService.Mark("Button 15")),
                AxisOrButton.Button16 => LocalizationService.Translate(LocalizationService.Mark("Button 16")),
                AxisOrButton.Pov1 => LocalizationService.Translate(LocalizationService.Mark("POV 1 up")),
                AxisOrButton.Pov2 => LocalizationService.Translate(LocalizationService.Mark("POV 1 right")),
                AxisOrButton.Pov3 => LocalizationService.Translate(LocalizationService.Mark("POV 1 down")),
                AxisOrButton.Pov4 => LocalizationService.Translate(LocalizationService.Mark("POV 1 left")),
                AxisOrButton.Pov5 => LocalizationService.Translate(LocalizationService.Mark("POV 2 up")),
                AxisOrButton.Pov6 => LocalizationService.Translate(LocalizationService.Mark("POV 2 right")),
                AxisOrButton.Pov7 => LocalizationService.Translate(LocalizationService.Mark("POV 2 down")),
                AxisOrButton.Pov8 => LocalizationService.Translate(LocalizationService.Mark("POV 2 left")),
                _ => axis.ToString()
            };
        }

        private static string GamepadAxis(AxisOrButton axis, ControllerGamepadFamily family)
        {
            return axis switch
            {
                AxisOrButton.AxisNone => LocalizationService.Translate(LocalizationService.Mark("none")),
                AxisOrButton.AxisXNeg => LocalizationService.Translate(LocalizationService.Mark("Left stick left")),
                AxisOrButton.AxisXPos => LocalizationService.Translate(LocalizationService.Mark("Left stick right")),
                AxisOrButton.AxisYNeg => LocalizationService.Translate(LocalizationService.Mark("Left stick down")),
                AxisOrButton.AxisYPos => LocalizationService.Translate(LocalizationService.Mark("Left stick up")),
                AxisOrButton.AxisRxNeg => LocalizationService.Translate(LocalizationService.Mark("Right stick left")),
                AxisOrButton.AxisRxPos => LocalizationService.Translate(LocalizationService.Mark("Right stick right")),
                AxisOrButton.AxisRyNeg => LocalizationService.Translate(LocalizationService.Mark("Right stick down")),
                AxisOrButton.AxisRyPos => LocalizationService.Translate(LocalizationService.Mark("Right stick up")),
                AxisOrButton.AxisZPos => LocalizationService.Translate(LocalizationService.Mark("Left trigger")),
                AxisOrButton.AxisRzPos => LocalizationService.Translate(LocalizationService.Mark("Right trigger")),
                AxisOrButton.Button1 => FaceButton(family, "A", "Cross", "B", "South"),
                AxisOrButton.Button2 => FaceButton(family, "B", "Circle", "A", "East"),
                AxisOrButton.Button3 => FaceButton(family, "X", "Square", "Y", "West"),
                AxisOrButton.Button4 => FaceButton(family, "Y", "Triangle", "X", "North"),
                AxisOrButton.Button5 => FamilyLabel(family, "Left bumper", "L1", "L", "Left shoulder"),
                AxisOrButton.Button6 => FamilyLabel(family, "Right bumper", "R1", "R", "Right shoulder"),
                AxisOrButton.Button7 => FamilyLabel(family, "View", "Share", "Minus", "Back"),
                AxisOrButton.Button8 => FamilyLabel(family, "Menu", "Options", "Plus", "Start"),
                AxisOrButton.Button9 => FamilyLabel(family, "Left stick button", "L3", "Left stick button", "Left stick"),
                AxisOrButton.Button10 => FamilyLabel(family, "Right stick button", "R3", "Right stick button", "Right stick"),
                AxisOrButton.Button11 => FamilyLabel(family, "Xbox button", "PS button", "Home", "Guide"),
                AxisOrButton.Button12 => FamilyLabel(family, "Share", "Touchpad", "Capture", "Misc 1"),
                AxisOrButton.Button13 => LocalizationService.Translate(LocalizationService.Mark("Left paddle 1")),
                AxisOrButton.Button14 => LocalizationService.Translate(LocalizationService.Mark("Right paddle 1")),
                AxisOrButton.Button15 => LocalizationService.Translate(LocalizationService.Mark("Left paddle 2")),
                AxisOrButton.Button16 => LocalizationService.Translate(LocalizationService.Mark("Right paddle 2")),
                AxisOrButton.Pov1 => LocalizationService.Translate(LocalizationService.Mark("D-pad up")),
                AxisOrButton.Pov2 => LocalizationService.Translate(LocalizationService.Mark("D-pad right")),
                AxisOrButton.Pov3 => LocalizationService.Translate(LocalizationService.Mark("D-pad down")),
                AxisOrButton.Pov4 => LocalizationService.Translate(LocalizationService.Mark("D-pad left")),
                _ => GenericAxis(axis)
            };
        }

        private static string FaceButton(ControllerGamepadFamily family, string xbox, string playStation, string nintendo, string semantic)
        {
            return FamilyLabel(family, xbox, playStation, nintendo, semantic);
        }

        private static string FamilyLabel(ControllerGamepadFamily family, string xbox, string playStation, string nintendo, string semantic)
        {
            var value = family switch
            {
                ControllerGamepadFamily.Xbox => xbox,
                ControllerGamepadFamily.PlayStation => playStation,
                ControllerGamepadFamily.Nintendo => nintendo,
                _ => semantic
            };
            return LocalizationService.Translate(LocalizationService.Mark(value));
        }
    }
}
