using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Invulnerable pickup visuals - 5-pointed star with rotating rings and orbiting particles.
    /// Based on original TypeScript Invulnerable.ts visuals.
    /// Color: Bright green (0x00FF00)
    /// </summary>
    public class InvulnerableVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color _starColor = new Color(0f, 1f, 0f, 0.9f); // Bright green
        [SerializeField] private Color _glowColor = new Color(0f, 1f, 0f, 0.6f); // Green glow
        [SerializeField] private Color _ringColor = new Color(0f, 1f, 0f, 0.5f); // Ring color

        [Header("Scale")]
        [SerializeField] private float _radius = 0.5f;

        // Components
        private Transform _star;
        private Transform _innerGlow;
        private Transform _ring1;
        private Transform _ring2;
        private Transform _outerHalo;
        private Transform[] _particles;

        private float _time;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Outer halo glow
            _outerHalo = CreateCircle("OuterHalo", _radius * 2.4f,
                new Color(_glowColor.r, _glowColor.g, _glowColor.b, 0.25f), 1);

            // Ring 1 (rotates slowly clockwise)
            _ring1 = CreateRing("Ring1", _radius * 1.2f, _radius * 1.4f,
                new Color(_ringColor.r, _ringColor.g, _ringColor.b, 0.6f), 3);

            // Ring 2 (counter-rotates faster)
            _ring2 = CreateRing("Ring2", _radius * 1.5f, _radius * 1.64f,
                new Color(_ringColor.r, _ringColor.g, _ringColor.b, 0.5f), 2);

            // Main star shape
            _star = CreateStar("Star", _radius, _radius * 0.5f, _starColor, 10);

            // Inner glow circle
            _innerGlow = CreateCircle("InnerGlow", _radius * 0.4f,
                new Color(_glowColor.r, _glowColor.g, _glowColor.b, 0.9f), 12);

            // Orbiting particles
            CreateParticles();
        }

        private void CreateParticles()
        {
            _particles = new Transform[12];

            for (int i = 0; i < 12; i++)
            {
                var particle = new GameObject($"Particle{i}");
                particle.transform.SetParent(transform, false);

                var sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(8);
                sr.color = new Color(_starColor.r, _starColor.g, _starColor.b, 0.7f);
                sr.sortingOrder = 8;

                particle.transform.localScale = Vector3.one * 0.08f;
                _particles[i] = particle.transform;
            }
        }

        private void Update()
        {
            if (_star == null) return;

            _time += Time.deltaTime;

            // Star rotation (slow)
            _star.Rotate(0, 0, Time.deltaTime * 115f); // ~2 rad/s

            // Star pulsing scale
            float starPulse = 1f + Mathf.Sin(_time * 4f) * 0.2f;
            _star.localScale = Vector3.one * _radius * 2f * starPulse;

            // Star opacity pulse
            var starSr = _star.GetComponent<SpriteRenderer>();
            if (starSr != null)
            {
                float starAlpha = 0.7f + Mathf.Sin(_time * 5f) * 0.2f;
                starSr.color = new Color(_starColor.r, _starColor.g, _starColor.b, starAlpha);
            }

            // Inner glow rapid pulse
            if (_innerGlow != null)
            {
                float innerPulse = 1f + Mathf.Sin(_time * 8f) * 0.4f;
                _innerGlow.localScale = Vector3.one * _radius * 0.8f * innerPulse;
            }

            // Ring 1 rotation and opacity pulse
            if (_ring1 != null)
            {
                _ring1.Rotate(0, 0, Time.deltaTime * 86f); // ~1.5 rad/s
                var sr = _ring1.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = 0.4f + Mathf.Sin(_time * 4f) * 0.2f;
                    sr.color = new Color(_ringColor.r, _ringColor.g, _ringColor.b, alpha);
                }
            }

            // Ring 2 counter-rotation and opacity pulse
            if (_ring2 != null)
            {
                _ring2.Rotate(0, 0, Time.deltaTime * -143f); // ~-2.5 rad/s
                var sr = _ring2.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = 0.35f + Mathf.Sin(_time * 5f + 1f) * 0.15f;
                    sr.color = new Color(_ringColor.r, _ringColor.g, _ringColor.b, alpha);
                }
            }

            // Outer halo breathing effect
            if (_outerHalo != null)
            {
                float haloPulse = 1f + Mathf.Sin(_time * 2f) * 0.3f;
                _outerHalo.localScale = Vector3.one * _radius * 4.8f * haloPulse;
                var sr = _outerHalo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = 0.15f + Mathf.Sin(_time * 2.5f) * 0.1f;
                    sr.color = new Color(_glowColor.r, _glowColor.g, _glowColor.b, alpha);
                }
            }

            // Floating motion (bob up/down)
            float bob = Mathf.Sin(_time * 2f) * 0.15f;
            transform.localPosition = new Vector3(transform.localPosition.x, bob, transform.localPosition.z);

            // Orbiting particles
            if (_particles != null)
            {
                for (int i = 0; i < _particles.Length; i++)
                {
                    if (_particles[i] == null) continue;

                    float angle = _time * 3f + (i / (float)_particles.Length) * Mathf.PI * 2f;
                    float dist = _radius * 1.8f;
                    _particles[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * dist,
                        Mathf.Sin(angle) * dist,
                        0
                    );

                    // Particle opacity pulse
                    float pAlpha = 0.5f + Mathf.Sin(_time * 6f + i * 0.5f) * 0.3f;
                    var sr = _particles[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = new Color(_starColor.r, _starColor.g, _starColor.b, pAlpha);
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

        private Transform CreateStar(string name, float outerRadius, float innerRadius, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateStarSprite(32, 5, innerRadius / outerRadius);
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
                    float alpha = dist < radius ? 1f - (dist / radius) * 0.5f : 0f;
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

        private Sprite CreateStarSprite(int resolution, int points, float innerRatio)
        {
            int size = resolution;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float outerRadius = size / 2f - 2;
            float innerRadius = outerRadius * innerRatio;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - center;
                    float py = y - center;
                    float angle = Mathf.Atan2(py, px);
                    float dist = Mathf.Sqrt(px * px + py * py);

                    // Star distance function
                    float pointAngle = Mathf.PI * 2f / points;
                    float starAngle = Mathf.Repeat(angle + Mathf.PI / 2f, pointAngle) - pointAngle / 2f;
                    float t = Mathf.Abs(starAngle) / (pointAngle / 2f);
                    float starRadius = Mathf.Lerp(outerRadius, innerRadius, t);

                    if (dist < starRadius)
                    {
                        float alpha = Mathf.Clamp01((starRadius - dist) / 3f);
                        pixels[y * size + x] = new Color(1, 1, 1, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
