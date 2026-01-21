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
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private string _levelFormat = "LEVEL {0}";

        [Header("Animation")]
        [SerializeField] private bool _animateLevelUp = true;
        [SerializeField] private float _punchScale = 1.3f;
        [SerializeField] private float _animDuration = 0.3f;

        // State
        private int _currentLevel = 1;
        private Vector3 _originalScale;
        private Coroutine _animCoroutine;

        private void Awake()
        {
            if (_levelText != null)
            {
                _originalScale = _levelText.transform.localScale;
            }
        }

        /// <summary>
        /// Set the current level
        /// </summary>
        public void SetLevel(int level)
        {
            bool isLevelUp = level > _currentLevel;
            _currentLevel = level;

            if (_levelText != null)
            {
                _levelText.text = string.Format(_levelFormat, level);
            }

            // Animate on level up
            if (isLevelUp && _animateLevelUp && _levelText != null)
            {
                if (_animCoroutine != null)
                {
                    StopCoroutine(_animCoroutine);
                }
                _animCoroutine = StartCoroutine(LevelUpAnimation());
            }
        }

        private IEnumerator LevelUpAnimation()
        {
            Transform target = _levelText.transform;
            Vector3 punchedScale = _originalScale * _punchScale;

            // Scale up
            float elapsed = 0f;
            float halfDuration = _animDuration * 0.5f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                target.localScale = Vector3.Lerp(_originalScale, punchedScale, t);
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                target.localScale = Vector3.Lerp(punchedScale, _originalScale, t);
                yield return null;
            }

            target.localScale = _originalScale;
            _animCoroutine = null;
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
