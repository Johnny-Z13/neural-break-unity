using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Z13.Core;

namespace NeuralBreak.Input
{
    /// <summary>
    /// Twin-stick shooter input manager.
    /// Left stick/WASD = movement, Right stick/Mouse = aim direction.
    /// Supports keyboard+mouse and gamepad input.
    ///
    /// TRUE SINGLETON - Lives in Boot scene, persists across all scenes.
    /// </summary>
    public class InputManager : MonoBehaviour, IBootable
    {
        public static InputManager Instance { get; private set; }

        [Header("Input Settings")]
        [SerializeField] private float m_gamepadDeadzone = 0.15f;
        [SerializeField] private bool m_autoFireWhenAiming = false; // Disabled - require explicit fire input

        [Header("Mouse Settings")]
        [SerializeField] private bool m_useMouseForAim = true;

        [Header("Input Actions Asset")]
        [SerializeField] private InputActionAsset m_inputActionsAsset;

        // Input action references
        private InputAction m_moveAction;
        private InputAction m_lookAction;
        private InputAction m_attackAction;
        private InputAction m_thrustAction;
        private InputAction m_dashAction;
        private InputAction m_smartBombAction;
        private InputAction m_submitAction;
        private InputAction m_cancelAction;

        // Current input values
        public Vector2 MoveInput { get; private set; }
        public Vector2 AimInput { get; private set; }
        public Vector2 AimDirection { get; private set; }
        public bool FireHeld { get; private set; }
        public bool ThrustHeld { get; private set; }
        public bool DashPressed { get; private set; }
        public bool PausePressed { get; private set; }

        // Twin-stick state
        public bool HasAimInput { get; private set; }
        public bool IsUsingGamepad { get; private set; }

        // Events for input actions
        public event Action OnFirePressed;
        public event Action OnFireReleased;
        public event Action OnThrustPressed;
        public event Action OnThrustReleased;
        public event Action OnDashPressed;
        public event Action OnSmartBombPressed;
        public event Action OnPausePressed;
        public event Action OnConfirmPressed;
        public event Action OnCancelPressed;

        // Reference to player transform for mouse aim calculation
        private Transform m_playerTransform;

        /// <summary>
        /// Called by BootManager for controlled initialization order.
        /// </summary>
        public void Initialize()
        {
            Instance = this;

            // Initialize aim direction to up (12 o'clock)
            AimDirection = Vector2.up;
            AimInput = Vector2.up;

            SetupInputActions();
            Debug.Log("[InputManager] Initialized via BootManager");
        }

        private void Awake()
        {
            // If already initialized by BootManager, skip
            if (Instance == this) return;

            // Fallback for running main scene directly (development only)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Development fallback - initialize directly
            Initialize();
            Debug.LogWarning("[InputManager] Initialized via Awake fallback - should use Boot scene in production");
        }

        private void SetupInputActions()
        {
            if (m_inputActionsAsset == null)
            {
                Debug.LogWarning("[InputManager] No InputActionAsset assigned, using keyboard fallback only");
                return;
            }

            // Get actions from the asset
            m_moveAction = m_inputActionsAsset.FindAction("Player/Move");
            m_lookAction = m_inputActionsAsset.FindAction("Player/Look");
            m_attackAction = m_inputActionsAsset.FindAction("Player/Attack");
            m_thrustAction = m_inputActionsAsset.FindAction("Player/Thrust");
            m_dashAction = m_inputActionsAsset.FindAction("Player/Dash");
            m_smartBombAction = m_inputActionsAsset.FindAction("Player/SmartBomb");
            m_submitAction = m_inputActionsAsset.FindAction("UI/Submit");
            m_cancelAction = m_inputActionsAsset.FindAction("UI/Cancel");
        }

