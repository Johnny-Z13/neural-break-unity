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
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private string _scoreFormat = "{0:N0}";

        [Header("Delta Popup")]
        [SerializeField] private TextMeshProUGUI _deltaText;
        [SerializeField] private float _deltaDisplayTime = 0.8f;
        [SerializeField] private float _deltaFadeTime = 0.3f;
        [SerializeField] private Vector2 _deltaOffset = new Vector2(0, 30f);

        [Header("Combo/Multiplier")]
        [SerializeField] private TextMeshProUGUI _comboText;
        [SerializeField] private TextMeshProUGUI _multiplierText;
        [SerializeField] private GameObject _comboContainer;

        [Header("Combo Milestone")]
        [SerializeField] private TextMeshProUGUI _milestoneText;
        [SerializeField] private float _milestoneDuration = 1.5f;
        [SerializeField] private float _milestoneScale = 1.5f;

        [Header("Animation")]
        [SerializeField] private float _scoreAnimSpeed = 15f;
        [SerializeField] private bool _animateScore = true;
        [SerializeField] private float _comboPunchScale = 1.2f;

        // Milestone thresholds and messages - use UITheme for consistency
        private static (int threshold, string message, Color color)[] ComboMilestones => UITheme.ComboMilestones;

        // State
        private int _currentScore;
        private int _displayedScore;
        private int _currentCombo;
        private float _currentMultiplier = 1f;
        private Coroutine _deltaCoroutine;
        private Coroutine _milestoneCoroutine;
        private Coroutine _multiplierPunchCoroutine;
        private Vector3 _comboOriginalScale;
        private Vector3 _multiplierOriginalScale;
        private int _lastMilestoneShown = 0;

        private void Awake()
        {
            if (_comboContainer != null)
            {
                _comboOriginalScale = _comboContainer.transform.localScale;
                _comboContainer.SetActive(false);
            }

            if (_multiplierText != null)
            {
                _multiplierOriginalScale = _multiplierText.transform.localScale;
            }

            if (_deltaText != null)
            {
                _deltaText.gameObject.SetActive(false);
            }

            if (_milestoneText != null)
            {
                _milestoneText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_animateScore) return;

            // Smooth score counting
            if (_displayedScore != _currentScore)
            {
                float diff = _currentScore - _displayedScore;
                int step = Mathf.CeilToInt(Mathf.Abs(diff) * Time.unscaledDeltaTime * _scoreAnimSpeed);
                step = Mathf.Max(1, step);

                if (diff > 0)
                {
                    _displayedScore = Mathf.Min(_displayedScore + step, _currentScore);
                }
                else
                {
                    _displayedScore = Mathf.Max(_displayedScore - step, _currentScore);
                }

                UpdateScoreText(_displayedScore);
            }
        }

        /// <summary>
        /// Update score display
        /// </summary>
        public void UpdateScore(int newScore, int delta, Vector3 worldPosition)
        {
            _currentScore = newScore;

            if (!_animateScore)
            {
                _displayedScore = newScore;
                UpdateScoreText(newScore);
            }

            // Show delta popup
            if (delta > 0 && _deltaText != null)
            {
                ShowDelta(delta);
            }
        }

        /// <summary>
        /// Update combo and multiplier display
        /// </summary>
        public void UpdateCombo(int comboCount, float multiplier)
        {
            int previousCombo = _currentCombo;
            float previousMultiplier = _currentMultiplier;
            _currentCombo = comboCount;
            _currentMultiplier = multiplier;

            bool showCombo = comboCount > 1 || multiplier > 1f;

            if (_comboContainer != null)
            {
                _comboContainer.SetActive(showCombo);

                if (showCombo && comboCount > previousCombo)
                {
                    // Punch scale on combo increase
                    StartCoroutine(PunchScale(_comboContainer.transform, _comboOriginalScale));
                }
            }

            if (_comboText != null)
            {
                _comboText.text = comboCount > 1 ? $"{comboCount}x COMBO" : "";
            }

            if (_multiplierText != null)
            {
                if (multiplier > 1f)
                {
                    _multiplierText.text = $"x{multiplier:F1}";

                    // Scale bump on multiplier increase
                    if (multiplier > previousMultiplier)
                    {
                        PunchMultiplier();
                    }
                }
                else
                {
                    _multiplierText.text = "";
                }
            }

            // Check for milestone
            CheckComboMilestone(comboCount);

            // Reset milestone tracking when combo breaks
            if (comboCount == 0)
            {
                _lastMilestoneShown = 0;
            }
        }

        private void PunchMultiplier()
        {
            if (_multiplierText == null) return;

            if (_multiplierPunchCoroutine != null)
            {
                StopCoroutine(_multiplierPunchCoroutine);
                _multiplierText.transform.localScale = _multiplierOriginalScale;
            }
            _multiplierPunchCoroutine = StartCoroutine(PunchScale(_multiplierText.transform, _multiplierOriginalScale));
        }

        private void CheckComboMilestone(int comboCount)
        {
            // Find highest applicable milestone not yet shown
            for (int i = ComboMilestones.Length - 1; i >= 0; i--)
            {
                var milestone = ComboMilestones[i];
                if (comboCount >= milestone.threshold && milestone.threshold > _lastMilestoneShown)
                {
                    ShowMilestone(milestone.message, milestone.color);
                    _lastMilestoneShown = milestone.threshold;
                    break;
                }
            }
        }

        private void ShowMilestone(string message, Color color)
        {
            if (_milestoneText == null) return;

            if (_milestoneCoroutine != null)
            {
                StopCoroutine(_milestoneCoroutine);
            }
            _milestoneCoroutine = StartCoroutine(MilestoneCoroutine(message, color));
        }

        private IEnumerator MilestoneCoroutine(string message, Color color)
        {
            _milestoneText.gameObject.SetActive(true);
            _milestoneText.text = message;
            _milestoneText.color = color;

            RectTransform rect = _milestoneText.rectTransform;
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = Vector3.one * _milestoneScale;

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
                _milestoneText.color = c;
                yield return null;
            }

            // Hold
            yield return new WaitForSecondsRealtime(_milestoneDuration - 0.3f);

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
                _milestoneText.color = c;
                yield return null;
            }

            _milestoneText.gameObject.SetActive(false);
            rect.localScale = originalScale;
            _milestoneCoroutine = null;
        }

        /// <summary>
        /// Reset display for new game
        /// </summary>
        public void ResetDisplay()
        {
            _currentScore = 0;
            _displayedScore = 0;
            _currentCombo = 0;
            _currentMultiplier = 1f;
            _lastMilestoneShown = 0;

            UpdateScoreText(0);

            if (_comboContainer != null)
            {
                _comboContainer.SetActive(false);
            }

            if (_deltaText != null)
            {
                _deltaText.gameObject.SetActive(false);
            }

            if (_milestoneText != null)
            {
                _milestoneText.gameObject.SetActive(false);
            }
        }

        private void UpdateScoreText(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = string.Format(_scoreFormat, score);
            }
        }

        private void ShowDelta(int delta)
        {
            if (_deltaCoroutine != null)
            {
                StopCoroutine(_deltaCoroutine);
            }
            _deltaCoroutine = StartCoroutine(DeltaPopupCoroutine(delta));
        }

        private IEnumerator DeltaPopupCoroutine(int delta)
        {
            _deltaText.gameObject.SetActive(true);
            _deltaText.text = $"+{delta:N0}";

            // Set initial position
            RectTransform rect = _deltaText.rectTransform;
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + _deltaOffset;

            // Fade in
            Color color = _deltaText.color;
            color.a = 1f;
            _deltaText.color = color;

            // Display time
            float elapsed = 0f;
            while (elapsed < _deltaDisplayTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _deltaDisplayTime;
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t * 0.5f);
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < _deltaFadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = 1f - (elapsed / _deltaFadeTime);
                _deltaText.color = color;
                yield return null;
            }

            _deltaText.gameObject.SetActive(false);
            rect.anchoredPosition = startPos;
            _deltaCoroutine = null;
        }

        private IEnumerator PunchScale(Transform target, Vector3 originalScale)
        {
            Vector3 punchedScale = originalScale * _comboPunchScale;

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
        private void DebugAddScore() => UpdateScore(_currentScore + 500, 500, Vector3.zero);

        [ContextMenu("Debug: Set 5x Combo")]
        private void DebugCombo() => UpdateCombo(5, 2.5f);

        [ContextMenu("Debug: Reset")]
        private void DebugReset() => ResetDisplay();

        #endregion
    }
}
