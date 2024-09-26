using Extensions;
using System.Diagnostics;

namespace XREngine
{
    public class Debug
    {
        public static Queue<(string, DateTime)> Output { get; } = new Queue<(string, DateTime)>();
        public static bool AllowOutput { get; set; } = true;

        /// <summary>
        /// Prints a message for debugging purposes.
        /// </summary>
        public static void Out(string message, params object[] args)
            => Out(EOutputVerbosity.Verbose, message, args);
        /// <summary>
        /// Prints a message for debugging purposes.
        /// </summary>
        public static void Out(EOutputVerbosity verbosity, string message, params object[] args)
            => Out(verbosity, true, message, args);
        /// <summary>
        /// Prints a message for debugging purposes.
        /// </summary>
        public static void Out(EOutputVerbosity verbosity, bool debugOnly, string message, params object[] args)
            => Out(verbosity, debugOnly, false, false, false, 0, 0, message, args);
        /// <summary>
        /// Prints a message for debugging purposes.
        /// </summary>
        public static void Out(
            EOutputVerbosity verbosity,
            bool debugOnly,
            bool printDate,
            bool printAppDomain,
            bool printStackTrace,
            int stackTraceIgnoredLineCount,
            int stackTraceIncludedLineCount,
            string message,
            params object[] args)
        {
#if DEBUG || EDITOR

            if (!AllowOutput)
            {
                Suppressed(message);
                return;
            }

            GameStartupSettings settings = Engine.GameSettings;

            if (verbosity > settings.OutputVerbosity)
            {
                Suppressed(message);
                return;
            }

            if (args.Length > 0)
                message = string.Format(message, args);

            if (printStackTrace)
                message += Environment.NewLine + GetStackTrace(stackTraceIgnoredLineCount, stackTraceIncludedLineCount);

            DateTime now = DateTime.Now;

            //double recentness = Settings.AllowedOutputRecencySeconds;
            //if (recentness > 0.0)
            //{
            //    List<string> removeKeys = [];
            //    RecentMessageCache.ForEach(x =>
            //    {
            //        TimeSpan span = now - x.Value;
            //        if (span.TotalSeconds >= recentness)
            //            removeKeys.Add(x.Key);
            //    });
            //    removeKeys.ForEach(x => RecentMessageCache.TryRemove(x, out _));

            //    if (RecentMessageCache.ContainsKey(message))
            //    {
            //        //Messages already cleaned above, just return here

            //        //TimeSpan span = now - RecentMessages[message];
            //        //if (span.TotalSeconds <= AllowedOutputRecentness)
            //        return;
            //    }
            //    else
            //        RecentMessageCache.TryAdd(message, now);
            //}

            bool printDomain = printAppDomain/* || Settings.PrintAppDomainInOutput*/;

            if (printDate && printDomain)
                message = $"[{AppDomain.CurrentDomain.FriendlyName} {now}] " + message;
            else if (printDomain)
                message = $"[{AppDomain.CurrentDomain.FriendlyName}] " + message;
            else if (printDate)
                message = $"[{now}] " + message;

            //if (debugOnly)
            //    XDebug.Print(message);
            //else
                Trace.WriteLine(message);
#endif
        }

        private static void Suppressed(string message)
            => Console.WriteLine($"[Suppressed] {message}");

        public static void LogException(Exception ex, string? message = null)
        {
#if DEBUG || EDITOR
            if (message != null)
                Out(EOutputVerbosity.Minimal, false, $"{message}{Environment.NewLine}{ex}");
            else
                Out(EOutputVerbosity.Minimal, false, ex.ToString());
#endif
        }
        public static void LogWarning(string message, int lineIgnoreCount = 0, int includedLineCount = 5)
        {
#if DEBUG || EDITOR
            Out(EOutputVerbosity.Normal, true, false, false, true, 4 + lineIgnoreCount, includedLineCount, message);
#endif
        }
        public static string GetStackTrace(int lineIgnoreCount = 3, int includedLineCount = -1, bool ignoreBeforeWndProc = true)
        {
            //Format and print stack trace
            string stackTrace = Environment.StackTrace;
            string atStr = "   at ";

            int at4th = stackTrace.FindOccurrence(0, lineIgnoreCount, atStr);
            if (at4th > 0)
                stackTrace = stackTrace[at4th..];

            if (ignoreBeforeWndProc)
            {
                //Everything before wndProc is almost always irrelevant
                int wndProc = stackTrace.IndexOf("WndProc(Message& m)");
                if (wndProc > 0)
                {
                    int at = stackTrace.FindFirstReverse(wndProc, atStr);
                    if (at > 0)
                        stackTrace = stackTrace[..at];
                }
            }

            if (includedLineCount >= 0)
            {
                int atXth = stackTrace.FindOccurrence(0, includedLineCount, atStr);
                if (atXth > 0)
                    stackTrace = stackTrace[..atXth];
            }

            return stackTrace;
        }
    }
}
