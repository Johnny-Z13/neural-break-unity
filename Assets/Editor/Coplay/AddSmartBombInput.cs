using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using System.Linq;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Adds SmartBomb, Thrust, and Dash actions to the Input Actions asset.
    /// </summary>
    public class AddSmartBombInput
    {
        public static void Execute()
        {
            Debug.Log("[AddSmartBombInput] Adding missing input actions...");

            // Load the input actions asset
            string assetPath = "Assets/InputSystem_Actions.inputactions";
            var inputActionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

            if (inputActionsAsset == null)
            {
                Debug.LogError($"[AddSmartBombInput] Could not find InputActionAsset at {assetPath}");
                return;
            }

            // Get the Player action map
            var playerMap = inputActionsAsset.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogError("[AddSmartBombInput] Could not find Player action map!");
                return;
            }

            // Check if SmartBomb action already exists
            var smartBombAction = playerMap.FindAction("SmartBomb");
            if (smartBombAction == null)
            {
                // Add SmartBomb action
                smartBombAction = playerMap.AddAction("SmartBomb", InputActionType.Button);
                Debug.Log("[AddSmartBombInput] Added SmartBomb action");

                // Add bindings for SmartBomb
                // Keyboard: B key
                smartBombAction.AddBinding("<Keyboard>/b", groups: "Keyboard&Mouse");
                // Gamepad: Left Shoulder button
                smartBombAction.AddBinding("<Gamepad>/leftShoulder", groups: "Gamepad");
                
                Debug.Log("[AddSmartBombInput] Added SmartBomb bindings (B key, Left Shoulder)");
            }
            else
            {
                Debug.Log("[AddSmartBombInput] SmartBomb action already exists");
            }

            // Check if Thrust action exists
            var thrustAction = playerMap.FindAction("Thrust");
            if (thrustAction == null)
            {
                // Add Thrust action
                thrustAction = playerMap.AddAction("Thrust", InputActionType.Button);
                Debug.Log("[AddSmartBombInput] Added Thrust action");

                // Add bindings for Thrust
                // Keyboard: Left Shift
                thrustAction.AddBinding("<Keyboard>/leftShift", groups: "Keyboard&Mouse");
                // Gamepad: Right Trigger
                thrustAction.AddBinding("<Gamepad>/rightTrigger", groups: "Gamepad");
                
                Debug.Log("[AddSmartBombInput] Added Thrust bindings (Left Shift, Right Trigger)");
            }
            else
            {
                Debug.Log("[AddSmartBombInput] Thrust action already exists");
            }

            // Check if Dash action exists
            var dashAction = playerMap.FindAction("Dash");
            if (dashAction == null)
            {
                // Add Dash action
                dashAction = playerMap.AddAction("Dash", InputActionType.Button);
                Debug.Log("[AddSmartBombInput] Added Dash action");

                // Add bindings for Dash
                // Keyboard: Space
                dashAction.AddBinding("<Keyboard>/space", groups: "Keyboard&Mouse");
                // Gamepad: Right Shoulder button
                dashAction.AddBinding("<Gamepad>/rightShoulder", groups: "Gamepad");
                
                Debug.Log("[AddSmartBombInput] Added Dash bindings (Space, Right Shoulder)");
            }
            else
            {
                Debug.Log("[AddSmartBombInput] Dash action already exists");
            }

            // Save the asset
            EditorUtility.SetDirty(inputActionsAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AddSmartBombInput] Input actions updated successfully!");
            Debug.Log("[AddSmartBombInput] SmartBomb: B key (keyboard) or Left Shoulder (gamepad)");
            Debug.Log("[AddSmartBombInput] Thrust: Left Shift (keyboard) or Right Trigger (gamepad)");
            Debug.Log("[AddSmartBombInput] Dash: Space (keyboard) or Right Shoulder (gamepad)");
        }
    }
}
