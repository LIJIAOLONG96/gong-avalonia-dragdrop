using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Xunit;

namespace GongSolutions.Avalonia.DragDrop.Tests;

public class TabControlDragTests
{
    [AvaloniaFact]
    public void DragCanStartFromTabHeader()
    {
        var handler = new RecordingDragHandler();
        var tabControl = new TabControl
        {
            ItemsSource = new[] { "Overview", "Details" },
            ItemTemplate = new FuncDataTemplate<string>((item, _) => new TextBlock { Text = item })
        };
        DragDrop.SetIsDragSource(tabControl, true);
        DragDrop.SetDragHandler(tabControl, handler);

        var window = new Window
        {
            Width = 600,
            Height = 300,
            Content = tabControl
        };
        window.Show();

        var tabItem = tabControl.GetVisualDescendants().OfType<TabItem>().First();
        var headerContent = tabItem.GetVisualDescendants().OfType<TextBlock>().First();
        var origin = GetPositionRelativeTo(headerContent, tabControl);
        var start = origin + new Vector(headerContent.Bounds.Width / 2, headerContent.Bounds.Height / 2);

        window.MouseMove(start, RawInputModifiers.None);
        window.MouseDown(start, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseMove(start + new Vector(10, 0), RawInputModifiers.LeftMouseButton);
        window.MouseUp(start + new Vector(10, 0), MouseButton.Left, RawInputModifiers.None);

        Assert.True(handler.Started);
        Assert.Equal("Overview", Assert.Single(handler.SourceItems));
        window.Close();
    }

    [AvaloniaFact]
    public void ReleasedButtonWithoutPointerReleaseCannotStartDrag()
    {
        var handler = new RecordingDragHandler();
        var tabControl = new TabControl
        {
            ItemsSource = new[] { "Overview", "Details" },
            ItemTemplate = new FuncDataTemplate<string>((item, _) => new TextBlock { Text = item })
        };
        DragDrop.SetIsDragSource(tabControl, true);
        DragDrop.SetDragHandler(tabControl, handler);

        var window = new Window { Width = 600, Height = 300, Content = tabControl };
        window.Show();

        var tabItem = tabControl.GetVisualDescendants().OfType<TabItem>().First();
        var headerContent = tabItem.GetVisualDescendants().OfType<TextBlock>().First();
        var origin = GetPositionRelativeTo(headerContent, tabControl);
        var start = origin + new Vector(headerContent.Bounds.Width / 2, headerContent.Bounds.Height / 2);

        window.MouseMove(start, RawInputModifiers.None);
        window.MouseDown(start, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseMove(start + new Vector(10, 0), RawInputModifiers.None);
        window.MouseMove(start + new Vector(20, 0), RawInputModifiers.LeftMouseButton);

        Assert.False(handler.Started);
        window.MouseUp(start + new Vector(20, 0), MouseButton.Left, RawInputModifiers.None);
        window.Close();
    }

    [AvaloniaFact]
    public void ReleaseOutsideSourceCancelsPendingDrag()
    {
        var handler = new RecordingDragHandler();
        var tabControl = new TabControl
        {
            Width = 300,
            ItemsSource = new[] { "Overview", "Details" },
            ItemTemplate = new FuncDataTemplate<string>((item, _) => new TextBlock { Text = item })
        };
        DragDrop.SetIsDragSource(tabControl, true);
        DragDrop.SetDragHandler(tabControl, handler);

        var saveButton = new Button { Content = "Save", Width = 100 };
        var layout = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal };
        layout.Children.Add(tabControl);
        layout.Children.Add(saveButton);
        var window = new Window { Width = 600, Height = 300, Content = layout };
        window.Show();

        var headerContent = tabControl.GetVisualDescendants().OfType<TabItem>().First()
            .GetVisualDescendants().OfType<TextBlock>().First();
        var origin = GetPositionRelativeTo(headerContent, window);
        var start = origin + new Vector(headerContent.Bounds.Width / 2, headerContent.Bounds.Height / 2);
        var savePosition = GetPositionRelativeTo(saveButton, window) + new Vector(20, 20);
        var sourceSawRelease = false;
        IPointer? pressedPointer = null;
        window.AddHandler(InputElement.PointerPressedEvent, (_, args) => pressedPointer = args.Pointer,
            RoutingStrategies.Tunnel, true);
        tabControl.PointerReleased += (_, _) => sourceSawRelease = true;

        window.MouseMove(start, RawInputModifiers.None);
        window.MouseDown(start, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        Assert.NotNull(pressedPointer);
        pressedPointer.Capture(saveButton);
        window.MouseUp(savePosition, MouseButton.Left, RawInputModifiers.None);
        window.MouseMove(start + new Vector(10, 0), RawInputModifiers.LeftMouseButton);

        Assert.False(sourceSawRelease);
        Assert.False(handler.Started);
        window.Close();
    }

    private static Point GetPositionRelativeTo(Visual visual, Visual ancestor)
    {
        var position = default(Point);
        for (var current = visual; !ReferenceEquals(current, ancestor); current = current.GetVisualParent()!)
        {
            position += current.Bounds.Position;
        }

        return position;
    }

    private sealed class RecordingDragHandler : DefaultDragHandler
    {
        public bool Started { get; private set; }
        public IReadOnlyList<object> SourceItems { get; private set; } = Array.Empty<object>();

        public override void StartDrag(IDragInfo dragInfo)
        {
            Started = true;
            SourceItems = dragInfo.SourceItems;
            dragInfo.Effects = DragDropEffects.None;
        }
    }
}
