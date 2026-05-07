using TopSpeed.Input;
using TopSpeed.Shortcuts;
using TS.Sdl.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ShortcutGestureBehaviorTests
{
    [Fact]
    public void TryResolveTriggeredAction_UsesGestureIntentWhenDefined()
    {
        var (service, _, _) = InputHarness.CreateService();
        using (service)
        {
            var catalog = new ShortcutCatalog();
            var triggerCount = 0;

            catalog.RegisterAction(
                "chat.next",
                "Next chat",
                "Moves to the next chat category.",
                InputKey.Right,
                ShortcutModifiers.None,
                () => triggerCount++,
                gestureIntent: GestureIntent.SwipeRight);
            catalog.SetGlobalActions(new[] { "chat.next" });

            service.SubmitGesture(new GestureEvent { Kind = GestureKind.Swipe, Direction = SwipeDirection.Right });

            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out var action).Should().BeTrue();
            action.Trigger();
            triggerCount.Should().Be(1);
            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out _).Should().BeFalse();
        }
    }

    [Fact]
    public void TryResolveTriggeredAction_FiresOnceWhenKeyAndGestureArriveTogether()
    {
        var (service, keyboard, _) = InputHarness.CreateService();
        using (service)
        {
            var catalog = new ShortcutCatalog();
            var triggerCount = 0;

            catalog.RegisterAction(
                "chat.next",
                "Next chat",
                "Moves to the next chat category.",
                InputKey.Right,
                ShortcutModifiers.None,
                () => triggerCount++,
                gestureIntent: GestureIntent.SwipeRight);
            catalog.SetGlobalActions(new[] { "chat.next" });

            keyboard.SetDown(InputKey.Right);
            service.SubmitGesture(new GestureEvent { Kind = GestureKind.Swipe, Direction = SwipeDirection.Right });

            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out var action).Should().BeTrue();
            action.Trigger();
            triggerCount.Should().Be(1);
            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out _).Should().BeFalse();
        }
    }

    [Fact]
    public void TryResolveTriggeredAction_UsesExactModifierMatchForKeyboardShortcuts()
    {
        var (service, keyboard, _) = InputHarness.CreateService();
        using (service)
        {
            var catalog = new ShortcutCatalog();

            catalog.RegisterAction(
                "history.previous",
                "Previous history item",
                "Moves to the previous history item.",
                InputKey.Comma,
                ShortcutModifiers.None,
                () => { });
            catalog.RegisterAction(
                "history.first",
                "First history item",
                "Moves to the first history item.",
                InputKey.Comma,
                new ShortcutModifiers(shift: true, control: false, alt: false),
                () => { });
            catalog.SetGlobalActions(new[] { "history.previous", "history.first" });

            keyboard.SetDown(InputKey.LeftShift, InputKey.Comma);
            service.Update();

            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out var action).Should().BeTrue();
            action.Id.Should().Be("history.first");
        }
    }

    [Fact]
    public void TryResolveTriggeredAction_DoesNotTriggerWhenUnboundModifierIsHeld()
    {
        var (service, keyboard, _) = InputHarness.CreateService();
        using (service)
        {
            var catalog = new ShortcutCatalog();

            catalog.RegisterAction(
                "history.next",
                "Next history item",
                "Moves to the next history item.",
                InputKey.Period,
                ShortcutModifiers.None,
                () => { });
            catalog.SetGlobalActions(new[] { "history.next" });

            keyboard.SetDown(InputKey.LeftShift, InputKey.Period);
            service.Update();

            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out _).Should().BeFalse();
        }
    }

    [Fact]
    public void ResetBindingsInGroup_ResetsOnlyBindingsInThatGroup()
    {
        var catalog = new ShortcutCatalog();
        catalog.RegisterAction("a1", "Action1", "Action 1", InputKey.A, ShortcutModifiers.None, () => { });
        catalog.RegisterAction("a2", "Action2", "Action 2", InputKey.B, ShortcutModifiers.None, () => { });
        catalog.SetGlobalActions(new[] { "a1", "a2" });
        catalog.SetMenuActions("menu_x", new[] { "a1" }, "Menu X");

        catalog.SetBinding("a1", InputKey.Z, new ShortcutModifiers(shift: true, control: false, alt: false));
        catalog.SetBinding("a2", InputKey.X, new ShortcutModifiers(shift: false, control: true, alt: false));

        catalog.ResetBindingsInGroup("menu:menu_x").Should().BeTrue();

        catalog.TryGetBinding("a1", out var binding1).Should().BeTrue();
        binding1.Key.Should().Be(InputKey.A);
        binding1.Modifiers.Should().Be(ShortcutModifiers.None);

        catalog.TryGetBinding("a2", out var binding2).Should().BeTrue();
        binding2.Key.Should().Be(InputKey.X);
        binding2.Modifiers.Should().Be(new ShortcutModifiers(shift: false, control: true, alt: false));
    }
}
