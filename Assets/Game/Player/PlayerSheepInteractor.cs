using UnityEngine;
using UnityEngine.InputSystem;

using Game.Animals.Core;
using Game.Paint;

namespace Game.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerSheepInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PaintShooter paintShooter;
        [SerializeField] private Camera playerCamera;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private PlayerInputActions _inputActions;

        /// <summary>
        /// Находит ссылки на стрельбу и камеру игрока.
        /// </summary>
        private void Awake()
        {
            _inputActions = new PlayerInputActions();

            if (paintShooter == null)
            {
                paintShooter = GetComponent<PaintShooter>();
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        /// <summary>
        /// Подписывается на действие игрока без оружия для запуска взаимодействия.
        /// </summary>
        private void OnEnable()
        {
            if (paintShooter != null)
            {
                paintShooter.UnarmedPrimaryAction += TryPetSheep;
            }

            _inputActions.Player.Interact.performed += OnInteract;
            _inputActions.Player.Enable();
        }

        /// <summary>
        /// Отписывается от действия игрока при отключении компонента.
        /// </summary>
        private void OnDisable()
        {
            if (paintShooter != null)
            {
                paintShooter.UnarmedPrimaryAction -= TryPetSheep;
            }

            if (_inputActions == null)
            {
                return;
            }

            _inputActions.Player.Interact.performed -= OnInteract;
            _inputActions.Player.Disable();
        }

        /// <summary>
        /// Освобождает экземпляр действий ввода после уничтожения компонента.
        /// </summary>
        private void OnDestroy()
        {
            if (_inputActions == null)
            {
                return;
            }

            _inputActions.Dispose();
            _inputActions = null;
        }

        /// <summary>
        /// Выполняет Raycast и передаёт животному команду поглаживания.
        /// </summary>
        private void TryPetSheep()
        {
            if (playerCamera == null)
            {
                return;
            }

            Ray ray = playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            IAnimalInteractable animal =
                hit.collider.GetComponentInParent(
                    typeof(IAnimalInteractable)
                ) as IAnimalInteractable;

            if (animal != null)
            {
                animal.TryInteract(
                    AnimalInteractionType.Pet,
                    transform
                );
            }
        }

        /// <summary>
        /// Передаёт овце команду стрижки при нажатии клавиши E.
        /// </summary>
        private void OnInteract(
            InputAction.CallbackContext context)
        {
            if (playerCamera == null)
            {
                return;
            }

            Ray ray = playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            IAnimalInteractable animal =
                hit.collider.GetComponentInParent(
                    typeof(IAnimalInteractable)
                ) as IAnimalInteractable;

            if (animal != null)
            {
                animal.TryInteract(
                    AnimalInteractionType.Shear,
                    transform
                );
            }
        }
    }
}
