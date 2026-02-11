using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// UFO visuals - Classic flying saucer with dome, running lights, and tractor beam!
    /// Gray metallic saucer with cyan accents and colored running lights.
    /// </summary>
    public class UFOVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color m_bodyColor = new Color(0.27f, 0.33f, 0.4f, 0.9f); // Gray-blue
        [SerializeField] private Color m_wireframeColor = new Color(0f, 1f, 1f, 0.7f); // Cyan
        [SerializeField] private Color m_domeColor = new Color(0.53f, 0.67f, 1f, 0.7f); // Light blue
        [SerializeField] private Color m_bottomGlowColor = new Color(0f, 1f, 0.53f, 0.5f); // Green
        [SerializeField] private Color m_laserColor = new Color(1f, 0f, 0f, 0.8f); // Red

        [Header("Scale")]
        [SerializeField] private float m_saucerRadius = 0.55f;

        // Components
        private Transform m_saucer;
        private Transform m_wireframe;
        private Transform m_dome;
        private Transform m_domeWireframe;
        private Transform m_bottomGlow;
        private Transform[] m_runningLights;
        private Transform m_laser;
        private SpriteRenderer m_laserRenderer;

        private float m_time;
        private int m_currentLight = 0;
        private float m_lightTimer = 0f;
        private bool m_laserActive = false;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Main saucer body (ellipse)
            m_saucer = CreateEllipse("Saucer", m_saucerRadius, m_saucerRadius * 0.3f, m_bodyColor, 10);

            // Wireframe outline
            m_wireframe = CreateEllipseOutline("Wireframe", m_saucerRadius * 1.05f, m_saucerRadius * 0.35f, m_wireframeColor, 11);

            // Top dome
            m_dome = CreateDome("Dome", m_saucerRadius * 0.5f, m_domeColor, 15);
            m_dome.localPosition = new Vector3(0, m_saucerRadius * 0.15f, 0);

            // Dome wireframe
            m_domeWireframe = CreateDomeOutline("DomeWireframe", m_saucerRadius * 0.52f, m_wireframeColor * 0.7f, 16);
            m_domeWireframe.localPosition = new Vector3(0, m_saucerRadius * 0.15f, 0);

            // Bottom glow (tractor beam area)
            m_bottomGlow = CreateCircle("BottomGlow", m_saucerRadius * 0.6f, m_bottomGlowColor, 5);
            m_bottomGlow.localPosition = new Vector3(0, -m_saucerRadius * 0.1f, 0);

            // Running lights around edge
            CreateRunningLights();

            // Laser beam (initially hidden)
            CreateLaser();
        }

        private void CreateRunningLights()
        {
            m_runningLights = new Transform[12];
            Color[] lightColors = new Color[]
            {
                Color.red, Color.red, Color.red, Color.red,
                Color.green, Color.green, Color.green, Color.green,
                new Color(0f, 0.53f, 1f), new Color(0f, 0.53f, 1f), new Color(0f, 0.53f, 1f), new Color(0f, 0.53f, 1f)
            };

            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * Mathf.PI * 2f;

                var light = new GameObject($"Light{i}");
                light.transform.SetParent(transform, false);

                var sr = light.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(16);
                sr.color = lightColors[i];
                sr.sortingOrder = 12;

                light.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * m_saucerRadius * 0.85f,
                    Mathf.Sin(angle) * m_saucerRadius * 0.15f,
                    0
                );
                light.transform.localScale = Vector3.one * 0.08f;

                m_runningLights[i] = light.transform;
            }
        }

        private void CreateLaser()
        {
            var laser = new GameObject("Laser");
            laser.transform.SetParent(transform, false);

            m_laserRenderer = laser.AddComponent<SpriteRenderer>();
            m_laserRenderer.sprite = CreateLaserSprite();
            m_laserRenderer.color = m_laserColor;
            m_laserRenderer.sortingOrder = 3;

            laser.transform.localPosition = new Vector3(0, -m_saucerRadius * 0.5f, 0);
            laser.transform.localScale = new Vector3(0.15f, 0f, 1f); // Start with no height

            m_laser = laser.transform;
        }

        private void Update()
        {
            if (m_saucer == null) return;

            m_time += Time.deltaTime;

            // Gentle hover motion
            float hover = Mathf.Sin(m_time * 2f) * 0.05f;
            transform.localPosition = new Vector3(transform.localPosition.x, hover, transform.localPosition.z);

            // Dome opacity pulse
            if (m_dome != null)
            {
                var sr = m_dome.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = 0.5f + Mathf.Sin(m_time * 3f) * 0.2f;
                    sr.color = new Color(m_domeColor.r, m_domeColor.g, m_domeColor.b, alpha);
                }
            }

            // Bottom glow pulse
            if (m_bottomGlow != null)
            {
                float glowPulse = 1f + Mathf.Sin(m_time * 4f) * 0.2f;
                m_bottomGlow.localScale = Vector3.one * m_saucerRadius * 1.2f * glowPulse;
            }

            // Running lights sequence
            AnimateRunningLights();

            // Animate laser if active
            if (m_laserActive && m_laser != null)
            {
                float laserLength = 2f + Mathf.Sin(m_time * 10f) * 0.3f;
                m_laser.localScale = new Vector3(0.15f + Mathf.Sin(m_time * 15f) * 0.03f, laserLength, 1f);

                // Laser flicker
                if (m_laserRenderer != null)
                {
                    float alpha = 0.6f + Mathf.Sin(m_time * 20f) * 0.3f;
                    m_laserRenderer.color = new Color(m_laserColor.r, m_laserColor.g, m_laserColor.b, alpha);
                }
            }
        }

        private void AnimateRunningLights()
        {
            m_lightTimer += Time.deltaTime;
            if (m_lightTimer > 0.1f)
            {
                m_lightTimer = 0f;
                m_currentLight = (m_currentLight + 1) % 12;
            }

            for (int i = 0; i < m_runningLights.Length; i++)
            {
                if (m_runningLights[i] == null) continue;
                var sr = m_runningLights[i].GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                // Brighten current light, dim others
                float brightness = (i == m_currentLight) ? 1f : 0.3f;
                Color c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, brightness);

                // Scale pulse for current light
                float scale = (i == m_currentLight) ? 0.12f : 0.08f;
                m_runningLights[i].localScale = Vector3.one * scale;
            }
        }

        public void SetLaserActive(bool active)
        {
            m_laserActive = active;
            if (m_laser != null)
            {
                m_laser.gameObject.SetActive(active);
            }
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        private Transform CreateCircle(string name, float radius, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(32);
            sr.color = color;
            sr.sortingOrder = sortOrder;
            go.transform.localScale = Vector3.one * radius * 2f;
            return go.transform;
        }

        private Transform CreateEllipse(string name, float width, float height, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(32);
            sr.color = color;
            sr.sortingOrder = sortOrder;
            go.transform.localScale = new Vector3(width * 2f, height * 2f, 1f);
            return go.transform;
        }

        private Transform CreateEllipseOutline(string name, float width, float height, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRingSprite(32, 0.85f);
            sr.color = color;
            sr.sortingOrder = sortOrder;
            go.transform.localScale = new Vector3(width * 2f, height * 2f, 1f);
            return go.transform;
        }

        private Transform CreateDome(string name, float radius, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDomeSprite();
            sr.color = color;
            sr.sortingOrder = sortOrder;
            go.transform.localScale = Vector3.one * radius * 2f;
            return go.transform;
        }

        private Transform CreateDomeOutline(string name, float radius, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDomeOutlineSprite();
            sr.color = color;
            sr.sortingOrder = sortOrder;
            go.transform.localScale = Vector3.one * radius * 2f;
            return go.transform;
        }

        private Sprite CreateCircleSprite(int resolution)
        {
            int size = resolution;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = dist < radius ? 1f : 0f;
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateRingSprite(int resolution, float innerRatio)
        {
            int size = resolution;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float outerRadius = size / 2f - 1;
            float innerRadius = outerRadius * innerRatio;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = (dist < outerRadius && dist > innerRadius) ? 1f : 0f;
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateDomeSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    // Only top half
                    float alpha = (dist < radius && y >= center) ? 1f : 0f;
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateDomeOutlineSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float outerRadius = size / 2f - 1;
            float innerRadius = outerRadius * 0.85f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    // Only top half, outline
                    float alpha = (dist < outerRadius && dist > innerRadius && y >= center) ? 1f : 0f;
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateLaserSprite()
        {
            int w = 8, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w / 2f) / (w / 2f);
                    // Gaussian falloff from center
                    float alpha = Mathf.Exp(-dx * dx * 3f) * (1f - t * 0.5f);
                    pixels[y * w + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), h);
        }
    }
}
