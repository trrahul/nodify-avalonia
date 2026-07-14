using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace Nodify.Avalonia.Playground.Diagnostics
{
    /// <summary>
    /// In-memory diagnostics surfaced by the on-screen HUD when <c>NODIFY_DIAG_LOG</c> is set.
    /// <para/>
    /// This is what makes the "run app → do operation → read the panel" verification loop work:
    /// e.g. after dragging a Connector, <see cref="ConnectionCount"/> should increase while
    /// <see cref="NodeMoveCount"/> should NOT (the node must not move when dragging a connector).
    /// </summary>
    public sealed class DiagState : INotifyPropertyChanged
    {
        public static DiagState Instance { get; } = new();

        private const int MaxEvents = 16;

        public bool IsEnabled => DiagLog.IsEnabled;

        /// <summary>Most-recent-first rolling log of diagnostic events.</summary>
        public ObservableCollection<string> Events { get; } = new();

        private int _nodeMoveCount;
        public int NodeMoveCount
        {
            get => _nodeMoveCount;
            private set => Set(ref _nodeMoveCount, value);
        }

        private int _connectionCount;
        public int ConnectionCount
        {
            get => _connectionCount;
            private set => Set(ref _connectionCount, value);
        }

        private int _dragCount;
        public int DragCount
        {
            get => _dragCount;
            private set => Set(ref _dragCount, value);
        }

        private bool _dragActive;
        public bool DragActive
        {
            get => _dragActive;
            private set => Set(ref _dragActive, value);
        }

        private string _lastEvent = "(none)";
        public string LastEvent
        {
            get => _lastEvent;
            private set => Set(ref _lastEvent, value);
        }

        /// <summary>
        /// Records a diagnostic event. Cheap and always-on; marshals to the UI thread so the HUD's
        /// bound collection/properties update safely. Called from <see cref="DiagLog.Write"/> so the
        /// existing event wiring in NodifyEditorView feeds both the file log and the HUD.
        /// </summary>
        public void Record(string category, string message)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => RecordCore(category, message));
                return;
            }

            RecordCore(category, message);
        }

        private void RecordCore(string category, string message)
        {
            switch (category)
            {
                case "LocationChanged":
                    NodeMoveCount++;
                    break;
                case "Connection" when message.StartsWith("Created", StringComparison.Ordinal):
                    ConnectionCount++;
                    break;
                case "Drag" when message.StartsWith("ItemsDragStarted", StringComparison.Ordinal):
                    DragActive = true;
                    DragCount++;
                    break;
                case "Drag" when message.StartsWith("ItemsDragCompleted", StringComparison.Ordinal):
                    DragActive = false;
                    break;
            }

            string entry = $"[{DateTime.Now:HH:mm:ss.fff}] {category}: {message}";
            LastEvent = entry;

            Events.Insert(0, entry);
            while (Events.Count > MaxEvents)
            {
                Events.RemoveAt(Events.Count - 1);
            }
        }

        /// <summary>Clears the counters and event log (used to establish a clean baseline).</summary>
        public void Reset()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(Reset);
                return;
            }

            NodeMoveCount = 0;
            ConnectionCount = 0;
            DragCount = 0;
            DragActive = false;
            LastEvent = "(reset)";
            Events.Clear();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
