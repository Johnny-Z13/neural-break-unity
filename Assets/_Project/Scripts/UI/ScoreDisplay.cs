using UnityEngine;
using TMPro;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays score, combo count, and multiplier.
    /// Includes animated score delta popup and combo milestone announcements.
    ///
    /// ZERO-ALLOCATION: All animations use timer-based Update instead of coroutines.
    /// </summary>
    public class ScoreDisplay : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private TextMeshProUGUI m_scoreText;

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
        private Vector3 m_comboOriginalScale;
        private Vector3 m_multiplierOriginalScale;
        private int m_lastMilestoneShown = 0;

        // Timer-based punch scale state (replaces PunchScale coroutine - zero allocation)
        private const float PUNCH_ANIM_DURATION = 0.1f;

        // Combo punch state
        private bool m_comboPunchActive;
        private float m_comboPunchElapsed;
        private bool m_comboPunchPhaseUp; // true = scaling up, false = scaling down

        // Multiplier punch state
        private bool m_multiplierPunchActive;
        private float m_multiplierPunchElapsed;
        private bool m_multiplierPunchPhaseUp;

        // Delta popup state (replaces DeltaPopupCoroutine - zero allocation)
        private bool m_deltaActive;
        private float m_deltaElapsed;
        private enum DeltaPhase { Display, FadeOut }
        private DeltaPhase m_deltaPhase;
        private Vector2 m_deltaStartPos;
        private Color m_deltaColor;

        // Milestone state (replaces MilestoneCoroutine - zero allocation)
        private bool m_milestoneActive;
        private float m_milestoneElapsed;
        private enum MilestonePhase { ScaleIn, Hold, FadeOut }
        private MilestonePhase m_milestonePhase;
        private Color m_milestoneColor;
        private Vector3 m_milestoneTargetScale;

        // Milestone timing constants
        private const float MILESTONE_SCALE_IN_DURATION = 0.15f;
        private const float MILESTONE_FADE_OUT_DURATION = 0.15f;

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
            // Smooth score counting
            if (m_animateScore && m_displayedScore != m_currentScore)
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

            float dt = Time.unscaledDeltaTime;

            // Timer-based combo punch (replaces PunchScale coroutine - zero allocation)
            if (m_comboPunchActive)
            {
                m_comboPunchElapsed += dt;
                UpdatePunchScale(
                    m_comboContainer.transform,
                    m_comboOriginalScale,
                    ref m_comboPunchElapsed,
                    ref m_comboPunchPhaseUp,
                    ref m_comboPunchActive);
            }

            // Timer-based multiplier punch (replaces PunchScale coroutine - zero allocation)
            if (m_multiplierPunchActive && m_multiplierText != null)
            {
                m_multiplierPunchElapsed += dt;
                UpdatePunchScale(
                    m_multiplierText.transform,
                    m_multiplierOriginalScale,
                    ref m_multiplierPunchElapsed,
                    ref m_multiplierPunchPhaseUp,
                    ref m_multiplierPunchActive);
            }

            // Timer-based delta popup (replaces DeltaPopupCoroutine - zero allocation)
            if (m_deltaActive)
            {
                m_deltaElapsed += dt;
                UpdateDelta();
            }

            // Timer-based milestone (replaces MilestoneCoroutine - zero allocation)
            if (m_milestoneActive)
            {
                m_milestoneElapsed += dt;
                UpdateMilestone();
            }
        }

        private void UpdatePunchScale(
            Transform target,
            Vector3 originalScale,
            ref float elapsed,
            ref bool phaseUp,
            ref bool active)
        {
            Vector3 punchedScale = originalScale * m_comboPunchScale;

            if (phaseUp)
            {
                if (elapsed >= PUNCH_ANIM_DURATION)
                {
                    target.localScale = punchedScale;
                    elapsed = 0f;
                    phaseUp = false;
                }
                else
                {
                    target.localScale = Vector3.Lerp(originalScale, punchedScale, elapsed / PUNCH_ANIM_DURATION);
                }
            }
            else
            {
                if (elapsed >= PUNCH_ANIM_DURATION)
                {
                    target.localScale = originalScale;
                    active = false;
                }
                else
                {
                    target.localScale = Vector3.Lerp(punchedScale, originalScale, elapsed / PUNCH_ANIM_DURATION);
                }
            }
        }

        private void UpdateDelta()
        {
            RectTransform rect = m_deltaText.rectTransform;

            switch (m_deltaPhase)
            {
                case DeltaPhase.Display:
                    if (m_deltaElapsed >= m_deltaDisplayTime)
                    {
                        m_deltaElapsed = 0f;
                        m_deltaPhase = DeltaPhase.FadeOut;
                    }
                    else
                    {
                        float t = m_deltaElapsed / m_deltaDisplayTime;
                        rect.anchoredPosition = Vector2.Lerp(m_deltaStartPos, m_deltaStartPos + m_deltaOffset, t * 0.5f);
                    }
                    break;

                case DeltaPhase.FadeOut:
                    if (m_deltaElapsed >= m_deltaFadeTime)
                    {
                        m_deltaText.gameObject.SetActive(false);
                        rect.anchoredPosition = m_deltaStartPos;
                        m_deltaActive = false;
                    }
                    else
                    {
                        Color c = m_deltaColor;
                        c.a = 1f - (m_deltaElapsed / m_deltaFadeTime);
                        m_deltaText.color = c;
                    }
                    break;
            }
        }

        private void UpdateMilestone()
        {
            RectTransform rect = m_milestoneText.rectTransform;

            switch (m_milestonePhase)
            {
                case MilestonePhase.ScaleIn:
                    if (m_milestoneElapsed >= MILESTONE_SCALE_IN_DURATION)
                    {
                        rect.localScale = m_milestoneTargetScale;
                        Color c1 = m_milestoneColor;
                        c1.a = 1f;
                        m_milestoneText.color = c1;
                        m_milestoneElapsed = 0f;
                        m_milestonePhase = MilestonePhase.Hold;
                    }
                    else
                    {
                        float t = m_milestoneElapsed / MILESTONE_SCALE_IN_DURATION;
                        rect.localScale = Vector3.Lerp(m_milestoneTargetScale * 1.5f, m_milestoneTargetScale, t);
                        Color c2 = m_milestoneColor;
                        c2.a = t;
                        m_milestoneText.color = c2;
                    }
                    break;

                case MilestonePhase.Hold:
                    float holdDuration = m_milestoneDuration - MILESTONE_SCALE_IN_DURATION - MILESTONE_FADE_OUT_DURATION;
                    if (m_milestoneElapsed >= holdDuration)
                    {
                        m_milestoneElapsed = 0f;
                        m_milestonePhase = MilestonePhase.FadeOut;
                    }
                    break;

                case MilestonePhase.FadeOut:
                    if (m_milestoneElapsed >= MILESTONE_FADE_OUT_DURATION)
                    {
                        m_milestoneText.gameObject.SetActive(false);
                        rect.localScale = Vector3.one;
                        m_milestoneActive = false;
                    }
                    else
                    {
                        float t = m_milestoneElapsed / MILESTONE_FADE_OUT_DURATION;
                        rect.localScale = Vector3.Lerp(m_milestoneTargetScale, Vector3.one * 0.8f, t);
                        Color c3 = m_milestoneColor;
                        c3.a = 1f - t;
                        m_milestoneText.color = c3;
                    }
                    break;
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

                if (showCombo && comboCount > previousCombo && gameObject.activeInHierarchy)
                {
                    // Punch scale on combo increase (timer-based - zero allocation)
                    m_comboPunchActive = true;
                    m_comboPunchElapsed = 0f;
                    m_comboPunchPhaseUp = true;
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

                    // Scale bump on multiplier increase (timer-based - zero allocation)
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
            if (m_multiplierText == null || !gameObject.activeInHierarchy) return;

            // Reset multiplier punch state (timer-based - zero allocation)
            m_multiplierPunchActive = true;
            m_multiplierPunchElapsed = 0f;
            m_multiplierPunchPhaseUp = true;
            m_multiplierText.transform.localScale = m_multiplierOriginalScale;
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
            if (m_milestoneText == null || !gameObject.activeInHierarchy) return;

            // Start milestone animation (timer-based - zero allocation)
            m_milestoneText.gameObject.SetActive(true);
            m_milestoneText.text = message;
            m_milestoneText.color = color;
            m_milestoneColor = color;
            m_milestoneTargetScale = Vector3.one * m_milestoneScale;
            m_milestoneActive = true;
            m_milestoneElapsed = 0f;
            m_milestonePhase = MilestonePhase.ScaleIn;
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

            // Reset animation states
            m_comboPunchActive = false;
            m_multiplierPunchActive = false;
            m_deltaActive = false;
            m_milestoneActive = false;

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

        private int m_lastDisplayedScore = -1;

        private void UpdateScoreText(int score)
        {
            if (m_scoreText != null && score != m_lastDisplayedScore)
            {
                m_lastDisplayedScore = score;
                m_scoreText.text = score.ToString("N0");
            }
        }

        private void ShowDelta(int delta)
        {
            if (!gameObject.activeInHierarchy) return;

            // Start delta popup animation (timer-based - zero allocation)
            m_deltaText.gameObject.SetActive(true);
            m_deltaText.text = $"+{delta:N0}";

            RectTransform rect = m_deltaText.rectTransform;
            m_deltaStartPos = rect.anchoredPosition;

            m_deltaColor = m_deltaText.color;
            m_deltaColor.a = 1f;
            m_deltaText.color = m_deltaColor;

            m_deltaActive = true;
            m_deltaElapsed = 0f;
            m_deltaPhase = DeltaPhase.Display;
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
