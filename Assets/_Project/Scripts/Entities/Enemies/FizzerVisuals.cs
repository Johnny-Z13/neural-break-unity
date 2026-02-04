using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Fizzer visuals - Tiny electric chaos ball!
    /// Cyan/electric blue core with spikes, orbiting sparks, and pulsing ring.
    /// Changed from green to cyan-blue to avoid green=good confusion.
    /// </summary>
    public class FizzerVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color m_coreColor = new Color(0.2f, 0.8f, 1f, 0.9f); // Electric cyan-blue
        [SerializeField] private Color m_innerColor = new Color(1f, 1f, 1f, 0.95f); // White hot center
        [SerializeField] private Color m_spikeColor = new Color(0f, 1f, 1f, 0.8f); // Cyan
        [SerializeField] private Color m_sparkColor1 = new Color(0.2f, 0.8f, 1f, 0.9f); // Cyan
        [SerializeField] private Color m_sparkColor2 = new Color(0f, 1f, 1f, 0.9f); // Bright cyan
        [SerializeField] private Color m_ringColor = new Color(0.2f, 0.8f, 1f, 0.6f); // Cyan

        [Header("Scale")]
        [SerializeField] private float m_radius = 0.25f;

        // Components
        private Transform m_core;
        private Transform m_inner;
        private Transform m_ring;
        private Transform[] m_spikes;
        private Transform[] m_sparks;
        private SpriteRenderer[] m_allRenderers;

        private float m_time;
        private float[] m_spikePhases;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Core sphere
            m_core = CreateCircle("Core", m_radius, m_coreColor, 10);

            // Inner hot center
            m_inner = CreateCircle("Inner", m_radius * 0.6f, m_innerColor, 12);

            // Pulsing ring
            m_ring = CreateRing("Ring", m_radius * 1.1f, m_radius * 1.3f, m_ringColor, 8);

            // Random direction spikes
            CreateSpikes();

            // Orbiting sparks
            CreateSparks();

            m_allRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void CreateSpikes()
        {
            m_spikes = new Transform[8];
            m_spikePhases = new float[8];

            for (int i = 0; i < 8; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                m_spikePhases[i] = Random.Range(0f, Mathf.PI * 2f);

                var spike = new GameObject($"Spike{i}");
                spike.transform.SetParent(transform, false);

                var sr = spike.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSpikeSprite();
                sr.color = m_spikeColor;
                sr.sortingOrder = 11;

                spike.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * m_radius * 0.7f,
                    Mathf.Sin(angle) * m_radius * 0.7f,
                    0
                );
                spike.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                spike.transform.localScale = new Vector3(0.08f, 0.2f, 1f);

                m_spikes[i] = spike.transform;
            }
        }

        private void CreateSparks()
        {
            m_sparks = new Transform[4];

            for (int i = 0; i < 4; i++)
            {
                var spark = new GameObject($"Spark{i}");
                spark.transform.SetParent(transform, false);

                var sr = spark.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(16);
                sr.color = (i % 2 == 0) ? m_sparkColor1 : m_sparkColor2;
                sr.sortingOrder = 15;

                spark.transform.localScale = Vector3.one * 0.08f;
                m_sparks[i] = spark.transform;
            }
        }

        private void Update()
        {
            if (m_core == null) return;

            m_time += Time.deltaTime;

            // Rapid overall rotation
            transform.Rotate(0, 0, Time.deltaTime * 360f);

            // Fast pulsing scale
            float pulse = 1f + Mathf.Sin(m_time * 12f) * 0.15f;
            m_core.localScale = Vector3.one * m_radius * 2f * pulse;

            // Inner pulse
            if (m_inner != null)
            {
                float innerPulse = 1f + Mathf.Sin(m_time * 15f) * 0.2f;
                m_inner.localScale = Vector3.one * m_radius * 1.2f * innerPulse;
            }

            // Ring rotation and pulse
            if (m_ring != null)
            {
                m_ring.Rotate(0, 0, Time.deltaTime * 180f);
                var sr = m_ring.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = 0.4f + Mathf.Sin(m_time * 10f) * 0.3f;
                    sr.color = new Color(m_ringColor.r, m_ringColor.g, m_ringColor.b, alpha);
                }
            }

            // Spike extension/retraction
            if (m_spikes != null)
            {
                for (int i = 0; i < m_spikes.Length; i++)
                {
                    if (m_spikes[i] == null) continue;
                    float ext = 0.15f + Mathf.Sin(m_time * 8f + m_spikePhases[i]) * 0.1f;
                    m_spikes[i].localScale = new Vector3(0.08f, ext, 1f);
                }
            }

            // Orbiting sparks
            if (m_sparks != null)
            {
                for (int i = 0; i < m_sparks.Length; i++)
                {
                    if (m_sparks[i] == null) continue;
                    float angle = m_time * 6f + (i / 4f) * Mathf.PI * 2f;
                    float dist = m_radius * 1.5f;
                    m_sparks[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * dist,
                        Mathf.Sin(angle) * dist,
                        0
                    );

                    // Spark pulse
                    float sparkPulse = 0.06f + Mathf.Sin(m_time * 20f + i) * 0.03f;
                    m_sparks[i].localScale = Vector3.one * sparkPulse;
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

        private Sprite CreateSpikeSprite()
        {
            int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Lightning bolt shape
            for (int y = 0; y < size; y++)
            {
                int width = Mathf.Max(1, (size - y) / 3);
                int centerX = size / 2;
                for (int x = centerX - width; x <= centerX + width; x++)
                {
                    if (x >= 0 && x < size)
                        pixels[y * size + x] = Color.white;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size);
        }
    }
}
