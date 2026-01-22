using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// SpeedUp pickup visuals - Green with motion blur speed lines.
    /// Based on original TypeScript SpeedUp.ts visuals.
    /// Very fast rotation and extra particles for "fizzy" effect.
    /// Colors: Deep Emerald (0x00AA44), Jade (0x00DD55) for speed lines
    /// </summary>
    public class SpeedUpVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color _primaryColor = new Color(0f, 0.67f, 0.27f, 0.9f); // Deep Emerald
        [SerializeField] private Color _speedLineColor = new Color(0f, 0.87f, 0.33f, 0.6f); // Jade
        [SerializeField] private Color _tertiaryColor = new Color(0f, 0.6f, 0.2f, 0.7f); // Forest

        [Header("Scale")]
        [SerializeField] private float _radius = 0.56f;

        // Components
        private Transform _mainGlow;
        private Transform _outerRing;
        private Transform _innerRing;
        private Transform _letter;
        private Transform[] _speedLines;
        private Transform[] _particles;

        private float _time;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Main glow circle
            _mainGlow = CreateCircle("MainGlow", _radius, _primaryColor, 5);

            // Outer ring
            _outerRing = CreateRing("OuterRing", _radius * 1.1f, _radius * 1.45f, _tertiaryColor, 3);

            // Inner ring
            _innerRing = CreateRing("InnerRing", _radius * 0.78f, _radius * 0.94f, _speedLineColor, 7);

            // Letter 'S'
            CreateLetter();

            // Speed lines (3 pairs = 6 total)
            CreateSpeedLines();

            // Extra particles (15 for fizzy effect)
            CreateParticles();
        }

        private void CreateLetter()
        {
            var letterGO = new GameObject("Letter");
            letterGO.transform.SetParent(transform, false);

            var sr = letterGO.AddComponent<SpriteRenderer>();
            sr.sprite = CreateLetterSprite();
            sr.color = Color.white;
            sr.sortingOrder = 12;

            letterGO.transform.localScale = Vector3.one * 0.4f;
            _letter = letterGO.transform;
        }

        private void CreateSpeedLines()
        {
            _speedLines = new Transform[6];
            float[] lengths = { 0.375f, 0.315f, 0.255f };

            for (int i = 0; i < 6; i++)
            {
                var line = new GameObject($"SpeedLine{i}");
                line.transform.SetParent(transform, false);

                var sr = line.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSpeedLineSprite();
                sr.color = new Color(_speedLineColor.r, _speedLineColor.g, _speedLineColor.b,
                    0.4f + (i % 3) * 0.1f);
                sr.sortingOrder = 2;

                // Position: 3 on each side, trailing behind
                int pair = i / 2;
                bool isTop = (i % 2 == 0);
                float yOffset = isTop ? 0.15f + pair * 0.12f : -0.15f - pair * 0.12f;
                float xOffset = -_radius * 1.2f - pair * 0.1f;

                line.transform.localPosition = new Vector3(xOffset, yOffset, 0);
                line.transform.localScale = new Vector3(lengths[pair], 0.05f, 1f);

                _speedLines[i] = line.transform;
            }
        }

        private void CreateParticles()
        {
            _particles = new Transform[15];

            for (int i = 0; i < 15; i++)
            {
                var particle = new GameObject($"Particle{i}");
                particle.transform.SetParent(transform, false);

                var sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(8);
                sr.color = (i % 2 == 0) ? _primaryColor : _speedLineColor;
                sr.sortingOrder = 15;

                particle.transform.localScale = Vector3.one * 0.04f;
                _particles[i] = particle.transform;
            }
        }

        private void Update()
        {
            if (_mainGlow == null) return;

            _time += Time.deltaTime;

            // VERY FAST rotation (4.0 rad/s = ~229 deg/s)
            transform.Rotate(0, 0, Time.deltaTime * 229f);

            // Very fast pulsing
            float pulse = 1f + Mathf.Sin(_time * 12f) * 0.18f;
            _mainGlow.localScale = Vector3.one * _radius * 2f * pulse;

            // Ring pulses
            if (_outerRing != null)
            {
                float outerPulse = 1f + Mathf.Sin(_time * 10f) * 0.12f;
                _outerRing.localScale = Vector3.one * _radius * 2.9f * outerPulse;
            }

            if (_innerRing != null)
            {
                float innerPulse = 1f + Mathf.Sin(_time * 11f + 0.5f) * 0.14f;
                _innerRing.localScale = Vector3.one * _radius * 1.88f * innerPulse;
            }

            // Letter counter-rotation
            if (_letter != null)
            {
                _letter.rotation = Quaternion.identity;
            }

            // Speed lines animation
            if (_speedLines != null)
            {
                for (int i = 0; i < _speedLines.Length; i++)
                {
                    if (_speedLines[i] == null) continue;

                    // Keep speed lines behind (counter-rotate)
                    _speedLines[i].rotation = Quaternion.identity;

                    // Pulse opacity
                    var sr = _speedLines[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        float alpha = 0.3f + Mathf.Sin(_time * 15f + i) * 0.2f;
                        sr.color = new Color(_speedLineColor.r, _speedLineColor.g, _speedLineColor.b, alpha);
                    }

                    // Slight length pulse
                    int pair = i / 2;
                    float[] baseLengths = { 0.375f, 0.315f, 0.255f };
                    float lengthPulse = baseLengths[pair] * (1f + Mathf.Sin(_time * 18f + i) * 0.15f);
                    _speedLines[i].localScale = new Vector3(lengthPulse, 0.05f, 1f);
                }
            }

            // Rapid orbiting particles (6 rad/s = ~344 deg/s)
            if (_particles != null)
            {
                for (int i = 0; i < _particles.Length; i++)
                {
                    if (_particles[i] == null) continue;

                    float angle = _time * 6f + (i / (float)_particles.Length) * Mathf.PI * 2f;
                    float dist = _radius * 1.5f + Mathf.Sin(_time * 8f + i) * 0.1f;
                    _particles[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * dist,
                        Mathf.Sin(angle) * dist,
                        0
                    );

                    // Rapid particle pulse
                    float pScale = 0.03f + Mathf.Sin(_time * 15f + i * 0.7f) * 0.02f;
                    _particles[i].localScale = Vector3.one * pScale;
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

        private Sprite CreateSpeedLineSprite()
        {
            int w = 32, h = 8;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Fade from left to right
                    float t = (float)x / w;
                    float alpha = 1f - t;
                    // Also fade at edges vertically
                    float yFade = 1f - Mathf.Abs((y - h / 2f) / (h / 2f));
                    pixels[y * w + x] = new Color(1, 1, 1, alpha * yFade);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(1f, 0.5f), w);
        }

        private Sprite CreateLetterSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;

            // Draw 'S' shape
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / (size * 0.4f);
                    float ny = (y - center) / (size * 0.4f);

                    bool inLetter = false;
                    // Top curve
                    if (ny > 0.2f && ny < 0.9f && Mathf.Abs(nx) < 0.5f)
                    {
                        if (ny > 0.6f && (nx > -0.4f || Mathf.Abs(ny - 0.75f) < 0.15f))
                            inLetter = true;
                    }
                    // Middle
                    if (Mathf.Abs(ny) < 0.25f && Mathf.Abs(nx) < 0.4f)
                        inLetter = true;
                    // Bottom curve
                    if (ny < -0.2f && ny > -0.9f && Mathf.Abs(nx) < 0.5f)
                    {
                        if (ny < -0.6f && (nx < 0.4f || Mathf.Abs(ny + 0.75f) < 0.15f))
                            inLetter = true;
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
