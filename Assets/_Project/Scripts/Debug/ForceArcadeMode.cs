using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Testing
{
    /// <summary>
    /// EMERGENCY FIX: Forces the game to start in Arcade mode and logs everything
    /// Add this to your scene to override any bad settings
    /// </summary>
    public class ForceArcadeMode : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("===========================================");
            Debug.Log("[ForceArcadeMode] FORCING ARCADE MODE");
            Debug.Log("===========================================");

            if (GameManager.Instance != null)
            {
                var gmType = typeof(GameManager);
                var currentModeField = gmType.GetField("m_currentMode",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (currentModeField != null)
                {
                    var currentMode = (GameMode)currentModeField.GetValue(GameManager.Instance);
                    Debug.Log($"[ForceArcadeMode] GameManager._currentMode WAS: {currentMode}");

                    currentModeField.SetValue(GameManager.Instance, GameMode.Arcade);
                    Debug.Log($"[ForceArcadeMode] GameManager._currentMode NOW: Arcade");
                }

                var autoStartField = gmType.GetField("m_autoStartOnPlay",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (autoStartField != null)
                {
                    bool autoStart = (bool)autoStartField.GetValue(GameManager.Instance);
                    Debug.Log($"[ForceArcadeMode] GameManager._autoStartOnPlay = {autoStart}");

                    if (autoStart)
                    {
                        autoStartField.SetValue(GameManager.Instance, false);
                        Debug.Log($"[ForceArcadeMode] DISABLED auto-start");
                    }
                }
            }

            var gameSetup = FindFirstObjectByType<GameSetup>();
            if (gameSetup != null)
            {
                var gsType = typeof(GameSetup);
                var autoStartField = gsType.GetField("m_autoStartGame",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (autoStartField != null)
                {
                    bool autoStart = (bool)autoStartField.GetValue(gameSetup);
                    Debug.Log($"[ForceArcadeMode] GameSetup._autoStartGame = {autoStart}");

                    if (autoStart)
                    {
                        autoStartField.SetValue(gameSetup, false);
                        Debug.Log($"[ForceArcadeMode] DISABLED GameSetup auto-start");
                    }
                }
            }

            var debugTest = FindFirstObjectByType<DebugGameTest>();
            if (debugTest != null)
            {
                var dtType = typeof(DebugGameTest);
                var testModeField = dtType.GetField("m_testModeEnabled",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (testModeField != null)
                {
                    bool testMode = (bool)testModeField.GetValue(debugTest);
                    Debug.Log($"[ForceArcadeMode] DebugGameTest.m_testModeEnabled = {testMode}");

                    if (testMode)
                    {
                        testModeField.SetValue(debugTest, false);
                        Debug.LogWarning($"[ForceArcadeMode] DISABLED DebugGameTest!");
                    }
                }
            }

            Debug.Log("===========================================");
            Debug.Log("[ForceArcadeMode] Game is ready for ARCADE");
            Debug.Log("===========================================");
        }
    }
}
