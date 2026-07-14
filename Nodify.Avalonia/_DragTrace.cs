using System;

namespace Nodify.Avalonia
{
    // Diagnostic drag tracing, gated by the NODIFY_DRAG_TRACE=1 environment variable.
    internal static class DragTrace
    {
        public static readonly bool Enabled =
            Environment.GetEnvironmentVariable("NODIFY_DRAG_TRACE") == "1";

        public static void Log(string msg)
        {
            if (Enabled)
            {
                Console.Out.WriteLine($"[DRAGTRACE] {msg}");
                Console.Out.Flush();
            }
        }
    }
}
