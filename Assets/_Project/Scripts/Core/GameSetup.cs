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
        [SerializeField] private SceneReferenceWiring _referenceWiring;
        [SerializeField] private PrefabSpriteSetup _spriteSetup;

        [Header("Auto-Find Settings")]
        [SerializeField] private bool _autoSetupOnAwake = true;
        [SerializeField] private bool _autoStartGame = false; // DISABLED - let user choose mode via UI

        // Static sprite accessors for backward compatibility
        public static Sprite CircleSprite => PrefabSpriteSetup.CircleSprite;
        public static Sprite SquareSprite => PrefabSpriteSetup.SquareSprite;

        private void Awake()
        {
            Debug.Log($"[GameSetup] Awake START at {Time.realtimeSinceStartup:F3}s");

            if (_autoSetupOnAwake)
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

            if (_autoStartGame)
            {
                // Give a frame for everything to initialize
                StartCoroutine(AutoStartGame());
            }
        }

        private System.Collections.IEnumerator AutoStartGame()
        {
            Debug.Log("[GameSetup] AutoStartGame coroutine starting...");

            // Wait for GameManager to exist
            float timeout = 3f;
            while (GameManager.Instance == null && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameSetup] GameManager.Instance is null after waiting!");
                yield break;
            }

            // Wait one more frame for safety
            yield return null;

            if (!GameManager.Instance.IsPlaying)
            {
                // Respect the GameManager's configured mode instead of hardcoding Arcade
                var mode = GameManager.Instance.CurrentMode;
                Debug.Log($"[GameSetup] Starting game in {mode} mode...");
                GameManager.Instance.StartGame(mode);
                Debug.Log($"[GameSetup] Auto-started game in {mode} mode");
            }
        }

        private void SetupSprites()
        {
            if (_spriteSetup == null)
            {
                Debug.LogError("[GameSetup] PrefabSpriteSetup component is missing! Please assign it in the Inspector.");
                return;
            }

            _spriteSetup.SetupAllSprites();
        }

        [ContextMenu("Setup References")]
        public void SetupReferences()
        {
            if (_referenceWiring == null)
            {
                Debug.LogError("[GameSetup] SceneReferenceWiring component is missing! Please assign it in the Inspector.");
                return;
            }

            _referenceWiring.WireSceneReferences();
        }
    }
}
