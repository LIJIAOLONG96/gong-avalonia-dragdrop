using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GongSolutions.Avalonia.DragDrop;

public static class DragDrop
{
    private static readonly DataFormat<object> GongDataFormat = global::Avalonia.Input.DataFormat.CreateInProcessFormat<object>("GongSolutions.Avalonia.DragDrop");
    private static readonly ConditionalWeakTable<Control, DragSession> Sessions = new();
    private static readonly ConditionalWeakTable<IDataTransfer, DragSession> TransferSessions = new();

    public static readonly AttachedProperty<bool> IsDragSourceProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsDragSource", typeof(DragDrop));

    public static readonly AttachedProperty<bool> IsDropTargetProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsDropTarget", typeof(DragDrop));

    public static readonly AttachedProperty<IDragSource?> DragHandlerProperty =
        AvaloniaProperty.RegisterAttached<Control, IDragSource?>("DragHandler", typeof(DragDrop));

    public static readonly AttachedProperty<IDropTarget?> DropHandlerProperty =
        AvaloniaProperty.RegisterAttached<Control, IDropTarget?>("DropHandler", typeof(DragDrop));

    public static readonly AttachedProperty<string?> DragDropContextProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("DragDropContext", typeof(DragDrop));

    public static readonly AttachedProperty<IDragInfoBuilder?> DragInfoBuilderProperty =
        AvaloniaProperty.RegisterAttached<Control, IDragInfoBuilder?>("DragInfoBuilder", typeof(DragDrop));

    public static readonly AttachedProperty<IDropInfoBuilder?> DropInfoBuilderProperty =
        AvaloniaProperty.RegisterAttached<Control, IDropInfoBuilder?>("DropInfoBuilder", typeof(DragDrop));

    public static readonly AttachedProperty<bool> SelectDroppedItemsProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("SelectDroppedItems", typeof(DragDrop), true);

    public static readonly AttachedProperty<ScrollingMode> ScrollingModeProperty =
        AvaloniaProperty.RegisterAttached<Control, ScrollingMode>("ScrollingMode", typeof(DragDrop), ScrollingMode.Both);

    public static readonly AttachedProperty<double> MinimumHorizontalDragDistanceProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("MinimumHorizontalDragDistance", typeof(DragDrop), 4);

    public static readonly AttachedProperty<double> MinimumVerticalDragDistanceProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("MinimumVerticalDragDistance", typeof(DragDrop), 4);

    public static readonly AttachedProperty<KeyModifiers> DragDropCopyKeyModifiersProperty =
        AvaloniaProperty.RegisterAttached<Control, KeyModifiers>("DragDropCopyKeyModifiers", typeof(DragDrop), KeyModifiers.Control);

    public static readonly AttachedProperty<bool> CanDragWithMouseRightButtonProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("CanDragWithMouseRightButton", typeof(DragDrop));

    public static readonly AttachedProperty<bool> DragSourceIgnoreProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("DragSourceIgnore", typeof(DragDrop));

    public static readonly AttachedProperty<IDataTemplate?> DragPreviewTemplateProperty =
        AvaloniaProperty.RegisterAttached<Control, IDataTemplate?>("DragPreviewTemplate", typeof(DragDrop));

    public static readonly AttachedProperty<bool> UseVisualSourceItemPreviewProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("UseVisualSourceItemPreview", typeof(DragDrop), true);

    public static readonly AttachedProperty<IDataTemplate?> EffectPreviewTemplateProperty =
        AvaloniaProperty.RegisterAttached<Control, IDataTemplate?>("EffectPreviewTemplate", typeof(DragDrop));

    public static readonly AttachedProperty<string?> DropHintTextProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("DropHintText", typeof(DragDrop));

    public static readonly AttachedProperty<IBrush> DropTargetAdornerBrushProperty =
        AvaloniaProperty.RegisterAttached<Control, IBrush>("DropTargetAdornerBrush", typeof(DragDrop), new SolidColorBrush(Color.FromRgb(37, 99, 235)));

