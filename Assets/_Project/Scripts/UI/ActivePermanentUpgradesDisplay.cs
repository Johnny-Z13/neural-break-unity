using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using System.Collections.Generic;
using Z13.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays active permanent upgrades in the HUD.
    /// Shows icons/names of all permanent upgrades selected during the run.
    /// </summary>
    public class ActivePermanentUpgradesDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform m_upgradeContainer;
        [SerializeField] private GameObject m_upgradeIconPrefab;

        [Header("Settings")]
        [SerializeField] private int m_maxDisplayed = 10;
        [SerializeField] private float m_iconSize = 32f;

        private PermanentUpgradeManager m_upgradeManager;
        private List<GameObject> m_activeIcons = new List<GameObject>();

        private void Start()
        {
            m_upgradeManager = PermanentUpgradeManager.Instance;
            if (m_upgradeManager != null)
            {
                m_upgradeManager.OnUpgradeAdded += OnUpgradeAdded;
            }

            // Subscribe to events
            EventBus.Subscribe<PermanentUpgradeAddedEvent>(OnPermanentUpgradeAdded);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            if (m_upgradeManager != null)
            {
                m_upgradeManager.OnUpgradeAdded -= OnUpgradeAdded;
            }

            EventBus.Unsubscribe<PermanentUpgradeAddedEvent>(OnPermanentUpgradeAdded);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnUpgradeAdded(UpgradeDefinition upgrade)
        {
            AddUpgradeIcon(upgrade);
        }

        private void OnPermanentUpgradeAdded(PermanentUpgradeAddedEvent evt)
        {
            AddUpgradeIcon(evt.upgrade);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            ClearIcons();
        }

        private void AddUpgradeIcon(UpgradeDefinition upgrade)
        {
            if (m_upgradeContainer == null) return;
            if (m_activeIcons.Count >= m_maxDisplayed) return;

            GameObject iconGO;

            if (m_upgradeIconPrefab != null)
            {
                iconGO = Instantiate(m_upgradeIconPrefab, m_upgradeContainer);
            }
            else
            {
                // Create simple icon
                iconGO = new GameObject($"Icon_{upgrade.upgradeId}", typeof(RectTransform));
                iconGO.transform.SetParent(m_upgradeContainer, false);

                var rect = iconGO.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(m_iconSize, m_iconSize);

                var img = iconGO.AddComponent<Image>();
                img.sprite = upgrade.icon;
                img.color = upgrade.iconColor;
                img.preserveAspect = true;

                // Add tooltip (upgrade name)
                var tooltip = iconGO.AddComponent<UpgradeTooltip>();
                tooltip.text = upgrade.displayName;
            }

            m_activeIcons.Add(iconGO);
        }

        private void ClearIcons()
        {
            foreach (var icon in m_activeIcons)
            {
                if (icon != null)
                {
                    Destroy(icon);
                }
            }
            m_activeIcons.Clear();
        }

        /// <summary>
        /// Simple tooltip component for upgrade icons.
        /// </summary>
        private class UpgradeTooltip : MonoBehaviour
        {
            public string text;

            // Future: implement actual tooltip display on hover
        }
    }
}
