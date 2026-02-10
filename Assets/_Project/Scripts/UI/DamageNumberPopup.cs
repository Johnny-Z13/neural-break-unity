using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays floating damage numbers when enemies are hit.
    /// Numbers float upward and fade out.
    /// Critical hits and kill shots are shown with different styles.
    ///
    /// ZERO-ALLOCATION: All text animations use Update-driven state machines instead of coroutines.
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

        // Animation constants
        private const float PUNCH_DURATION = 0.1f;

        // UI Components
        private Canvas m_canvas;
        private Queue<DamageText> m_pool = new Queue<DamageText>();
        private List<DamageText> m_activeTexts = new List<DamageText>();

        // Cached references
        private Transform m_playerTransform;

        // Cached camera reference (Camera.main allocates ~64 bytes per call via FindGameObjectWithTag)
        private Camera m_mainCamera;

        // Animation phases for each text (replaces coroutine state machine)
        private enum AnimPhase { PunchUp, PunchDown, FloatAndFade, Done }

        private class DamageText
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public TextMeshProUGUI text;
            public CanvasGroup canvasGroup;

            // Animation state (replaces coroutine - zero allocation)
            public AnimPhase phase;
            public float elapsed;
            public Vector3 startPos;
            public Vector3 endPos;
            public Vector3 startScale;
            public Vector3 punchScale;
            public float fadeStartTime;
            public float floatDuration;
            public float fadeDelay;
        }

        private void Awake()
        {
            m_mainCamera = Camera.main;

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
            // Re-cache camera (may have changed on scene reload)
            m_mainCamera = Camera.main;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);

        }

        private void Update()
        {
            // Update all active text animations (replaces StartCoroutine(AnimateText) - zero allocation)
            float dt = Time.unscaledDeltaTime;
            for (int i = m_activeTexts.Count - 1; i >= 0; i--)
            {
                var dmgText = m_activeTexts[i];
                dmgText.elapsed += dt;

                switch (dmgText.phase)
                {
                    case AnimPhase.PunchUp:
                        if (dmgText.elapsed >= PUNCH_DURATION)
                        {
                            dmgText.rectTransform.localScale = dmgText.punchScale;
                            dmgText.elapsed = 0f;
                            dmgText.phase = AnimPhase.PunchDown;
                        }
                        else
                        {
                            float t = dmgText.elapsed / PUNCH_DURATION;
                            dmgText.rectTransform.localScale = Vector3.Lerp(dmgText.startScale, dmgText.punchScale, t);
                        }
                        break;

                    case AnimPhase.PunchDown:
                        if (dmgText.elapsed >= PUNCH_DURATION)
                        {
                            dmgText.rectTransform.localScale = dmgText.startScale;
                            dmgText.elapsed = 0f;
                            dmgText.phase = AnimPhase.FloatAndFade;
                        }
                        else
                        {
                            float t = dmgText.elapsed / PUNCH_DURATION;
                            dmgText.rectTransform.localScale = Vector3.Lerp(dmgText.punchScale, dmgText.startScale, t);
                        }
                        break;

                    case AnimPhase.FloatAndFade:
                        if (dmgText.elapsed >= dmgText.floatDuration)
                        {
                            // Animation complete - return to pool
                            dmgText.gameObject.SetActive(false);
                            dmgText.phase = AnimPhase.Done;
                            m_activeTexts.RemoveAt(i);
                            m_pool.Enqueue(dmgText);
                        }
                        else
                        {
                            float t = dmgText.elapsed / dmgText.floatDuration;
                            // Float up with ease out
                            float easeT = 1f - Mathf.Pow(1f - t, 2f);
                            dmgText.rectTransform.position = Vector3.Lerp(dmgText.startPos, dmgText.endPos, easeT);

                            // Fade out at the end
                            if (dmgText.elapsed > dmgText.fadeStartTime)
                            {
                                float fadeT = (dmgText.elapsed - dmgText.fadeStartTime) / dmgText.fadeDelay;
                                dmgText.canvasGroup.alpha = 1f - fadeT;
                            }
                        }
                        break;
                }
            }
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

            // Convert world position to screen position with random offset (use cached camera - Camera.main allocates per call)
            if (m_mainCamera == null) return;
            Vector3 screenPos = m_mainCamera.WorldToScreenPoint(worldPosition);
            screenPos.x += Random.Range(-m_randomOffset, m_randomOffset);
            screenPos.y += Random.Range(-m_randomOffset * 0.5f, m_randomOffset * 0.5f);

            dmgText.rectTransform.position = screenPos;
            dmgText.rectTransform.localScale = Vector3.one * scale;
            dmgText.gameObject.SetActive(true);

            // Initialize animation state (replaces StartCoroutine(AnimateText) - zero allocation)
            dmgText.phase = AnimPhase.PunchUp;
            dmgText.elapsed = 0f;
            dmgText.startPos = screenPos;
            dmgText.endPos = screenPos + Vector3.up * m_floatDistance;
            dmgText.startScale = Vector3.one * scale;
            dmgText.punchScale = Vector3.one * scale * 1.3f;
            dmgText.floatDuration = m_floatDuration;
            dmgText.fadeDelay = m_fadeDelay;
            dmgText.fadeStartTime = m_floatDuration - m_fadeDelay;
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