    public static readonly AttachedProperty<bool> ShowDropTargetIndicatorProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("ShowDropTargetIndicator", typeof(DragDrop), true);

    public static readonly AttachedProperty<IDropTargetAdornerFactory?> DropTargetAdornerFactoryProperty =
        AvaloniaProperty.RegisterAttached<Control, IDropTargetAdornerFactory?>("DropTargetAdornerFactory", typeof(DragDrop));

    public static readonly AttachedProperty<IDropTargetItemsSorter?> DropTargetItemsSorterProperty =
        AvaloniaProperty.RegisterAttached<Control, IDropTargetItemsSorter?>("DropTargetItemsSorter", typeof(DragDrop));

    public static readonly AttachedProperty<IDropIndexResolver?> DropIndexResolverProperty =
        AvaloniaProperty.RegisterAttached<Control, IDropIndexResolver?>("DropIndexResolver", typeof(DragDrop));

    public static readonly AttachedProperty<IDropGroupResolver?> DropGroupResolverProperty =
        AvaloniaProperty.RegisterAttached<Control, IDropGroupResolver?>("DropGroupResolver", typeof(DragDrop));

    public static IDragSource DefaultDragHandler { get; } = new DefaultDragHandler();
    public static IDropTarget DefaultDropHandler { get; } = new DefaultDropHandler();

    static DragDrop()
    {
        IsDragSourceProperty.Changed.AddClassHandler<Control>(OnIsDragSourceChanged);
        IsDropTargetProperty.Changed.AddClassHandler<Control>(OnIsDropTargetChanged);
    }

