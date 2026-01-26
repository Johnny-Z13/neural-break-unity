using UnityEngine;
using System;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Coordinates ship customization system.
    /// Main entry point for applying skins and managing customization state.
    /// </summary>
    public class ShipCustomization : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private string _currentSkinId = "default";

        // Subsystems
        private ShipVisualsRenderer _visualsRenderer;
        private ShipCustomizationSaveSystem _saveSystem;
        private PlayerController _player;
        private ShipSkin _currentSkin;

        public ShipSkin CurrentSkin => _currentSkin;
        public System.Collections.Generic.IReadOnlyList<ShipSkin> AllSkins => ShipCustomizationData.GetAllSkins();

        public event Action<ShipSkin> OnSkinChanged;
        public event Action<ShipSkin> OnSkinUnlocked;

        private void Awake()
        {

            // Initialize subsystems
            _visualsRenderer = new ShipVisualsRenderer();
            _saveSystem = new ShipCustomizationSaveSystem();
            _saveSystem.OnSkinUnlocked += HandleSkinUnlocked;
        }

        private void Start()
        {
            // Find player
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                _player = playerGO.GetComponent<PlayerController>();
                _visualsRenderer.Initialize(_player);
            }

            // Load and apply saved skin
            _currentSkinId = _saveSystem.LoadSelectedSkinId();
            ApplySkin(_currentSkinId);

            // Subscribe to events for unlock checking
            EventBus.Subscribe<EnemyKilledEvent>(OnGameEvent);
            EventBus.Subscribe<BossDefeatedEvent>(OnGameEvent);
            EventBus.Subscribe<PlayerLevelUpEvent>(OnGameEvent);
            EventBus.Subscribe<AchievementUnlockedEvent>(OnGameEvent);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnGameEvent);
            EventBus.Unsubscribe<BossDefeatedEvent>(OnGameEvent);
            EventBus.Unsubscribe<PlayerLevelUpEvent>(OnGameEvent);
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnGameEvent);

            if (_saveSystem != null)
            {
                _saveSystem.OnSkinUnlocked -= HandleSkinUnlocked;
            }

        }

        #region Public API

        /// <summary>
        /// Apply a skin by ID
        /// </summary>
        public bool ApplySkin(string skinId)
        {
            var skin = ShipCustomizationData.GetSkin(skinId);
            if (skin == null)
            {
                Debug.LogWarning($"[ShipCustomization] Skin not found: {skinId}");
                return false;
            }

            if (!_saveSystem.IsSkinUnlocked(skinId))
            {
                Debug.LogWarning($"[ShipCustomization] Skin not unlocked: {skinId}");
                return false;
            }

            _currentSkin = skin;
            _currentSkinId = skinId;

            _visualsRenderer.ApplyVisuals(skin);
            _saveSystem.SaveSelectedSkinId(skinId);

            OnSkinChanged?.Invoke(skin);
            Debug.Log($"[ShipCustomization] Applied skin: {skin.name}");
            return true;
        }

        /// <summary>
        /// Check if a skin is unlocked
        /// </summary>
        public bool IsSkinUnlocked(string skinId)
        {
            return _saveSystem.IsSkinUnlocked(skinId);
        }

        /// <summary>
        /// Get unlock progress for a skin
        /// </summary>
        public (int current, int required) GetUnlockProgress(string skinId)
        {
            return _saveSystem.GetUnlockProgress(skinId);
        }

        #endregion

        #region Event Handlers

        private void OnGameEvent<T>(T evt)
        {
            _saveSystem.CheckAndUnlockSkins();
        }

        private void HandleSkinUnlocked(ShipSkin skin)
        {
            OnSkinUnlocked?.Invoke(skin);
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: List All Skins")]
        private void DebugListSkins()
        {
            foreach (var skin in AllSkins)
            {
                bool unlocked = IsSkinUnlocked(skin.id);
                Debug.Log($"{skin.name} ({skin.id}): {(unlocked ? "UNLOCKED" : "LOCKED")} - {skin.unlockRequirement} {skin.unlockValue}");
            }
        }

        [ContextMenu("Debug: Unlock All Skins")]
        private void DebugUnlockAll()
        {
            _saveSystem.UnlockAllSkins();
        }

        [ContextMenu("Debug: Apply Random Skin")]
        private void DebugRandomSkin()
        {
            var unlockedSkins = new System.Collections.Generic.List<ShipSkin>();
            foreach (var skin in AllSkins)
            {
                if (IsSkinUnlocked(skin.id))
                {
                    unlockedSkins.Add(skin);
                }
            }

            if (unlockedSkins.Count > 0)
            {
                var randomSkin = unlockedSkins[UnityEngine.Random.Range(0, unlockedSkins.Count)];
                ApplySkin(randomSkin.id);
            }
        }

        #endregion
    }
}
