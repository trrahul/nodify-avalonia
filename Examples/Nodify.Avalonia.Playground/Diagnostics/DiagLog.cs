using System;
using System.IO;

namespace Nodify.Avalonia.Playground.Diagnostics
{
    /// <summary>
    /// Opt-in, env-gated file logger for verifying runtime behavior (node movement, drag lifecycle,
    /// pending/created connections). Set <c>NODIFY_DIAG_LOG</c> to a file path before launching the
    /// Playground to enable it; when unset, every method is a no-op.
    /// </summary>
    public static class DiagLog
    {
        private static readonly object _lock = new();
        private static readonly string? _path;

        /// <summary>True when NODIFY_DIAG_LOG was set at process start and logging is active.</summary>
        public static bool IsEnabled => _path != null;

        static DiagLog()
        {
            string? path = Environment.GetEnvironmentVariable("NODIFY_DIAG_LOG");

            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    _path = path;
                }
                catch
                {
                    // If the path can't be prepared, stay disabled rather than throwing from a
                    // static constructor and taking down the app.
                    _path = null;
                }
            }
        }

        /// <summary>
        /// Records a diagnostic event when <c>NODIFY_DIAG_LOG</c> is set.
        /// </summary>
        public static void Write(string category, string message)
        {
            if (_path == null)
            {
                return;
            }

            DiagState.Instance.Record(category, message);

            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}";

            lock (_lock)
            {
                try
                {
                    using var writer = new StreamWriter(_path, append: true);
                    writer.WriteLine(line);
                    writer.Flush();
                }
                catch
                {
                    // Best-effort diagnostics only; never let logging failures affect the app.
                }
            }
        }
    }
}
