using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// DataMite visuals - Classic 80s Asteroids wireframe style!
    /// Orange glowing circle with wireframe, aura, core, and rotating spikes.
    /// </summary>
    public class DataMiteVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color m_bodyColor = new Color(1f, 0.27f, 0f, 0.6f); // #FF4400
        [SerializeField] private Color m_wireframeColor = new Color(1f, 0.4f, 0f, 0.9f); // #FF6600
        [SerializeField] private Color m_glowColor = new Color(1f, 0.13f, 0f, 0.4f); // #FF2200
        [SerializeField] private Color m_coreColor = new Color(1f, 1f, 1f, 0.9f); // White

        [Header("Scale")]
        [SerializeField] private float m_radius = 0.56f; // Visual component radius
        [SerializeField] private float m_scale = 0.75f; // Overall transform scale (25% smaller = 0.75)

        // Visual components
        private Transform m_body;
        private Transform m_wireframe;
        private Transform m_glow;
        private Transform m_aura;
        private Transform m_core;
        private Transform m_spikesContainer;
        private SpriteRenderer[] m_allRenderers;

        private float m_time;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Main body (filled circle)
            m_body = CreateCircle("Body", m_radius, m_bodyColor, 10);

            // Wireframe outline
            m_wireframe = CreateRing("Wireframe", m_radius * 0.95f, m_radius, m_wireframeColor, 11);

            // Outer glow
            m_glow = CreateCircle("Glow", m_radius * 1.25f, m_glowColor, 5);

            // Pulsing aura ring
            m_aura = CreateRing("Aura", m_radius, m_radius * 1.25f, new Color(m_bodyColor.r, m_bodyColor.g, m_bodyColor.b, 0.3f), 6);

            // Inner core
            m_core = CreateCircle("Core", m_radius * 0.3f, m_coreColor, 15);

            // Rotating spikes
            CreateSpikes();

            m_allRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void CreateSpikes()
        {
            m_spikesContainer = new GameObject("Spikes").transform;
            m_spikesContainer.SetParent(transform, false);

            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                var spike = CreateSpike($"Spike{i}", angle);
                spike.SetParent(m_spikesContainer, false);
            }
        }

        private Transform CreateSpike(string name, float angle)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateTriangleSprite();
            sr.color = m_wireframeColor;
            sr.sortingOrder = 12;

            float dist = m_radius * 0.8f;
            go.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * dist,
                Mathf.Sin(angle) * dist,
                0
            );
            go.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg - 90);
            go.transform.localScale = new Vector3(0.1f, 0.15f, 1f);

            return go.transform;
        }

        private void Update()
        {
            if (m_body == null) return;

            m_time += Time.deltaTime;

            // Pulse scale (incorporate m_scale so it actually affects size)
            float pulse = 1f + Mathf.Sin(m_time * 4f) * 0.08f;
            transform.localScale = Vector3.one * m_scale * pulse;

            // Rotate wireframe
            if (m_wireframe != null)
            {
                m_wireframe.Rotate(0, 0, Time.deltaTime * 90f);
            }

            // Pulse glow opacity
            if (m_glow != null)
            {
                var sr = m_glow.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = 0.3f + Mathf.Sin(m_time * 3f) * 0.15f;
                    sr.color = new Color(m_glowColor.r, m_glowColor.g, m_glowColor.b, alpha);
                }
            }

            // Pulse aura
            if (m_aura != null)
            {
                float auraScale = 1f + Mathf.Sin(m_time * 5f) * 0.1f;
                m_aura.localScale = Vector3.one * auraScale;
            }

            // Pulse core
            if (m_core != null)
            {
                float coreScale = 1f + Mathf.Sin(m_time * 6f) * 0.15f;
                m_core.localScale = Vector3.one * coreScale;
            }

            // Rotate spikes
            if (m_spikesContainer != null)
            {
                m_spikesContainer.Rotate(0, 0, Time.deltaTime * 120f);
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
                    float alpha = dist < radius ? 1f : 0f;
                    // Soft edge
                    if (dist > radius - 2 && dist < radius)
                        alpha = 1f - (dist - (radius - 2)) / 2f;
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

        private Sprite CreateTriangleSprite()
        {
            int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Simple triangle pointing up
            for (int y = 0; y < size; y++)
            {
                int halfWidth = (size - y) / 2;
                int startX = size / 2 - halfWidth;
                int endX = size / 2 + halfWidth;
                for (int x = startX; x <= endX; x++)
                {
                    if (x >= 0 && x < size)
                        pixels[y * size + x] = Color.white;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
