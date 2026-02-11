using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Displays visual warning indicators before enemies spawn.
    /// Shows pulsing circles/icons at spawn locations.
    /// </summary>
    public class SpawnWarningIndicator : MonoBehaviour
    {
        [Header("Warning Settings")]
        [SerializeField] private float m_defaultWarningDuration = 0.8f;
        [SerializeField] private float m_bossWarningDuration = 2f;
        [SerializeField] private int m_poolSize = 10;

        [Header("Visual Settings")]
        [SerializeField] private float m_warningRadius = 1.5f;
        [SerializeField] private float m_pulseSpeed = 8f;
        [SerializeField] private float m_pulseMinScale = 0.3f;
        [SerializeField] private float m_pulseMaxScale = 1.2f;
        [SerializeField] private int m_circleSegments = 32;

        [Header("Colors by Threat Level")]
        [SerializeField] private Color m_lowThreatColor = new Color(1f, 1f, 0f, 0.6f);     // Yellow
        [SerializeField] private Color m_mediumThreatColor = new Color(1f, 0.5f, 0f, 0.7f); // Orange
        [SerializeField] private Color m_highThreatColor = new Color(1f, 0f, 0f, 0.8f);     // Red
        [SerializeField] private Color m_bossThreatColor = new Color(1f, 0f, 0.5f, 0.9f);   // Magenta

        private bool m_warningsEnabled = true;
        private List<WarningInstance> m_warningPool = new List<WarningInstance>();
        private List<WarningInstance> m_activeWarnings = new List<WarningInstance>();

        private class WarningInstance
        {
            public GameObject gameObject;
            public LineRenderer circleRenderer;
            public LineRenderer innerCircle;
            public SpriteRenderer exclamationMark;
            public bool isActive;
            public float elapsed;
            public float duration;
            public Color color;
            public EnemyType enemyType;
        }

        private void Start()
        {
            InitializePool();
            EventBus.Subscribe<EnemySpawnWarningEvent>(OnSpawnWarning);

            // Load setting from PlayerPrefs
            m_warningsEnabled = PlayerPrefs.GetInt("NeuralBreak_SpawnWarnings", 1) == 1;
        }

        /// <summary>
        /// Enable or disable spawn warnings
        /// </summary>
        public void SetWarningsEnabled(bool enabled)
        {
            m_warningsEnabled = enabled;
            if (!enabled)
            {
                ClearAllWarnings();
            }
        }

        /// <summary>
        /// Check if warnings are enabled
        /// </summary>
        public bool AreWarningsEnabled => m_warningsEnabled;

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemySpawnWarningEvent>(OnSpawnWarning);
        }

        private void InitializePool()
        {
            for (int i = 0; i < m_poolSize; i++)
            {
                var warning = CreateWarningObject();
                warning.gameObject.SetActive(false);
                m_warningPool.Add(warning);
            }
        }

        private WarningInstance CreateWarningObject()
        {
            var instance = new WarningInstance();

            // Create parent object
            instance.gameObject = new GameObject("SpawnWarning");
            instance.gameObject.transform.SetParent(transform);

            // Create outer circle using LineRenderer
            var outerGO = new GameObject("OuterCircle");
            outerGO.transform.SetParent(instance.gameObject.transform);
            outerGO.transform.localPosition = Vector3.zero;

            instance.circleRenderer = outerGO.AddComponent<LineRenderer>();
            ConfigureCircleRenderer(instance.circleRenderer, m_warningRadius);

            // Create inner circle (smaller, pulsing opposite)
            var innerGO = new GameObject("InnerCircle");
            innerGO.transform.SetParent(instance.gameObject.transform);
            innerGO.transform.localPosition = Vector3.zero;

            instance.innerCircle = innerGO.AddComponent<LineRenderer>();
            ConfigureCircleRenderer(instance.innerCircle, m_warningRadius * 0.5f);

            // Create exclamation mark sprite
            var exclamGO = new GameObject("Exclamation");
            exclamGO.transform.SetParent(instance.gameObject.transform);
            exclamGO.transform.localPosition = Vector3.zero;

            instance.exclamationMark = exclamGO.AddComponent<SpriteRenderer>();
            instance.exclamationMark.sprite = CreateExclamationSprite();
            instance.exclamationMark.sortingOrder = 50;

            return instance;
        }

        private void ConfigureCircleRenderer(LineRenderer lr, float radius)
        {
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.sortingOrder = 49;

            // Create material
            var mat = new Material(Shader.Find("Sprites/Default"));
            lr.material = mat;

            // Generate circle points
            lr.positionCount = m_circleSegments;
            for (int i = 0; i < m_circleSegments; i++)
            {
                float angle = (float)i / m_circleSegments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                lr.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        private Sprite CreateExclamationSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            // Clear texture
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw exclamation mark
            int centerX = size / 2;
            int dotY = 4;
            int dotRadius = 3;
            int barStartY = 10;
            int barEndY = 28;
            int barWidth = 3;

            // Draw dot
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centerX;
                    float dy = y - dotY;
                    if (dx * dx + dy * dy <= dotRadius * dotRadius)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }

            // Draw bar
            for (int y = barStartY; y <= barEndY; y++)
            {
                for (int x = centerX - barWidth; x <= centerX + barWidth; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        private void OnSpawnWarning(EnemySpawnWarningEvent evt)
        {
            ShowWarning(evt.spawnPosition, evt.enemyType, evt.warningDuration);
        }

        public void ShowWarning(Vector3 position, EnemyType enemyType, float duration = 0f)
        {
            // Check if warnings are enabled (but always show boss warnings)
            if (!m_warningsEnabled && enemyType != EnemyType.Boss)
            {
                return;
            }

            if (duration <= 0)
            {
                duration = enemyType == EnemyType.Boss ? m_bossWarningDuration : m_defaultWarningDuration;
            }

            // Get warning from pool
            WarningInstance warning = GetPooledWarning();
            if (warning == null) return;

            warning.gameObject.transform.position = position;
            warning.enemyType = enemyType;
            warning.duration = duration;
            warning.elapsed = 0f;
            warning.isActive = true;
            warning.color = GetColorForEnemyType(enemyType);

            // Set initial colors
            warning.circleRenderer.startColor = warning.color;
            warning.circleRenderer.endColor = warning.color;
            warning.innerCircle.startColor = warning.color;
            warning.innerCircle.endColor = warning.color;
            warning.exclamationMark.color = warning.color;

            warning.gameObject.SetActive(true);
            m_activeWarnings.Add(warning);
        }

        private WarningInstance GetPooledWarning()
        {
            foreach (var warning in m_warningPool)
            {
                if (!warning.isActive)
                {
                    return warning;
                }
            }

            // Expand pool if needed
            if (m_warningPool.Count < m_poolSize * 2)
            {
                var newWarning = CreateWarningObject();
                m_warningPool.Add(newWarning);
                return newWarning;
            }

            return null;
        }

        private Color GetColorForEnemyType(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Boss:
                    return m_bossThreatColor;
                case EnemyType.VoidSphere:
                case EnemyType.ChaosWorm:
                    return m_highThreatColor;
                case EnemyType.UFO:
                case EnemyType.CrystalShard:
                case EnemyType.Fizzer:
                    return m_mediumThreatColor;
                default:
                    return m_lowThreatColor;
            }
        }

        private void Update()
        {
            for (int i = m_activeWarnings.Count - 1; i >= 0; i--)
            {
                var warning = m_activeWarnings[i];
                warning.elapsed += Time.deltaTime;

                float progress = warning.elapsed / warning.duration;

                if (progress >= 1f)
                {
                    // Warning complete
                    warning.isActive = false;
                    warning.gameObject.SetActive(false);
                    m_activeWarnings.RemoveAt(i);
                    continue;
                }

                // Animate warning
                AnimateWarning(warning, progress);
            }
        }

        private void AnimateWarning(WarningInstance warning, float progress)
        {
            // Pulsing scale
            float pulse = Mathf.Sin(warning.elapsed * m_pulseSpeed) * 0.5f + 0.5f;
            float scale = Mathf.Lerp(m_pulseMinScale, m_pulseMaxScale, pulse);

            warning.circleRenderer.transform.localScale = Vector3.one * scale;

            // Inner circle pulses opposite
            float innerPulse = Mathf.Sin(warning.elapsed * m_pulseSpeed + Mathf.PI) * 0.5f + 0.5f;
            float innerScale = Mathf.Lerp(m_pulseMinScale * 0.5f, m_pulseMaxScale * 0.6f, innerPulse);
            warning.innerCircle.transform.localScale = Vector3.one * innerScale;

            // Exclamation bobs and flashes
            float bob = Mathf.Sin(warning.elapsed * m_pulseSpeed * 2f) * 0.1f;
            warning.exclamationMark.transform.localPosition = new Vector3(0, bob, 0);

            // Fade in quickly, then pulse brightness
            float alpha = Mathf.Min(1f, progress * 4f); // Quick fade in
            float brightness = 0.7f + Mathf.Sin(warning.elapsed * m_pulseSpeed * 1.5f) * 0.3f;

            Color c = warning.color;
            c.a = alpha * brightness;
            warning.circleRenderer.startColor = c;
            warning.circleRenderer.endColor = c;

            c.a = alpha * brightness * 0.8f;
            warning.innerCircle.startColor = c;
            warning.innerCircle.endColor = c;

            // Exclamation intensifies near end
            float exclamAlpha = alpha * (0.5f + progress * 0.5f);
            c = warning.color;
            c.a = exclamAlpha;
            warning.exclamationMark.color = c;

            // Scale up exclamation near end
            float exclamScale = 1f + progress * 0.5f;
            warning.exclamationMark.transform.localScale = Vector3.one * exclamScale;

            // Line width increases near end
            float lineWidth = 0.08f + progress * 0.04f;
            warning.circleRenderer.startWidth = lineWidth;
            warning.circleRenderer.endWidth = lineWidth;
        }

        public void ClearAllWarnings()
        {
            foreach (var warning in m_activeWarnings)
            {
                warning.isActive = false;
                warning.gameObject.SetActive(false);
            }
            m_activeWarnings.Clear();
        }
    }
}