        private void OnEnable()
        {
            // Reset input state to prevent auto-firing on game start
            FireHeld = false;
            ThrustHeld = false;
            MoveInput = Vector2.zero;
            AimInput = Vector2.zero;
            HasAimInput = false;

            if (m_inputActionsAsset != null)
            {
                m_inputActionsAsset.Enable();
            }

            // Subscribe to actions
            if (m_moveAction != null)
            {
                m_moveAction.performed += OnMove;
                m_moveAction.canceled += OnMove;
            }

            if (m_lookAction != null)
            {
                m_lookAction.performed += OnLook;
                m_lookAction.canceled += OnLook;
            }

            if (m_attackAction != null)
            {
                m_attackAction.performed += OnAttack;
                m_attackAction.canceled += OnAttackCanceled;
            }

            if (m_thrustAction != null)
            {
                m_thrustAction.performed += OnThrustPerformed;
                m_thrustAction.canceled += OnThrustCanceled;
            }

            if (m_dashAction != null)
            {
                m_dashAction.performed += OnDashPerformed;
            }

            if (m_smartBombAction != null)
            {
                m_smartBombAction.performed += OnSmartBombPerformed;
                Debug.Log("[InputManager] SmartBomb action subscribed successfully!");
            }
            else
            {
                Debug.LogError("[InputManager] SmartBomb action is NULL! Check InputActionAsset has 'Player/SmartBomb' action.");
            }

            if (m_submitAction != null)
            {
                m_submitAction.performed += OnSubmit;
            }

            if (m_cancelAction != null)
            {
                m_cancelAction.performed += OnCancel;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from actions
            if (m_moveAction != null)
            {
                m_moveAction.performed -= OnMove;
                m_moveAction.canceled -= OnMove;
            }

            if (m_lookAction != null)
            {
                m_lookAction.performed -= OnLook;
                m_lookAction.canceled -= OnLook;
            }

            if (m_attackAction != null)
            {
                m_attackAction.performed -= OnAttack;
                m_attackAction.canceled -= OnAttackCanceled;
            }

            if (m_thrustAction != null)
            {
                m_thrustAction.performed -= OnThrustPerformed;
                m_thrustAction.canceled -= OnThrustCanceled;
            }

            if (m_dashAction != null)
            {
                m_dashAction.performed -= OnDashPerformed;
            }

            if (m_smartBombAction != null)
            {
                m_smartBombAction.performed -= OnSmartBombPerformed;
            }

            if (m_submitAction != null)
            {
                m_submitAction.performed -= OnSubmit;
            }

            if (m_cancelAction != null)
            {
                m_cancelAction.performed -= OnCancel;
            }

            if (m_inputActionsAsset != null)
            {
                m_inputActionsAsset.Disable();
            }
        }

        private void Update()
        {
            // Auto-find player transform if not set
            if (m_playerTransform == null)
            {
                var player = UnityEngine.Object.FindFirstObjectByType<NeuralBreak.Entities.PlayerController>();
                if (player != null)
                {
                    m_playerTransform = player.transform;
                    Debug.Log("[InputManager] Auto-found player transform");
                }
            }

            // Handle keyboard inputs directly
            HandleKeyboardInput();

            // Poll gamepad movement directly (backup for Input System)
            PollGamepadMovement();

            // Update aim direction (twin-stick)
            UpdateAimDirection();
        }

        private void LateUpdate()
        {
            // Reset single-frame inputs
            DashPressed = false;
            PausePressed = false;
        }

        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                Debug.LogWarning("[InputManager] No keyboard detected!");
                return;
            }

            // WASD movement fallback (if no input action asset)
            if (m_moveAction == null)
            {
                Vector2 move = Vector2.zero;
                if (keyboard.wKey.isPressed) move.y += 1;
                if (keyboard.sKey.isPressed) move.y -= 1;
                if (keyboard.aKey.isPressed) move.x -= 1;
                if (keyboard.dKey.isPressed) move.x += 1;
                
                if (move != Vector2.zero)
                {
                    Debug.Log($"[InputManager] WASD input detected: {move}");
                }
                MoveInput = move.normalized;
            }

            // Thrust (Shift key - hold)
            if (m_thrustAction == null)
            {
                bool shiftHeld = keyboard.leftShiftKey.isPressed;
                if (shiftHeld && !ThrustHeld)
                {
                    ThrustHeld = true;
                    OnThrustPressed?.Invoke();
                }
                else if (!shiftHeld && ThrustHeld)
                {
                    ThrustHeld = false;
                    OnThrustReleased?.Invoke();
                }
            }

