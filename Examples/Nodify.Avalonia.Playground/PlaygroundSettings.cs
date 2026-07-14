using System.Collections.Generic;
using System.Linq;
using Nodify.Avalonia;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Playground
{
    public class PlaygroundSettings : ReactiveObject
    {
        private readonly IReadOnlyList<ISettingViewModel> _settings;

        private PlaygroundSettings()
        {
            _settings = new List<ISettingViewModel>
            {
                new ProxySettingViewModel<EditorGesturesMappings>(() => EditorGesturesMappings, v => EditorGesturesMappings = v,
                    "Editor input mappings"),
                new ProxySettingViewModel<EditorInputMode>(() => EditorInputMode, v => EditorInputMode = v,
                    "Editor input mode"),
                new ProxySettingViewModel<bool>(() => ShowMinimap, v => ShowMinimap = v,
                    "Show minimap", "Set Enable nodes dragging optimization to false for realtime updates."),
                new ProxySettingViewModel<bool>(() => DisableMinimapControls, v => DisableMinimapControls = v,
                    "Disable minimap controls", "Whether the minimap can move and zoom the viewport."),
                new ProxySettingViewModel<bool>(() => ResizeToViewport, v => ResizeToViewport = v,
                    "Minimap resize to viewport", "Whether the minimap should resize to also display the viewport."),
                new ProxySettingViewModel<PointEditor>(() => MinimapMaxViewportOffset, v => MinimapMaxViewportOffset = v,
                    "Minimap max viewport offset", "The max position from the items extent that the viewport can move to."),
                new ProxySettingViewModel<bool>(() => ShowGridLines, v => ShowGridLines = v,
                    "Show grid lines", "Whether the editor background grid is visible."),
                new ProxySettingViewModel<bool>(() => ShouldConnectNodes, v => ShouldConnectNodes = v,
                    "Connect nodes", "Automatically connect nodes when generating the graph."),
                new ProxySettingViewModel<bool>(() => AsyncLoading, v => AsyncLoading = v,
                    "Async loading", "Load nodes and connections on a background thread."),
                new ProxySettingViewModel<uint>(() => MinNodes, v => MinNodes = v,
                    "Min nodes", "Minimum number of nodes to generate."),
                new ProxySettingViewModel<uint>(() => MaxNodes, v => MaxNodes = v,
                    "Max nodes", "Maximum number of nodes to generate."),
                new ProxySettingViewModel<uint>(() => MinConnectors, v => MinConnectors = v,
                    "Min connectors", "Minimum number of input/output connectors per node."),
                new ProxySettingViewModel<uint>(() => MaxConnectors, v => MaxConnectors = v,
                    "Max connectors", "Maximum number of input/output connectors per node."),
                new ProxySettingViewModel<uint>(() => PerformanceTestNodes, v => PerformanceTestNodes = v,
                    "Performance test nodes", "Number of nodes to generate in the performance test."),
                new ProxySettingViewModel<bool>(() => UseCustomConnectors, v => UseCustomConnectors = v,
                    "Use custom shapes for connectors", "Use custom connector shapes instead of the default."),
            };
        }

        public static PlaygroundSettings Instance { get; } = new PlaygroundSettings();

        /// <summary>
        /// Data-driven list of settings exposed to SettingsView, filtered/sorted by <see cref="SearchText"/>.
        /// </summary>
        public IEnumerable<ISettingViewModel> Settings => FilterAndSort(_settings);

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _searchText, value))
                {
                    this.RaisePropertyChanged(nameof(Settings));
                }
            }
        }

        private EditorGesturesMappings _editorGesturesMappings;
        public EditorGesturesMappings EditorGesturesMappings
        {
            get => _editorGesturesMappings;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _editorGesturesMappings, value))
                {
                    EditorGestures.Mappings.Apply(value);
                }
            }
        }

        private EditorInputMode _editorInputMode;
        public EditorInputMode EditorInputMode
        {
            get => _editorInputMode;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _editorInputMode, value))
                {
                    EditorGestures.Mappings.Apply(value);
                }
            }
        }

        /// <summary>
        /// Filters <paramref name="settings"/> by case-insensitive substring match on Name or Description,
        /// then sorts so Name-matches come before Description-only matches.
        /// </summary>
        public IEnumerable<ISettingViewModel> FilterAndSort(IEnumerable<ISettingViewModel> settings)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return settings;
            }

            string searchText = SearchText!.ToLowerInvariant();

            var matchingValues = settings.Where(s =>
                s.Name.ToLowerInvariant().Contains(searchText) ||
                (s.Description?.ToLowerInvariant()?.Contains(searchText) ?? false));
            var sortedValues = matchingValues.OrderByDescending(s =>
                s.Name.ToLowerInvariant().Contains(searchText));

            return sortedValues;
        }

        private bool _showMinimap = true;
        public bool ShowMinimap
        {
            get => _showMinimap;
            set => this.RaiseAndSetIfChanged(ref _showMinimap, value);
        }

        private bool _disableMinimapControls = false;
        public bool DisableMinimapControls
        {
            get => _disableMinimapControls;
            set => this.RaiseAndSetIfChanged(ref _disableMinimapControls, value);
        }

        private bool _resizeToViewport = false;
        public bool ResizeToViewport
        {
            get => _resizeToViewport;
            set => this.RaiseAndSetIfChanged(ref _resizeToViewport, value);
        }

        private PointEditor _minimapMaxViewportOffset = new PointEditor { X = 2000, Y = 2000 };
        public PointEditor MinimapMaxViewportOffset
        {
            get => _minimapMaxViewportOffset;
            set => this.RaiseAndSetIfChanged(ref _minimapMaxViewportOffset, value);
        }

        private bool _shouldConnectNodes = true;
        public bool ShouldConnectNodes
        {
            get => _shouldConnectNodes;
            set => this.RaiseAndSetIfChanged(ref _shouldConnectNodes, value);
        }

        private bool _asyncLoading = true;
        public bool AsyncLoading
        {
            get => _asyncLoading;
            set => this.RaiseAndSetIfChanged(ref _asyncLoading, value);
        }

        private uint _minNodes = 10;
        public uint MinNodes
        {
            get => _minNodes;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _minNodes, value))
                {
                    MaxNodes = MaxNodes < MinNodes ? MinNodes : MaxNodes;
                }
            }
        }

        private uint _maxNodes = 100;
        public uint MaxNodes
        {
            get => _maxNodes;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _maxNodes, value))
                {
                    MaxNodes = MaxNodes < MinNodes ? MinNodes : MaxNodes;
                }
            }
        }

        private uint _minConnectors = 0;
        public uint MinConnectors
        {
            get => _minConnectors;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _minConnectors, value))
                {
                    MaxConnectors = MaxConnectors < MinConnectors ? MinConnectors : MaxConnectors;
                }
            }
        }

        private uint _maxConnectors = 4;
        public uint MaxConnectors
        {
            get => _maxConnectors;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _maxConnectors, value))
                {
                    MaxConnectors = MaxConnectors < MinConnectors ? MinConnectors : MaxConnectors;
                }
            }
        }

        private uint _performanceTestNodes = 1000;
        public uint PerformanceTestNodes
        {
            get => _performanceTestNodes;
            set => this.RaiseAndSetIfChanged(ref _performanceTestNodes, value);
        }

        private bool _showGridLines = true;
        public bool ShowGridLines
        {
            get => _showGridLines;
            set => this.RaiseAndSetIfChanged(ref _showGridLines, value);
        }

        private bool _customConnectors = true;
        public bool UseCustomConnectors
        {
            get => _customConnectors;
            set => this.RaiseAndSetIfChanged(ref _customConnectors, value);
        }
    }
}
