using System.Collections.Generic;
using System.Linq;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Shared;
using ReactiveUI;
using StringMath;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class ExpressionOperationViewModel : OperationViewModel
    {
        private MathExpr? _expr;
        private string? _expression;
        public string? Expression
        {
            get => _expression;
            set => this.IsRaiseAndSetIfChanged(ref _expression, value)
                .Then(GenerateInput);
        }

        private void GenerateInput()
        {
            try
            {
                _expr = Expression!.ToMathExpr();
                ConnectorViewModel[]? toRemove = Input.Where(i => !_expr.LocalVariables.Contains(i.Title)).ToArray();
                toRemove.ForEach(i => Input.Remove(i));
                HashSet<string> existingVars = Input.Select(s => s.Title).Where(s => s != null).ToHashSet()!;

                foreach (string variable in _expr.LocalVariables.Except(existingVars))
                {
                    Input.Add(new ConnectorViewModel
                    {
                        Title = variable
                    });
                }

                OnInputValueChanged();
            }
            catch
            {

            }
        }

        protected override void OnInputValueChanged()
        {
            if (Output != null && _expr != null)
            {
                try
                {
                    Input.ForEach(i => _expr.Substitute(i.Title!, i.Value));
                    Output.Value = _expr.Result;
                }
                catch
                {

                }
            }
        }
    }
}
