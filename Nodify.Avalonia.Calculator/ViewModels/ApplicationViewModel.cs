using System;
using System.Linq;
using System.Windows.Input;
using DynamicData;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class ApplicationViewModel : ViewModelBase
    {
        public NodifyObservableCollection<EditorViewModel> Editors { get; } = new NodifyObservableCollection<EditorViewModel>();

        public ApplicationViewModel()
        {
            AddEditorCommand = ReactiveCommand.Create(() => Editors.Add(new EditorViewModel
            {
                Name = $"Editor {Editors.Count + 1}"
            }));
            CloseEditorCommand = ReactiveCommand.Create<Guid>(
                id => Editors.RemoveOne(editor => editor.Id == id),this.WhenAnyValue(v => v.Editors,v => v.SelectedEditor,(e,se)=>e.Count > 0 && se != null));
            Editors.WhenAdded((editor) =>
            {
                if (AutoSelectNewEditor || Editors.Count == 1)
                {
                    SelectedEditor = editor;
                }
                editor.OnOpenInnerCalculator += OnOpenInnerCalculator;
            })
            .WhenRemoved((editor) =>
            {
                editor.OnOpenInnerCalculator -= OnOpenInnerCalculator;
                var childEditors = Editors.Where(ed => ed.Parent == editor).ToList();
                childEditors.ForEach(ed => Editors.Remove(ed));
            });
            Editors.Add(new EditorViewModel
            {
                Name = $"Editor {Editors.Count + 1}"
            });
        }

        private void OnOpenInnerCalculator(EditorViewModel parentEditor, CalculatorViewModel calculator)
        {
            var editor = Editors.FirstOrDefault(e => e.Calculator == calculator);
            if (editor != null)
            {
                SelectedEditor = editor;
            }
            else
            {
                var childEditor = new EditorViewModel
                {
                    Parent = parentEditor,
                    Calculator = calculator,
                    Name = $"[Inner] Editor {Editors.Count + 1}"
                };
                Editors.Add(childEditor);
            }
        }

        public ICommand AddEditorCommand { get; }
        public ICommand CloseEditorCommand { get; }

        private EditorViewModel? _selectedEditor;
        public EditorViewModel? SelectedEditor
        {
            get => _selectedEditor;
            set => this.RaiseAndSetIfChanged(ref _selectedEditor, value);
        }

        private bool _autoSelectNewEditor = true;
        public bool AutoSelectNewEditor
        {
            get => _autoSelectNewEditor;
            set => this.RaiseAndSetIfChanged(ref _autoSelectNewEditor , value); 
        }
    }
}
