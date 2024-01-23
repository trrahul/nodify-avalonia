namespace Nodify.Avalonia.Calculator.Operations
{
    public interface IOperation
    {
        double Execute(params double[] operands);
    }
}
