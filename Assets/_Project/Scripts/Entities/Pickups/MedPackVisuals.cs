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
        [SerializeField] private Color _baseColor = new Color(0f, 1f, 0f, 0.9f); // Bright green
        [SerializeField] private Color _glowColor = new Color(0f, 1f, 0f, 0.6f); // Green glow
        [SerializeField] private Color _crossColor = new Color(1f, 1f, 1f, 0.95f); // White cross

        [Header("Scale")]
        [SerializeField] private float _radius = 0.5f;

        // Components
        private Transform _glow;
        private Transform _outerGlow;
        private Transform _crossVertical;
        private Transform _crossHorizontal;
        private Transform _wireframe;
        private Transform[] _particles;

        private float _time;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Outer glow sphere
            _outerGlow = CreateCircle("OuterGlow", _radius * 1.25f,
                new Color(_glowColor.r, _glowColor.g, _glowColor.b, 0.3f), 1);

            // Main glow sphere
            _glow = CreateCircle("Glow", _radius, _glowColor, 5);

            // Cross shape - vertical bar
            _crossVertical = CreateRect("CrossVertical", 0.15f, 0.56f, _crossColor, 10);

            // Cross shape - horizontal bar
            _crossHorizontal = CreateRect("CrossHorizontal", 0.56f, 0.15f, _crossColor, 10);

            // Wireframe outline
            _wireframe = CreateRing("Wireframe", _radius * 0.9f, _radius,
                new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.7f), 8);

            // Orbiting particles
            CreateParticles();
        }

        private void CreateParticles()
        {
            _particles = new Transform[10];

            for (int i = 0; i < 10; i++)
            {
                var particle = new GameObject($"Particle{i}");
                particle.transform.SetParent(transform, false);

                var sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(8);
                sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.8f);
                sr.sortingOrder = 15;

                particle.transform.localScale = Vector3.one * 0.06f;
                _particles[i] = particle.transform;
            }
        }

        private void Update()
        {
            if (_glow == null) return;

            _time += Time.deltaTime;

            // Pulsing scale
            float pulse = 0.85f + Mathf.Sin(_time * 4f) * 0.2f;
            _glow.localScale = Vector3.one * _radius * 2f * pulse;

            // Outer glow pulse
            if (_outerGlow != null)
            {
                float outerPulse = 1f + Mathf.Sin(_time * 3f) * 0.15f;
                _outerGlow.localScale = Vector3.one * _radius * 2.5f * outerPulse;
            }

            // Cross glow pulse
            if (_crossVertical != null)
            {
                float crossAlpha = 0.7f + Mathf.Sin(_time * 5f) * 0.25f;
                var sr = _crossVertical.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(_crossColor.r, _crossColor.g, _crossColor.b, crossAlpha);
                }
            }
            if (_crossHorizontal != null)
            {
                float crossAlpha = 0.7f + Mathf.Sin(_time * 5f) * 0.25f;
                var sr = _crossHorizontal.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(_crossColor.r, _crossColor.g, _crossColor.b, crossAlpha);
                }
            }

            // Wireframe rotation
            if (_wireframe != null)
            {
                _wireframe.Rotate(0, 0, Time.deltaTime * 30f);
            }

            // Orbiting particles
            if (_particles != null)
            {
                for (int i = 0; i < _particles.Length; i++)
                {
                    if (_particles[i] == null) continue;

                    float angle = _time * 2f + (i / (float)_particles.Length) * Mathf.PI * 2f;
                    float dist = _radius * 1.25f;
                    _particles[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * dist,
                        Mathf.Sin(angle) * dist,
                        0
                    );

                    // Particle pulse
                    float pAlpha = 0.5f + Mathf.Sin(_time * 6f + i) * 0.3f;
                    var sr = _particles[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, pAlpha);
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
