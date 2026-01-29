using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// CrystalShardSwarm visuals - Prismatic cyan crystal shards orbiting a core!
    /// Central octahedron with orbiting cone shards and lightning effects.
    /// </summary>
    public class CrystalShardVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color _coreColor = new Color(0f, 1f, 1f, 0.6f); // Cyan
        [SerializeField] private Color _wireframeColor = new Color(0f, 1f, 1f, 0.9f); // Bright cyan
        [SerializeField] private Color _shardColor = new Color(0f, 0.8f, 1f, 0.8f); // Light cyan
        [SerializeField] private Color _lightningColor = new Color(0f, 1f, 1f, 0.8f); // Cyan

        [Header("Settings")]
        [SerializeField] private float _coreRadius = 0.4f;
        [SerializeField] private int _shardCount = 6;
        [SerializeField] private float _orbitRadius = 2f;

        // Components
        private Transform _core;
        private Transform _coreWireframe;
        private Transform[] _shards;
        private Transform[] _shardTips;
        private Transform[] _rings;
        private LineRenderer[] _lightning;

        private float _time;
        private float[] _shardAngles;
        private float[] _shardRadii;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            ClearChildren();

            // Central core
            _core = CreateDiamond("Core", _coreRadius, _coreColor, 10);
            _coreWireframe = CreateDiamondOutline("CoreWireframe", _coreRadius * 1.1f, _wireframeColor, 11);

            // Orbiting shards
            CreateShards();

            // Energy rings
            CreateRings();

            // Lightning between shards
            CreateLightning();
        }

        private void CreateShards()
        {
            _shards = new Transform[_shardCount];
            _shardTips = new Transform[_shardCount];
            _shardAngles = new float[_shardCount];
            _shardRadii = new float[_shardCount];

            for (int i = 0; i < _shardCount; i++)
            {
                _shardAngles[i] = (i / (float)_shardCount) * Mathf.PI * 2f;
                _shardRadii[i] = _orbitRadius + Random.Range(-0.3f, 0.3f);

                // Shard container
                var shard = new GameObject($"Shard{i}");
                shard.transform.SetParent(transform, false);

                // Shard body (triangle/cone)
                var body = new GameObject("Body");
                body.transform.SetParent(shard.transform, false);
                var bodySr = body.AddComponent<SpriteRenderer>();
                bodySr.sprite = CreateShardSprite();

                // Prismatic color variation
                float hue = 0.5f + Random.Range(-0.1f, 0.1f);
                Color shardCol = Color.HSVToRGB(hue, 0.7f, 0.9f);
                shardCol.a = 0.8f;
                bodySr.color = shardCol;
                bodySr.sortingOrder = 15;
                body.transform.localScale = new Vector3(0.3f, 0.9f, 1f);

                // Shard wireframe
                var wire = new GameObject("Wireframe");
                wire.transform.SetParent(shard.transform, false);
                var wireSr = wire.AddComponent<SpriteRenderer>();
                wireSr.sprite = CreateShardOutlineSprite();
                wireSr.color = _wireframeColor;
                wireSr.sortingOrder = 16;
                wire.transform.localScale = new Vector3(0.32f, 0.92f, 1f);

                // Glowing tip
                var tip = new GameObject("Tip");
                tip.transform.SetParent(shard.transform, false);
                var tipSr = tip.AddComponent<SpriteRenderer>();
                tipSr.sprite = CreateCircleSprite(16);
                tipSr.color = new Color(1f, 1f, 1f, 0.9f);
                tipSr.sortingOrder = 17;
                tip.transform.localPosition = new Vector3(0, 0.45f, 0);
                tip.transform.localScale = Vector3.one * 0.15f;
                _shardTips[i] = tip.transform;

                _shards[i] = shard.transform;
            }
        }

        private void CreateRings()
        {
            _rings = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                float ringRadius = _orbitRadius * (0.9f + i * 0.15f);
                float hue = 0.5f + i * 0.1f;
                Color ringCol = Color.HSVToRGB(hue, 0.6f, 0.8f);
                ringCol.a = 0.4f - i * 0.1f;

                var ring = CreateRing($"Ring{i}", ringRadius * 0.95f, ringRadius, ringCol, 5 + i);
                _rings[i] = ring;
            }
        }

        private void CreateLightning()
        {
            _lightning = new LineRenderer[_shardCount];

            for (int i = 0; i < _shardCount; i++)
            {
                var lightningGO = new GameObject($"Lightning{i}");
                lightningGO.transform.SetParent(transform, false);

                var lr = lightningGO.AddComponent<LineRenderer>();
                lr.positionCount = 4;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.02f;
                lr.startColor = _lightningColor;
                lr.endColor = new Color(_lightningColor.r, _lightningColor.g, _lightningColor.b, 0.3f);
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.sortingOrder = 12;
                lr.useWorldSpace = false;

                _lightning[i] = lr;
            }
        }

        private void Update()
        {
            if (_core == null) return;

            _time += Time.deltaTime;

            // Core rotation and pulse
            _core.Rotate(0, 0, Time.deltaTime * 60f);
            float corePulse = 1f + Mathf.Sin(_time * 4f) * 0.1f;
            _core.localScale = Vector3.one * _coreRadius * 2f * corePulse;

            if (_coreWireframe != null)
            {
                _coreWireframe.Rotate(0, 0, Time.deltaTime * -45f);
                _coreWireframe.localScale = Vector3.one * _coreRadius * 2.2f * corePulse;
            }

            // Animate shards
            if (_shards != null)
            {
                for (int i = 0; i < _shards.Length; i++)
                {
                    if (_shards[i] == null) continue;

                    // Orbit
                    _shardAngles[i] += Time.deltaTime * 1.5f;
                    float radius = _shardRadii[i] + Mathf.Sin(_time * 2f + i) * 0.3f;

                    Vector3 pos = new Vector3(
                        Mathf.Cos(_shardAngles[i]) * radius,
                        Mathf.Sin(_shardAngles[i]) * radius,
                        0
                    );
                    _shards[i].localPosition = pos;

                    // Point outward
                    float angle = _shardAngles[i] * Mathf.Rad2Deg + 90f;
                    _shards[i].localRotation = Quaternion.Euler(0, 0, angle);

                    // Shard spin
                    _shards[i].GetChild(0).Rotate(0, 0, Time.deltaTime * 120f);

                    // Rainbow color shifting like original TypeScript
                    var bodySr = _shards[i].GetChild(0).GetComponent<SpriteRenderer>();
                    if (bodySr != null)
                    {
                        float hue = (_time * 0.25f + i * 0.12f) % 1f;
                        Color rainbow = Color.HSVToRGB(hue, 0.9f, 0.9f);
                        rainbow.a = 0.85f;
                        bodySr.color = rainbow;
                    }

                    // Tip pulse with color
                    if (_shardTips[i] != null)
                    {
                        float tipPulse = 0.12f + Mathf.Sin(_time * 8f + i) * 0.06f;
                        _shardTips[i].localScale = Vector3.one * tipPulse;

                        var tipSr = _shardTips[i].GetComponent<SpriteRenderer>();
                        if (tipSr != null)
                        {
                            float tipHue = (_time * 0.4f + i * 0.15f) % 1f;
                            Color tipColor = Color.HSVToRGB(tipHue, 0.7f, 1f);
                            tipColor.a = 0.9f;
                            tipSr.color = tipColor;
                        }
                    }
                }
            }

            // Rotate rings
            if (_rings != null)
            {
                for (int i = 0; i < _rings.Length; i++)
                {
                    if (_rings[i] == null) continue;
                    float speed = 20f + i * 15f;
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    _rings[i].Rotate(0, 0, Time.deltaTime * speed * dir);
                }
            }

            // Update lightning
            if (_lightning != null && _shards != null)
            {
                for (int i = 0; i < _lightning.Length; i++)
                {
                    if (_lightning[i] == null || _shards[i] == null) continue;

                    int nextIdx = (i + 1) % _shards.Length;
                    if (_shards[nextIdx] == null) continue;

                    Vector3 start = _shards[i].localPosition;
                    Vector3 end = _shards[nextIdx].localPosition;
                    Vector3 mid1 = Vector3.Lerp(start, end, 0.33f) + Random.insideUnitSphere * 0.2f;
                    mid1.z = 0;
                    Vector3 mid2 = Vector3.Lerp(start, end, 0.66f) + Random.insideUnitSphere * 0.2f;
                    mid2.z = 0;

                    _lightning[i].SetPosition(0, start);
                    _lightning[i].SetPosition(1, mid1);
                    _lightning[i].SetPosition(2, mid2);
                    _lightning[i].SetPosition(3, end);

                    // Rainbow color shifting like original TypeScript
                    float lightningHue = (_time * 0.6f + i * 0.12f) % 1f;
                    Color lightningRainbow = Color.HSVToRGB(lightningHue, 1f, 0.9f);

                    // Flicker
                    float alpha = 0.5f + Mathf.Sin(_time * 25f + i * 2f) * 0.4f;
                    lightningRainbow.a = alpha;
                    _lightning[i].startColor = lightningRainbow;
                    _lightning[i].endColor = new Color(lightningRainbow.r, lightningRainbow.g, lightningRainbow.b, alpha * 0.3f);
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

        private Transform CreateDiamond(string name, float radius, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDiamondSprite();
            sr.color = color;
            sr.sortingOrder = sortOrder;
            go.transform.localScale = Vector3.one * radius * 2f;
            return go.transform;
        }

        private Transform CreateDiamondOutline(string name, float radius, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDiamondOutlineSprite();
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
                    float dist = dx + dy;
                    float alpha = dist < center - 1 ? 1f : 0f;
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
                    float alpha = (dist < center - 1 && dist > center - 3) ? 1f : 0f;
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateShardSprite()
        {
            int w = 16, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                float halfWidth = (1f - t) * w / 2f;
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w / 2f);
                    float alpha = dx < halfWidth ? 1f : 0f;
                    pixels[y * w + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);
        }

        private Sprite CreateShardOutlineSprite()
        {
            int w = 16, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                float halfWidth = (1f - t) * w / 2f;
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w / 2f);
                    // Outline only
                    float alpha = (dx < halfWidth && dx > halfWidth - 2) ? 1f : 0f;
                    if (y < 2 || y > h - 3) alpha = dx < halfWidth ? 1f : 0f; // Cap ends
                    pixels[y * w + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);
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
