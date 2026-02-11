using UnityEngine;
using TMPro;
using System.Collections;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays score, combo count, and multiplier.
    /// Includes animated score delta popup and combo milestone announcements.
    /// </summary>
    public class ScoreDisplay : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private TextMeshProUGUI m_scoreText;
        [SerializeField] private string m_scoreFormat = "{0:N0}";

        [Header("Delta Popup")]
        [SerializeField] private TextMeshProUGUI m_deltaText;
        [SerializeField] private float m_deltaDisplayTime = 0.8f;
        [SerializeField] private float m_deltaFadeTime = 0.3f;
        [SerializeField] private Vector2 m_deltaOffset = new Vector2(0, 30f);

        [Header("Combo/Multiplier")]
        [SerializeField] private TextMeshProUGUI m_comboText;
        [SerializeField] private TextMeshProUGUI m_multiplierText;
        [SerializeField] private GameObject m_comboContainer;

        [Header("Combo Milestone")]
        [SerializeField] private TextMeshProUGUI m_milestoneText;
        [SerializeField] private float m_milestoneDuration = 1.5f;
        [SerializeField] private float m_milestoneScale = 1.5f;

        [Header("Animation")]
        [SerializeField] private float m_scoreAnimSpeed = 15f;
        [SerializeField] private bool m_animateScore = true;
        [SerializeField] private float m_comboPunchScale = 1.2f;

        // Milestone thresholds and messages - use UITheme for consistency
        private static (int threshold, string message, Color color)[] ComboMilestones => UITheme.ComboMilestones;

        // State
        private int m_currentScore;
        private int m_displayedScore;
        private int m_currentCombo;
        private float m_currentMultiplier = 1f;
        private Coroutine m_deltaCoroutine;
        private Coroutine m_milestoneCoroutine;
        private Coroutine m_multiplierPunchCoroutine;
        private Vector3 m_comboOriginalScale;
        private Vector3 m_multiplierOriginalScale;
        private int m_lastMilestoneShown = 0;

        private void Awake()
        {
            if (m_comboContainer != null)
            {
                m_comboOriginalScale = m_comboContainer.transform.localScale;
                m_comboContainer.SetActive(false);
            }

            if (m_multiplierText != null)
            {
                m_multiplierOriginalScale = m_multiplierText.transform.localScale;
            }

            if (m_deltaText != null)
            {
                m_deltaText.gameObject.SetActive(false);
            }

            if (m_milestoneText != null)
            {
                m_milestoneText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!m_animateScore) return;

            // Smooth score counting
            if (m_displayedScore != m_currentScore)
            {
                float diff = m_currentScore - m_displayedScore;
                int step = Mathf.CeilToInt(Mathf.Abs(diff) * Time.unscaledDeltaTime * m_scoreAnimSpeed);
                step = Mathf.Max(1, step);

                if (diff > 0)
                {
                    m_displayedScore = Mathf.Min(m_displayedScore + step, m_currentScore);
                }
                else
                {
                    m_displayedScore = Mathf.Max(m_displayedScore - step, m_currentScore);
                }

                UpdateScoreText(m_displayedScore);
            }
        }

        /// <summary>
        /// Update score display
        /// </summary>
        public void UpdateScore(int newScore, int delta, Vector3 worldPosition)
        {
            m_currentScore = newScore;

            if (!m_animateScore)
            {
                m_displayedScore = newScore;
                UpdateScoreText(newScore);
            }

            // Show delta popup
            if (delta > 0 && m_deltaText != null)
            {
                ShowDelta(delta);
            }
        }

        /// <summary>
        /// Update combo and multiplier display
        /// </summary>
        public void UpdateCombo(int comboCount, float multiplier)
        {
            int previousCombo = m_currentCombo;
            float previousMultiplier = m_currentMultiplier;
            m_currentCombo = comboCount;
            m_currentMultiplier = multiplier;

            bool showCombo = comboCount > 1 || multiplier > 1f;

            if (m_comboContainer != null)
            {
                m_comboContainer.SetActive(showCombo);

                if (showCombo && comboCount > previousCombo)
                {
                    // Punch scale on combo increase
                    StartCoroutine(PunchScale(m_comboContainer.transform, m_comboOriginalScale));
                }
            }

            if (m_comboText != null)
            {
                m_comboText.text = comboCount > 1 ? $"{comboCount}x COMBO" : "";
            }

            if (m_multiplierText != null)
            {
                if (multiplier > 1f)
                {
                    m_multiplierText.text = $"x{multiplier:F1}";

                    // Scale bump on multiplier increase
                    if (multiplier > previousMultiplier)
                    {
                        PunchMultiplier();
                    }
                }
                else
                {
                    m_multiplierText.text = "";
                }
            }

            // Check for milestone
            CheckComboMilestone(comboCount);

            // Reset milestone tracking when combo breaks
            if (comboCount == 0)
            {
                m_lastMilestoneShown = 0;
            }
        }

        private void PunchMultiplier()
        {
            if (m_multiplierText == null) return;

            if (m_multiplierPunchCoroutine != null)
            {
                StopCoroutine(m_multiplierPunchCoroutine);
                m_multiplierText.transform.localScale = m_multiplierOriginalScale;
            }
            m_multiplierPunchCoroutine = StartCoroutine(PunchScale(m_multiplierText.transform, m_multiplierOriginalScale));
        }

        private void CheckComboMilestone(int comboCount)
        {
            // Find highest applicable milestone not yet shown
            for (int i = ComboMilestones.Length - 1; i >= 0; i--)
            {
                var milestone = ComboMilestones[i];
                if (comboCount >= milestone.threshold && milestone.threshold > m_lastMilestoneShown)
                {
                    ShowMilestone(milestone.message, milestone.color);
                    m_lastMilestoneShown = milestone.threshold;
                    break;
                }
            }
        }

        private void ShowMilestone(string message, Color color)
        {
            if (m_milestoneText == null) return;

            if (m_milestoneCoroutine != null)
            {
                StopCoroutine(m_milestoneCoroutine);
            }
            m_milestoneCoroutine = StartCoroutine(MilestoneCoroutine(message, color));
        }

        private IEnumerator MilestoneCoroutine(string message, Color color)
        {
            m_milestoneText.gameObject.SetActive(true);
            m_milestoneText.text = message;
            m_milestoneText.color = color;

            RectTransform rect = m_milestoneText.rectTransform;
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = Vector3.one * m_milestoneScale;

            // Scale up and fade in
            float elapsed = 0f;
            float scaleInDuration = 0.15f;
            while (elapsed < scaleInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / scaleInDuration;
                rect.localScale = Vector3.Lerp(targetScale * 1.5f, targetScale, t);
                Color c = color;
                c.a = t;
                m_milestoneText.color = c;
                yield return null;
            }

            // Hold
            yield return new WaitForSecondsRealtime(m_milestoneDuration - 0.3f);

            // Scale down and fade out
            elapsed = 0f;
            float fadeOutDuration = 0.15f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeOutDuration;
                rect.localScale = Vector3.Lerp(targetScale, originalScale * 0.8f, t);
                Color c = color;
                c.a = 1f - t;
                m_milestoneText.color = c;
                yield return null;
            }

            m_milestoneText.gameObject.SetActive(false);
            rect.localScale = originalScale;
            m_milestoneCoroutine = null;
        }

        /// <summary>
        /// Reset display for new game
        /// </summary>
        public void ResetDisplay()
        {
            m_currentScore = 0;
            m_displayedScore = 0;
            m_currentCombo = 0;
            m_currentMultiplier = 1f;
            m_lastMilestoneShown = 0;

            UpdateScoreText(0);

            if (m_comboContainer != null)
            {
                m_comboContainer.SetActive(false);
            }

            if (m_deltaText != null)
            {
                m_deltaText.gameObject.SetActive(false);
            }

            if (m_milestoneText != null)
            {
                m_milestoneText.gameObject.SetActive(false);
            }
        }

        private void UpdateScoreText(int score)
        {
            if (m_scoreText != null)
            {
                m_scoreText.text = string.Format(m_scoreFormat, score);
            }
        }

        private void ShowDelta(int delta)
        {
            if (m_deltaCoroutine != null)
            {
                StopCoroutine(m_deltaCoroutine);
            }
            m_deltaCoroutine = StartCoroutine(DeltaPopupCoroutine(delta));
        }

        private IEnumerator DeltaPopupCoroutine(int delta)
        {
            m_deltaText.gameObject.SetActive(true);
            m_deltaText.text = $"+{delta:N0}";

            // Set initial position
            RectTransform rect = m_deltaText.rectTransform;
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + m_deltaOffset;

            // Fade in
            Color color = m_deltaText.color;
            color.a = 1f;
            m_deltaText.color = color;

            // Display time
            float elapsed = 0f;
            while (elapsed < m_deltaDisplayTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_deltaDisplayTime;
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t * 0.5f);
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < m_deltaFadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = 1f - (elapsed / m_deltaFadeTime);
                m_deltaText.color = color;
                yield return null;
            }

            m_deltaText.gameObject.SetActive(false);
            rect.anchoredPosition = startPos;
            m_deltaCoroutine = null;
        }

        private IEnumerator PunchScale(Transform target, Vector3 originalScale)
        {
            Vector3 punchedScale = originalScale * m_comboPunchScale;

            // Scale up
            float elapsed = 0f;
            float duration = 0.1f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(originalScale, punchedScale, elapsed / duration);
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(punchedScale, originalScale, elapsed / duration);
                yield return null;
            }

            target.localScale = originalScale;
        }

        #region Debug

        [ContextMenu("Debug: Add 500 Score")]
        private void DebugAddScore() => UpdateScore(m_currentScore + 500, 500, Vector3.zero);

        [ContextMenu("Debug: Set 5x Combo")]
        private void DebugCombo() => UpdateCombo(5, 2.5f);

        [ContextMenu("Debug: Reset")]
        private void DebugReset() => ResetDisplay();

        #endregion
    }
}
