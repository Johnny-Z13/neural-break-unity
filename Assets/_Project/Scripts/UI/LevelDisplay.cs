using UnityEngine;
using TMPro;
using System.Collections;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays current level with optional level-up animation.
    /// </summary>
    public class LevelDisplay : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI m_levelText;
        [SerializeField] private string m_levelFormat = "LEVEL {0}";

        [Header("Animation")]
        [SerializeField] private bool m_animateLevelUp = true;
        [SerializeField] private float m_punchScale = 1.3f;
        [SerializeField] private float m_animDuration = 0.3f;

        // State
        private int m_currentLevel = 1;
        private Vector3 m_originalScale;
        private Coroutine m_animCoroutine;

        private void Awake()
        {
            if (m_levelText != null)
            {
                m_originalScale = m_levelText.transform.localScale;
            }
        }

        /// <summary>
        /// Set the current level
        /// </summary>
        public void SetLevel(int level)
        {
            bool isLevelUp = level > m_currentLevel;
            m_currentLevel = level;

            if (m_levelText != null)
            {
                m_levelText.text = string.Format(m_levelFormat, level);
            }

            // Animate on level up (only if active)
            if (isLevelUp && m_animateLevelUp && m_levelText != null && gameObject.activeInHierarchy)
            {
                if (m_animCoroutine != null)
                {
                    StopCoroutine(m_animCoroutine);
                }
                m_animCoroutine = StartCoroutine(LevelUpAnimation());
            }
        }

        private IEnumerator LevelUpAnimation()
        {
            Transform target = m_levelText.transform;
            Vector3 punchedScale = m_originalScale * m_punchScale;

            // Scale up
            float elapsed = 0f;
            float halfDuration = m_animDuration * 0.5f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                target.localScale = Vector3.Lerp(m_originalScale, punchedScale, t);
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                target.localScale = Vector3.Lerp(punchedScale, m_originalScale, t);
                yield return null;
            }

            target.localScale = m_originalScale;
            m_animCoroutine = null;
        }

        #region Debug

        [ContextMenu("Debug: Level 1")]
        private void DebugLevel1() => SetLevel(1);

        [ContextMenu("Debug: Level 10")]
        private void DebugLevel10() => SetLevel(10);

        [ContextMenu("Debug: Level 99")]
        private void DebugLevel99() => SetLevel(99);

        #endregion
    }
}
