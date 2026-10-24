using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -20f;

        [Header("Look")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        private CharacterController _characterController;
        private PlayerInputActions _inputActions;

        private float _verticalVelocity;
        private float _cameraPitch;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            _inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
        }

        private void Update()
        {
            HandleMovement();
            HandleLook();
        }

        private void HandleMovement()
        {
            Vector2 input = _inputActions.Player.Move.ReadValue<Vector2>();

            Vector3 direction =
                transform.right * input.x +
                transform.forward * input.y;

            direction = Vector3.ClampMagnitude(direction, 1f);

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = direction * moveSpeed;
            velocity.y = _verticalVelocity;

            _characterController.Move(velocity * Time.deltaTime);
        }

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