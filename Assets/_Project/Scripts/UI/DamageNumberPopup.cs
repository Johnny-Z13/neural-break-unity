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

        [Header("Settings")]
        [SerializeField] private float m_floatDistance = 50f;
        [SerializeField] private float m_floatDuration = 0.8f;
        [SerializeField] private float m_fadeDelay = 0.4f;
        [SerializeField] private float m_randomOffset = 20f;
        [SerializeField] private bool m_showDamageNumbers = true;
        [SerializeField] private bool m_showKillText = true;

        [Header("Normal Hit (Uses UITheme.DamageStyle)")]
        [SerializeField] private float m_normalFontSize = 16f;
        [SerializeField] private Color m_normalColor = default;

        [Header("Big Hit (High Damage)")]
        [SerializeField] private float m_bigHitFontSize = 24f;
        [SerializeField] private Color m_bigHitColor = default;
        [SerializeField] private int m_bigHitThreshold = 20;

        [Header("Kill Shot")]
        [SerializeField] private float m_killFontSize = 28f;
        [SerializeField] private Color m_killColor = default;

        [Header("Heal")]
        [SerializeField] private float m_healFontSize = 20f;
        [SerializeField] private Color m_healColor = default;

        [Header("XP Gain")]
        [SerializeField] private float m_xpFontSize = 14f;
        [SerializeField] private Color m_xpColor = default;

        [Header("Pool Settings")]
        [SerializeField] private int m_poolSize = 30;

        // UI Components
        private Canvas m_canvas;
        private Queue<DamageText> m_pool = new Queue<DamageText>();
        private List<DamageText> m_activeTexts = new List<DamageText>();

        // Cached references
        private Transform m_playerTransform;

        private class DamageText
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public TextMeshProUGUI text;
            public CanvasGroup canvasGroup;
        }

        private void Awake()
        {

            // Apply UITheme colors if not set
            ApplyThemeColors();

            CreateCanvas();
            CreatePool();
        }

        private void ApplyThemeColors()
        {
            // Use UITheme.DamageStyle colors as defaults
            if (m_normalColor == default) m_normalColor = UITheme.DamageStyle.NormalColor;
            if (m_bigHitColor == default) m_bigHitColor = UITheme.DamageStyle.BigHitColor;
            if (m_killColor == default) m_killColor = UITheme.DamageStyle.CriticalColor;
            if (m_healColor == default) m_healColor = UITheme.DamageStyle.HealColor;
            if (m_xpColor == default) m_xpColor = UITheme.DamageStyle.XPColor;
        }

        private void Start()
        {
            EventBus.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            // NOTE: Level-up is handled by LevelUpAnnouncement - removed duplicate
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Clear cached player reference on new game
            m_playerTransform = null;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);

        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("DamageNumberCanvas");
            canvasGO.transform.SetParent(transform);
            m_canvas = canvasGO.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = 150;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        private void CreatePool()
        {
            for (int i = 0; i < m_poolSize; i++)
            {
                var dmgText = CreateDamageText();
                dmgText.gameObject.SetActive(false);
                m_pool.Enqueue(dmgText);
            }
        }

        private DamageText CreateDamageText()
        {
            var dmgText = new DamageText();

            dmgText.gameObject = new GameObject("DamageNumber");
            dmgText.gameObject.transform.SetParent(m_canvas.transform);

            dmgText.rectTransform = dmgText.gameObject.AddComponent<RectTransform>();
            dmgText.rectTransform.sizeDelta = new Vector2(100, 50);

            dmgText.canvasGroup = dmgText.gameObject.AddComponent<CanvasGroup>();

            dmgText.text = dmgText.gameObject.AddComponent<TextMeshProUGUI>();
            dmgText.text.alignment = TextAlignmentOptions.Center;
            dmgText.text.fontStyle = FontStyles.Bold;
            dmgText.text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

            // Add outline
            var outline = dmgText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return dmgText;
        }

        private void OnEnemyDamaged(EnemyDamagedEvent evt)
        {
            if (!m_showDamageNumbers) return;

            bool isBigHit = evt.damage >= m_bigHitThreshold;
            ShowNumber(
                evt.position,
                evt.damage.ToString(),
                isBigHit ? m_bigHitFontSize : m_normalFontSize,
                isBigHit ? m_bigHitColor : m_normalColor,
                isBigHit ? 1.2f : 1f
            );
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (!m_showKillText) return;

            // Show XP gain
            ShowNumber(
                evt.position + Vector3.up * 0.3f,
                $"+{evt.xpValue} XP",
                m_xpFontSize,
                m_xpColor,
                0.8f
            );
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            // Cache player transform on first use
            if (m_playerTransform == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    m_playerTransform = playerGO.transform;
                }
            }

            if (m_playerTransform == null) return;

            ShowNumber(
                m_playerTransform.position,
                $"+{evt.amount}",
                m_healFontSize,
                m_healColor,
                1f
            );
        }

        // Level-up notification removed - handled by LevelUpAnnouncement.cs
        // This prevents duplicate center-screen notifications

        public void ShowNumber(Vector3 worldPosition, string text, float fontSize, Color color, float scale = 1f)
        {
            DamageText dmgText;

            if (m_pool.Count > 0)
            {
                dmgText = m_pool.Dequeue();
            }
            else
            {
                // Pool exhausted, reuse oldest active
                if (m_activeTexts.Count > 0)
                {
                    dmgText = m_activeTexts[0];
                    m_activeTexts.RemoveAt(0);
                    StopCoroutine("AnimateText");
                }
                else
                {
                    dmgText = CreateDamageText();
                }
            }

            m_activeTexts.Add(dmgText);

            // Setup text
            dmgText.text.text = text;
            dmgText.text.fontSize = fontSize * scale;
            dmgText.text.color = color;
            dmgText.canvasGroup.alpha = 1f;

            // Convert world position to screen position with random offset
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            screenPos.x += Random.Range(-m_randomOffset, m_randomOffset);
            screenPos.y += Random.Range(-m_randomOffset * 0.5f, m_randomOffset * 0.5f);

            dmgText.rectTransform.position = screenPos;
            dmgText.rectTransform.localScale = Vector3.one * scale;
            dmgText.gameObject.SetActive(true);

            StartCoroutine(AnimateText(dmgText));
        }

        private IEnumerator AnimateText(DamageText dmgText)
        {
            Vector3 startPos = dmgText.rectTransform.position;
            Vector3 endPos = startPos + Vector3.up * m_floatDistance;
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
            float fadeStartTime = m_floatDuration - m_fadeDelay;

            while (elapsed < m_floatDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_floatDuration;

                // Float up with ease out
                float easeT = 1f - Mathf.Pow(1f - t, 2f);
                dmgText.rectTransform.position = Vector3.Lerp(startPos, endPos, easeT);

                // Fade out at the end
                if (elapsed > fadeStartTime)
                {
                    float fadeT = (elapsed - fadeStartTime) / m_fadeDelay;
                    dmgText.canvasGroup.alpha = 1f - fadeT;
                }

                yield return null;
            }

            // Return to pool
            dmgText.gameObject.SetActive(false);
            m_activeTexts.Remove(dmgText);
            m_pool.Enqueue(dmgText);
        }

        #region Debug

        [ContextMenu("Debug: Show 25 Damage")]
        private void DebugShowDamage()
        {
            ShowNumber(Vector3.zero, "25", m_normalFontSize, m_normalColor);
        }

        [ContextMenu("Debug: Show Big Hit")]
        private void DebugShowBigHit()
        {
            ShowNumber(Vector3.zero, "100", m_bigHitFontSize, m_bigHitColor, 1.2f);
        }

        [ContextMenu("Debug: Show Kill")]
        private void DebugShowKill()
        {
            ShowNumber(Vector3.zero, "KILL!", m_killFontSize, m_killColor, 1.5f);
        }

        #endregion
    }
}
