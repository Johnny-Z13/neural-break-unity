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
        [SerializeField] private int _cardsPerSelection = 3;
        [SerializeField] private bool _allowDuplicates = false;

        [Header("Rarity Weights")]
        [SerializeField] private float _commonWeight = 60f;
        [SerializeField] private float _rareWeight = 25f;
        [SerializeField] private float _epicWeight = 12f;
        [SerializeField] private float _legendaryWeight = 3f;

        [Header("Debug")]
        [SerializeField] private bool _logSelections = true;

        private List<UpgradeDefinition> _allUpgrades = new List<UpgradeDefinition>();
        private PermanentUpgradeManager _permanentUpgrades;
        private bool _isLoaded;

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
            _permanentUpgrades = PermanentUpgradeManager.Instance;
            if (_permanentUpgrades == null)
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
            _allUpgrades.Clear();

            // Load all UpgradeDefinition assets from Resources/Upgrades
            var upgrades = Resources.LoadAll<UpgradeDefinition>("Upgrades");

            Debug.Log($"[UpgradePoolManager] Resources.LoadAll found {upgrades.Length} UpgradeDefinition assets");

            foreach (var upgrade in upgrades)
            {
                Debug.Log($"[UpgradePoolManager]   Checking: {upgrade.name} - isPermanent: {upgrade.isPermanent}");
                if (upgrade.isPermanent)
                {
                    _allUpgrades.Add(upgrade);
                }
            }

            _isLoaded = true;

            Debug.Log($"[UpgradePoolManager] Loaded {_allUpgrades.Count} permanent upgrades from Resources");
        }

        #endregion

        #region Selection Generation

        /// <summary>
        /// Generate a random selection of upgrades for the player to choose from.
        /// </summary>
        public List<UpgradeDefinition> GenerateSelection()
        {
            if (!_isLoaded)
            {
                LogHelper.LogWarning("[UpgradePoolManager] Upgrades not loaded yet");
                LoadUpgrades();
            }

            if (_allUpgrades.Count == 0)
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
            for (int i = 0; i < _cardsPerSelection && eligible.Count > 0; i++)
            {
                var upgrade = SelectWeightedRandom(eligible);
                if (upgrade != null)
                {
                    selection.Add(upgrade);

                    // Remove from eligible pool if duplicates not allowed
                    if (!_allowDuplicates)
                    {
                        eligible.Remove(upgrade);
                    }
                }
            }

            if (_logSelections)
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

            if (_permanentUpgrades == null)
            {
                // If no manager, all upgrades are eligible
                eligible.AddRange(_allUpgrades);
                return eligible;
            }

            var activeUpgrades = _permanentUpgrades.GetActiveUpgrades();
            int playerLevel = GameManager.Instance != null ? GameManager.Instance.Stats.level : 1;

            foreach (var upgrade in _allUpgrades)
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
                UpgradeTier.Common => _commonWeight,
                UpgradeTier.Rare => _rareWeight,
                UpgradeTier.Epic => _epicWeight,
                UpgradeTier.Legendary => _legendaryWeight,
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
            LogHelper.Log($"[UpgradePoolManager] Total Upgrades: {_allUpgrades.Count}");
            foreach (var upgrade in _allUpgrades)
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
