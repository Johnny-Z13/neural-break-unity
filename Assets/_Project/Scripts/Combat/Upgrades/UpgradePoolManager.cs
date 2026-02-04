using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Utils;
using System.Collections.Generic;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Manages the pool of available upgrades and generates random selections.
    /// Handles weighted selection based on rarity tiers.
    /// Singleton pattern for global access.
    /// </summary>
    public class UpgradePoolManager : MonoBehaviour
    {
        public static UpgradePoolManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int m_cardsPerSelection = 3;
        [SerializeField] private bool m_allowDuplicates = false;

        [Header("Rarity Weights")]
        [SerializeField] private float m_commonWeight = 60f;
        [SerializeField] private float m_rareWeight = 25f;
        [SerializeField] private float m_epicWeight = 12f;
        [SerializeField] private float m_legendaryWeight = 3f;

        [Header("Debug")]
        [SerializeField] private bool m_logSelections = true;

        private List<UpgradeDefinition> m_allUpgrades = new List<UpgradeDefinition>();
        private PermanentUpgradeManager m_permanentUpgrades;
        private bool m_isLoaded;

        #region Lifecycle

        private void Awake()
        {
            Debug.Log($"[UpgradePoolManager] Awake called. Current Instance: {(Instance != null ? Instance.gameObject.name : "null")}");

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[UpgradePoolManager] Duplicate instance detected, destroying self");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Debug.Log($"[UpgradePoolManager] Singleton Instance set to: {gameObject.name}");
        }

        private void Start()
        {
            m_permanentUpgrades = PermanentUpgradeManager.Instance;
            if (m_permanentUpgrades == null)
            {
                LogHelper.LogWarning("[UpgradePoolManager] PermanentUpgradeManager not found");
            }

            LoadUpgrades();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Upgrade Loading

        /// <summary>
        /// Load all upgrade definitions from Resources.
        /// </summary>
        public void LoadUpgrades()
        {
            m_allUpgrades.Clear();

            // Load all UpgradeDefinition assets from Resources/Upgrades
            var upgrades = Resources.LoadAll<UpgradeDefinition>("Upgrades");

            Debug.Log($"[UpgradePoolManager] Resources.LoadAll found {upgrades.Length} UpgradeDefinition assets");

            foreach (var upgrade in upgrades)
            {
                Debug.Log($"[UpgradePoolManager]   Checking: {upgrade.name} - isPermanent: {upgrade.isPermanent}");
                if (upgrade.isPermanent)
                {
                    m_allUpgrades.Add(upgrade);
                }
            }

            m_isLoaded = true;

            Debug.Log($"[UpgradePoolManager] Loaded {m_allUpgrades.Count} permanent upgrades from Resources");
        }

        #endregion

        #region Selection Generation

        /// <summary>
        /// Generate a random selection of upgrades for the player to choose from.
        /// </summary>
        public List<UpgradeDefinition> GenerateSelection()
        {
            if (!m_isLoaded)
            {
                LogHelper.LogWarning("[UpgradePoolManager] Upgrades not loaded yet");
                LoadUpgrades();
            }

            if (m_allUpgrades.Count == 0)
            {
                LogHelper.LogError("[UpgradePoolManager] No upgrades available for selection");
                return new List<UpgradeDefinition>();
            }

            var selection = new List<UpgradeDefinition>();
            var eligible = GetEligibleUpgrades();

            if (eligible.Count == 0)
            {
                LogHelper.LogWarning("[UpgradePoolManager] No eligible upgrades available");
                return selection;
            }

            // Generate selection
            for (int i = 0; i < m_cardsPerSelection && eligible.Count > 0; i++)
            {
                var upgrade = SelectWeightedRandom(eligible);
                if (upgrade != null)
                {
                    selection.Add(upgrade);

                    // Remove from eligible pool if duplicates not allowed
                    if (!m_allowDuplicates)
                    {
                        eligible.Remove(upgrade);
                    }
                }
            }

            if (m_logSelections)
            {
                LogHelper.Log($"[UpgradePoolManager] Generated selection of {selection.Count} upgrades:");
                foreach (var upgrade in selection)
                {
                    LogHelper.Log($"  - {upgrade.displayName} ({upgrade.tier})");
                }
            }

            return selection;
        }

        /// <summary>
        /// Get all upgrades eligible for selection based on current game state.
        /// </summary>
        private List<UpgradeDefinition> GetEligibleUpgrades()
        {
            var eligible = new List<UpgradeDefinition>();

            if (m_permanentUpgrades == null)
            {
                // If no manager, all upgrades are eligible
                eligible.AddRange(m_allUpgrades);
                return eligible;
            }

            var activeUpgrades = m_permanentUpgrades.GetActiveUpgrades();
            int playerLevel = GameManager.Instance != null ? GameManager.Instance.Stats.level : 1;

            foreach (var upgrade in m_allUpgrades)
            {
                if (upgrade.IsEligible(playerLevel, activeUpgrades))
                {
                    eligible.Add(upgrade);
                }
            }

            return eligible;
        }

        /// <summary>
        /// Select a random upgrade from the pool using weighted random selection.
        /// </summary>
        private UpgradeDefinition SelectWeightedRandom(List<UpgradeDefinition> pool)
        {
            if (pool.Count == 0) return null;

            // Calculate total weight
            float totalWeight = 0f;
            foreach (var upgrade in pool)
            {
                totalWeight += GetTierWeight(upgrade.tier) * upgrade.spawnWeight;
            }

            // Random selection
            float random = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var upgrade in pool)
            {
                currentWeight += GetTierWeight(upgrade.tier) * upgrade.spawnWeight;
                if (random <= currentWeight)
                {
                    return upgrade;
                }
            }

            // Fallback to last upgrade
            return pool[pool.Count - 1];
        }

        /// <summary>
        /// Get the base weight for a tier.
        /// </summary>
        private float GetTierWeight(UpgradeTier tier)
        {
            return tier switch
            {
                UpgradeTier.Common => m_commonWeight,
                UpgradeTier.Rare => m_rareWeight,
                UpgradeTier.Epic => m_epicWeight,
                UpgradeTier.Legendary => m_legendaryWeight,
                _ => 1f
            };
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Generate Selection")]
        private void DebugGenerateSelection()
        {
            var selection = GenerateSelection();
            LogHelper.Log($"[UpgradePoolManager] Generated {selection.Count} upgrades");
        }

        [ContextMenu("Debug: Log All Upgrades")]
        private void DebugLogAllUpgrades()
        {
            LogHelper.Log($"[UpgradePoolManager] Total Upgrades: {m_allUpgrades.Count}");
            foreach (var upgrade in m_allUpgrades)
            {
                LogHelper.Log($"  - {upgrade.displayName} ({upgrade.tier}) - ID: {upgrade.upgradeId}");
            }
        }

        [ContextMenu("Debug: Log Eligible Upgrades")]
        private void DebugLogEligibleUpgrades()
        {
            var eligible = GetEligibleUpgrades();
            LogHelper.Log($"[UpgradePoolManager] Eligible Upgrades: {eligible.Count}");
            foreach (var upgrade in eligible)
            {
                LogHelper.Log($"  - {upgrade.displayName} ({upgrade.tier})");
            }
        }

        [ContextMenu("Debug: Reload Upgrades")]
        private void DebugReloadUpgrades()
        {
            LoadUpgrades();
        }

        #endregion
    }
}
