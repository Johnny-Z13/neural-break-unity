using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Input;
using NeuralBreak.Config;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Player movement, dash, and arena boundary handling.
    /// Based on TypeScript Player.ts - top-down movement with dash ability.
    /// All values driven by ConfigProvider - no magic numbers.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _dashFeedback;
        [SerializeField] private MMF_Player _dashReadyFeedback;

        [Header("Dash Trail")]
        [SerializeField] private TrailRenderer _dashTrail;
        [SerializeField] private Color _trailStartColor = new Color(0.2f, 0.9f, 1f, 0.8f);
        [SerializeField] private Color _trailEndColor = new Color(0.2f, 0.9f, 1f, 0f);
        [SerializeField] private float _trailTime = 0.15f;
        [SerializeField] private float _trailWidth = 0.5f;

        [Header("Thrust Trail")]
        [SerializeField] private TrailRenderer _thrustTrail;
        [SerializeField] private Color _thrustTrailStartColor = new Color(1f, 0.6f, 0.2f, 0.6f);
        [SerializeField] private Color _thrustTrailEndColor = new Color(1f, 0.4f, 0.1f, 0f);
        [SerializeField] private float _thrustTrailTime = 0.25f;
        [SerializeField] private float _thrustTrailWidth = 0.3f;

        // Config-driven properties (cached for performance)
        private PlayerConfig Config => ConfigProvider.Player;
        private float BaseSpeed => Config.baseSpeed;
        private float Acceleration => Config.acceleration;
        private float Deceleration => Config.deceleration;
        private float DashSpeed => Config.dashSpeed;
        private float DashDuration => Config.dashDuration;
        private float DashCooldown => Config.dashCooldown;
        private float ArenaBoundary => Config.arenaRadius;
        private float BoundaryPush => Config.boundaryPushStrength;
        private float ThrustSpeedMultiplier => Config.thrustSpeedMultiplier;
        private float ThrustAccelTime => Config.thrustAccelerationTime;
        private float ThrustDecelTime => Config.thrustDecelerationTime;

        // Components
        private Rigidbody2D _rb;
        private InputManager _input;

        // Movement state
        private Vector2 _currentVelocity;
        private Vector2 _lastMoveDirection = Vector2.up;
        private Vector2 _aimDirection = Vector2.up; // Smoothed for visuals
        private Vector2 _rawAimDirection = Vector2.up; // Instant for shooting
        private float _currentSpeedMultiplier = 1f;

        // Dash state
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private Vector2 _dashDirection;
        private bool _dashReady = true;

        // Thrust state
        private bool _isThrusting;
        private float _thrustMultiplier = 1f; // Current thrust multiplier (1.0 to ThrustSpeedMultiplier)

        // Aim indicator
        private GameObject _aimIndicator;
        private LineRenderer _aimLine;

        // Public accessors
        public Vector2 Position => _rb.position;
        public Vector2 Velocity => _currentVelocity;
        public Vector2 FacingDirection => _rawAimDirection; // Raw aim for shooting (no lag)
        public Vector2 SmoothedAimDirection => _aimDirection; // Smoothed for visual rotation
        public Vector2 MoveDirection => _lastMoveDirection; // Actual move direction
        public bool IsDashing => _isDashing;
        public bool DashReady => _dashCooldownTimer <= 0f;
        public float DashCooldownPercent => _dashCooldownTimer / DashCooldown;
        public bool IsThrusting => _isThrusting;
        public float ThrustPercent => (_thrustMultiplier - 1f) / (ThrustSpeedMultiplier - 1f);
        public float CurrentSpeed => BaseSpeed * _currentSpeedMultiplier * _thrustMultiplier;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Start()
        {
            _input = InputManager.Instance;

            if (_input != null)
            {
                _input.OnDashPressed += TryDash;
                _input.OnThrustPressed += OnThrustPressed;
                _input.OnThrustReleased += OnThrustReleased;
                _input.SetPlayerTransform(transform);
            }

            // Apply generated player sprite (triangle pointing up)
            ApplyPlayerSprite();

            // Setup dash trail
            SetupDashTrail();

            // Setup thrust trail
            SetupThrustTrail();

            // Setup aim indicator
            SetupAimIndicator();
        }

        private void ApplyPlayerSprite()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            // Player is a cyan/teal triangle pointing up
            Color playerColor = new Color(0.2f, 0.9f, 1f);
            var sprite = Graphics.SpriteGenerator.CreateTriangle(64, playerColor, "Player");
            sr.sprite = sprite;
            sr.color = Color.white;
        }

        private void SetupDashTrail()
        {
            // Create trail renderer if not assigned
            if (_dashTrail == null)
            {
                _dashTrail = GetComponent<TrailRenderer>();
                if (_dashTrail == null)
                {
                    _dashTrail = gameObject.AddComponent<TrailRenderer>();
                }
            }

            // Configure trail
            _dashTrail.time = _trailTime;
            _dashTrail.startWidth = _trailWidth;
            _dashTrail.endWidth = 0.1f;
            _dashTrail.startColor = _trailStartColor;
            _dashTrail.endColor = _trailEndColor;
            _dashTrail.numCornerVertices = 4;
            _dashTrail.numCapVertices = 4;
            _dashTrail.minVertexDistance = 0.1f;

            // Use default sprites material for trail
            _dashTrail.material = new Material(Shader.Find("Sprites/Default"));
            _dashTrail.material.color = Color.white;

            // Start disabled
            _dashTrail.emitting = false;
        }

        private void SetupThrustTrail()
        {
            // Create separate trail renderer for thrust
            if (_thrustTrail == null)
            {
                GameObject thrustTrailObj = new GameObject("ThrustTrail");
                thrustTrailObj.transform.SetParent(transform);
                thrustTrailObj.transform.localPosition = Vector3.zero;
                _thrustTrail = thrustTrailObj.AddComponent<TrailRenderer>();
            }

            // Configure thrust trail (orange/flame color)
            _thrustTrail.time = _thrustTrailTime;
            _thrustTrail.startWidth = _thrustTrailWidth;
            _thrustTrail.endWidth = 0.05f;
            _thrustTrail.startColor = _thrustTrailStartColor;
            _thrustTrail.endColor = _thrustTrailEndColor;
            _thrustTrail.numCornerVertices = 4;
            _thrustTrail.numCapVertices = 4;
            _thrustTrail.minVertexDistance = 0.05f;

            // Use default sprites material for trail
            _thrustTrail.material = new Material(Shader.Find("Sprites/Default"));
            _thrustTrail.material.color = Color.white;

            // Start disabled
            _thrustTrail.emitting = false;
        }

        private void SetupAimIndicator()
        {
            // Create aim indicator object
            _aimIndicator = new GameObject("AimIndicator");
            _aimIndicator.transform.SetParent(transform);
            _aimIndicator.transform.localPosition = Vector3.zero;

            // Add line renderer for aim line
            _aimLine = _aimIndicator.AddComponent<LineRenderer>();
            _aimLine.positionCount = 2;
            _aimLine.startWidth = 0.08f;
            _aimLine.endWidth = 0.02f;
            _aimLine.material = new Material(Shader.Find("Sprites/Default"));

            // Cyan color matching player
            Color aimColor = new Color(0.2f, 0.9f, 1f, 0.6f);
            _aimLine.startColor = aimColor;
            _aimLine.endColor = new Color(aimColor.r, aimColor.g, aimColor.b, 0.1f);
            _aimLine.sortingOrder = 5;

            _aimLine.useWorldSpace = true;
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnDashPressed -= TryDash;
                _input.OnThrustPressed -= OnThrustPressed;
                _input.OnThrustReleased -= OnThrustReleased;
            }
        }

        private void Update()
        {
            UpdateDashCooldown();
            UpdateThrust();
            UpdateAimDirection();
            UpdateAimIndicator();
        }

        private void FixedUpdate()
        {
            if (_isDashing)
            {
                UpdateDash();
            }
            else
            {
                UpdateMovement();
            }

            EnforceBoundary();
            UpdatePlayerRotation();
        }

        /// <summary>
        /// Update aim direction from input (twin-stick)
        /// </summary>
        private void UpdateAimDirection()
        {
            if (_input == null) return;

            // Get aim direction from input manager
            Vector2 inputAim = _input.GetAimDirection();

            Vector2 targetAim = _rawAimDirection;
            if (inputAim.sqrMagnitude > 0.01f)
            {
                targetAim = inputAim.normalized;
            }
            else if (_lastMoveDirection.sqrMagnitude > 0.01f)
            {
                // Fall back to move direction if no aim input
                targetAim = _lastMoveDirection;
            }

            // Store RAW aim direction for shooting (instant, no lag)
            _rawAimDirection = targetAim;

            // Smooth aim direction for VISUAL rotation only (prevents jittery sprite)
            float aimSmoothing = 25f; // Higher = faster response
            _aimDirection = Vector2.Lerp(_aimDirection, targetAim, aimSmoothing * Time.deltaTime);

            // Ensure directions stay normalized
            if (_aimDirection.sqrMagnitude > 0.01f)
            {
                _aimDirection = _aimDirection.normalized;
            }
            if (_rawAimDirection.sqrMagnitude > 0.01f)
            {
                _rawAimDirection = _rawAimDirection.normalized;
            }
        }

        /// <summary>
        /// Update aim indicator visual
        /// </summary>
        private void UpdateAimIndicator()
        {
            if (_aimLine == null) return;

            // Show aim line
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)_aimDirection * 2.5f;

            _aimLine.SetPosition(0, start);
            _aimLine.SetPosition(1, end);
        }

        /// <summary>
        /// Rotate player sprite to face aim direction
        /// </summary>
        private void UpdatePlayerRotation()
        {
            if (_aimDirection.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        #region Movement

        private void UpdateMovement()
        {
            if (_input == null) return;

            Vector2 inputDir = _input.MoveInput;
            float targetSpeed = BaseSpeed * _currentSpeedMultiplier * _thrustMultiplier;

            if (inputDir.sqrMagnitude > 0.01f)
            {
                // Accelerate toward target velocity
                Vector2 targetVelocity = inputDir.normalized * targetSpeed;
                _currentVelocity = Vector2.MoveTowards(
                    _currentVelocity,
                    targetVelocity,
                    Acceleration * Time.fixedDeltaTime
                );

                // Track last direction for firing
                _lastMoveDirection = inputDir.normalized;
            }
            else
            {
                // Decelerate to stop
                _currentVelocity = Vector2.MoveTowards(
                    _currentVelocity,
                    Vector2.zero,
                    Deceleration * Time.fixedDeltaTime
                );
            }

            _rb.linearVelocity = _currentVelocity;
        }

        private void EnforceBoundary()
        {
            Vector2 pos = _rb.position;
            Vector2 boundaryForce = Vector2.zero;

            // Check each boundary
            if (pos.x > ArenaBoundary)
            {
                boundaryForce.x = -BoundaryPush;
            }
            else if (pos.x < -ArenaBoundary)
            {
                boundaryForce.x = BoundaryPush;
            }

            if (pos.y > ArenaBoundary)
            {
                boundaryForce.y = -BoundaryPush;
            }
            else if (pos.y < -ArenaBoundary)
            {
                boundaryForce.y = BoundaryPush;
            }

            // Apply soft push
            if (boundaryForce != Vector2.zero)
            {
                _rb.linearVelocity += boundaryForce * Time.fixedDeltaTime * 10f;

                // Hard clamp as last resort
                pos.x = Mathf.Clamp(pos.x, -ArenaBoundary - 1f, ArenaBoundary + 1f);
                pos.y = Mathf.Clamp(pos.y, -ArenaBoundary - 1f, ArenaBoundary + 1f);
                _rb.position = pos;
            }
        }

        #endregion

        #region Dash

        private void TryDash()
        {
            if (_isDashing || _dashCooldownTimer > 0f) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            // Use current move input or last direction
            Vector2 dashDir = _input.HasMoveInput()
                ? _input.MoveInput.normalized
                : _lastMoveDirection;

            StartDash(dashDir);
        }

        private void StartDash(Vector2 direction)
        {
            _isDashing = true;
            _dashTimer = DashDuration;
            _dashDirection = direction;
            _dashCooldownTimer = DashCooldown;
            _dashReady = false;

            // Enable dash trail
            if (_dashTrail != null)
            {
                _dashTrail.Clear();
                _dashTrail.emitting = true;
            }

            // Play dash feedback
            _dashFeedback?.PlayFeedbacks();

            // Publish event
            EventBus.Publish(new PlayerDashedEvent { direction = direction });
        }

        private void UpdateDash()
        {
            _dashTimer -= Time.fixedDeltaTime;

            if (_dashTimer <= 0f)
            {
                EndDash();
                return;
            }

            // Move at dash speed
            _rb.linearVelocity = _dashDirection * DashSpeed;
            _currentVelocity = _rb.linearVelocity;
        }

        private void EndDash()
        {
            _isDashing = false;

            // Disable dash trail
            if (_dashTrail != null)
            {
                _dashTrail.emitting = false;
            }

            // Reduce velocity after dash
            _currentVelocity = _dashDirection * (BaseSpeed * 0.5f);
            _rb.linearVelocity = _currentVelocity;
        }

        private void UpdateDashCooldown()
        {
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;

                if (_dashCooldownTimer <= 0f && !_dashReady)
                {
                    _dashReady = true;
                    _dashReadyFeedback?.PlayFeedbacks();
                }
            }
        }

        #endregion

        #region Thrust

        private void OnThrustPressed()
        {
            if (_isDashing) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            _isThrusting = true;

            // Enable thrust trail
            if (_thrustTrail != null)
            {
                _thrustTrail.Clear();
                _thrustTrail.emitting = true;
            }

            // Publish event
            EventBus.Publish(new PlayerThrustStartedEvent());
        }

        private void OnThrustReleased()
        {
            _isThrusting = false;

            // Publish event
            EventBus.Publish(new PlayerThrustEndedEvent());
        }

        private void UpdateThrust()
        {
            float targetMultiplier = _isThrusting ? ThrustSpeedMultiplier : 1f;

            if (_thrustMultiplier < targetMultiplier)
            {
                // Accelerating thrust
                float accelRate = (ThrustSpeedMultiplier - 1f) / ThrustAccelTime;
                _thrustMultiplier = Mathf.Min(targetMultiplier, _thrustMultiplier + accelRate * Time.deltaTime);
            }
            else if (_thrustMultiplier > targetMultiplier)
            {
                // Decelerating thrust
                float decelRate = (ThrustSpeedMultiplier - 1f) / ThrustDecelTime;
                _thrustMultiplier = Mathf.Max(targetMultiplier, _thrustMultiplier - decelRate * Time.deltaTime);
            }

            // Update thrust trail based on current multiplier
            if (_thrustTrail != null)
            {
                bool shouldEmit = _thrustMultiplier > 1.05f;
                if (_thrustTrail.emitting != shouldEmit)
                {
                    _thrustTrail.emitting = shouldEmit;
                }
            }
        }

        #endregion

        #region Speed Modifiers

        /// <summary>
        /// Apply speed modifier from power-ups
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            _currentSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        /// <summary>
        /// Add to speed multiplier (from speed-up pickups)
        /// </summary>
        public void AddSpeedBonus(float bonus)
        {
            _currentSpeedMultiplier += bonus;
        }

        /// <summary>
        /// Reset speed to base
        /// </summary>
        public void ResetSpeed()
        {
            _currentSpeedMultiplier = 1f;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Teleport player to position
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            _rb.position = position;
            _currentVelocity = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// Stop all movement
        /// </summary>
        public void Stop()
        {
            _currentVelocity = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;
            _isDashing = false;
        }

        /// <summary>
        /// Check if player is invulnerable (during dash)
        /// </summary>
        public bool IsInvulnerable()
        {
            return _isDashing;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw arena boundary (use config if available, fallback to 25)
            float boundary = ConfigProvider.Player?.arenaRadius ?? 25f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(boundary * 2, boundary * 2, 0));

            // Draw move direction (yellow)
            Gizmos.color = Color.yellow;
            Vector3 pos = transform.position;
            Gizmos.DrawLine(pos, pos + (Vector3)_lastMoveDirection * 1.5f);

            // Draw aim direction (cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pos, pos + (Vector3)_aimDirection * 2.5f);
        }

        #endregion
    }
}
