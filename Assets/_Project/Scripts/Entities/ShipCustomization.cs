using UnityEngine;
using System;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Coordinates ship customization system.
    /// Main entry point for applying skins and managing customization state.
    /// </summary>
    public class ShipCustomization : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private string m_currentSkinId = "default";

        // Subsystems
        private ShipVisualsRenderer m_visualsRenderer;
        private ShipCustomizationSaveSystem m_saveSystem;
        private PlayerController m_player;
        private ShipSkin m_currentSkin;

        public ShipSkin CurrentSkin => m_currentSkin;
        public System.Collections.Generic.IReadOnlyList<ShipSkin> AllSkins => ShipCustomizationData.GetAllSkins();

        public event Action<ShipSkin> OnSkinChanged;
        public event Action<ShipSkin> OnSkinUnlocked;

        private void Awake()
        {

            // Initialize subsystems
            m_visualsRenderer = new ShipVisualsRenderer();
            m_saveSystem = new ShipCustomizationSaveSystem();
            m_saveSystem.OnSkinUnlocked += HandleSkinUnlocked;
        }

        private void Start()
        {
            // Find player
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                m_player = playerGO.GetComponent<PlayerController>();
                m_visualsRenderer.Initialize(m_player);
            }

            // Load and apply saved skin
            m_currentSkinId = m_saveSystem.LoadSelectedSkinId();
            ApplySkin(m_currentSkinId);

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

            if (m_saveSystem != null)
            {
                m_saveSystem.OnSkinUnlocked -= HandleSkinUnlocked;
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

            if (!m_saveSystem.IsSkinUnlocked(skinId))
            {
                Debug.LogWarning($"[ShipCustomization] Skin not unlocked: {skinId}");
                return false;
            }

            m_currentSkin = skin;
            m_currentSkinId = skinId;

            m_visualsRenderer.ApplyVisuals(skin);
            m_saveSystem.SaveSelectedSkinId(skinId);

            OnSkinChanged?.Invoke(skin);
            Debug.Log($"[ShipCustomization] Applied skin: {skin.name}");
            return true;
        }

        /// <summary>
        /// Check if a skin is unlocked
        /// </summary>
        public bool IsSkinUnlocked(string skinId)
        {
            return m_saveSystem.IsSkinUnlocked(skinId);
        }

        /// <summary>
        /// Get unlock progress for a skin
        /// </summary>
        public (int current, int required) GetUnlockProgress(string skinId)
        {
            return m_saveSystem.GetUnlockProgress(skinId);
        }

        #endregion

        #region Event Handlers

        private void OnGameEvent<T>(T evt)
        {
            m_saveSystem.CheckAndUnlockSkins();
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
            m_saveSystem.UnlockAllSkins();
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
