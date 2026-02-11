using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// PowerUp visuals - Green glowing circle with rings and orbiting particles.
    /// Used for PowerUp, Shield, SpeedUp, and other weapon pickups.
    /// Based on original TypeScript PowerUp.ts visuals.
    /// Colors: Deep Emerald (0x00AA44), Jade (0x00DD55), Forest (0x009933)
    /// </summary>
    public class PowerUpVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color m_primaryColor = new Color(0f, 0.67f, 0.27f, 0.9f); // Deep Emerald #00AA44
        [SerializeField] private Color m_secondaryColor = new Color(0f, 0.87f, 0.33f, 0.8f); // Jade #00DD55
        [SerializeField] private Color m_tertiaryColor = new Color(0f, 0.6f, 0.2f, 0.7f); // Forest #009933
        [SerializeField] private Color m_letterColor = Color.white;

        [Header("Settings")]
        [SerializeField] private float m_radius = 0.56f;
        [SerializeField] private char m_letter = 'P';
        [SerializeField] private float m_rotationSpeed = 200f; // degrees per second (3.5 rad/s)

        // Components
        private Transform m_mainGlow;
        private Transform m_outerRing;
        private Transform m_innerRing;
        private Transform m_letterObject;
        private Transform[] m_particles;

        private float m_time;

        private void Start()
        {
            GenerateVisuals();
        }

        public void SetLetter(char letter)
        {
            m_letter = letter;
            if (m_letterObject != null)
            {
                var tm = m_letterObject.GetComponent<TextMesh>();
                if (tm != null) tm.text = letter.ToString();
            }
        }

        public void SetColors(Color primary, Color secondary, Color tertiary)
        {
            m_primaryColor = primary;
            m_secondaryColor = secondary;
            m_tertiaryColor = tertiary;
            // Regenerate visuals with new colors
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Main glow circle - Deep Emerald
            m_mainGlow = CreateCircle("MainGlow", m_radius, m_primaryColor, 5);

            // Outer ring - Forest Green
            m_outerRing = CreateRing("OuterRing", m_radius * 1.1f, m_radius * 1.45f, m_tertiaryColor, 3);

            // Inner ring - Jade Green
            m_innerRing = CreateRing("InnerRing", m_radius * 0.78f, m_radius * 0.94f, m_secondaryColor, 7);

            // Letter (using TextMesh for simplicity, or sprite)
            CreateLetter();

            // Orbiting particles (12 alternating emerald/jade)
            CreateParticles();
        }

        private void CreateLetter()
        {
            var letterGO = new GameObject("Letter");
            letterGO.transform.SetParent(transform, false);

            // Use a simple sprite circle as a placeholder for the letter
            // In a full implementation, you'd use TextMeshPro or a font sprite
            var sr = letterGO.AddComponent<SpriteRenderer>();
            sr.sprite = CreateLetterSprite(m_letter);
            sr.color = m_letterColor;
            sr.sortingOrder = 12;

            letterGO.transform.localScale = Vector3.one * 0.4f;
            m_letterObject = letterGO.transform;
        }

        private void CreateParticles()
        {
            m_particles = new Transform[12];

            for (int i = 0; i < 12; i++)
            {
                var particle = new GameObject($"Particle{i}");
                particle.transform.SetParent(transform, false);

                var sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(8);
                // Alternate between emerald and jade
                sr.color = (i % 2 == 0) ? m_primaryColor : m_secondaryColor;
                sr.sortingOrder = 15;

                particle.transform.localScale = Vector3.one * 0.05f;
                m_particles[i] = particle.transform;
            }
        }

        private void Update()
        {
            if (m_mainGlow == null) return;

            m_time += Time.deltaTime;

            // Fast rotation
            transform.Rotate(0, 0, Time.deltaTime * m_rotationSpeed);

            // Dramatic pulsing
            float pulse = 1f + Mathf.Sin(m_time * 8f) * 0.15f;
            m_mainGlow.localScale = Vector3.one * m_radius * 2f * pulse;

            // Outer ring pulse
            if (m_outerRing != null)
            {
                float outerPulse = 1f + Mathf.Sin(m_time * 6f) * 0.1f;
                m_outerRing.localScale = Vector3.one * m_radius * 2.9f * outerPulse;
                var sr = m_outerRing.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = 0.5f + Mathf.Sin(m_time * 7f) * 0.2f;
                    sr.color = new Color(m_tertiaryColor.r, m_tertiaryColor.g, m_tertiaryColor.b, alpha);
                }
            }

            // Inner ring pulse
            if (m_innerRing != null)
            {
                float innerPulse = 1f + Mathf.Sin(m_time * 7f + 0.5f) * 0.12f;
                m_innerRing.localScale = Vector3.one * m_radius * 1.88f * innerPulse;
            }

            // Letter counter-rotation (stay readable)
            if (m_letterObject != null)
            {
                m_letterObject.rotation = Quaternion.identity;
            }

            // Orbiting particles
            if (m_particles != null)
            {
                for (int i = 0; i < m_particles.Length; i++)
                {
                    if (m_particles[i] == null) continue;

                    float angle = m_time * 3f + (i / (float)m_particles.Length) * Mathf.PI * 2f;
                    float dist = m_radius * 1.6f;
                    m_particles[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * dist,
                        Mathf.Sin(angle) * dist,
                        0
                    );

                    // Particle pulse
                    float pScale = 0.04f + Mathf.Sin(m_time * 10f + i * 0.5f) * 0.02f;
                    m_particles[i].localScale = Vector3.one * pScale;
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
                    float alpha = dist < radius ? 1f - (dist / radius) * 0.4f : 0f;
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

        private Sprite CreateLetterSprite(char letter)
        {
            // Create a simple representation of the letter
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;

            // Draw letter shape based on character
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inLetter = false;
                    float nx = (x - center) / (size * 0.4f);
                    float ny = (y - center) / (size * 0.4f);

                    switch (letter)
                    {
                        case 'P':
                            // Vertical bar + top curve
                            inLetter = (Mathf.Abs(nx + 0.3f) < 0.15f) ||
                                      (ny > 0.1f && nx > -0.3f && nx < 0.4f && Mathf.Abs(ny - 0.5f) < 0.25f);
                            break;
                        case 'S':
                            // S-curve approximation
                            inLetter = (ny > 0.3f && Mathf.Abs(nx) < 0.4f && Mathf.Abs(ny - 0.6f) < 0.2f) ||
                                      (ny < -0.3f && Mathf.Abs(nx) < 0.4f && Mathf.Abs(ny + 0.6f) < 0.2f) ||
                                      (Mathf.Abs(ny) < 0.2f && Mathf.Abs(nx) < 0.3f);
                            break;
                        case 'R':
                            // R shape
                            inLetter = (Mathf.Abs(nx + 0.3f) < 0.12f) ||
                                      (ny > 0.2f && nx > -0.3f && nx < 0.35f && Mathf.Abs(ny - 0.55f) < 0.2f) ||
                                      (ny < 0.1f && nx > -0.1f && ny > nx * 1.5f - 0.7f && ny < nx * 1.5f - 0.5f);
                            break;
                        case 'H':
                            // H shape
                            inLetter = (Mathf.Abs(nx + 0.3f) < 0.12f) ||
                                      (Mathf.Abs(nx - 0.3f) < 0.12f) ||
                                      (Mathf.Abs(ny) < 0.12f && Mathf.Abs(nx) < 0.35f);
                            break;
                        case 'I':
                            // I shape
                            inLetter = Mathf.Abs(nx) < 0.12f;
                            break;
                        default:
                            // Generic circle for unknown letters
                            inLetter = Vector2.Distance(new Vector2(nx, ny), Vector2.zero) < 0.5f;
                            break;
                    }

                    pixels[y * size + x] = inLetter ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
