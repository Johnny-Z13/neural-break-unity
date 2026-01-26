using UnityEngine;
using NeuralBreak.Config;

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
        [SerializeField] private Color _boundaryColor = new Color(0.2f, 0.6f, 1f, 0.5f);
        [SerializeField] private Color _warningColor = new Color(1f, 0.3f, 0.2f, 0.8f);
        [SerializeField] private float _lineWidth = 0.15f;
        [SerializeField] private int _segments = 64;

        [Header("Warning Effect")]
        [SerializeField] private float _warningDistance = 3f;
        [SerializeField] private float _pulseSpeed = 3f;

        private LineRenderer _lineRenderer;
        private float _radius;
        private Transform _playerTransform;
        private Material _lineMaterial;
        private float _pulseTimer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            SetupLineRenderer();
        }

        private void Start()
        {
            // Get arena radius from config
            _radius = ConfigProvider.Player?.arenaRadius ?? 30f;

            // Subscribe to game events
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);

            // Generate circle points
            GenerateCircle();
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Clear cached player reference on new game
            _playerTransform = null;
        }

        private void Update()
        {
            // Cache player transform on first use
            if (_playerTransform == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    _playerTransform = playerGO.transform;
                }
            }

            // Pulse when player is near boundary
            if (_playerTransform != null)
            {
                float playerDistance = _playerTransform.position.magnitude;
                float distanceToEdge = _radius - playerDistance;

                if (distanceToEdge < _warningDistance)
                {
                    // Pulse warning color
                    _pulseTimer += Time.deltaTime * _pulseSpeed;
                    float pulse = (Mathf.Sin(_pulseTimer * Mathf.PI * 2f) + 1f) * 0.5f;
                    float intensity = 1f - (distanceToEdge / _warningDistance);

                    Color currentColor = Color.Lerp(_boundaryColor, _warningColor, intensity * pulse);
                    _lineRenderer.startColor = currentColor;
                    _lineRenderer.endColor = currentColor;
                }
                else
                {
                    _lineRenderer.startColor = _boundaryColor;
                    _lineRenderer.endColor = _boundaryColor;
                    _pulseTimer = 0f;
                }
            }
        }

        private void SetupLineRenderer()
        {
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.loop = true;
            _lineRenderer.startWidth = _lineWidth;
            _lineRenderer.endWidth = _lineWidth;
            _lineRenderer.startColor = _boundaryColor;
            _lineRenderer.endColor = _boundaryColor;

            // Create a simple additive material for glow
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            _lineRenderer.material = _lineMaterial;

            // Set sorting order to be behind most things but visible
            _lineRenderer.sortingOrder = -100;
        }

        private void GenerateCircle()
        {
            _lineRenderer.positionCount = _segments;

            for (int i = 0; i < _segments; i++)
            {
                float angle = (float)i / _segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * _radius;
                float y = Mathf.Sin(angle) * _radius;
                _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        /// <summary>
        /// Update radius (e.g., if config changes)
        /// </summary>
        public void SetRadius(float radius)
        {
            _radius = radius;
            GenerateCircle();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);

            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            float radius = Application.isPlaying ? _radius : (ConfigProvider.Player?.arenaRadius ?? 30f);
            Gizmos.color = _boundaryColor;
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
