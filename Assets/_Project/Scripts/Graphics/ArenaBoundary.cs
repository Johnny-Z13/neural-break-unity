using UnityEngine;
using NeuralBreak.Config;
using NeuralBreak.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Renders the arena boundary as a circular line.
    /// Uses LineRenderer for a glowing edge effect.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ArenaBoundary : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private Color m_boundaryColor = new Color(0.2f, 0.6f, 1f, 0.5f);
        [SerializeField] private Color m_warningColor = new Color(1f, 0.3f, 0.2f, 0.8f);
        [SerializeField] private float m_lineWidth = 0.15f;
        [SerializeField] private int m_segments = 64;

        [Header("Warning Effect")]
        [SerializeField] private float m_warningDistance = 3f;
        [SerializeField] private float m_pulseSpeed = 3f;

        private LineRenderer m_lineRenderer;
        private float m_radius;
        private Transform m_playerTransform;
        private Material m_lineMaterial;
        private float m_pulseTimer;

        private void Awake()
        {
            m_lineRenderer = GetComponent<LineRenderer>();
            SetupLineRenderer();
        }

        private void Start()
        {
            // Get arena radius from config
            m_radius = ConfigProvider.Player?.arenaRadius ?? 30f;

            // Subscribe to game events
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);

            // Generate circle points
            GenerateCircle();
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Clear cached player reference on new game
            m_playerTransform = null;
        }

        private void Update()
        {
            // Cache player transform on first use
            if (m_playerTransform == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    m_playerTransform = playerGO.transform;
                }
            }

            // Pulse when player is near boundary
            if (m_playerTransform != null)
            {
                float playerDistance = m_playerTransform.position.magnitude;
                float distanceToEdge = m_radius - playerDistance;

                if (distanceToEdge < m_warningDistance)
                {
                    // Pulse warning color
                    m_pulseTimer += Time.deltaTime * m_pulseSpeed;
                    float pulse = (Mathf.Sin(m_pulseTimer * Mathf.PI * 2f) + 1f) * 0.5f;
                    float intensity = 1f - (distanceToEdge / m_warningDistance);

                    Color currentColor = Color.Lerp(m_boundaryColor, m_warningColor, intensity * pulse);
                    m_lineRenderer.startColor = currentColor;
                    m_lineRenderer.endColor = currentColor;
                }
                else
                {
                    m_lineRenderer.startColor = m_boundaryColor;
                    m_lineRenderer.endColor = m_boundaryColor;
                    m_pulseTimer = 0f;
                }
            }
        }

        private void SetupLineRenderer()
        {
            m_lineRenderer.useWorldSpace = true;
            m_lineRenderer.loop = true;
            m_lineRenderer.startWidth = m_lineWidth;
            m_lineRenderer.endWidth = m_lineWidth;
            m_lineRenderer.startColor = m_boundaryColor;
            m_lineRenderer.endColor = m_boundaryColor;

            // Create a simple additive material for glow
            m_lineMaterial = new Material(Shader.Find("Sprites/Default"));
            m_lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m_lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m_lineRenderer.material = m_lineMaterial;

            // Set sorting order to be behind most things but visible
            m_lineRenderer.sortingOrder = -100;
        }

        private void GenerateCircle()
        {
            m_lineRenderer.positionCount = m_segments;

            for (int i = 0; i < m_segments; i++)
            {
                float angle = (float)i / m_segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * m_radius;
                float y = Mathf.Sin(angle) * m_radius;
                m_lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        /// <summary>
        /// Update radius (e.g., if config changes)
        /// </summary>
        public void SetRadius(float radius)
        {
            m_radius = radius;
            GenerateCircle();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);

            if (m_lineMaterial != null)
            {
                Destroy(m_lineMaterial);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            float radius = Application.isPlaying ? m_radius : (ConfigProvider.Player?.arenaRadius ?? 30f);
            Gizmos.color = m_boundaryColor;
            DrawGizmoCircle(Vector3.zero, radius, 64);
        }

        private void DrawGizmoCircle(Vector3 center, float radius, int segments)
        {
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
#endif
    }
}
