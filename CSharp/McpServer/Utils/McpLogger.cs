using System;
using System.Diagnostics;

namespace McpServerLib.Utils
{
    public static class McpLogger
    {
        [Conditional("DEBUG")]
        public static void Debug(string message)
        {
            Console.Error.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} {message}");
        }

        [Conditional("DEBUG")]
        public static void Debug(string message, params object[] args)
        {
            Console.Error.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} {string.Format(message, args)}");
        }

        [Conditional("DEBUG")]
        public static void Error(string message)
        {
            Console.Error.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} {message}");
        }

        [Conditional("DEBUG")]
        public static void Error(string message, Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} {message}");
            Console.Error.WriteLine($"[ERROR] Exception: {ex}");
        }

        [Conditional("DEBUG")]
        public static void Info(string message)
        {
            Console.Error.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss.fff} {message}");
        }

        [Conditional("DEBUG")]
        public static void Warning(string message)
        {
            Console.Error.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss.fff} {message}");
        }
    }
}