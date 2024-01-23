using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Threading;

namespace Nodify.Avalonia.Extensions;

public class CanvasAttached 
{
    public static readonly AttachedProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.RegisterAttached<CanvasAttached, Canvas, IEnumerable?>("Items");

    public static IEnumerable? GetItems(Canvas canvas) => canvas.GetValue(ItemsProperty);
    public static void SetItems(Canvas canvas,IEnumerable? value) => canvas.SetValue(ItemsProperty,value);

    public static readonly AttachedProperty<DataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.RegisterAttached<CanvasAttached, Canvas, DataTemplate?>("ItemTemplate");

    private readonly Canvas _canvas;

    public static DataTemplate? GetItemTemplate(Canvas canvas) => canvas.GetValue(ItemTemplateProperty);
    public static void SetItemTemplate(Canvas canvas,DataTemplate? value) => canvas.SetValue(ItemTemplateProperty,value);
    static CanvasAttached()
    {
        ItemsProperty.Changed.AddClassHandler<Canvas,IEnumerable?>(OnChildrenBindingPropertyChanged);
    }

    public CanvasAttached(Canvas canvas,INotifyCollectionChanged items)
    {
        _canvas = canvas;
        items.CollectionChanged += ItemsOnCollectionChanged;
    }

    private void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var template = _canvas.GetValue(ItemTemplateProperty);
            template ??= new DataTemplate();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        var control = template.Build(item);
                        control.DataContext = item;
                        _canvas.Children.Add(control);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    _canvas.Children.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });
    }

    private static void OnChildrenBindingPropertyChanged(Canvas canvas, AvaloniaPropertyChangedEventArgs<IEnumerable?> args)
    {
        canvas.Children.Clear();
        if (args.NewValue.Value == null)
        {

        }
        else
        {
            if (args.NewValue.Value is INotifyCollectionChanged items)
            {
                new CanvasAttached(canvas,items);
            }

            var template = canvas.GetValue(ItemTemplateProperty);
            template ??= new DataTemplate();
            foreach (var item in args.NewValue.Value)
            {
                var control = template.Build(item);
                control.DataContext = item;
                canvas.Children.Add(control);
            }
        }
    }

}