            // Fire (Mouse left button - hold to fire)
            if (m_attackAction == null)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    bool mouseHeld = mouse.leftButton.isPressed;
                    FireHeld = mouseHeld;
                }
            }

            // Dash (Space key)
            if (m_dashAction == null && keyboard.spaceKey.wasPressedThisFrame)
            {
                DashPressed = true;
                OnDashPressed?.Invoke();
            }

            // Pause (Escape key)
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                PausePressed = true;
                OnPausePressed?.Invoke();
            }

            // Detect gamepad input (check both sticks)
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float leftStickMag = gamepad.leftStick.ReadValue().magnitude;
                float rightStickMag = gamepad.rightStick.ReadValue().magnitude;
                if (leftStickMag > m_gamepadDeadzone || rightStickMag > m_gamepadDeadzone)
                {
                    IsUsingGamepad = true;
                }
            }

            if (Mouse.current != null && Mouse.current.delta.ReadValue().magnitude > 0.1f)
            {
                IsUsingGamepad = false;
            }
        }

        /// <summary>
        /// Poll gamepad left stick directly for movement (backup)
        /// </summary>
        private void PollGamepadMovement()
        {
            var gamepad = Gamepad.current;
            if (gamepad == null) return;

            Vector2 leftStick = gamepad.leftStick.ReadValue();
            if (leftStick.magnitude > m_gamepadDeadzone)
            {
                // Gamepad has priority when actively used
                MoveInput = leftStick;
                IsUsingGamepad = true;
            }
        }

        /// <summary>
        /// Calculate aim direction from right stick (gamepad) or mouse position
        /// </summary>
        private void UpdateAimDirection()
        {
            // Start with current aim direction (maintain last aim when no input)
            Vector2 aimDir = AimDirection;
            bool hasNewInput = false;

            // Check for gamepad right stick input
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 rightStick = gamepad.rightStick.ReadValue();
                if (rightStick.magnitude > m_gamepadDeadzone)
                {
                    aimDir = rightStick.normalized;
                    HasAimInput = true;
                    IsUsingGamepad = true;
                    hasNewInput = true;

                    // Auto-fire when aiming with right stick
                    if (m_autoFireWhenAiming && !FireHeld)
                    {
                        FireHeld = true;
                        OnFirePressed?.Invoke();
                    }
                }
                else if (IsUsingGamepad)
                {
                    // Stop firing when right stick released (gamepad only)
                    if (m_autoFireWhenAiming && FireHeld && !gamepad.rightTrigger.isPressed)
                    {
                        FireHeld = false;
                        OnFireReleased?.Invoke();
                    }
                    HasAimInput = false;
                    // Don't update aimDir - maintain last direction
                }
            }

            // Mouse aim (when not using gamepad or mouse is primary)
            if (m_useMouseForAim && !IsUsingGamepad)
            {
                var mouse = Mouse.current;
                if (mouse != null && m_playerTransform != null)
                {
                    // Get mouse world position
                    Vector2 mouseScreenPos = mouse.position.ReadValue();
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
                        Vector2 playerPos = m_playerTransform.position;
                        Vector2 toMouse = (Vector2)mouseWorldPos - playerPos;

                        if (toMouse.magnitude > 0.1f)
                        {
                            aimDir = toMouse.normalized;
                            HasAimInput = true;
                            hasNewInput = true;
                        }
                    }
                }

                // Mouse button fires (left click)
                if (mouse != null)
                {
                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        FireHeld = true;
                        OnFirePressed?.Invoke();
                    }
                    else if (mouse.leftButton.wasReleasedThisFrame)
                    {
                        FireHeld = false;
                        OnFireReleased?.Invoke();
                    }
                }
            }

            // Only update AimDirection if we have new input or if it's uninitialized
            if (hasNewInput || AimDirection.sqrMagnitude < 0.01f)
            {
                AimDirection = aimDir.normalized;
                AimInput = aimDir;
            }
        }

        #region Input Callbacks

        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();

            // Apply deadzone for gamepad
            if (input.magnitude < m_gamepadDeadzone)
            {
                input = Vector2.zero;
            }

            MoveInput = input;
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            AimInput = context.ReadValue<Vector2>();
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            FireHeld = true;
            OnFirePressed?.Invoke();
        }

        private void OnAttackCanceled(InputAction.CallbackContext context)
        {
            FireHeld = false;
            OnFireReleased?.Invoke();
        }

        private void OnThrustPerformed(InputAction.CallbackContext context)
        {
            ThrustHeld = true;
            OnThrustPressed?.Invoke();
        }

        private void OnThrustCanceled(InputAction.CallbackContext context)
        {
            ThrustHeld = false;
            OnThrustReleased?.Invoke();
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            DashPressed = true;
            OnDashPressed?.Invoke();
        }

        private void OnSmartBombPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[InputManager] SmartBomb input received!");
            OnSmartBombPressed?.Invoke();
        }

        private void OnSubmit(InputAction.CallbackContext context)
        {
            OnConfirmPressed?.Invoke();
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            OnCancelPressed?.Invoke();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the player transform for mouse aim calculations
        /// </summary>
        public void SetPlayerTransform(Transform playerTransform)
        {
            m_playerTransform = playerTransform;
        }

        /// <summary>
        /// Get movement as a normalized Vector3 (for 3D movement)
        /// </summary>
        public Vector3 GetMoveDirection()
        {
            return new Vector3(MoveInput.x, 0f, MoveInput.y).normalized;
        }

        /// <summary>
        /// Get movement as Vector2 (for 2D/top-down)
        /// </summary>
        public Vector2 GetMoveDirection2D()
        {
            return MoveInput.normalized;
        }

        /// <summary>
        /// Get the aim direction (right stick or mouse direction)
        /// </summary>
        public Vector2 GetAimDirection()
        {
            return AimDirection;
        }

        /// <summary>
        /// Check if any movement input is active
        /// </summary>
        public bool HasMoveInput()
        {
            return MoveInput.sqrMagnitude > m_gamepadDeadzone * m_gamepadDeadzone;
        }

        /// <summary>
        /// Check if player is actively aiming (right stick or mouse aim)
        /// </summary>
        public bool HasActiveAim()
        {
            return HasAimInput;
        }

        #endregion
    }
}
