using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays floating damage numbers when enemies are hit.
    /// Numbers float upward and fade out.
    /// Critical hits and kill shots are shown with different styles.
    /// </summary>
    public class DamageNumberPopup : MonoBehaviour
    {
        public static DamageNumberPopup Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float _floatDistance = 50f;
        [SerializeField] private float _floatDuration = 0.8f;
        [SerializeField] private float _fadeDelay = 0.4f;
        [SerializeField] private float _randomOffset = 20f;
        [SerializeField] private bool _showDamageNumbers = true;
        [SerializeField] private bool _showKillText = true;

        [Header("Normal Hit")]
        [SerializeField] private float _normalFontSize = 18f;
        [SerializeField] private Color _normalColor = Color.white;

        [Header("Big Hit (High Damage)")]
        [SerializeField] private float _bigHitFontSize = 26f;
        [SerializeField] private Color _bigHitColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private int _bigHitThreshold = 20;

        [Header("Kill Shot")]
        [SerializeField] private float _killFontSize = 32f;
        [SerializeField] private Color _killColor = new Color(1f, 0.3f, 0.3f);

        [Header("Heal")]
        [SerializeField] private float _healFontSize = 22f;
        [SerializeField] private Color _healColor = new Color(0.3f, 1f, 0.4f);

        [Header("XP Gain")]
        [SerializeField] private float _xpFontSize = 16f;
        [SerializeField] private Color _xpColor = new Color(0.5f, 0.8f, 1f);

        [Header("Pool Settings")]
        [SerializeField] private int _poolSize = 30;

        // UI Components
        private Canvas _canvas;
        private Queue<DamageText> _pool = new Queue<DamageText>();
        private List<DamageText> _activeTexts = new List<DamageText>();

        private class DamageText
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public TextMeshProUGUI text;
            public CanvasGroup canvasGroup;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateCanvas();
            CreatePool();
        }

        private void Start()
        {
            EventBus.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("DamageNumberCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        private void CreatePool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var dmgText = CreateDamageText();
                dmgText.gameObject.SetActive(false);
                _pool.Enqueue(dmgText);
            }
        }

        private DamageText CreateDamageText()
        {
            var dmgText = new DamageText();

            dmgText.gameObject = new GameObject("DamageNumber");
            dmgText.gameObject.transform.SetParent(_canvas.transform);

            dmgText.rectTransform = dmgText.gameObject.AddComponent<RectTransform>();
            dmgText.rectTransform.sizeDelta = new Vector2(100, 50);

            dmgText.canvasGroup = dmgText.gameObject.AddComponent<CanvasGroup>();

            dmgText.text = dmgText.gameObject.AddComponent<TextMeshProUGUI>();
            dmgText.text.alignment = TextAlignmentOptions.Center;
            dmgText.text.fontStyle = FontStyles.Bold;
            dmgText.text.enableWordWrapping = false;

            // Add outline
            var outline = dmgText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return dmgText;
        }

        private void OnEnemyDamaged(EnemyDamagedEvent evt)
        {
            if (!_showDamageNumbers) return;

            bool isBigHit = evt.damage >= _bigHitThreshold;
            ShowNumber(
                evt.position,
                evt.damage.ToString(),
                isBigHit ? _bigHitFontSize : _normalFontSize,
                isBigHit ? _bigHitColor : _normalColor,
                isBigHit ? 1.2f : 1f
            );
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (!_showKillText) return;

            // Show XP gain
            ShowNumber(
                evt.position + Vector3.up * 0.3f,
                $"+{evt.xpValue} XP",
                _xpFontSize,
                _xpColor,
                0.8f
            );
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            var player = FindFirstObjectByType<Entities.PlayerController>();
            if (player == null) return;

            ShowNumber(
                player.transform.position,
                $"+{evt.amount}",
                _healFontSize,
                _healColor,
                1f
            );
        }

        private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
        {
            var player = FindFirstObjectByType<Entities.PlayerController>();
            if (player == null) return;

            ShowNumber(
                player.transform.position + Vector3.up * 0.5f,
                $"LEVEL {evt.newLevel}!",
                _killFontSize,
                new Color(1f, 0.9f, 0.3f),
                1.5f
            );
        }

        public void ShowNumber(Vector3 worldPosition, string text, float fontSize, Color color, float scale = 1f)
        {
            DamageText dmgText;

            if (_pool.Count > 0)
            {
                dmgText = _pool.Dequeue();
            }
            else
            {
                // Pool exhausted, reuse oldest active
                if (_activeTexts.Count > 0)
                {
                    dmgText = _activeTexts[0];
                    _activeTexts.RemoveAt(0);
                    StopCoroutine("AnimateText");
                }
                else
                {
                    dmgText = CreateDamageText();
                }
            }

            _activeTexts.Add(dmgText);

            // Setup text
            dmgText.text.text = text;
            dmgText.text.fontSize = fontSize * scale;
            dmgText.text.color = color;
            dmgText.canvasGroup.alpha = 1f;

            // Convert world position to screen position with random offset
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            screenPos.x += Random.Range(-_randomOffset, _randomOffset);
            screenPos.y += Random.Range(-_randomOffset * 0.5f, _randomOffset * 0.5f);

            dmgText.rectTransform.position = screenPos;
            dmgText.rectTransform.localScale = Vector3.one * scale;
            dmgText.gameObject.SetActive(true);

            StartCoroutine(AnimateText(dmgText));
        }

        private IEnumerator AnimateText(DamageText dmgText)
        {
            Vector3 startPos = dmgText.rectTransform.position;
            Vector3 endPos = startPos + Vector3.up * _floatDistance;
            Vector3 startScale = dmgText.rectTransform.localScale;
            Vector3 punchScale = startScale * 1.3f;

            float elapsed = 0f;

            // Initial punch scale
            float punchDuration = 0.1f;
            while (elapsed < punchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / punchDuration;
                dmgText.rectTransform.localScale = Vector3.Lerp(startScale, punchScale, t);
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < punchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / punchDuration;
                dmgText.rectTransform.localScale = Vector3.Lerp(punchScale, startScale, t);
                yield return null;
            }

            // Float and fade
            elapsed = 0f;
            float fadeStartTime = _floatDuration - _fadeDelay;

            while (elapsed < _floatDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _floatDuration;

                // Float up with ease out
                float easeT = 1f - Mathf.Pow(1f - t, 2f);
                dmgText.rectTransform.position = Vector3.Lerp(startPos, endPos, easeT);

                // Fade out at the end
                if (elapsed > fadeStartTime)
                {
                    float fadeT = (elapsed - fadeStartTime) / _fadeDelay;
                    dmgText.canvasGroup.alpha = 1f - fadeT;
                }

                yield return null;
            }

            // Return to pool
            dmgText.gameObject.SetActive(false);
            _activeTexts.Remove(dmgText);
            _pool.Enqueue(dmgText);
        }

        #region Debug

        [ContextMenu("Debug: Show 25 Damage")]
        private void DebugShowDamage()
        {
            ShowNumber(Vector3.zero, "25", _normalFontSize, _normalColor);
        }

        [ContextMenu("Debug: Show Big Hit")]
        private void DebugShowBigHit()
        {
            ShowNumber(Vector3.zero, "100", _bigHitFontSize, _bigHitColor, 1.2f);
        }

        [ContextMenu("Debug: Show Kill")]
        private void DebugShowKill()
        {
            ShowNumber(Vector3.zero, "KILL!", _killFontSize, _killColor, 1.5f);
        }

        #endregion
    }
}
