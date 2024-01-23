using System;

namespace Nodify.Avalonia.Calculator.Operations
{
    public class ValueOperation : IOperation
    {
        private readonly Func<double> _func;

        public ValueOperation(Func<double> func) => _func = func;

        public double Execute(params double[] operands)
            => _func();
    }
}
