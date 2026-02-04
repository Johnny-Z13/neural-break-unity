using UnityEngine;
using NeuralBreak.Entities;
using NeuralBreak.Combat;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Minimal coordinator that orchestrates scene setup.
    /// Delegates sprite setup and reference wiring to specialized components.
    /// </summary>
    public class GameSetup : MonoBehaviour
    {
        [Header("Setup Components")]
        [SerializeField] private SceneReferenceWiring m_referenceWiring;
        [SerializeField] private PrefabSpriteSetup m_spriteSetup;

        [Header("Auto-Find Settings")]
        [SerializeField] private bool m_autoSetupOnAwake = true;
        [SerializeField] private bool m_autoStartGame = false;

        // Static sprite accessors for backward compatibility
        public static Sprite CircleSprite => PrefabSpriteSetup.CircleSprite;
        public static Sprite SquareSprite => PrefabSpriteSetup.SquareSprite;

        private void Awake()
        {
            Debug.Log($"[GameSetup] Awake START at {Time.realtimeSinceStartup:F3}s");

            if (m_autoSetupOnAwake)
            {
                Debug.Log($"[GameSetup] SetupReferences START at {Time.realtimeSinceStartup:F3}s");
                SetupReferences();
                Debug.Log($"[GameSetup] SetupReferences DONE at {Time.realtimeSinceStartup:F3}s");

                Debug.Log($"[GameSetup] SetupSprites START at {Time.realtimeSinceStartup:F3}s");
                SetupSprites();
                Debug.Log($"[GameSetup] SetupSprites DONE at {Time.realtimeSinceStartup:F3}s");
            }

            Debug.Log($"[GameSetup] Awake DONE at {Time.realtimeSinceStartup:F3}s");
        }

        private void Start()
        {
            Debug.Log("[GameSetup] Start called");

            if (m_autoStartGame)
            {
                AutoStartGame();
            }
        }

        private void AutoStartGame()
        {
            // GameStateManager is guaranteed to exist (Boot scene) - no timeout needed!
            if (GameStateManager.Instance == null)
            {
                Debug.LogError("[GameSetup] GameStateManager.Instance is null! Boot scene may not have loaded.");
                return;
            }

            if (!GameStateManager.Instance.IsPlaying)
            {
                var mode = GameStateManager.Instance.CurrentMode;
                Debug.Log($"[GameSetup] Starting game in {mode} mode...");
                GameStateManager.Instance.StartGame(mode);
                Debug.Log($"[GameSetup] Auto-started game in {mode} mode");
            }
        }

        private void SetupSprites()
        {
            if (m_spriteSetup == null)
            {
                Debug.LogError("[GameSetup] PrefabSpriteSetup component is missing! Please assign it in the Inspector.");
                return;
            }

            m_spriteSetup.SetupAllSprites();
        }

        [ContextMenu("Setup References")]
        public void SetupReferences()
        {
            if (m_referenceWiring == null)
            {
                Debug.LogError("[GameSetup] SceneReferenceWiring component is missing! Please assign it in the Inspector.");
                return;
            }

            m_referenceWiring.WireSceneReferences();
        }
    }
}