    public static bool GetIsDragSource(Control element) => element.GetValue(IsDragSourceProperty);
    public static void SetIsDragSource(Control element, bool value) => element.SetValue(IsDragSourceProperty, value);
    public static bool GetIsDropTarget(Control element) => element.GetValue(IsDropTargetProperty);
    public static void SetIsDropTarget(Control element, bool value) => element.SetValue(IsDropTargetProperty, value);
    public static IDragSource? GetDragHandler(Control element) => element.GetValue(DragHandlerProperty);
    public static void SetDragHandler(Control element, IDragSource? value) => element.SetValue(DragHandlerProperty, value);
    public static IDropTarget? GetDropHandler(Control element) => element.GetValue(DropHandlerProperty);
    public static void SetDropHandler(Control element, IDropTarget? value) => element.SetValue(DropHandlerProperty, value);
    public static string? GetDragDropContext(Control element) => element.GetValue(DragDropContextProperty);
    public static void SetDragDropContext(Control element, string? value) => element.SetValue(DragDropContextProperty, value);
    public static IDragInfoBuilder? GetDragInfoBuilder(Control element) => element.GetValue(DragInfoBuilderProperty);
    public static void SetDragInfoBuilder(Control element, IDragInfoBuilder? value) => element.SetValue(DragInfoBuilderProperty, value);
    public static IDropInfoBuilder? GetDropInfoBuilder(Control element) => element.GetValue(DropInfoBuilderProperty);
    public static void SetDropInfoBuilder(Control element, IDropInfoBuilder? value) => element.SetValue(DropInfoBuilderProperty, value);
    public static bool GetSelectDroppedItems(Control element) => element.GetValue(SelectDroppedItemsProperty);
    public static void SetSelectDroppedItems(Control element, bool value) => element.SetValue(SelectDroppedItemsProperty, value);
    public static ScrollingMode GetScrollingMode(Control element) => element.GetValue(ScrollingModeProperty);
    public static void SetScrollingMode(Control element, ScrollingMode value) => element.SetValue(ScrollingModeProperty, value);
    public static double GetMinimumHorizontalDragDistance(Control element) => element.GetValue(MinimumHorizontalDragDistanceProperty);
    public static void SetMinimumHorizontalDragDistance(Control element, double value) => element.SetValue(MinimumHorizontalDragDistanceProperty, value);
    public static double GetMinimumVerticalDragDistance(Control element) => element.GetValue(MinimumVerticalDragDistanceProperty);
    public static void SetMinimumVerticalDragDistance(Control element, double value) => element.SetValue(MinimumVerticalDragDistanceProperty, value);
    public static KeyModifiers GetDragDropCopyKeyModifiers(Control element) => element.GetValue(DragDropCopyKeyModifiersProperty);
    public static void SetDragDropCopyKeyModifiers(Control element, KeyModifiers value) => element.SetValue(DragDropCopyKeyModifiersProperty, value);
    public static bool GetCanDragWithMouseRightButton(Control element) => element.GetValue(CanDragWithMouseRightButtonProperty);
    public static void SetCanDragWithMouseRightButton(Control element, bool value) => element.SetValue(CanDragWithMouseRightButtonProperty, value);
    public static bool GetDragSourceIgnore(Control element) => element.GetValue(DragSourceIgnoreProperty);
    public static void SetDragSourceIgnore(Control element, bool value) => element.SetValue(DragSourceIgnoreProperty, value);
    public static IDataTemplate? GetDragPreviewTemplate(Control element) => element.GetValue(DragPreviewTemplateProperty);
    public static void SetDragPreviewTemplate(Control element, IDataTemplate? value) => element.SetValue(DragPreviewTemplateProperty, value);
    public static bool GetUseVisualSourceItemPreview(Control element) => element.GetValue(UseVisualSourceItemPreviewProperty);
    public static void SetUseVisualSourceItemPreview(Control element, bool value) => element.SetValue(UseVisualSourceItemPreviewProperty, value);
    public static IDataTemplate? GetEffectPreviewTemplate(Control element) => element.GetValue(EffectPreviewTemplateProperty);
    public static void SetEffectPreviewTemplate(Control element, IDataTemplate? value) => element.SetValue(EffectPreviewTemplateProperty, value);
    public static string? GetDropHintText(Control element) => element.GetValue(DropHintTextProperty);
    public static void SetDropHintText(Control element, string? value) => element.SetValue(DropHintTextProperty, value);
    public static IBrush GetDropTargetAdornerBrush(Control element) => element.GetValue(DropTargetAdornerBrushProperty);
    public static void SetDropTargetAdornerBrush(Control element, IBrush value) => element.SetValue(DropTargetAdornerBrushProperty, value);
    public static bool GetShowDropTargetIndicator(Control element) => element.GetValue(ShowDropTargetIndicatorProperty);
    public static void SetShowDropTargetIndicator(Control element, bool value) => element.SetValue(ShowDropTargetIndicatorProperty, value);
    public static IDropTargetAdornerFactory? GetDropTargetAdornerFactory(Control element) => element.GetValue(DropTargetAdornerFactoryProperty);
    public static void SetDropTargetAdornerFactory(Control element, IDropTargetAdornerFactory? value) => element.SetValue(DropTargetAdornerFactoryProperty, value);
    public static IDropTargetItemsSorter? GetDropTargetItemsSorter(Control element) => element.GetValue(DropTargetItemsSorterProperty);
    public static void SetDropTargetItemsSorter(Control element, IDropTargetItemsSorter? value) => element.SetValue(DropTargetItemsSorterProperty, value);
    public static IDropIndexResolver? GetDropIndexResolver(Control element) => element.GetValue(DropIndexResolverProperty);
    public static void SetDropIndexResolver(Control element, IDropIndexResolver? value) => element.SetValue(DropIndexResolverProperty, value);
    public static IDropGroupResolver? GetDropGroupResolver(Control element) => element.GetValue(DropGroupResolverProperty);
    public static void SetDropGroupResolver(Control element, IDropGroupResolver? value) => element.SetValue(DropGroupResolverProperty, value);

