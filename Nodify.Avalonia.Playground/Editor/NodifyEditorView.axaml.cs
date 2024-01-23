using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace Nodify.Avalonia.Playground.Editor
{
    public partial class NodifyEditorView : UserControl
    {
        public NodifyEditor EditorInstance => Editor;
        public NodifyEditorView()
        {
            InitializeComponent();
        }
    }
}
