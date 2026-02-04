using UnityEngine;
using UnityEngine.UI;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays smart bomb count with icons.
    /// Wired by HUDBuilderArcade - no scale pulses, static icons only.
    /// </summary>
    public class BombDisplay : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color m_activeBombColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color m_inactiveBombColor = new Color(0.25f, 0.2f, 0.15f, 0.6f);

        // Wired by HUDBuilderArcade
        [SerializeField] private Image[] m_bombIcons;

        private int m_currentBombs;
        private int m_maxBombs;

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
            m_currentBombs = evt.count;
            m_maxBombs = evt.maxCount;
            UpdateIcons();
        }

        private void OnBombActivated(SmartBombActivatedEvent evt)
        {
            StartCoroutine(FlashEffect());
        }

        private void UpdateIcons()
        {
            if (m_bombIcons == null) return;

            for (int i = 0; i < m_bombIcons.Length; i++)
            {
                if (m_bombIcons[i] == null) continue;

                bool isActive = i < m_currentBombs;
                m_bombIcons[i].color = isActive ? m_activeBombColor : m_inactiveBombColor;
                m_bombIcons[i].gameObject.SetActive(i < m_maxBombs);
            }
        }

        private System.Collections.IEnumerator FlashEffect()
        {
            // Brief white flash on activation
            if (m_bombIcons != null)
            {
                foreach (var icon in m_bombIcons)
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
            m_bombIcons = icons;
        }
    }
}
