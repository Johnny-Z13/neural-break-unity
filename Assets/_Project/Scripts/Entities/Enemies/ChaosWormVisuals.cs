using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// ChaosWorm visuals - Rainbow segmented worm with octahedron segments!
    /// Each segment has its own color from the rainbow spectrum.
    /// </summary>
    public class ChaosWormVisuals : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int m_segmentCount = 8;
        [SerializeField] private float m_baseRadius = 0.45f;
        [SerializeField] private float m_taperAmount = 0.03f;
        [SerializeField] private float m_segmentSpacing = 0.6f;

        // Segments
        private Transform[] m_segments;
        private Transform[] m_auras;
        private Transform[] m_cores;
        private SpriteRenderer[] m_segmentRenderers;
        private float[] m_segmentOffsets;

        private float m_time;
        private float m_hueOffset;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            m_segments = new Transform[m_segmentCount];
            m_auras = new Transform[m_segmentCount];
            m_cores = new Transform[m_segmentCount];
            m_segmentRenderers = new SpriteRenderer[m_segmentCount];
            m_segmentOffsets = new float[m_segmentCount];

            for (int i = 0; i < m_segmentCount; i++)
            {
                CreateSegment(i);
                m_segmentOffsets[i] = i * 0.3f; // Wave offset
            }
        }

        private void CreateSegment(int index)
        {
            float segmentRadius = m_baseRadius - (index * m_taperAmount);
            float hue = (index / (float)m_segmentCount);

            // Segment container
            var segment = new GameObject($"Segment{index}");
            segment.transform.SetParent(transform, false);
            segment.transform.localPosition = new Vector3(-index * m_segmentSpacing, 0, 0);
            m_segments[index] = segment.transform;

            // Main body (diamond/octahedron approximation)
            var body = new GameObject("Body");
            body.transform.SetParent(segment.transform, false);
            var bodySr = body.AddComponent<SpriteRenderer>();
            bodySr.sprite = CreateDiamondSprite();
            Color bodyColor = Color.HSVToRGB(hue, 0.9f, 0.6f);
            bodyColor.a = 0.8f;
            bodySr.color = bodyColor;
            bodySr.sortingOrder = 10 + index;
            body.transform.localScale = Vector3.one * segmentRadius * 2f;
            m_segmentRenderers[index] = bodySr;

            // Wireframe
            var wireframe = new GameObject("Wireframe");
            wireframe.transform.SetParent(segment.transform, false);
            var wireSr = wireframe.AddComponent<SpriteRenderer>();
            wireSr.sprite = CreateDiamondOutlineSprite();
            Color wireColor = Color.HSVToRGB(hue, 0.9f, 0.8f);
            wireColor.a = 0.9f;
            wireSr.color = wireColor;
            wireSr.sortingOrder = 11 + index;
            wireframe.transform.localScale = Vector3.one * segmentRadius * 2.1f;

            // Aura ring
            var aura = new GameObject("Aura");
            aura.transform.SetParent(segment.transform, false);
            var auraSr = aura.AddComponent<SpriteRenderer>();
            auraSr.sprite = CreateRingSprite(32, 0.7f);
            Color auraColor = Color.HSVToRGB(hue, 0.8f, 0.7f);
            auraColor.a = 0.4f;
            auraSr.color = auraColor;
            auraSr.sortingOrder = 8 + index;
            aura.transform.localScale = Vector3.one * segmentRadius * 3f;
            m_auras[index] = aura.transform;

            // Core glow
            var core = new GameObject("Core");
            core.transform.SetParent(segment.transform, false);
            var coreSr = core.AddComponent<SpriteRenderer>();
            coreSr.sprite = CreateCircleSprite(16);
            Color coreColor = Color.HSVToRGB(hue, 0.5f, 1f);
            coreColor.a = 0.9f;
            coreSr.color = coreColor;
            coreSr.sortingOrder = 12 + index;
            core.transform.localScale = Vector3.one * segmentRadius * 0.5f;
            m_cores[index] = core.transform;
        }

        private void Update()
        {
            if (m_segments == null || m_segments.Length == 0) return;

            m_time += Time.deltaTime;
            m_hueOffset += Time.deltaTime * 0.1f; // Slow rainbow shift

            // Animate each segment
            for (int i = 0; i < m_segmentCount; i++)
            {
                if (m_segments[i] == null) continue;

                // Wave motion
                float waveY = Mathf.Sin(m_time * 3f + m_segmentOffsets[i]) * 0.2f;
                float waveX = Mathf.Cos(m_time * 2f + m_segmentOffsets[i]) * 0.1f;
                Vector3 basePos = new Vector3(-i * m_segmentSpacing + waveX, waveY, 0);
                m_segments[i].localPosition = basePos;

                // Rotation
                m_segments[i].Rotate(0, 0, Time.deltaTime * (60f + i * 10f));

                // Scale pulse
                float pulse = 1f + Mathf.Sin(m_time * 4f + i * 0.5f) * 0.1f;
                float segmentRadius = m_baseRadius - (i * m_taperAmount);

                // Update body scale
                if (m_segments[i].childCount > 0)
                {
                    m_segments[i].GetChild(0).localScale = Vector3.one * segmentRadius * 2f * pulse;
                }

                // Update colors (rainbow shift)
                if (m_segmentRenderers[i] != null)
                {
                    float hue = ((i / (float)m_segmentCount) + m_hueOffset) % 1f;
                    Color newColor = Color.HSVToRGB(hue, 0.9f, 0.6f);
                    newColor.a = 0.8f;
                    m_segmentRenderers[i].color = newColor;
                }

                // Aura pulse
                if (m_auras[i] != null)
                {
                    float auraPulse = 1f + Mathf.Sin(m_time * 5f + i * 0.3f) * 0.2f;
                    m_auras[i].localScale = Vector3.one * segmentRadius * 3f * auraPulse;
                    m_auras[i].Rotate(0, 0, Time.deltaTime * 30f);
                }

                // Core pulse
                if (m_cores[i] != null)
                {
                    float corePulse = 1f + Mathf.Sin(m_time * 6f + i * 0.4f) * 0.3f;
                    m_cores[i].localScale = Vector3.one * segmentRadius * 0.5f * corePulse;
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

        private Sprite CreateDiamondSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center);
                    float dy = Mathf.Abs(y - center);
                    // Diamond: |x| + |y| < size/2
                    float dist = dx + dy;
                    float alpha = dist < center - 1 ? 1f : 0f;
                    if (dist > center - 3 && dist < center - 1)
                        alpha = (center - 1 - dist) / 2f;
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateDiamondOutlineSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center);
                    float dy = Mathf.Abs(y - center);
                    float dist = dx + dy;
                    // Outline only
                    float alpha = (dist < center - 1 && dist > center - 3) ? 1f : 0f;
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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
    }
}
