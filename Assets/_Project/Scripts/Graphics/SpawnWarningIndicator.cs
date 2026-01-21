using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeuralBreak.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Displays visual warning indicators before enemies spawn.
    /// Shows pulsing circles/icons at spawn locations.
    /// </summary>
    public class SpawnWarningIndicator : MonoBehaviour
    {
        [Header("Warning Settings")]
        [SerializeField] private float _defaultWarningDuration = 0.8f;
        [SerializeField] private float _bossWarningDuration = 2f;
        [SerializeField] private int _poolSize = 10;

        [Header("Visual Settings")]
        [SerializeField] private float _warningRadius = 1.5f;
        [SerializeField] private float _pulseSpeed = 8f;
        [SerializeField] private float _pulseMinScale = 0.3f;
        [SerializeField] private float _pulseMaxScale = 1.2f;
        [SerializeField] private int _circleSegments = 32;

        [Header("Colors by Threat Level")]
        [SerializeField] private Color _lowThreatColor = new Color(1f, 1f, 0f, 0.6f);     // Yellow
        [SerializeField] private Color _mediumThreatColor = new Color(1f, 0.5f, 0f, 0.7f); // Orange
        [SerializeField] private Color _highThreatColor = new Color(1f, 0f, 0f, 0.8f);     // Red
        [SerializeField] private Color _bossThreatColor = new Color(1f, 0f, 0.5f, 0.9f);   // Magenta

        private bool _warningsEnabled = true;
        private List<WarningInstance> _warningPool = new List<WarningInstance>();
        private List<WarningInstance> _activeWarnings = new List<WarningInstance>();

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
            _warningsEnabled = PlayerPrefs.GetInt("NeuralBreak_SpawnWarnings", 1) == 1;
        }

        /// <summary>
        /// Enable or disable spawn warnings
        /// </summary>
        public void SetWarningsEnabled(bool enabled)
        {
            _warningsEnabled = enabled;
            if (!enabled)
            {
                ClearAllWarnings();
            }
        }

        /// <summary>
        /// Check if warnings are enabled
        /// </summary>
        public bool AreWarningsEnabled => _warningsEnabled;

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemySpawnWarningEvent>(OnSpawnWarning);
        }

        private void InitializePool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var warning = CreateWarningObject();
                warning.gameObject.SetActive(false);
                _warningPool.Add(warning);
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
            ConfigureCircleRenderer(instance.circleRenderer, _warningRadius);

            // Create inner circle (smaller, pulsing opposite)
            var innerGO = new GameObject("InnerCircle");
            innerGO.transform.SetParent(instance.gameObject.transform);
            innerGO.transform.localPosition = Vector3.zero;

            instance.innerCircle = innerGO.AddComponent<LineRenderer>();
            ConfigureCircleRenderer(instance.innerCircle, _warningRadius * 0.5f);

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
            lr.positionCount = _circleSegments;
            for (int i = 0; i < _circleSegments; i++)
            {
                float angle = (float)i / _circleSegments * Mathf.PI * 2f;
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
            if (!_warningsEnabled && enemyType != EnemyType.Boss)
            {
                return;
            }

            if (duration <= 0)
            {
                duration = enemyType == EnemyType.Boss ? _bossWarningDuration : _defaultWarningDuration;
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
            _activeWarnings.Add(warning);
        }

        private WarningInstance GetPooledWarning()
        {
            foreach (var warning in _warningPool)
            {
                if (!warning.isActive)
                {
                    return warning;
                }
            }

            // Expand pool if needed
            if (_warningPool.Count < _poolSize * 2)
            {
                var newWarning = CreateWarningObject();
                _warningPool.Add(newWarning);
                return newWarning;
            }

            return null;
        }

        private Color GetColorForEnemyType(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Boss:
                    return _bossThreatColor;
                case EnemyType.VoidSphere:
                case EnemyType.ChaosWorm:
                    return _highThreatColor;
                case EnemyType.UFO:
                case EnemyType.CrystalShard:
                case EnemyType.Fizzer:
                    return _mediumThreatColor;
                default:
                    return _lowThreatColor;
            }
        }

        private void Update()
        {
            for (int i = _activeWarnings.Count - 1; i >= 0; i--)
            {
                var warning = _activeWarnings[i];
                warning.elapsed += Time.deltaTime;

                float progress = warning.elapsed / warning.duration;

                if (progress >= 1f)
                {
                    // Warning complete
                    warning.isActive = false;
                    warning.gameObject.SetActive(false);
                    _activeWarnings.RemoveAt(i);
                    continue;
                }

                // Animate warning
                AnimateWarning(warning, progress);
            }
        }

        private void AnimateWarning(WarningInstance warning, float progress)
        {
            // Pulsing scale
            float pulse = Mathf.Sin(warning.elapsed * _pulseSpeed) * 0.5f + 0.5f;
            float scale = Mathf.Lerp(_pulseMinScale, _pulseMaxScale, pulse);

            warning.circleRenderer.transform.localScale = Vector3.one * scale;

            // Inner circle pulses opposite
            float innerPulse = Mathf.Sin(warning.elapsed * _pulseSpeed + Mathf.PI) * 0.5f + 0.5f;
            float innerScale = Mathf.Lerp(_pulseMinScale * 0.5f, _pulseMaxScale * 0.6f, innerPulse);
            warning.innerCircle.transform.localScale = Vector3.one * innerScale;

            // Exclamation bobs and flashes
            float bob = Mathf.Sin(warning.elapsed * _pulseSpeed * 2f) * 0.1f;
            warning.exclamationMark.transform.localPosition = new Vector3(0, bob, 0);

            // Fade in quickly, then pulse brightness
            float alpha = Mathf.Min(1f, progress * 4f); // Quick fade in
            float brightness = 0.7f + Mathf.Sin(warning.elapsed * _pulseSpeed * 1.5f) * 0.3f;

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
            foreach (var warning in _activeWarnings)
            {
                warning.isActive = false;
                warning.gameObject.SetActive(false);
            }
            _activeWarnings.Clear();
        }
    }
}
