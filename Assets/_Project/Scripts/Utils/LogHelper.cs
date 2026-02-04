// LogHelper is now provided by Z13.Core package.
// This file provides a namespace alias for backward compatibility.

namespace NeuralBreak.Utils
{
    /// <summary>
    /// LogHelper alias for backward compatibility.
    /// The actual implementation is in Z13.Core.LogHelper.
    /// </summary>
    public static class LogHelper
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(string message)
            => Z13.Core.LogHelper.Log(message);

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(string message, UnityEngine.Object context)
            => Z13.Core.LogHelper.Log(message, context);

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(string message)
            => Z13.Core.LogHelper.LogWarning(message);

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(string message, UnityEngine.Object context)
            => Z13.Core.LogHelper.LogWarning(message, context);

        public static void LogError(string message)
            => Z13.Core.LogHelper.LogError(message);

        public static void LogError(string message, UnityEngine.Object context)
            => Z13.Core.LogHelper.LogError(message, context);
    }
}
