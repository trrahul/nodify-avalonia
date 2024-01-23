using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Shared.Controls
{
    [TemplatePart(Name = ElementTextBox, Type = typeof(TextBox))]
    public class EditableTextBlock : TemplatedControl
    {
        private const string ElementTextBox = "PART_TextBox";

        public static readonly StyledProperty<bool> IsEditingProperty = AvaloniaProperty.Register<EditableTextBlock,bool>(nameof(IsEditing), defaultBindingMode:BindingMode.TwoWay, coerce:CoerceIsEditing);
        public static readonly StyledProperty<bool> IsEditableProperty = AvaloniaProperty.Register<EditableTextBlock,bool>(nameof(IsEditable), true);
        public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<EditableTextBlock>(new StyledPropertyMetadata<string?>(defaultBindingMode:BindingMode.TwoWay));
        public static readonly StyledProperty<bool> AcceptsReturnProperty = TextBox.AcceptsReturnProperty.AddOwner<EditableTextBlock>(new StyledPropertyMetadata<bool>(false));
        public static readonly AttachedProperty<TextWrapping> TextWrappingProperty = TextBlock.TextWrappingProperty.AddOwner<EditableTextBlock>(new StyledPropertyMetadata<TextWrapping>(TextWrapping.Wrap));
        public static readonly AttachedProperty<TextTrimming> TextTrimmingProperty = TextBlock.TextTrimmingProperty.AddOwner<EditableTextBlock>(new StyledPropertyMetadata<TextTrimming>(TextTrimming.CharacterEllipsis));
        public static readonly StyledProperty<int> MinLinesProperty =
            AvaloniaProperty.Register<EditableTextBlock, int>(nameof(MinLines));
        public static readonly StyledProperty<int> MaxLinesProperty = TextBox.MaxLinesProperty.AddOwner<EditableTextBlock>();
        public static readonly StyledProperty<int> MaxLengthProperty = TextBox.MaxLengthProperty.AddOwner<EditableTextBlock>();

        private static void OnIsEditingChanged(EditableTextBlock editableTextBlock, AvaloniaPropertyChangedEventArgs<bool> avaloniaPropertyChangedEventArgs) { }

        private static bool CoerceIsEditing(AvaloniaObject avaloniaObject, bool b)
        {
            if (!((EditableTextBlock)avaloniaObject).IsEditable)
            {
                return false;
            }

            return b;
        }

        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool IsEditing
        {
            get => GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public bool IsEditable
        {
            get => GetValue(IsEditableProperty);
            set => SetValue(IsEditableProperty, value);
        }

        public bool AcceptsReturn
        {
            get => GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        public int MaxLength
        {
            get => GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public int MinLines
        {
            get => GetValue(MinLinesProperty);
            set => SetValue(MaxLinesProperty, value);
        }

        public int MaxLines
        {
            get => GetValue(MaxLinesProperty);
            set => SetValue(MaxLinesProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public TextTrimming TextTrimming
        {
            get => GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        protected TextBox? TextBox { get; private set; }

        static EditableTextBlock()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(EditableTextBlock), new FrameworkPropertyMetadata(typeof(EditableTextBlock)));
            FocusableProperty.OverrideMetadata<EditableTextBlock>(new StyledPropertyMetadata<bool>(true));
            IsEditableProperty.Changed.AddClassHandler<EditableTextBlock, bool>(OnIsEditingChanged);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            TextBox = e.NameScope.Find<TextBox>(ElementTextBox);

            if (TextBox != null)
            {
                TextBox.LostFocus += OnLostFocus;
                //TextBox.LostKeyboardFocus += OnLostFocus;
                TextBox.GetBindingObservable(IsVisibleProperty).Subscribe(new AnonymousObserver<BindingValue<bool>>(OnTextBoxVisiblityChanged));

                if (IsEditing)
                {
                    TextBox.Focus();
                    TextBox.SelectAll();
                }
            }
        }

        private void OnTextBoxVisiblityChanged(BindingValue<bool> bindingValue)
        {
            if (IsEditing && TextBox != null)
            {
                TextBox.Focus();
                if(TextBox.IsFocused) //todo maybe wrong
                {
                    TextBox.SelectAll();
                }
                else
                {
                    IsEditing = false;
                }
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (IsEditing)
            {
                e.Handled = true;
            }
            else if (IsEditable && e.GetChangedButton() == MouseButton.Left && e.ClickCount == 2)
            {
                IsEditing = true;
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (IsEditing)
            {
                e.Handled = true;
            }
        }

        private void OnLostFocus(object? sender, RoutedEventArgs e)
        {
            IsEditing = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (IsEditing && e.Key == Key.Escape || !AcceptsReturn && e.Key == Key.Enter)
            {
                IsEditing = false;
            }

            if(e.Key == Key.Enter && IsFocused && !IsEditing)
            {
                IsEditing = true;
            }
        }
    }
}
