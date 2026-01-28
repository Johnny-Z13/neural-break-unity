using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using System.Collections.Generic;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays active permanent upgrades in the HUD.
    /// Shows icons/names of all permanent upgrades selected during the run.
    /// </summary>
    public class ActivePermanentUpgradesDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _upgradeContainer;
        [SerializeField] private GameObject _upgradeIconPrefab;

        [Header("Settings")]
        [SerializeField] private int _maxDisplayed = 10;
        [SerializeField] private float _iconSize = 32f;

        private PermanentUpgradeManager _upgradeManager;
        private List<GameObject> _activeIcons = new List<GameObject>();

        private void Start()
        {
            _upgradeManager = PermanentUpgradeManager.Instance;
            if (_upgradeManager != null)
            {
                _upgradeManager.OnUpgradeAdded += OnUpgradeAdded;
            }

            // Subscribe to events
            EventBus.Subscribe<PermanentUpgradeAddedEvent>(OnPermanentUpgradeAdded);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            if (_upgradeManager != null)
            {
                _upgradeManager.OnUpgradeAdded -= OnUpgradeAdded;
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
            if (_upgradeContainer == null) return;
            if (_activeIcons.Count >= _maxDisplayed) return;

            GameObject iconGO;

            if (_upgradeIconPrefab != null)
            {
                iconGO = Instantiate(_upgradeIconPrefab, _upgradeContainer);
            }
            else
            {
                // Create simple icon
                iconGO = new GameObject($"Icon_{upgrade.upgradeId}", typeof(RectTransform));
                iconGO.transform.SetParent(_upgradeContainer, false);

                var rect = iconGO.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(_iconSize, _iconSize);

                var img = iconGO.AddComponent<Image>();
                img.sprite = upgrade.icon;
                img.color = upgrade.iconColor;
                img.preserveAspect = true;

                // Add tooltip (upgrade name)
                var tooltip = iconGO.AddComponent<UpgradeTooltip>();
                tooltip.text = upgrade.displayName;
            }

            _activeIcons.Add(iconGO);
        }

        private void ClearIcons()
        {
            foreach (var icon in _activeIcons)
            {
                if (icon != null)
                {
                    Destroy(icon);
                }
            }
            _activeIcons.Clear();
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
