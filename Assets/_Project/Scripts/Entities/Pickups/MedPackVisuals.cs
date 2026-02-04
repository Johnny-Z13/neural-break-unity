using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// MedPack visuals - Green cross with glowing sphere and orbiting particles.
    /// Based on original TypeScript MedPack.ts visuals.
    /// Color: Bright green (0x00FF00)
    /// </summary>
    public class MedPackVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color m_baseColor = new Color(0f, 1f, 0f, 0.9f); // Bright green
        [SerializeField] private Color m_glowColor = new Color(0f, 1f, 0f, 0.6f); // Green glow
        [SerializeField] private Color m_crossColor = new Color(1f, 1f, 1f, 0.95f); // White cross

        [Header("Scale")]
        [SerializeField] private float m_radius = 0.5f;

        // Components
        private Transform m_glow;
        private Transform m_outerGlow;
        private Transform m_crossVertical;
        private Transform m_crossHorizontal;
        private Transform m_wireframe;
        private Transform[] m_particles;

        private float m_time;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Outer glow sphere
            m_outerGlow = CreateCircle("OuterGlow", m_radius * 1.25f,
                new Color(m_glowColor.r, m_glowColor.g, m_glowColor.b, 0.3f), 1);

            // Main glow sphere
            m_glow = CreateCircle("Glow", m_radius, m_glowColor, 5);

            // Cross shape - vertical bar
            m_crossVertical = CreateRect("CrossVertical", 0.15f, 0.56f, m_crossColor, 10);

            // Cross shape - horizontal bar
            m_crossHorizontal = CreateRect("CrossHorizontal", 0.56f, 0.15f, m_crossColor, 10);

            // Wireframe outline
            m_wireframe = CreateRing("Wireframe", m_radius * 0.9f, m_radius,
                new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, 0.7f), 8);

            // Orbiting particles
            CreateParticles();
        }

        private void CreateParticles()
        {
            m_particles = new Transform[10];

            for (int i = 0; i < 10; i++)
            {
                var particle = new GameObject($"Particle{i}");
                particle.transform.SetParent(transform, false);

                var sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(8);
                sr.color = new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, 0.8f);
                sr.sortingOrder = 15;

                particle.transform.localScale = Vector3.one * 0.06f;
                m_particles[i] = particle.transform;
            }
        }

        private void Update()
        {
            if (m_glow == null) return;

            m_time += Time.deltaTime;

            // Pulsing scale
            float pulse = 0.85f + Mathf.Sin(m_time * 4f) * 0.2f;
            m_glow.localScale = Vector3.one * m_radius * 2f * pulse;

            // Outer glow pulse
            if (m_outerGlow != null)
            {
                float outerPulse = 1f + Mathf.Sin(m_time * 3f) * 0.15f;
                m_outerGlow.localScale = Vector3.one * m_radius * 2.5f * outerPulse;
            }

            // Cross glow pulse
            if (m_crossVertical != null)
            {
                float crossAlpha = 0.7f + Mathf.Sin(m_time * 5f) * 0.25f;
                var sr = m_crossVertical.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(m_crossColor.r, m_crossColor.g, m_crossColor.b, crossAlpha);
                }
            }
            if (m_crossHorizontal != null)
            {
                float crossAlpha = 0.7f + Mathf.Sin(m_time * 5f) * 0.25f;
                var sr = m_crossHorizontal.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(m_crossColor.r, m_crossColor.g, m_crossColor.b, crossAlpha);
                }
            }

            // Wireframe rotation
            if (m_wireframe != null)
            {
                m_wireframe.Rotate(0, 0, Time.deltaTime * 30f);
            }

            // Orbiting particles
            if (m_particles != null)
            {
                for (int i = 0; i < m_particles.Length; i++)
                {
                    if (m_particles[i] == null) continue;

                    float angle = m_time * 2f + (i / (float)m_particles.Length) * Mathf.PI * 2f;
                    float dist = m_radius * 1.25f;
                    m_particles[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * dist,
                        Mathf.Sin(angle) * dist,
                        0
                    );

                    // Particle pulse
                    float pAlpha = 0.5f + Mathf.Sin(m_time * 6f + i) * 0.3f;
                    var sr = m_particles[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, pAlpha);
                    }
                }
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

        private Transform CreateRect(string name, float width, float height, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(16, 16);
            sr.color = color;
            sr.sortingOrder = sortOrder;
            go.transform.localScale = new Vector3(width, height, 1f);
            return go.transform;
        }

        private Transform CreateRing(string name, float innerRadius, float outerRadius, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRingSprite(32, innerRadius / outerRadius);
            sr.color = color;
            sr.sortingOrder = sortOrder;
            go.transform.localScale = Vector3.one * outerRadius * 2f;
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
                    float alpha = dist < radius ? 1f - (dist / radius) * 0.3f : 0f;
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateRectSprite(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), Mathf.Max(width, height));
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
    }
}
