using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Xaml.Interactivity;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Nodify.Avalonia.StateMachine.Behaviours")]

namespace Nodify.Avalonia.StateMachine.Behaviours;

public class DataTrigger : Trigger
{
    private readonly List<IDisposable> activeBindings = new();
    private IDisposable? boundSubscription;

    public static readonly StyledProperty<string> PropertyProperty =
        AvaloniaProperty.Register<DataTrigger, string>(nameof(Property));
    
    public static readonly StyledProperty<object?> BoundProperty =
        AvaloniaProperty.Register<DataTrigger, object?>(nameof(Bound));

    public static readonly StyledProperty<ComparisonConditionType> ComparisonConditionProperty =
        AvaloniaProperty.Register<DataTrigger, ComparisonConditionType>(nameof(ComparisonCondition));

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<DataTrigger, object?>(nameof(Value));
    
    public string Property
    {
        get => GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }
    
    public object? Bound
    {
        get => GetValue(BoundProperty);
        set => SetValue(BoundProperty, value);
    }

    public ComparisonConditionType ComparisonCondition
    {
        get => GetValue(ComparisonConditionProperty);
        set => SetValue(ComparisonConditionProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool UseDataContext { get; set; } = true;
    
    public object? Source { get; set; }

    protected override void OnAttached()
    {
        base.OnAttached();
        boundSubscription = this.Bind(BoundProperty, new Binding(Source == null && UseDataContext ? (Property == "." ? "DataContext" : $"DataContext.{Property}") : Property) { Source = Source ?? AssociatedObject });
        // Re-evaluate explicitly: on reattach the bound value may be unchanged, so the
        // BoundProperty change observer alone would leave the trigger inactive.
        Evaluate(null);
    }

    protected override void OnDetaching()
    {
        ClearActiveBindings();
        boundSubscription?.Dispose();
        boundSubscription = null;
        base.OnDetaching();
    }

    static DataTrigger()
    {
        BoundProperty.Changed.Subscribe(
            new AnonymousObserver<AvaloniaPropertyChangedEventArgs<object?>>(OnValueChanged));

        ComparisonConditionProperty.Changed.Subscribe(
            (IObserver<AvaloniaPropertyChangedEventArgs<ComparisonConditionType>>)new AnonymousObserver<AvaloniaPropertyChangedEventArgs<ComparisonConditionType>>(OnValueChanged));

        ValueProperty.Changed.Subscribe(
            new AnonymousObserver<AvaloniaPropertyChangedEventArgs<object?>>(OnValueChanged));
    }

    [RequiresUnreferencedCode("This functionality is not compatible with trimming.")]
    private static bool Compare(object? leftOperand, ComparisonConditionType operatorType, object? rightOperand)
    {
        if (leftOperand is not null && rightOperand is not null &&
            leftOperand.GetType() != rightOperand.GetType())
        {
            var value = rightOperand.ToString();
            var destinationType = leftOperand.GetType();
            if (value is not null)
            {
                rightOperand = TypeConverterHelper.Convert(value, destinationType);
            }
        }

        var leftComparableOperand = leftOperand as IComparable;
        var rightComparableOperand = rightOperand as IComparable;
        if (leftComparableOperand is not null && rightComparableOperand is not null)
        {
            return EvaluateComparable(leftComparableOperand, operatorType, rightComparableOperand);
        }

        switch (operatorType)
        {
            case ComparisonConditionType.Equal:
                return Equals(leftOperand, rightOperand);

            case ComparisonConditionType.NotEqual:
                return !Equals(leftOperand, rightOperand);

            case ComparisonConditionType.LessThan:
            case ComparisonConditionType.LessThanOrEqual:
            case ComparisonConditionType.GreaterThan:
            case ComparisonConditionType.GreaterThanOrEqual:
            {
                throw leftComparableOperand switch
                {
                    null when rightComparableOperand is null => new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture,
                        "Binding property of type {0} and Value property of type {1} cannot be used with operator {2}.",
                        leftOperand?.GetType().Name ?? "null", rightOperand?.GetType().Name ?? "null",
                        operatorType.ToString())),
                    null => new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                        "Binding property of type {0} cannot be used with operator {1}.",
                        leftOperand?.GetType().Name ?? "null", operatorType.ToString())),
                    _ => new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                        "Value property of type {0} cannot be used with operator {1}.",
                        rightOperand?.GetType().Name ?? "null", operatorType.ToString()))
                };
            }
        }

        return false;
    }

    [RequiresUnreferencedCode("This functionality is not compatible with trimming.")]
    private static bool EvaluateComparable(IComparable leftOperand, ComparisonConditionType operatorType, IComparable rightOperand)
    {
        object? convertedOperand = null;
        try
        {
            convertedOperand = Convert.ChangeType(rightOperand, leftOperand.GetType(), CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
        }
        catch (InvalidCastException)
        {
        }

        if (convertedOperand is null)
        {
            return operatorType == ComparisonConditionType.NotEqual;
        }

        var comparison = leftOperand.CompareTo((IComparable)convertedOperand);
        return operatorType switch
        {
            ComparisonConditionType.Equal => comparison == 0,
            ComparisonConditionType.NotEqual => comparison != 0,
            ComparisonConditionType.LessThan => comparison < 0,
            ComparisonConditionType.LessThanOrEqual => comparison <= 0,
            ComparisonConditionType.GreaterThan => comparison > 0,
            ComparisonConditionType.GreaterThanOrEqual => comparison >= 0,
            _ => false
        };
    }

    private static void OnValueChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Sender is DataTrigger behavior)
        {
            behavior.Evaluate(args);
        }
    }

    private void Evaluate(AvaloniaPropertyChangedEventArgs? args)
    {
        // Deactivate first so an active trigger whose bound value goes null (data-context
        // replacement, template recycling) reverts its setters instead of leaving them applied.
        ClearActiveBindings();

        if (AssociatedObject is null)
        {
            return;
        }

        // Bindings are null when first initialized, so this null check is required.
        if (Bound is null)
        {
            return;
        }

        if (Compare(Bound, ComparisonCondition, Value))
        {
            foreach (var result in Interaction.ExecuteActions(AssociatedObject, Actions, args))
            {
                if (result is IDisposable disposable)
                    activeBindings.Add(disposable);
            }
        }
    }

    private void ClearActiveBindings()
    {
        foreach (var b in activeBindings)
            b.Dispose();
        activeBindings.Clear();
    }
}
