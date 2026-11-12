using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.5f;

        [Header("Look")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        private CharacterController _characterController;
        private PlayerInputActions _inputActions;

        private float _verticalVelocity;
        private float _cameraPitch;

        /// <summary>
        /// Находит CharacterController и создаёт экземпляр действий ввода игрока.
        /// </summary>
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            _inputActions = new PlayerInputActions();
        }

        /// <summary>
        /// Включает карту действий игрока при активации контроллера.
        /// </summary>
        private void OnEnable()
        {
            _inputActions.Player.Enable();
        }

        /// <summary>
        /// Отключает карту действий игрока при деактивации контроллера.
        /// </summary>
        private void OnDisable()
        {
            _inputActions.Player.Disable();
        }

        /// <summary>
        /// Обрабатывает перемещение и вращение игрока каждый кадр.
        /// </summary>
        private void Update()
        {
            HandleMovement();
            HandleLook();
        }

        /// <summary>
        /// Перемещает игрока, переключает скорость по Shift и выполняет прыжок по Space.
        /// </summary>
        private void HandleMovement()
        {
            Vector2 input = _inputActions.Player.Move.ReadValue<Vector2>();

            Vector3 direction =
                transform.right * input.x +
                transform.forward * input.y;

            direction = Vector3.ClampMagnitude(direction, 1f);

            bool isGrounded =
                _characterController.isGrounded;

            bool jumpPressed =
                Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame;

            if (isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            if (isGrounded && jumpPressed)
            {
                _verticalVelocity =
                    Mathf.Sqrt(
                        Mathf.Max(0f, jumpHeight) *
                        -2f *
                        gravity
                    );
            }

            _verticalVelocity += gravity * Time.deltaTime;

            bool isRunning =
                Keyboard.current != null &&
                (Keyboard.current.leftShiftKey.isPressed ||
                 Keyboard.current.rightShiftKey.isPressed);

            float currentSpeed =
                isRunning
                    ? runSpeed
                    : moveSpeed;

            Vector3 velocity = direction * currentSpeed;
            velocity.y = _verticalVelocity;

            _characterController.Move(velocity * Time.deltaTime);
        }

        /// <summary>
        /// Поворачивает игрока и камеру согласно текущему движению мыши.
        /// </summary>
        private void HandleLook()
        {
            Vector2 lookInput = _inputActions.Player.Look.ReadValue<Vector2>();

            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(
                _cameraPitch,
                minPitch,
                maxPitch
            );

            cameraRoot.localRotation =
                Quaternion.Euler(_cameraPitch, 0f, 0f);
        }
    }
}
