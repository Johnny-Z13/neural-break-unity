using System.Diagnostics;
using UnityEngine;

namespace NeuralBreak.Utils
{
    /// <summary>
    /// Performance-optimized logging helper that strips debug logs from production builds.
    /// Uses [Conditional] attribute for zero runtime overhead when not in editor.
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Logs an informational message. Only included in editor builds.
        /// Zero overhead in production builds due to [Conditional] attribute.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// Logs an informational message with context. Only included in editor builds.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Log(string message, Object context)
        {
            Debug.Log(message, context);
        }

        /// <summary>
        /// Logs a warning message. Only included in editor builds.
        /// Zero overhead in production builds due to [Conditional] attribute.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        /// <summary>
        /// Logs a warning message with context. Only included in editor builds.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(string message, Object context)
        {
            Debug.LogWarning(message, context);
        }

        /// <summary>
        /// Logs an error message. Always included in all builds.
        /// Errors are important for production debugging and crash reports.
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }

        /// <summary>
        /// Logs an error message with context. Always included in all builds.
        /// </summary>
        public static void LogError(string message, Object context)
        {
            Debug.LogError(message, context);
        }
    }
}
