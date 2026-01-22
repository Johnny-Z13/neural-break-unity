using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// VoidSphere visuals - Massive black hole with purple energy rings!
    /// Dark core with rotating rings, tendrils, and gravity particles.
    /// </summary>
    public class VoidSphereVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color _coreColor = new Color(0f, 0f, 0f, 0.98f); // Black
        [SerializeField] private Color _innerColor = new Color(0.07f, 0f, 0.13f, 0.9f); // Dark purple
        [SerializeField] private Color _ringColor = new Color(0.4f, 0f, 0.53f, 0.7f); // Purple
        [SerializeField] private Color _tendrilColor = new Color(0.5f, 0f, 1f, 0.6f); // Bright purple
        [SerializeField] private Color _particleColor = new Color(1f, 0f, 1f, 0.8f); // Magenta

        [Header("Scale")]
        [SerializeField] private float _coreRadius = 2.4f; // Extra Large!

        // Components
        private Transform _core;
        private Transform _inner;
        private Transform _distortion;
        private Transform[] _rings;
        private Transform[] _tendrils;
        private Transform[] _particles;

        private float _time;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Outer distortion field
            _distortion = CreateCircle("Distortion", _coreRadius * 2.5f,
                new Color(_ringColor.r, _ringColor.g, _ringColor.b, 0.2f), 1);

            // Main void core (black)
            _core = CreateCircle("Core", _coreRadius, _coreColor, 10);

            // Inner purple glow
            _inner = CreateCircle("Inner", _coreRadius * 0.75f, _innerColor, 11);

            // Energy rings
            CreateRings();

            // Energy tendrils
            CreateTendrils();

            // Gravity particles
            CreateParticles();
        }

        private void CreateRings()
        {
            _rings = new Transform[7];
            for (int i = 0; i < 7; i++)
            {
                float ringRadius = _coreRadius * (1.3f + i * 0.3f);
                float hue = 0.8f + i * 0.03f;
                Color ringCol = Color.HSVToRGB(hue, 0.8f, 0.6f);
                ringCol.a = 0.5f - i * 0.05f;

                var ring = CreateRing($"Ring{i}", ringRadius * 0.9f, ringRadius, ringCol, 5 + i);
                _rings[i] = ring;
            }
        }

        private void CreateTendrils()
        {
            _tendrils = new Transform[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * Mathf.PI * 2f;

                var tendril = new GameObject($"Tendril{i}");
                tendril.transform.SetParent(transform, false);

                var sr = tendril.AddComponent<SpriteRenderer>();
                sr.sprite = CreateTendrilSprite();
                sr.color = _tendrilColor;
                sr.sortingOrder = 8;

                tendril.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * _coreRadius * 1.5f,
                    Mathf.Sin(angle) * _coreRadius * 1.5f,
                    0
                );
                tendril.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg + 90);
                tendril.transform.localScale = new Vector3(0.15f, 1f, 1f);

                _tendrils[i] = tendril.transform;
            }
        }

        private void CreateParticles()
        {
            _particles = new Transform[30];
            for (int i = 0; i < 30; i++)
            {
                var particle = new GameObject($"Particle{i}");
                particle.transform.SetParent(transform, false);

                var sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(8);

                // Vary colors between purple, pink, and cyan
                float hue = Random.Range(0.75f, 0.9f);
                Color col = Color.HSVToRGB(hue, 0.7f, 0.8f);
                col.a = Random.Range(0.5f, 0.9f);
                sr.color = col;
                sr.sortingOrder = 15;

                // Random orbital position
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(_coreRadius * 1.5f, _coreRadius * 4f);
                particle.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * dist,
                    Mathf.Sin(angle) * dist,
                    0
                );
                particle.transform.localScale = Vector3.one * Random.Range(0.05f, 0.15f);

                _particles[i] = particle.transform;
            }
        }

        private void Update()
        {
            if (_core == null) return;

            _time += Time.deltaTime;

            // Core breathing
            float corePulse = 1f + Mathf.Sin(_time * 2f) * 0.05f;
            _core.localScale = Vector3.one * _coreRadius * 2f * corePulse;

            // Inner pulse
            if (_inner != null)
            {
                float innerPulse = 1f + Mathf.Sin(_time * 3f) * 0.1f;
                _inner.localScale = Vector3.one * _coreRadius * 1.5f * innerPulse;
            }

            // Distortion breathing
            if (_distortion != null)
            {
                float distPulse = 1f + Mathf.Sin(_time * 1.5f) * 0.15f;
                _distortion.localScale = Vector3.one * _coreRadius * 5f * distPulse;
            }

            // Rotate rings
            if (_rings != null)
            {
                for (int i = 0; i < _rings.Length; i++)
                {
                    if (_rings[i] == null) continue;
                    float speed = 30f + i * 10f;
                    float direction = (i % 2 == 0) ? 1f : -1f;
                    _rings[i].Rotate(0, 0, Time.deltaTime * speed * direction);

                    // Ring opacity pulse
                    var sr = _rings[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        float alpha = 0.3f + Mathf.Sin(_time * 2f + i * 0.5f) * 0.2f;
                        Color c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }
                }
            }

            // Animate tendrils
            if (_tendrils != null)
            {
                for (int i = 0; i < _tendrils.Length; i++)
                {
                    if (_tendrils[i] == null) continue;
                    float angle = (i / 6f) * Mathf.PI * 2f + _time * 0.5f;
                    float dist = _coreRadius * (1.5f + Mathf.Sin(_time * 2f + i) * 0.3f);

                    _tendrils[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * dist,
                        Mathf.Sin(angle) * dist,
                        0
                    );
                    _tendrils[i].localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg + 90);

                    // Tendril length pulse
                    float length = 0.8f + Mathf.Sin(_time * 3f + i * 0.5f) * 0.3f;
                    _tendrils[i].localScale = new Vector3(0.15f, length, 1f);
                }
            }

            // Spiral particles inward
            if (_particles != null)
            {
                for (int i = 0; i < _particles.Length; i++)
                {
                    if (_particles[i] == null) continue;

                    Vector3 pos = _particles[i].localPosition;
                    float dist = pos.magnitude;
                    float angle = Mathf.Atan2(pos.y, pos.x);

                    // Spiral inward
                    angle += Time.deltaTime * (3f / Mathf.Max(dist, 0.5f));
                    dist -= Time.deltaTime * 0.5f;

                    // Reset if too close
                    if (dist < _coreRadius * 0.5f)
                    {
                        dist = _coreRadius * 4f;
                        angle = Random.Range(0f, Mathf.PI * 2f);
                    }

                    _particles[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * dist,
                        Mathf.Sin(angle) * dist,
                        0
                    );
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

        private Sprite CreateTendrilSprite()
        {
            int w = 8, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                float width = (1f - t) * w * 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w / 2f);
                    float alpha = dx < width ? (1f - t) : 0f;
                    pixels[y * w + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);
        }
    }
}
