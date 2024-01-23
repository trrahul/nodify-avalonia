using Nodify.Avalonia.Extensions;
using ReactiveUI;

namespace Nodify.Avalonia.Playground
{
    public class PlaygroundSettings : ReactiveObject
    {
        private PlaygroundSettings() { }

        public static PlaygroundSettings Instance { get; } = new PlaygroundSettings();

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
