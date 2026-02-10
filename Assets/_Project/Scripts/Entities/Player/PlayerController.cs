using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Input;
using NeuralBreak.Config;
using Z13.Core;

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
        // Note: MMFeedbacks removed

        [Header("Dash Trail")]
        [SerializeField] private TrailRenderer m_dashTrail;
        [SerializeField] private Color m_trailStartColor = new Color(0.2f, 0.9f, 1f, 0.8f);
        [SerializeField] private Color m_trailEndColor = new Color(0.2f, 0.9f, 1f, 0f);
        [SerializeField] private float m_trailTime = 0.15f;
        [SerializeField] private float m_trailWidth = 0.5f;

        [Header("Thrust Trail")]
        [SerializeField] private TrailRenderer m_thrustTrail;
        [SerializeField] private Color m_thrustTrailStartColor = new Color(1f, 0.6f, 0.2f, 0.6f);
        [SerializeField] private Color m_thrustTrailEndColor = new Color(1f, 0.4f, 0.1f, 0f);
        [SerializeField] private float m_thrustTrailTime = 0.25f;
        [SerializeField] private float m_thrustTrailWidth = 0.3f;

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
        private ControlScheme CurrentControlScheme => Config.controlScheme;

        // Components
        private Rigidbody2D m_rb;
        private InputManager m_input;

        // Movement state
        private Vector2 m_currentVelocity;
        private Vector2 m_lastMoveDirection = Vector2.up;
        private Vector2 m_aimDirection = Vector2.up; // Smoothed for visuals
        private Vector2 m_rawAimDirection = Vector2.up; // Instant for shooting
        private float m_currentSpeedMultiplier = 1f;

        // Classic rotate controls state
        private float m_currentRotation = 0f; // For ClassicRotate/TankControls

        // Dash state
        private bool m_isDashing;
        private float m_dashTimer;
        private float m_dashCooldownTimer;
        private Vector2 m_dashDirection;
        private bool m_dashReady = true;

        // Thrust state
        private bool m_isThrusting;
        private float m_thrustMultiplier = 1f; // Current thrust multiplier (1.0 to ThrustSpeedMultiplier)

        // Aim indicator
        private GameObject m_aimIndicator;
        private LineRenderer m_aimLine;

        // Cached colors to avoid allocations
        private static readonly Color s_playerColor = new Color(0.2f, 0.9f, 1f);
        private static readonly Color s_aimColor = new Color(0.2f, 0.9f, 1f, 0.6f);

        // Cached Vector3 for zero-allocation gizmos
        private Vector3 m_cachedGizmoVector;

        // Public accessors
        public Vector2 Position => m_rb.position;
        public Vector2 Velocity => m_currentVelocity;
        public Vector2 FacingDirection => m_rawAimDirection; // Raw aim for shooting (no lag)
        public Vector2 SmoothedAimDirection => m_aimDirection; // Smoothed for visual rotation
        public Vector2 MoveDirection => m_lastMoveDirection; // Actual move direction
        public bool IsDashing => m_isDashing;
        public bool DashReady => m_dashCooldownTimer <= 0f;
        public float DashCooldownPercent => m_dashCooldownTimer / DashCooldown;
        public bool IsThrusting => m_isThrusting;
        public float ThrustPercent => (m_thrustMultiplier - 1f) / (ThrustSpeedMultiplier - 1f);
        public float CurrentSpeed => BaseSpeed * m_currentSpeedMultiplier * m_thrustMultiplier;

        private void Awake()
        {
            m_rb = GetComponent<Rigidbody2D>();
            m_rb.gravityScale = 0f;
            m_rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Start()
        {
            m_input = InputManager.Instance;

            if (m_input != null)
            {
                m_input.OnDashPressed += TryDash;
                m_input.OnThrustPressed += OnThrustPressed;
                m_input.OnThrustReleased += OnThrustReleased;
                m_input.SetPlayerTransform(transform);
                Debug.Log("[PlayerController] InputManager connected successfully");
            }
            else
            {
                Debug.LogError("[PlayerController] InputManager.Instance is NULL!");
            }

            // Subscribe to player death and game start events
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);

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

            // Player is a cyan/teal triangle pointing up - use cached color
            var sprite = Graphics.SpriteGenerator.CreateTriangle(64, s_playerColor, "Player");
            sr.sprite = sprite;
            sr.color = Color.white;
        }

        private void SetupDashTrail()
        {
            // Create trail renderer if not assigned
            if (m_dashTrail == null)
            {
                m_dashTrail = GetComponent<TrailRenderer>();
                if (m_dashTrail == null)
                {
                    m_dashTrail = gameObject.AddComponent<TrailRenderer>();
                }
            }

            // Configure trail
            m_dashTrail.time = m_trailTime;
            m_dashTrail.startWidth = m_trailWidth;
            m_dashTrail.endWidth = 0.1f;
            m_dashTrail.startColor = m_trailStartColor;
            m_dashTrail.endColor = m_trailEndColor;
            m_dashTrail.numCornerVertices = 4;
            m_dashTrail.numCapVertices = 4;
            m_dashTrail.minVertexDistance = 0.1f;

            // Use default sprites material for trail
            m_dashTrail.material = new Material(Shader.Find("Sprites/Default"));
            m_dashTrail.material.color = Color.white;

            // Start disabled
            m_dashTrail.emitting = false;
        }

        private void SetupThrustTrail()
        {
            // Create separate trail renderer for thrust
            if (m_thrustTrail == null)
            {
                GameObject thrustTrailObj = new GameObject("ThrustTrail");
                thrustTrailObj.transform.SetParent(transform);
                thrustTrailObj.transform.localPosition = Vector3.zero;
                m_thrustTrail = thrustTrailObj.AddComponent<TrailRenderer>();
            }

            // Configure thrust trail (orange/flame color)
            m_thrustTrail.time = m_thrustTrailTime;
            m_thrustTrail.startWidth = m_thrustTrailWidth;
            m_thrustTrail.endWidth = 0.05f;
            m_thrustTrail.startColor = m_thrustTrailStartColor;
            m_thrustTrail.endColor = m_thrustTrailEndColor;
            m_thrustTrail.numCornerVertices = 4;
            m_thrustTrail.numCapVertices = 4;
            m_thrustTrail.minVertexDistance = 0.05f;

            // Use default sprites material for trail
            m_thrustTrail.material = new Material(Shader.Find("Sprites/Default"));
            m_thrustTrail.material.color = Color.white;

            // Start disabled
            m_thrustTrail.emitting = false;
        }

        private void SetupAimIndicator()
        {
            // Create aim indicator object
            m_aimIndicator = new GameObject("AimIndicator");
            m_aimIndicator.transform.SetParent(transform);
            m_aimIndicator.transform.localPosition = Vector3.zero;

            // Add line renderer for aim line
            m_aimLine = m_aimIndicator.AddComponent<LineRenderer>();
            m_aimLine.positionCount = 2;
            m_aimLine.startWidth = 0.08f;
            m_aimLine.endWidth = 0.02f;
            m_aimLine.material = new Material(Shader.Find("Sprites/Default"));

            // Cyan color matching player - use cached colors
            m_aimLine.startColor = s_aimColor;
            m_aimLine.endColor = new Color(s_aimColor.r, s_aimColor.g, s_aimColor.b, 0.1f);
            m_aimLine.sortingOrder = 5;

            m_aimLine.useWorldSpace = true;
        }

        private void OnDestroy()
        {
            if (m_input != null)
            {
                m_input.OnDashPressed -= TryDash;
                m_input.OnThrustPressed -= OnThrustPressed;
                m_input.OnThrustReleased -= OnThrustReleased;
            }
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Re-enable movement (in case it was disabled on death)
            enabled = true;

            // Reset state
            m_currentVelocity = Vector2.zero;
            m_rb.linearVelocity = Vector2.zero;
            m_isDashing = false;
            m_isThrusting = false;
            m_dashCooldownTimer = 0f;

            // Show aim indicator when game starts/restarts
            ShowAimIndicator();
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            // DISABLE MOVEMENT - Stop all input processing
            enabled = false;

            // Hide aim indicator when player dies
            HideAimIndicator();

            // Clear movement
            m_currentVelocity = Vector2.zero;
            m_rb.linearVelocity = Vector2.zero;
            m_isDashing = false;
            m_isThrusting = false;

            // Hide trails
            if (m_dashTrail != null)
            {
                m_dashTrail.emitting = false;
                m_dashTrail.Clear();
            }
            if (m_thrustTrail != null)
            {
                m_thrustTrail.emitting = false;
                m_thrustTrail.Clear();
            }
        }

        /// <summary>
        /// Hide the aim indicator (called on death)
        /// </summary>
        public void HideAimIndicator()
        {
            if (m_aimLine != null)
            {
                m_aimLine.enabled = false;
            }
            if (m_aimIndicator != null)
            {
                m_aimIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Show the aim indicator (called on respawn/reset)
        /// </summary>
        public void ShowAimIndicator()
        {
            if (m_aimLine != null)
            {
                m_aimLine.enabled = true;
            }
            if (m_aimIndicator != null)
            {
                m_aimIndicator.SetActive(true);
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
            if (m_isDashing)
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
            if (m_input == null) return;

            // Get aim direction from input manager
            Vector2 inputAim = m_input.GetAimDirection();

            // Different behavior based on control scheme
            Vector2 targetAim = m_rawAimDirection; // Default: maintain last aim

            switch (CurrentControlScheme)
            {
                case NeuralBreak.Config.ControlScheme.TwinStick:
                case NeuralBreak.Config.ControlScheme.FaceMovement:
                    // Twin-stick: Only update aim when stick is moved
                    // When stick is centered, maintain last aim direction
                    if (inputAim.sqrMagnitude > 0.01f)
                    {
                        targetAim = inputAim.normalized;
                    }
                    // else: keep targetAim = m_rawAimDirection (maintain last aim)
                    break;

                case NeuralBreak.Config.ControlScheme.ClassicRotate:
                case NeuralBreak.Config.ControlScheme.TankControls:
                    // These modes handle aim in their movement functions
                    // TankControls still updates from mouse/stick
                    if (CurrentControlScheme == NeuralBreak.Config.ControlScheme.TankControls && inputAim.sqrMagnitude > 0.01f)
                    {
                        targetAim = inputAim.normalized;
                    }
                    break;
            }

            // Store RAW aim direction for shooting (instant, no lag)
            m_rawAimDirection = targetAim;

            // Smooth aim direction for VISUAL rotation only (prevents jittery sprite)
            float aimSmoothing = 25f; // Higher = faster response
            m_aimDirection = Vector2.Lerp(m_aimDirection, targetAim, aimSmoothing * Time.deltaTime);

            // Ensure directions stay normalized
            if (m_aimDirection.sqrMagnitude > 0.01f)
            {
                m_aimDirection = m_aimDirection.normalized;
            }
            if (m_rawAimDirection.sqrMagnitude > 0.01f)
            {
                m_rawAimDirection = m_rawAimDirection.normalized;
            }
        }

        /// <summary>
        /// Update aim indicator visual
        /// </summary>
        private void UpdateAimIndicator()
        {
            if (m_aimLine == null) return;

            // Show aim line
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)m_aimDirection * 2.5f;

            m_aimLine.SetPosition(0, start);
            m_aimLine.SetPosition(1, end);
        }

        /// <summary>
        /// Rotate player sprite based on control scheme
        /// </summary>
        private void UpdatePlayerRotation()
        {
            Vector2 rotationDirection = Vector2.zero;

            switch (CurrentControlScheme)
            {
                case NeuralBreak.Config.ControlScheme.TwinStick:
                    // Face aim direction (classic twin-stick)
                    rotationDirection = m_aimDirection;
                    break;

                case NeuralBreak.Config.ControlScheme.FaceMovement:
                    // Face movement direction (no strafing visual)
                    rotationDirection = m_currentVelocity;
                    // Fall back to aim direction if stationary
                    if (rotationDirection.sqrMagnitude < 0.1f)
                    {
                        rotationDirection = m_aimDirection;
                    }
                    break;

                case NeuralBreak.Config.ControlScheme.ClassicRotate:
                case NeuralBreak.Config.ControlScheme.TankControls:
                    // Use manual rotation from input
                    transform.rotation = Quaternion.Euler(0f, 0f, m_currentRotation);
                    return; // Early return, rotation is already set
            }

            // Apply rotation for TwinStick and FaceMovement modes
            if (rotationDirection.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(rotationDirection.y, rotationDirection.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        #region Movement

        private void UpdateMovement()
        {
            if (m_input == null)
            {
                // Try to get InputManager again if it was null
                m_input = InputManager.Instance;
                if (m_input == null) return;
            }

            // Route to appropriate control scheme
            switch (CurrentControlScheme)
            {
                case NeuralBreak.Config.ControlScheme.TwinStick:
                case NeuralBreak.Config.ControlScheme.FaceMovement:
                    UpdateMovement_TwinStick();
                    break;

                case NeuralBreak.Config.ControlScheme.ClassicRotate:
                    UpdateMovement_ClassicRotate();
                    break;

                case NeuralBreak.Config.ControlScheme.TankControls:
                    UpdateMovement_TankControls();
                    break;
            }
        }

        /// <summary>
        /// Twin-stick movement: WASD moves in any direction
        /// </summary>
        private void UpdateMovement_TwinStick()
        {
            Vector2 inputDir = m_input.MoveInput;
            float targetSpeed = BaseSpeed * m_currentSpeedMultiplier * m_thrustMultiplier;

            if (inputDir.sqrMagnitude > 0.01f)
            {
                // Accelerate toward target velocity
                Vector2 targetVelocity = inputDir.normalized * targetSpeed;
                m_currentVelocity = Vector2.MoveTowards(
                    m_currentVelocity,
                    targetVelocity,
                    Acceleration * Time.fixedDeltaTime
                );

                // Track last direction for firing
                m_lastMoveDirection = inputDir.normalized;
            }
            else
            {
                // Decelerate to stop
                m_currentVelocity = Vector2.MoveTowards(
                    m_currentVelocity,
                    Vector2.zero,
                    Deceleration * Time.fixedDeltaTime
                );
            }

            m_rb.linearVelocity = m_currentVelocity;
        }

        /// <summary>
        /// Classic Asteroids-style rotation: A/D rotates, W/S moves forward/back
        /// </summary>
        private void UpdateMovement_ClassicRotate()
        {
            Vector2 inputDir = m_input.MoveInput;
            float targetSpeed = BaseSpeed * m_currentSpeedMultiplier * m_thrustMultiplier;

            // Rotation from left/right input
            float rotationInput = inputDir.x;
            float rotationSpeed = 180f; // degrees per second
            m_currentRotation -= rotationInput * rotationSpeed * Time.fixedDeltaTime;

            // Calculate forward direction from rotation
            float radians = m_currentRotation * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));

            // Movement from forward/back input
            float thrustInput = inputDir.y;

            if (Mathf.Abs(thrustInput) > 0.01f)
            {
                // Move in facing direction
                Vector2 targetVelocity = forward * thrustInput * targetSpeed;
                m_currentVelocity = Vector2.MoveTowards(
                    m_currentVelocity,
                    targetVelocity,
                    Acceleration * Time.fixedDeltaTime
                );

                m_lastMoveDirection = forward;
            }
            else
            {
                // Decelerate
                m_currentVelocity = Vector2.MoveTowards(
                    m_currentVelocity,
                    Vector2.zero,
                    Deceleration * Time.fixedDeltaTime
                );
            }

            m_rb.linearVelocity = m_currentVelocity;

            // Update aim direction to match facing
            m_rawAimDirection = forward;
            m_aimDirection = forward;
        }

        /// <summary>
        /// Tank controls: W/S moves forward/back, A/D rotates ship
        /// </summary>
        private void UpdateMovement_TankControls()
        {
            Vector2 inputDir = m_input.MoveInput;
            float targetSpeed = BaseSpeed * m_currentSpeedMultiplier * m_thrustMultiplier;

            // Rotation from left/right input
            float rotationInput = inputDir.x;
            float rotationSpeed = 120f; // degrees per second (slightly slower than classic)
            m_currentRotation -= rotationInput * rotationSpeed * Time.fixedDeltaTime;

            // Calculate forward direction from rotation
            float radians = m_currentRotation * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));

            // Movement from forward/back input
            float moveInput = inputDir.y;

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                // Move in facing direction
                Vector2 targetVelocity = forward * moveInput * targetSpeed;
                m_currentVelocity = Vector2.MoveTowards(
                    m_currentVelocity,
                    targetVelocity,
                    Acceleration * Time.fixedDeltaTime
                );

                m_lastMoveDirection = forward;
            }
            else
            {
                // Decelerate
                m_currentVelocity = Vector2.MoveTowards(
                    m_currentVelocity,
                    Vector2.zero,
                    Deceleration * Time.fixedDeltaTime
                );
            }

            m_rb.linearVelocity = m_currentVelocity;

            // Aim is independent for tank controls (can still aim with mouse/right stick)
        }

        private void EnforceBoundary()
        {
            Vector2 pos = m_rb.position;
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
                m_rb.linearVelocity += boundaryForce * Time.fixedDeltaTime * 10f;

                // Hard clamp as last resort
                pos.x = Mathf.Clamp(pos.x, -ArenaBoundary - 1f, ArenaBoundary + 1f);
                pos.y = Mathf.Clamp(pos.y, -ArenaBoundary - 1f, ArenaBoundary + 1f);
                m_rb.position = pos;
            }
        }

        #endregion

        #region Dash

        private void TryDash()
        {
            if (m_isDashing || m_dashCooldownTimer > 0f) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            // Use current move input or last direction
            Vector2 dashDir = m_input.HasMoveInput()
                ? m_input.MoveInput.normalized
                : m_lastMoveDirection;

            StartDash(dashDir);
        }

        private void StartDash(Vector2 direction)
        {
            m_isDashing = true;
            m_dashTimer = DashDuration;
            m_dashDirection = direction;
            m_dashCooldownTimer = DashCooldown;
            m_dashReady = false;

            // Enable dash trail
            if (m_dashTrail != null)
            {
                m_dashTrail.Clear();
                m_dashTrail.emitting = true;
            }

            // Feedback (Feel removed)

            // Publish event
            EventBus.Publish(new PlayerDashedEvent { direction = direction });
        }

        private void UpdateDash()
        {
            m_dashTimer -= Time.fixedDeltaTime;

            if (m_dashTimer <= 0f)
            {
                EndDash();
                return;
            }

            // Move at dash speed
            m_rb.linearVelocity = m_dashDirection * DashSpeed;
            m_currentVelocity = m_rb.linearVelocity;
        }

        private void EndDash()
        {
            m_isDashing = false;

            // Disable dash trail
            if (m_dashTrail != null)
            {
                m_dashTrail.emitting = false;
            }

            // Reduce velocity after dash
            m_currentVelocity = m_dashDirection * (BaseSpeed * 0.5f);
            m_rb.linearVelocity = m_currentVelocity;
        }

        private void UpdateDashCooldown()
        {
            if (m_dashCooldownTimer > 0f)
            {
                m_dashCooldownTimer -= Time.deltaTime;

                if (m_dashCooldownTimer <= 0f && !m_dashReady)
                {
                    m_dashReady = true;
                    // Feedback (Feel removed)
                }
            }
        }

        #endregion

        #region Thrust

        private void OnThrustPressed()
        {
            if (m_isDashing) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            m_isThrusting = true;

            // Enable thrust trail
            if (m_thrustTrail != null)
            {
                m_thrustTrail.Clear();
                m_thrustTrail.emitting = true;
            }

            // Publish event
            EventBus.Publish(new PlayerThrustStartedEvent());
        }

        private void OnThrustReleased()
        {
            m_isThrusting = false;

            // Publish event
            EventBus.Publish(new PlayerThrustEndedEvent());
        }

        private void UpdateThrust()
        {
            float targetMultiplier = m_isThrusting ? ThrustSpeedMultiplier : 1f;

            if (m_thrustMultiplier < targetMultiplier)
            {
                // Accelerating thrust
                float accelRate = (ThrustSpeedMultiplier - 1f) / ThrustAccelTime;
                m_thrustMultiplier = Mathf.Min(targetMultiplier, m_thrustMultiplier + accelRate * Time.deltaTime);
            }
            else if (m_thrustMultiplier > targetMultiplier)
            {
                // Decelerating thrust
                float decelRate = (ThrustSpeedMultiplier - 1f) / ThrustDecelTime;
                m_thrustMultiplier = Mathf.Max(targetMultiplier, m_thrustMultiplier - decelRate * Time.deltaTime);
            }

            // Update thrust trail based on current multiplier
            if (m_thrustTrail != null)
            {
                bool shouldEmit = m_thrustMultiplier > 1.05f;
                if (m_thrustTrail.emitting != shouldEmit)
                {
                    m_thrustTrail.emitting = shouldEmit;
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
            m_currentSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        /// <summary>
        /// Add to speed multiplier (from speed-up pickups)
        /// </summary>
        public void AddSpeedBonus(float bonus)
        {
            m_currentSpeedMultiplier += bonus;
        }

        /// <summary>
        /// Reset speed to base
        /// </summary>
        public void ResetSpeed()
        {
            m_currentSpeedMultiplier = 1f;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Teleport player to position
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            m_rb.position = position;
            m_currentVelocity = Vector2.zero;
            m_rb.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// Stop all movement
        /// </summary>
        public void Stop()
        {
            m_currentVelocity = Vector2.zero;
            m_rb.linearVelocity = Vector2.zero;
            m_isDashing = false;
        }

        /// <summary>
        /// Check if player is invulnerable (during dash)
        /// </summary>
        public bool IsInvulnerable()
        {
            return m_isDashing;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw arena boundary (use config if available, fallback to 25)
            float boundary = ConfigProvider.Player?.arenaRadius ?? 25f;
            Gizmos.color = Color.cyan;

            // Zero-allocation: use cached Vector3 and Set() method
            m_cachedGizmoVector.Set(boundary * 2, boundary * 2, 0);
            Gizmos.DrawWireCube(Vector3.zero, m_cachedGizmoVector);

            // Draw move direction (yellow)
            Gizmos.color = Color.yellow;
            Vector3 pos = transform.position;
            Gizmos.DrawLine(pos, pos + (Vector3)m_lastMoveDirection * 1.5f);

            // Draw aim direction (cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pos, pos + (Vector3)m_aimDirection * 2.5f);
        }

        #endregion
    }
}
