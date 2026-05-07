using TopSpeed.Core.Multiplayer.Chat;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class HistoryBufferBehaviorTests
{
    [Fact]
    public void MoveToNextAndPrevious_DoNotWrapWhenWrapNavigationDisabled()
    {
        var buffers = new HistoryBuffers(10);

        var previousAtStart = buffers.MoveToPrevious(wrapNavigation: false);
        previousAtStart.Moved.Should().BeFalse();
        previousAtStart.EdgeReached.Should().BeTrue();
        buffers.Current.Should().Be(HistoryBuffer.All);

        buffers.MoveToNext(wrapNavigation: false);
        buffers.Current.Should().Be(HistoryBuffer.GlobalChat);
        buffers.MoveToNext(wrapNavigation: false);
        buffers.Current.Should().Be(HistoryBuffer.RoomChat);
        buffers.MoveToNext(wrapNavigation: false);
        buffers.Current.Should().Be(HistoryBuffer.Connections);
        buffers.MoveToNext(wrapNavigation: false);
        buffers.Current.Should().Be(HistoryBuffer.RoomEvents);

        var nextAtEnd = buffers.MoveToNext(wrapNavigation: false);
        nextAtEnd.Moved.Should().BeFalse();
        nextAtEnd.EdgeReached.Should().BeTrue();
        buffers.Current.Should().Be(HistoryBuffer.RoomEvents);
    }

    [Fact]
    public void ItemNavigation_DoesNotWrapAndSupportsFirstLast()
    {
        var buffers = new HistoryBuffers(10);
        buffers.AddGlobalChat("one");
        buffers.AddGlobalChat("two");
        buffers.MoveToNext(wrapNavigation: false);

        buffers.GetCurrentFocusedItemText().Should().Be("two");

        var nextAtEnd = buffers.MoveCurrentItem(1, wrapNavigation: false);
        nextAtEnd.Moved.Should().BeFalse();
        nextAtEnd.EdgeReached.Should().BeTrue();
        nextAtEnd.Text.Should().Be("two");

        var previous = buffers.MoveCurrentItem(-1, wrapNavigation: false);
        previous.Moved.Should().BeTrue();
        previous.Text.Should().Be("one");

        var previousAtStart = buffers.MoveCurrentItem(-1, wrapNavigation: false);
        previousAtStart.Moved.Should().BeFalse();
        previousAtStart.EdgeReached.Should().BeTrue();
        previousAtStart.Text.Should().Be("one");

        var last = buffers.MoveCurrentItemToLast();
        last.Moved.Should().BeTrue();
        last.Text.Should().Be("two");

        var first = buffers.MoveCurrentItemToFirst();
        first.Moved.Should().BeTrue();
        first.Text.Should().Be("one");

        var firstAtEdge = buffers.MoveCurrentItemToFirst();
        firstAtEdge.Moved.Should().BeFalse();
        firstAtEdge.EdgeReached.Should().BeTrue();
        firstAtEdge.Text.Should().Be("one");
    }
}

