using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// 80s Vector Art ScanDrone visuals - Battlezone/Tron style!
    /// Procedurally generates the drone's visual elements:
    /// - Hexagonal body with wireframe outline
    /// - Rotating radar dish with sweep cone
    /// - Scanning grid with animated beam
    /// - 6 sensor eyes around the hexagon
    /// - Antenna with pulsing beacon
    /// - Outer detection ring with markers
    /// </summary>
    public class ScanDroneVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color m_bodyColor = new Color(1f, 0.4f, 0f, 0.3f); // Orange
        [SerializeField] private Color m_wireframeColor = new Color(1f, 0.53f, 0f, 0.9f); // Brighter orange
        [SerializeField] private Color m_accentColor = new Color(0f, 1f, 1f, 0.8f); // Cyan
        [SerializeField] private Color m_alertColor = new Color(1f, 0f, 0f, 0.9f); // Red
        [SerializeField] private Color m_scanGridColor = new Color(1f, 0.27f, 0f, 0.4f); // Dark orange

        [Header("Animation")]
        [SerializeField] private float m_radarRotationSpeed = 2f;
        [SerializeField] private float m_wireframeRotationSpeed = 1f;
        [SerializeField] private float m_outerRingRotationSpeed = 1f;
        [SerializeField] private float m_pulseSpeed = 4f;
        [SerializeField] private float m_alertPulseSpeed = 12f;

        [Header("Scale")]
        [SerializeField] private float m_scale = 1.2f; // 20% larger - matches collision

        // Visual components
        private Transform m_hexBody;
        private Transform m_hexWireframe;
        private Transform m_radarDish;
        private Transform m_scanGrid;
        private Transform m_scanBeam;
        private Transform m_outerRing;
        private Transform m_beacon;
        private Transform[] m_sensorEyes;
        private SpriteRenderer[] m_sensorEyeRenderers;
        private SpriteRenderer m_hexBodyRenderer;
        private SpriteRenderer m_scanBeamRenderer;
        private SpriteRenderer[] m_allRenderers;

        // State
        private bool m_isAlerted;
        private float m_radarAngle;
        private float m_wireframeAngle;
        private float m_outerRingAngle;
        private float m_scanBeamY;
        private float m_time;

        private void Start()
        {
            GenerateVisuals();
        }

        public void GenerateVisuals()
        {
            // Clean up existing children
            foreach (Transform child in transform)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            // Create hexagonal body
            m_hexBody = CreateHexagon("HexBody", m_scale * 0.35f, m_bodyColor);
            m_hexBody.localPosition = Vector3.zero;
            m_hexBodyRenderer = m_hexBody.GetComponent<SpriteRenderer>();

            // Create wireframe outline (slightly larger)
            m_hexWireframe = CreateHexagonOutline("HexWireframe", m_scale * 0.37f, m_wireframeColor);
            m_hexWireframe.localPosition = Vector3.zero;

            // Create radar dish group
            m_radarDish = CreateRadarDish();
            m_radarDish.localPosition = new Vector3(0, 0, -0.1f); // In front

            // Create scanning grid
            m_scanGrid = CreateScanningGrid();
            m_scanGrid.localPosition = new Vector3(0, 0, 0.1f); // Behind

            // Create sensor eyes
            CreateSensorEyes();

            // Create antenna with beacon
            CreateAntenna();

            // Create outer detection ring
            m_outerRing = CreateOuterRing();
            m_outerRing.localPosition = Vector3.zero;

            // Gather all renderers for color manipulation
            m_allRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private Transform CreateHexagon(string name, float radius, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateHexagonSprite(radius, true);
            sr.color = color;
            sr.sortingOrder = 10;

            return go.transform;
        }

        private Transform CreateHexagonOutline(string name, float radius, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateHexagonSprite(radius, false);
            sr.color = color;
            sr.sortingOrder = 11;

            return go.transform;
        }

        private Sprite CreateHexagonSprite(float radius, bool filled)
        {
            int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = new Color(0, 0, 0, 0);
            Color white = Color.white;

            // Clear texture
            Color[] pixels = new Color[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            float center = texSize / 2f;
            float pixelRadius = texSize / 2f - 2;

            // Draw hexagon
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;

                    // Check if point is inside hexagon
                    float angle = Mathf.Atan2(dy, dx);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Hexagon distance function
                    float hexAngle = Mathf.Repeat(angle + Mathf.PI / 6f, Mathf.PI / 3f) - Mathf.PI / 6f;
                    float hexDist = pixelRadius * Mathf.Cos(Mathf.PI / 6f) / Mathf.Cos(hexAngle);

                    if (filled)
                    {
                        if (dist < hexDist)
                        {
                            pixels[y * texSize + x] = white;
                        }
                    }
                    else
                    {
                        // Outline only
                        float lineWidth = 2f;
                        if (dist < hexDist && dist > hexDist - lineWidth)
                        {
                            pixels[y * texSize + x] = white;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize / (radius * 2));
        }

        private Transform CreateRadarDish()
        {
            var group = new GameObject("RadarDish");
            group.transform.SetParent(transform, false);

            // Dish ring
            var ring = new GameObject("DishRing");
            ring.transform.SetParent(group.transform, false);
            var ringSr = ring.AddComponent<SpriteRenderer>();
            ringSr.sprite = CreateRingSprite(m_scale * 0.12f, m_scale * 0.18f);
            ringSr.color = m_accentColor;
            ringSr.sortingOrder = 15;

            // Sweep line
            var sweep = new GameObject("SweepLine");
            sweep.transform.SetParent(group.transform, false);
            var sweepSr = sweep.AddComponent<SpriteRenderer>();
            sweepSr.sprite = CreateLineSprite(m_scale * 0.4f, 0.02f);
            sweepSr.color = new Color(0f, 1f, 0f, 0.9f); // Green
            sweepSr.sortingOrder = 16;
            sweep.transform.localPosition = new Vector3(m_scale * 0.2f, 0, 0);

            // Sweep cone
            var cone = new GameObject("SweepCone");
            cone.transform.SetParent(group.transform, false);
            var coneSr = cone.AddComponent<SpriteRenderer>();
            coneSr.sprite = CreateConeSprite(m_scale * 0.4f, 30f);
            coneSr.color = new Color(0f, 1f, 0f, 0.3f); // Transparent green
            coneSr.sortingOrder = 14;

            return group.transform;
        }

        private Transform CreateScanningGrid()
        {
            var group = new GameObject("ScanGrid");
            group.transform.SetParent(transform, false);

            float gridSize = m_scale * 0.5f;
            int lineCount = 5;

            // Horizontal lines
            for (int i = 0; i < lineCount; i++)
            {
                float y = (i - lineCount / 2f) * (gridSize * 2 / lineCount);
                var line = CreateGridLine($"HLine{i}", gridSize * 2, 0.01f, new Vector3(0, y, 0));
                line.SetParent(group.transform, false);
            }

            // Vertical lines
            for (int i = 0; i < lineCount; i++)
            {
                float x = (i - lineCount / 2f) * (gridSize * 2 / lineCount);
                var line = CreateGridLine($"VLine{i}", 0.01f, gridSize * 2, new Vector3(x, 0, 0));
                line.SetParent(group.transform, false);
            }

            // Scanning beam
            var beam = new GameObject("ScanBeam");
            beam.transform.SetParent(group.transform, false);
            var beamSr = beam.AddComponent<SpriteRenderer>();
            beamSr.sprite = CreateRectSprite(gridSize * 2, 0.08f);
            beamSr.color = new Color(1f, 0f, 0f, 0.6f); // Red
            beamSr.sortingOrder = 8;
            m_scanBeam = beam.transform;
            m_scanBeamRenderer = beamSr;

            return group.transform;
        }

        private Transform CreateGridLine(string name, float width, float height, Vector3 pos)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(width, height);
            sr.color = m_scanGridColor;
            sr.sortingOrder = 5;
            go.transform.localPosition = pos;
            return go.transform;
        }

        private void CreateSensorEyes()
        {
            m_sensorEyes = new Transform[6];
            m_sensorEyeRenderers = new SpriteRenderer[6];
            float hexRadius = m_scale * 0.35f;

            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * (hexRadius + 0.08f),
                    Mathf.Sin(angle) * (hexRadius + 0.08f),
                    -0.05f
                );

                // Sensor ring
                var ring = new GameObject($"SensorRing{i}");
                ring.transform.SetParent(transform, false);
                ring.transform.localPosition = pos;
                var ringSr = ring.AddComponent<SpriteRenderer>();
                ringSr.sprite = CreateRingSprite(0.03f, 0.05f);
                ringSr.color = m_wireframeColor;
                ringSr.sortingOrder = 12;

                // Sensor eye (inner)
                var eye = new GameObject($"SensorEye{i}");
                eye.transform.SetParent(transform, false);
                eye.transform.localPosition = pos + new Vector3(0, 0, -0.01f);
                var eyeSr = eye.AddComponent<SpriteRenderer>();
                eyeSr.sprite = CreateCircleSprite(0.025f);
                eyeSr.color = m_alertColor;
                eyeSr.sortingOrder = 13;

                m_sensorEyes[i] = eye.transform;
                m_sensorEyeRenderers[i] = eyeSr;
            }
        }

        private void CreateAntenna()
        {
            // Antenna stalk (line)
            var stalk = new GameObject("AntennaStalk");
            stalk.transform.SetParent(transform, false);
            var stalkSr = stalk.AddComponent<SpriteRenderer>();
            stalkSr.sprite = CreateRectSprite(0.02f, m_scale * 0.3f);
            stalkSr.color = m_accentColor;
            stalkSr.sortingOrder = 9;
            stalk.transform.localPosition = new Vector3(0, m_scale * 0.15f, -0.02f);

            // Beacon (octahedron approximated as diamond)
            var beacon = new GameObject("Beacon");
            beacon.transform.SetParent(transform, false);
            var beaconSr = beacon.AddComponent<SpriteRenderer>();
            beaconSr.sprite = CreateDiamondSprite(0.06f);
            beaconSr.color = m_alertColor;
            beaconSr.sortingOrder = 20;
            beacon.transform.localPosition = new Vector3(0, m_scale * 0.3f + 0.03f, -0.02f);
            m_beacon = beacon.transform;
        }

        private Transform CreateOuterRing()
        {
            var group = new GameObject("OuterRing");
            group.transform.SetParent(transform, false);

            // Main ring
            var ring = new GameObject("Ring");
            ring.transform.SetParent(group.transform, false);
            var ringSr = ring.AddComponent<SpriteRenderer>();
            ringSr.sprite = CreateRingSprite(m_scale * 0.55f, m_scale * 0.58f);
            ringSr.color = new Color(m_wireframeColor.r, m_wireframeColor.g, m_wireframeColor.b, 0.5f);
            ringSr.sortingOrder = 2;

            // Detection markers (dashed effect)
            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * Mathf.PI * 2f;
                var marker = new GameObject($"Marker{i}");
                marker.transform.SetParent(group.transform, false);
                var markerSr = marker.AddComponent<SpriteRenderer>();
                markerSr.sprite = CreateRectSprite(0.02f, 0.05f);
                markerSr.color = m_scanGridColor;
                markerSr.sortingOrder = 3;

                float dist = m_scale * 0.6f;
                marker.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * dist,
                    Mathf.Sin(angle) * dist,
                    0
                );
                marker.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg + 90);
            }

            return group.transform;
        }

        // Sprite creation helpers
        private Sprite CreateCircleSprite(float radius)
        {
            int texSize = 32;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[texSize * texSize];

            float center = texSize / 2f;
            float pixelRadius = texSize / 2f - 1;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * texSize + x] = dist < pixelRadius ? Color.white : clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize / (radius * 2));
        }

        private Sprite CreateRingSprite(float innerRadius, float outerRadius)
        {
            int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[texSize * texSize];

            float center = texSize / 2f;
            float ratio = innerRadius / outerRadius;
            float pixelOuter = texSize / 2f - 1;
            float pixelInner = pixelOuter * ratio;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * texSize + x] = (dist < pixelOuter && dist > pixelInner) ? Color.white : clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize / (outerRadius * 2));
        }

        private Sprite CreateRectSprite(float width, float height)
        {
            int texW = Mathf.Max(4, Mathf.RoundToInt(width * 100));
            int texH = Mathf.Max(4, Mathf.RoundToInt(height * 100));
            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[texW * texH];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), texW / width);
        }

        private Sprite CreateLineSprite(float length, float thickness)
        {
            return CreateRectSprite(length, thickness);
        }

        private Sprite CreateConeSprite(float length, float angleDeg)
        {
            int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            float halfAngle = angleDeg * 0.5f * Mathf.Deg2Rad;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dx = x - texSize / 2f;
                    float dy = y - texSize / 2f;

                    // Check if in cone (pointing right)
                    if (dx > 0)
                    {
                        float angle = Mathf.Abs(Mathf.Atan2(dy, dx));
                        if (angle < halfAngle)
                        {
                            pixels[y * texSize + x] = Color.white;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0, 0.5f), texSize / length);
        }

        private Sprite CreateDiamondSprite(float size)
        {
            int texSize = 32;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            float center = texSize / 2f;
            float halfSize = texSize / 2f - 2;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dx = Mathf.Abs(x - center);
                    float dy = Mathf.Abs(y - center);

                    // Diamond shape: |x| + |y| < size
                    if (dx + dy < halfSize)
                    {
                        pixels[y * texSize + x] = Color.white;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize / size);
        }

        private void Update()
        {
            if (m_radarDish == null) return;

            m_time += Time.deltaTime;
            float pulseSpeed = m_isAlerted ? m_alertPulseSpeed : m_pulseSpeed;
            float pulse = 1f + Mathf.Sin(m_time * pulseSpeed) * (m_isAlerted ? 0.15f : 0.05f);

            // Overall scale pulse (incorporate m_scale so it actually affects size)
            transform.localScale = Vector3.one * m_scale * pulse;

            // Rotate radar dish
            float radarSpeed = m_isAlerted ? m_radarRotationSpeed * 4f : m_radarRotationSpeed;
            m_radarAngle += radarSpeed * Time.deltaTime * 360f;
            m_radarDish.localRotation = Quaternion.Euler(0, 0, m_radarAngle);

            // Rotate wireframe
            float wireSpeed = m_isAlerted ? m_wireframeRotationSpeed * 4f : m_wireframeRotationSpeed;
            m_wireframeAngle += wireSpeed * Time.deltaTime * 360f;
            if (m_hexWireframe != null)
                m_hexWireframe.localRotation = Quaternion.Euler(0, 0, m_wireframeAngle);

            // Rotate outer ring (opposite direction)
            float outerSpeed = m_isAlerted ? m_outerRingRotationSpeed * 3f : m_outerRingRotationSpeed;
            m_outerRingAngle -= outerSpeed * Time.deltaTime * 360f;
            if (m_outerRing != null)
                m_outerRing.localRotation = Quaternion.Euler(0, 0, m_outerRingAngle);

            // Animate scan beam (up and down)
            if (m_scanBeam != null)
            {
                float beamSpeed = m_isAlerted ? 6f : 2f;
                m_scanBeamY = Mathf.Sin(m_time * beamSpeed) * m_scale * 0.3f;
                m_scanBeam.localPosition = new Vector3(0, m_scanBeamY, 0);
            }

            // Animate beacon
            if (m_beacon != null)
            {
                float strobeSpeed = m_isAlerted ? 20f : 5f;
                float beaconPulse = 1f + Mathf.Sin(m_time * strobeSpeed) * 0.3f;
                m_beacon.localScale = Vector3.one * beaconPulse;
                m_beacon.Rotate(0, 0, Time.deltaTime * 300f);
            }

            // Animate sensor eyes (blinking pattern)
            if (m_sensorEyes != null)
            {
                float blinkSpeed = m_isAlerted ? 8f : 3f;
                for (int i = 0; i < m_sensorEyes.Length; i++)
                {
                    if (m_sensorEyes[i] == null) continue;

                    float blinkPhase = (m_time * blinkSpeed + i * 0.5f) % 1f;
                    float eyeScale = blinkPhase > 0.5f ? 1.3f : 0.8f;
                    m_sensorEyes[i].localScale = Vector3.one * eyeScale;

                    var sr = m_sensorEyeRenderers[i];
                    if (sr != null)
                    {
                        float alpha = blinkPhase > 0.5f ? 1f : 0.3f;
                        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
                    }
                }
            }
        }

        public void SetAlerted(bool alerted)
        {
            if (m_isAlerted == alerted) return;
            m_isAlerted = alerted;

            // Change body color based on alert state
            if (m_hexBodyRenderer != null)
            {
                m_hexBodyRenderer.color = m_isAlerted ? new Color(1f, 0f, 0f, 0.5f) : m_bodyColor;
            }

            // Change scan beam color
            if (m_scanBeamRenderer != null)
            {
                m_scanBeamRenderer.color = m_isAlerted ? new Color(1f, 0f, 0f, 0.8f) : new Color(1f, 0.27f, 0f, 0.6f);
            }
        }

        public void SetSpawning(float alpha)
        {
            foreach (var sr in m_allRenderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a *= alpha;
                    sr.color = c;
                }
            }
        }
    }
}
