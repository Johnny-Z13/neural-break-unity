using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace NeuralBreak.Input
{
    /// <summary>
    /// Twin-stick shooter input manager.
    /// Left stick/WASD = movement, Right stick/Mouse = aim direction.
    /// Supports keyboard+mouse and gamepad input.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Input Settings")]
        [SerializeField] private float _gamepadDeadzone = 0.15f;
        [SerializeField] private bool _autoFireWhenAiming = true;

        [Header("Mouse Settings")]
        [SerializeField] private bool _useMouseForAim = true;

        [Header("Input Actions Asset")]
        [SerializeField] private InputActionAsset _inputActionsAsset;

        // Input action references
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _attackAction;
        private InputAction _thrustAction;
        private InputAction _dashAction;
        private InputAction _submitAction;
        private InputAction _cancelAction;

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
        public event Action OnPausePressed;
        public event Action OnConfirmPressed;
        public event Action OnCancelPressed;

        // Reference to player transform for mouse aim calculation
        private Transform _playerTransform;

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupInputActions();
        }

        private void SetupInputActions()
        {
            if (_inputActionsAsset == null)
            {
                Debug.LogWarning("[InputManager] No InputActionAsset assigned, using keyboard fallback only");
                return;
            }

            // Get actions from the asset
            _moveAction = _inputActionsAsset.FindAction("Player/Move");
            _lookAction = _inputActionsAsset.FindAction("Player/Look");
            _attackAction = _inputActionsAsset.FindAction("Player/Attack");
            _thrustAction = _inputActionsAsset.FindAction("Player/Thrust");
            _dashAction = _inputActionsAsset.FindAction("Player/Dash");
            _submitAction = _inputActionsAsset.FindAction("UI/Submit");
            _cancelAction = _inputActionsAsset.FindAction("UI/Cancel");
        }

        private void OnEnable()
        {
            if (_inputActionsAsset != null)
            {
                _inputActionsAsset.Enable();
            }

            // Subscribe to actions
            if (_moveAction != null)
            {
                _moveAction.performed += OnMove;
                _moveAction.canceled += OnMove;
            }

            if (_lookAction != null)
            {
                _lookAction.performed += OnLook;
                _lookAction.canceled += OnLook;
            }

            if (_attackAction != null)
            {
                _attackAction.performed += OnAttack;
                _attackAction.canceled += OnAttackCanceled;
            }

            if (_thrustAction != null)
            {
                _thrustAction.performed += OnThrustPerformed;
                _thrustAction.canceled += OnThrustCanceled;
            }

            if (_dashAction != null)
            {
                _dashAction.performed += OnDashPerformed;
            }

            if (_submitAction != null)
            {
                _submitAction.performed += OnSubmit;
            }

            if (_cancelAction != null)
            {
                _cancelAction.performed += OnCancel;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from actions
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMove;
            }

            if (_lookAction != null)
            {
                _lookAction.performed -= OnLook;
                _lookAction.canceled -= OnLook;
            }

            if (_attackAction != null)
            {
                _attackAction.performed -= OnAttack;
                _attackAction.canceled -= OnAttackCanceled;
            }

            if (_thrustAction != null)
            {
                _thrustAction.performed -= OnThrustPerformed;
                _thrustAction.canceled -= OnThrustCanceled;
            }

            if (_dashAction != null)
            {
                _dashAction.performed -= OnDashPerformed;
            }

            if (_submitAction != null)
            {
                _submitAction.performed -= OnSubmit;
            }

            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancel;
            }

            if (_inputActionsAsset != null)
            {
                _inputActionsAsset.Disable();
            }
        }

        private void Update()
        {
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
            if (keyboard == null) return;

            // WASD movement fallback (if no input action asset)
            if (_moveAction == null)
            {
                Vector2 move = Vector2.zero;
                if (keyboard.wKey.isPressed) move.y += 1;
                if (keyboard.sKey.isPressed) move.y -= 1;
                if (keyboard.aKey.isPressed) move.x -= 1;
                if (keyboard.dKey.isPressed) move.x += 1;
                MoveInput = move.normalized;
            }

            // Thrust (Shift key - hold)
            if (_thrustAction == null)
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

            // Dash (Space key)
            if (_dashAction == null && keyboard.spaceKey.wasPressedThisFrame)
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
                if (leftStickMag > _gamepadDeadzone || rightStickMag > _gamepadDeadzone)
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
            if (leftStick.magnitude > _gamepadDeadzone)
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
            Vector2 aimDir = Vector2.up; // Default aim direction

            // Check for gamepad right stick input
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 rightStick = gamepad.rightStick.ReadValue();
                if (rightStick.magnitude > _gamepadDeadzone)
                {
                    aimDir = rightStick.normalized;
                    HasAimInput = true;
                    IsUsingGamepad = true;

                    // Auto-fire when aiming with right stick
                    if (_autoFireWhenAiming && !FireHeld)
                    {
                        FireHeld = true;
                        OnFirePressed?.Invoke();
                    }
                }
                else if (IsUsingGamepad)
                {
                    // Stop firing when right stick released (gamepad only)
                    if (_autoFireWhenAiming && FireHeld && !gamepad.rightTrigger.isPressed)
                    {
                        FireHeld = false;
                        OnFireReleased?.Invoke();
                    }
                    HasAimInput = false;
                }
            }

            // Mouse aim (when not using gamepad or mouse is primary)
            if (_useMouseForAim && !IsUsingGamepad)
            {
                var mouse = Mouse.current;
                if (mouse != null && _playerTransform != null)
                {
                    // Get mouse world position
                    Vector2 mouseScreenPos = mouse.position.ReadValue();
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
                        Vector2 playerPos = _playerTransform.position;
                        Vector2 toMouse = (Vector2)mouseWorldPos - playerPos;

                        if (toMouse.magnitude > 0.1f)
                        {
                            aimDir = toMouse.normalized;
                            HasAimInput = true;
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

            // Store normalized aim direction
            AimDirection = aimDir.normalized;
            AimInput = aimDir;
        }

        #region Input Callbacks

        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();

            // Apply deadzone for gamepad
            if (input.magnitude < _gamepadDeadzone)
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
            _playerTransform = playerTransform;
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
            return MoveInput.sqrMagnitude > _gamepadDeadzone * _gamepadDeadzone;
        }

        /// <summary>
        /// Check if player is actively aiming (right stick or mouse aim)
        /// </summary>
        public bool HasActiveAim()
        {
            return HasAimInput;
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
