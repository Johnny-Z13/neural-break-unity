using UnityEngine;
using NeuralBreak.Config;

namespace NeuralBreak.Debug
{
    /// <summary>
    /// Debug UI to toggle control schemes at runtime for testing.
    /// Press F1-F4 to switch between control schemes.
    /// </summary>
    public class ControlSchemeDebugger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _showUI = true;
        [SerializeField] private KeyCode _toggleUIKey = KeyCode.F12;

        private Rect _windowRect = new Rect(10, 10, 300, 200);

        private void Update()
        {
            // Toggle UI visibility
            if (Input.GetKeyDown(_toggleUIKey))
            {
                _showUI = !_showUI;
            }

            // Quick hotkeys
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetControlScheme(ControlScheme.TwinStick);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                SetControlScheme(ControlScheme.FaceMovement);
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                SetControlScheme(ControlScheme.ClassicRotate);
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                SetControlScheme(ControlScheme.TankControls);
            }
        }

        private void OnGUI()
        {
            if (!_showUI) return;

            _windowRect = GUI.Window(0, _windowRect, DrawWindow, "Control Scheme Debugger");
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.Label("Press F1-F4 to change control scheme:");
            GUILayout.Space(10);

            var config = ConfigProvider.Player;
            if (config == null)
            {
                GUILayout.Label("PlayerConfig not found!");
                return;
            }

            // Current scheme
            GUILayout.Label($"Current: {config.controlScheme}", GUI.skin.box);
            GUILayout.Space(10);

            // Buttons
            if (GUILayout.Button("F1: Twin Stick Shooter"))
            {
                SetControlScheme(ControlScheme.TwinStick);
            }
            GUILayout.Label("  Move: WASD, Aim: Mouse/Right Stick\n  Ship faces aim direction", GUI.skin.box);
            GUILayout.Space(5);

            if (GUILayout.Button("F2: Face Movement"))
            {
                SetControlScheme(ControlScheme.FaceMovement);
            }
            GUILayout.Label("  Move: WASD, Aim: Mouse/Right Stick\n  Ship faces movement (no strafe)", GUI.skin.box);
            GUILayout.Space(5);

            if (GUILayout.Button("F3: Classic Rotate (Asteroids)"))
            {
                SetControlScheme(ControlScheme.ClassicRotate);
            }
            GUILayout.Label("  A/D: Rotate, W/S: Thrust Forward/Back\n  Aim follows ship rotation", GUI.skin.box);
            GUILayout.Space(5);

            if (GUILayout.Button("F4: Tank Controls"))
            {
                SetControlScheme(ControlScheme.TankControls);
            }
            GUILayout.Label("  A/D: Rotate, W/S: Move Forward/Back\n  Aim independent (mouse/right stick)", GUI.skin.box);
            GUILayout.Space(10);

            GUILayout.Label($"Press {_toggleUIKey} to hide/show", GUI.skin.box);

            GUI.DragWindow();
        }

        private void SetControlScheme(ControlScheme scheme)
        {
            var config = ConfigProvider.Player;
            if (config != null)
            {
                config.controlScheme = scheme;
                UnityEngine.Debug.Log($"[ControlScheme] Switched to: {scheme}");
            }
        }
    }
}