    private static void OnIsDragSourceChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        control.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        control.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        control.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);

        if (args.GetNewValue<bool>())
        {
            control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            control.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
            control.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        }
    }

    private static void OnIsDropTargetChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        global::Avalonia.Input.DragDrop.SetAllowDrop(control, args.GetNewValue<bool>());
        control.RemoveHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragEnter);
        control.RemoveHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver);
        control.RemoveHandler(global::Avalonia.Input.DragDrop.DragLeaveEvent, OnDragLeave);
        control.RemoveHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop);

        if (args.GetNewValue<bool>())
        {
            control.AddHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragEnter, RoutingStrategies.Bubble, true);
            control.AddHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Bubble, true);
            control.AddHandler(global::Avalonia.Input.DragDrop.DragLeaveEvent, OnDragLeave, RoutingStrategies.Bubble, true);
            control.AddHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop, RoutingStrategies.Bubble, true);
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (sender is not Control control)
        {
            return;
        }

        var session = GetSession(control);

        var properties = args.GetCurrentPoint(control).Properties;
        if (!properties.IsLeftButtonPressed
            && (!GetCanDragWithMouseRightButton(control) || !properties.IsRightButtonPressed)
            || IsDragSourceIgnored(control, args.Source as Visual))
        {
            return;
        }

        session.ActiveDragInfo = GetDragInfoBuilder(control)?.CreateDragInfo(control, args)
                 ?? new DragInfo(control, args);
        session.TriggerEvent = args;
    }

    private static async void OnPointerMoved(object? sender, PointerEventArgs args)
    {
        if (sender is not Control control)
        {
            return;
        }

        var session = GetSession(control);
        if (session.ActiveDragInfo is null || session.TriggerEvent is null || session.DragInProgress)
        {
            return;
        }

        var properties = args.GetCurrentPoint(control).Properties;
        if (!properties.IsLeftButtonPressed
            && (!GetCanDragWithMouseRightButton(control) || !properties.IsRightButtonPressed))
        {
            session.ActiveDragInfo = null;
            session.TriggerEvent = null;
            return;
        }

        var position = args.GetPosition(control);
        var delta = position - session.ActiveDragInfo.DragStartPosition;
        if (Math.Abs(delta.X) < GetMinimumHorizontalDragDistance(control)
            && Math.Abs(delta.Y) < GetMinimumVerticalDragDistance(control))
        {
            return;
        }

        var dragInfo = session.ActiveDragInfo;
        var handler = GetDragHandler(control) ?? DefaultDragHandler;
        if (!handler.CanStartDrag(dragInfo))
        {
            return;
        }

        handler.StartDrag(dragInfo);
        if (dragInfo.Effects == DragDropEffects.None || dragInfo.Data is null && dragInfo.DataTransfer is null)
        {
            return;
        }

        var transfer = dragInfo.DataTransfer;
        if (transfer is null)
        {
            var item = new DataTransferItem();
            item.Set(GongDataFormat, dragInfo.Data);
            var defaultTransfer = new DataTransfer();
            defaultTransfer.Add(item);
            transfer = defaultTransfer;
        }

        try
        {
            session.DragInProgress = true;
            TransferSessions.Add(transfer, session);
            var result = await global::Avalonia.Input.DragDrop.DoDragDropAsync(session.TriggerEvent, transfer, dragInfo.Effects);
            if (result == DragDropEffects.None)
            {
                handler.DragCancelled();
            }

            handler.DragDropOperationFinished(result, dragInfo);
        }
        catch (Exception exception) when (handler.TryCatchOccurredException(exception))
        {
        }
        finally
        {
            TransferSessions.Remove(transfer);
            CancelAutoExpand(session);
            ClearDropAdorner(session);
            session.DragInProgress = false;
            session.ActiveDragInfo = null;
            session.TriggerEvent = null;
        }
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        if (sender is Control control && GetSession(control) is { DragInProgress: false } session)
        {
            session.ActiveDragInfo = null;
            session.TriggerEvent = null;
        }
    }

    private static void OnTopLevelPointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        OnPointerReleased(sender, args);
    }

    private static void OnDragOver(object? sender, DragEventArgs args)
    {
        if (sender is not Control control || !IsNearestDropTarget(control, args.Source as Visual))
        {
            return;
        }

        var dropInfo = CreateDropInfo(control, args);
        var session = GetSession(args.DataTransfer, control);
        (GetDropHandler(control) ?? DefaultDropHandler).DragOver(dropInfo);
        Scroll(dropInfo, args);
        UpdateAutoExpand(session, dropInfo);
        UpdateDropAdorner(session, dropInfo);
        args.DragEffects = dropInfo.Effects;
        args.Handled = true;
    }

    private static void OnDragEnter(object? sender, DragEventArgs args)
    {
        if (sender is not Control control || !IsNearestDropTarget(control, args.Source as Visual))
        {
            return;
        }

        var dropInfo = CreateDropInfo(control, args);
        var session = GetSession(args.DataTransfer, control);
        var dropHandler = GetDropHandler(control) ?? DefaultDropHandler;
        dropHandler.DragEnter(dropInfo);
        dropHandler.DragOver(dropInfo);
        UpdateAutoExpand(session, dropInfo);
        UpdateDropAdorner(session, dropInfo);
        args.DragEffects = dropInfo.Effects;
        args.Handled = true;
    }

    private static void OnDragLeave(object? sender, DragEventArgs args)
    {
        if (sender is not Control control || !IsNearestDropTarget(control, args.Source as Visual))
        {
            return;
        }

        var session = GetSession(args.DataTransfer, control);
        (GetDropHandler(control) ?? DefaultDropHandler).DragLeave(CreateDropInfo(control, args));
        CancelAutoExpand(session);
        ClearDropAdorner(session);
        args.Handled = true;
    }

    private static void OnDrop(object? sender, DragEventArgs args)
    {
        if (sender is not Control control || !IsNearestDropTarget(control, args.Source as Visual))
        {
            return;
        }

        var dropInfo = CreateDropInfo(control, args);
        var session = GetSession(args.DataTransfer, control);
        var dropHandler = GetDropHandler(control) ?? DefaultDropHandler;
        dropHandler.DragOver(dropInfo);
        dropHandler.Drop(dropInfo);
        (session.ActiveDragInfo is null ? DefaultDragHandler : GetDragHandler(session.ActiveDragInfo.VisualSource) ?? DefaultDragHandler).Dropped(dropInfo);
        args.DragEffects = dropInfo.Effects;
        args.Handled = true;
        CancelAutoExpand(session);
        ClearDropAdorner(session);
    }

    private static bool IsNearestDropTarget(Control target, Visual? eventSource)
    {
        for (var visual = eventSource; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is Control control && GetIsDropTarget(control))
            {
                return ReferenceEquals(control, target);
            }
        }

        return true;
    }

    private static IDropInfo CreateDropInfo(Control control, DragEventArgs args)
    {
        var dragInfo = GetSession(args.DataTransfer, control).ActiveDragInfo;
        return GetDropInfoBuilder(control)?.CreateDropInfo(control, args, dragInfo)
               ?? new DropInfo(control, args, dragInfo);
    }

    private static bool IsDragSourceIgnored(Control source, Visual? eventSource)
    {
        var isTabHeader = source is TabControl tabControl
                          && ItemsControlDragDropHelper.FindContainer(tabControl, eventSource) is TabItem tabItem
                          && ItemsControlDragDropHelper.IsTabHeader(eventSource, tabItem);

        for (var visual = eventSource; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is Control control && GetDragSourceIgnore(control))
            {
                return true;
            }

            if (ReferenceEquals(visual, source))
            {
                break;
            }

            if (!isTabHeader && visual is TextBox or Button or Slider or ScrollBar or ComboBox or MenuItem)
            {
                return true;
            }
        }

        return false;
    }

    private static void Scroll(IDropInfo dropInfo, DragEventArgs args)
    {
        if (dropInfo.TargetScrollViewer is not { } scrollViewer)
        {
            return;
        }

        var position = args.GetPosition(scrollViewer);
        var scrollingMode = GetScrollingMode(dropInfo.VisualTarget);
        var verticalMargin = Math.Min(32, scrollViewer.Viewport.Height / 2);
        var horizontalMargin = Math.Min(32, scrollViewer.Viewport.Width / 2);

        if (scrollingMode is ScrollingMode.VerticalOnly or ScrollingMode.Both && position.Y < verticalMargin)
        {
            scrollViewer.LineUp();
        }
        else if (scrollingMode is ScrollingMode.VerticalOnly or ScrollingMode.Both
                 && position.Y > scrollViewer.Viewport.Height - verticalMargin)
        {
            scrollViewer.LineDown();
        }

        if (scrollingMode is ScrollingMode.HorizontalOnly or ScrollingMode.Both && position.X < horizontalMargin)
        {
            scrollViewer.LineLeft();
        }
        else if (scrollingMode is ScrollingMode.HorizontalOnly or ScrollingMode.Both
                 && position.X > scrollViewer.Viewport.Width - horizontalMargin)
        {
            scrollViewer.LineRight();
        }
    }

    private static void UpdateAutoExpand(DragSession session, IDropInfo dropInfo)
    {
        if (dropInfo.Effects == DragDropEffects.None
            || !dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter)
            || dropInfo.VisualTargetItem is not TreeViewItem { IsExpanded: false, ItemCount: > 0 } target)
        {
            CancelAutoExpand(session);
            return;
        }

        if (ReferenceEquals(target, session.PendingExpandTarget))
        {
            return;
        }

        CancelAutoExpand(session);
        session.PendingExpandTarget = target;
        session.ExpandCancellation = new CancellationTokenSource();
        _ = ExpandAfterDelayAsync(session, target, session.ExpandCancellation.Token);
    }

    private static async Task ExpandAfterDelayAsync(DragSession session, TreeViewItem target, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(600, cancellationToken);
            Dispatcher.UIThread.Post(() =>
            {
                if (!cancellationToken.IsCancellationRequested && ReferenceEquals(target, session.PendingExpandTarget))
                {
                    target.IsExpanded = true;
                    CancelAutoExpand(session);
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void CancelAutoExpand(DragSession session)
    {
        session.ExpandCancellation?.Cancel();
        session.ExpandCancellation?.Dispose();
        session.ExpandCancellation = null;
        session.PendingExpandTarget = null;
    }

    private static void UpdateDropAdorner(DragSession session, IDropInfo dropInfo)
    {
        if (dropInfo.Effects == DragDropEffects.None)
        {
            ClearDropAdorner(session);
            return;
        }

        if (!ReferenceEquals(session.AdornedTarget, dropInfo.VisualTarget))
        {
            ClearDropAdorner(session);
            session.AdornedTarget = dropInfo.VisualTarget;
            session.DropTargetAdorner = GetDropTargetAdornerFactory(dropInfo.VisualTarget)?.Create(dropInfo.VisualTarget)
                                        ?? new DropTargetAdorner(dropInfo.VisualTarget);
            AdornerLayer.SetAdorner(session.AdornedTarget, session.DropTargetAdorner.Visual);
        }

        session.DropTargetAdorner?.Update(dropInfo);
    }

    private static void ClearDropAdorner(DragSession session)
    {
        if (session.AdornedTarget is not null)
        {
            AdornerLayer.SetAdorner(session.AdornedTarget, null);
        }

        session.AdornedTarget = null;
        session.DropTargetAdorner = null;
    }

    private static DragSession GetSession(Control control)
    {
        return Sessions.GetValue(TopLevel.GetTopLevel(control) ?? control, static owner =>
        {
            owner.AddHandler(InputElement.PointerReleasedEvent, OnTopLevelPointerReleased, RoutingStrategies.Bubble, true);
            return new DragSession();
        });
    }

    private static DragSession GetSession(IDataTransfer transfer, Control fallback)
    {
        return TransferSessions.TryGetValue(transfer, out var session) ? session : GetSession(fallback);
    }

    private sealed class DragSession
    {
        public IDragInfo? ActiveDragInfo { get; set; }
        public PointerPressedEventArgs? TriggerEvent { get; set; }
        public bool DragInProgress { get; set; }
        public IDropTargetAdorner? DropTargetAdorner { get; set; }
        public Control? AdornedTarget { get; set; }
        public TreeViewItem? PendingExpandTarget { get; set; }
        public CancellationTokenSource? ExpandCancellation { get; set; }
    }
}