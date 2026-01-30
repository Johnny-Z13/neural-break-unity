using UnityEngine;
using UnityEngine.UI;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays smart bomb count with icons.
    /// Wired by HUDBuilderArcade - no scale pulses, static icons only.
    /// </summary>
    public class BombDisplay : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color _activeBombColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _inactiveBombColor = new Color(0.25f, 0.2f, 0.15f, 0.6f);

        // Wired by HUDBuilderArcade
        [SerializeField] private Image[] _bombIcons;

        private int _currentBombs;
        private int _maxBombs;

        private void Awake()
        {
            EventBus.Subscribe<SmartBombCountChangedEvent>(OnBombCountChanged);
            EventBus.Subscribe<SmartBombActivatedEvent>(OnBombActivated);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SmartBombCountChangedEvent>(OnBombCountChanged);
            EventBus.Unsubscribe<SmartBombActivatedEvent>(OnBombActivated);
        }

        private void OnBombCountChanged(SmartBombCountChangedEvent evt)
        {
            _currentBombs = evt.count;
            _maxBombs = evt.maxCount;
            UpdateIcons();
        }

        private void OnBombActivated(SmartBombActivatedEvent evt)
        {
            StartCoroutine(FlashEffect());
        }

        private void UpdateIcons()
        {
            if (_bombIcons == null) return;

            for (int i = 0; i < _bombIcons.Length; i++)
            {
                if (_bombIcons[i] == null) continue;

                bool isActive = i < _currentBombs;
                _bombIcons[i].color = isActive ? _activeBombColor : _inactiveBombColor;
                _bombIcons[i].gameObject.SetActive(i < _maxBombs);
            }
        }

        private System.Collections.IEnumerator FlashEffect()
        {
            // Brief white flash on activation
            if (_bombIcons != null)
            {
                foreach (var icon in _bombIcons)
                {
                    if (icon != null && icon.gameObject.activeSelf)
                    {
                        icon.color = Color.white;
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);

            UpdateIcons();
        }

        /// <summary>
        /// Set bomb icon references (called by HUDBuilderArcade)
        /// </summary>
        public void SetIcons(Image[] icons)
        {
            _bombIcons = icons;
        }
    }
}
