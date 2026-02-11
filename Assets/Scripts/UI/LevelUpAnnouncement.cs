using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays dramatic level-up announcement when player levels up.
    /// Features scale animation, particle-like effects, and glow.
    /// </summary>
    public class LevelUpAnnouncement : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float m_displayDuration = 1.5f;
        [SerializeField] private float m_scaleInDuration = 0.2f;
        [SerializeField] private float m_scaleOutDuration = 0.3f;
        [SerializeField] private float m_maxScale = 1.3f;
        [SerializeField] private float m_pulseFrequency = 8f;
        [SerializeField] private float m_pulseAmplitude = 0.05f;

        [Header("Colors (Uses UITheme)")]
        [SerializeField] private bool m_useThemeColors = true;

        private Color LevelUpColor => m_useThemeColors ? UITheme.Good : m_customLevelUpColor;
        private Color GlowColor => m_useThemeColors ? UITheme.GoodGlow : m_customGlowColor;

        [SerializeField] private Color m_customLevelUpColor = new Color(1f, 0.9f, 0.3f);
        [SerializeField] private Color m_customGlowColor = new Color(1f, 0.8f, 0.2f, 0.5f);

        // UI Components
        private Canvas m_canvas;
        private CanvasGroup m_canvasGroup;
        private TextMeshProUGUI m_levelUpText;
        private TextMeshProUGUI m_levelNumberText;
        private Image m_glowImage;
        private RectTransform m_container;

        // Animation
        private Coroutine m_announcementCoroutine;

        private void Start()
        {
            CreateUI();

            if (FindFirstObjectByType<PlayerLevelSystem>() != null)
            {
                FindFirstObjectByType<PlayerLevelSystem>().OnLevelUp += OnLevelUp;
            }
        }

        private void OnDestroy()
        {
            if (FindFirstObjectByType<PlayerLevelSystem>() != null)
            {
                FindFirstObjectByType<PlayerLevelSystem>().OnLevelUp -= OnLevelUp;
            }
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("LevelUpCanvas");
            canvasGO.transform.SetParent(transform);
            m_canvas = canvasGO.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = UITheme.SortOrder.LevelUp;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            m_canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            m_canvasGroup.alpha = 0;
            m_canvasGroup.blocksRaycasts = false;
            m_canvasGroup.interactable = false;

            // Container (positioned at bottom of top third - doesn't occlude player)
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            m_container = containerGO.AddComponent<RectTransform>();
            m_container.anchorMin = new Vector2(0.5f, 0.5f);
            m_container.anchorMax = new Vector2(0.5f, 0.5f);
            m_container.pivot = new Vector2(0.5f, 0.5f);
            m_container.anchoredPosition = new Vector2(0, 250); // Top third (was 50)
            m_container.sizeDelta = new Vector2(400, 150);

            // Glow background
            var glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(m_container);
            var glowRect = glowGO.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-100, -50);
            glowRect.offsetMax = new Vector2(100, 50);

            m_glowImage = glowGO.AddComponent<Image>();
            m_glowImage.color = GlowColor;
            m_glowImage.sprite = CreateGlowSprite();

            // "LEVEL UP!" text
            var levelUpGO = new GameObject("LevelUpText");
            levelUpGO.transform.SetParent(m_container);
            var levelUpRect = levelUpGO.AddComponent<RectTransform>();
            levelUpRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelUpRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelUpRect.pivot = new Vector2(0.5f, 0.5f);
            levelUpRect.anchoredPosition = new Vector2(0, 20);
            levelUpRect.sizeDelta = new Vector2(400, 60);

            m_levelUpText = levelUpGO.AddComponent<TextMeshProUGUI>();
            m_levelUpText.text = "POWER UP!";
            m_levelUpText.fontSize = 48;
            m_levelUpText.fontStyle = FontStyles.Bold;
            m_levelUpText.color = LevelUpColor;
            m_levelUpText.alignment = TextAlignmentOptions.Center;
            m_levelUpText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

            // Add outline
            var outline = levelUpGO.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.5f);
            outline.effectDistance = new Vector2(2, -2);

            // Power level number text
            var levelNumGO = new GameObject("LevelNumberText");
            levelNumGO.transform.SetParent(m_container);
            var levelNumRect = levelNumGO.AddComponent<RectTransform>();
            levelNumRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelNumRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelNumRect.pivot = new Vector2(0.5f, 0.5f);
            levelNumRect.anchoredPosition = new Vector2(0, -30);
            levelNumRect.sizeDelta = new Vector2(250, 50);

            m_levelNumberText = levelNumGO.AddComponent<TextMeshProUGUI>();
            m_levelNumberText.text = "POWER = 2";
            m_levelNumberText.fontSize = 32;
            m_levelNumberText.fontStyle = FontStyles.Bold;
            m_levelNumberText.color = Color.white;
            m_levelNumberText.alignment = TextAlignmentOptions.Center;
        }

        private Sprite CreateGlowSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (dist / maxDist));
                    alpha = alpha * alpha; // Quadratic falloff
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void OnLevelUp(int newLevel)
        {
            ShowAnnouncement(newLevel);
        }

        public void ShowAnnouncement(int level)
        {
            if (m_announcementCoroutine != null)
            {
                StopCoroutine(m_announcementCoroutine);
            }
            m_announcementCoroutine = StartCoroutine(AnnouncementCoroutine(level));
        }

        private IEnumerator AnnouncementCoroutine(int level)
        {
            m_levelNumberText.text = $"POWER = {level}";
            m_container.localScale = Vector3.zero;
            m_canvasGroup.alpha = 0;

            // Scale in
            float elapsed = 0f;
            while (elapsed < m_scaleInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_scaleInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                m_container.localScale = Vector3.one * m_maxScale * easeT;
                m_canvasGroup.alpha = easeT;

                yield return null;
            }

            m_container.localScale = Vector3.one * m_maxScale;
            m_canvasGroup.alpha = 1f;

            // Hold with pulse
            elapsed = 0f;
            float holdDuration = m_displayDuration - m_scaleInDuration - m_scaleOutDuration;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                // Pulse scale
                float pulse = 1f + Mathf.Sin(elapsed * m_pulseFrequency) * m_pulseAmplitude;
                m_container.localScale = Vector3.one * m_maxScale * pulse;

                // Glow pulse
                Color glowC = GlowColor;
                glowC.a = GlowColor.a * (0.7f + Mathf.Sin(elapsed * m_pulseFrequency * 0.5f) * 0.3f);
                m_glowImage.color = glowC;

                yield return null;
            }

            // Scale down and fade out
            elapsed = 0f;
            Vector3 startScale = m_container.localScale;
            while (elapsed < m_scaleOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_scaleOutDuration;
                float easeT = t * t * t; // Ease in cubic

                m_container.localScale = Vector3.Lerp(startScale, Vector3.one * 0.5f, easeT);
                m_canvasGroup.alpha = 1f - easeT;

                yield return null;
            }

            m_canvasGroup.alpha = 0;
            m_announcementCoroutine = null;
        }

        #region Debug

        [ContextMenu("Debug: Show Level 5")]
        private void DebugShowLevel5() => ShowAnnouncement(5);

        [ContextMenu("Debug: Show Level 10")]
        private void DebugShowLevel10() => ShowAnnouncement(10);

        [ContextMenu("Debug: Show Level 25")]
        private void DebugShowLevel25() => ShowAnnouncement(25);

        #endregion
    }
}
