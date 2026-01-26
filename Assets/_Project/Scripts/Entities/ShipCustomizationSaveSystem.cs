using UnityEngine;
using System;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Handles saving and loading ship customization data.
    /// Manages unlock states and progress tracking.
    /// </summary>
    public class ShipCustomizationSaveSystem
    {
        public event Action<ShipSkin> OnSkinUnlocked;

        /// <summary>
        /// Check if a skin is unlocked
        /// </summary>
        public bool IsSkinUnlocked(string skinId)
        {
            var skin = ShipCustomizationData.GetSkin(skinId);
            if (skin == null) return false;

            // Default skins are always unlocked
            if (skin.unlockRequirement == UnlockRequirement.Default)
            {
                return true;
            }

            // Check save system
            if (SaveSystem.Instance != null && SaveSystem.Instance.IsShipSkinUnlocked(skinId))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get unlock progress for a skin
        /// </summary>
        public (int current, int required) GetUnlockProgress(string skinId)
        {
            var skin = ShipCustomizationData.GetSkin(skinId);
            if (skin == null) return (0, 0);

            int current = 0;
            int required = skin.unlockValue;

            if (SaveSystem.Instance != null && SaveSystem.Instance.CurrentSave != null)
            {
                var save = SaveSystem.Instance.CurrentSave;

                switch (skin.unlockRequirement)
                {
                    case UnlockRequirement.Score:
                        current = save.highScore;
                        break;
                    case UnlockRequirement.Level:
                        current = save.highestLevel;
                        break;
                    case UnlockRequirement.Kills:
                        current = save.totalEnemiesKilled;
                        break;
                    case UnlockRequirement.Bosses:
                        current = save.totalBossesKilled;
                        break;
                    case UnlockRequirement.Combo:
                        current = save.highestCombo;
                        break;
                    case UnlockRequirement.Time:
                        current = Mathf.RoundToInt(save.longestSurvivalTime);
                        break;
                }
            }

            return (current, required);
        }

        /// <summary>
        /// Check all skins and unlock those that meet requirements
        /// </summary>
        public void CheckAndUnlockSkins()
        {
            if (SaveSystem.Instance == null || SaveSystem.Instance.CurrentSave == null) return;

            var save = SaveSystem.Instance.CurrentSave;
            var allSkins = ShipCustomizationData.GetAllSkins();

            foreach (var skin in allSkins)
            {
                if (IsSkinUnlocked(skin.id)) continue;

                bool shouldUnlock = false;

                switch (skin.unlockRequirement)
                {
                    case UnlockRequirement.Score:
                        shouldUnlock = save.highScore >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Level:
                        shouldUnlock = save.highestLevel >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Kills:
                        shouldUnlock = save.totalEnemiesKilled >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Bosses:
                        shouldUnlock = save.totalBossesKilled >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Combo:
                        shouldUnlock = save.highestCombo >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Time:
                        shouldUnlock = save.longestSurvivalTime >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Achievement:
                        // Special case - check all achievements
                        if (AchievementSystem.Instance != null)
                        {
                            shouldUnlock = AchievementSystem.Instance.GetUnlockedCount() >= AchievementSystem.Instance.GetTotalCount();
                        }
                        break;
                }

                if (shouldUnlock)
                {
                    UnlockSkin(skin.id);
                }
            }
        }

        /// <summary>
        /// Unlock a specific skin
        /// </summary>
        private void UnlockSkin(string skinId)
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.UnlockShipSkin(skinId);
            }

            var skin = ShipCustomizationData.GetSkin(skinId);
            if (skin != null)
            {
                OnSkinUnlocked?.Invoke(skin);
                Debug.Log($"[ShipCustomizationSaveSystem] Unlocked skin: {skin.name}");

                // Show unlock notification
                EventBus.Publish(new AchievementUnlockedEvent
                {
                    name = $"Ship Unlocked: {skin.name}",
                    description = skin.description
                });
            }
        }

        /// <summary>
        /// Load the currently selected skin ID
        /// </summary>
        public string LoadSelectedSkinId()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.CurrentSave != null)
            {
                return SaveSystem.Instance.CurrentSave.selectedShipSkin;
            }
            return "default";
        }

        /// <summary>
        /// Save the currently selected skin ID
        /// </summary>
        public void SaveSelectedSkinId(string skinId)
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.SetSelectedShipSkin(skinId);
            }
        }

        /// <summary>
        /// Unlock all skins (for debug/testing)
        /// </summary>
        public void UnlockAllSkins()
        {
            var allSkins = ShipCustomizationData.GetAllSkins();
            foreach (var skin in allSkins)
            {
                if (SaveSystem.Instance != null)
                {
                    SaveSystem.Instance.UnlockShipSkin(skin.id);
                }
            }
            Debug.Log("[ShipCustomizationSaveSystem] All skins unlocked!");
        }
    }
}
