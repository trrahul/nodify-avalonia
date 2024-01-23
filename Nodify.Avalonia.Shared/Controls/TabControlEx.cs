using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Nodify.Avalonia.Shared.Controls
{
    [TemplatePart(Name = ElementScrollViewer, Type = typeof(ScrollViewer))]
    public class TabControlEx : TabControl
    {
        private const string ElementScrollViewer = "PART_ScrollViewer";

        public static readonly StyledProperty<ICommand?> AddTabCommandProperty = AvaloniaProperty.Register<TabControlEx,ICommand?>(nameof(AddTabCommand));
        public static readonly StyledProperty<bool> AutoScrollToEndProperty = AvaloniaProperty.Register<TabControlEx,bool>(nameof(AutoScrollToEnd));

        public ICommand? AddTabCommand
        {
            get => GetValue(AddTabCommandProperty);
            set => SetValue(AddTabCommandProperty, value);
        }
        public bool AutoScrollToEnd
        {
            get => GetValue(AutoScrollToEndProperty);
            set => SetValue(AutoScrollToEndProperty, value);
        }

        protected ScrollViewer? ScrollViewer { get; private set; }

        static TabControlEx()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(TabControlEx), new FrameworkPropertyMetadata(typeof(TabControlEx)));
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            ScrollViewer = e.NameScope.Find<ScrollViewer>(ElementScrollViewer);
            if(ScrollViewer != null)
            {
                ScrollViewer.ScrollChanged += OnScrollChanged;
            }
        }

        private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentDelta.Length > 0 && e.ViewportDelta.Length < e.ExtentDelta.Length && AutoScrollToEnd)
            {
                ScrollViewer.Offset = new Vector(double.PositiveInfinity, double.PositiveInfinity); //todo double check
            }
        }

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new TabItemEx();
        }
    }
}
