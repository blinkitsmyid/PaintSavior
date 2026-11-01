using UnityEngine;
using UnityEngine.InputSystem;

using Game.Items;

namespace Game.Player
{
    /// <summary>
    /// Обрабатывает действие Interact и передаёт найденному кусту инвентарь игрока.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerBerryInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private BerryInventory berryInventory;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private PlayerInputActions _inputActions;

        /// <summary>
        /// Создаёт экземпляр действий ввода и автоматически находит ссылки на камеру и инвентарь.
        /// </summary>
        private void Awake()
        {
            _inputActions =
                new PlayerInputActions();

            if (playerCamera == null)
            {
                playerCamera =
                    GetComponentInChildren<Camera>();
            }

            if (berryInventory == null)
            {
                berryInventory =
                    GetComponent<BerryInventory>();
            }
        }

        /// <summary>
        /// Включает карту игрока и подписывается на действие E.
        /// </summary>
        private void OnEnable()
        {
            _inputActions.Player.Interact.performed += OnInteract;
            _inputActions.Player.Enable();
        }

        /// <summary>
        /// Отписывается от действия E и отключает карту игрока.
        /// </summary>
        private void OnDisable()
        {
            if (_inputActions == null)
            {
                return;
            }

            _inputActions.Player.Interact.performed -= OnInteract;
            _inputActions.Player.Disable();
        }

        /// <summary>
        /// Освобождает созданный экземпляр действий Input System после уничтожения компонента.
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
        /// Выполняет взаимодействие с кустом, находящимся в центре экрана на доступной дистанции.
        /// </summary>
        private void OnInteract(
            InputAction.CallbackContext context)
        {
            if (playerCamera == null || berryInventory == null)
            {
                return;
            }

            Ray interactionRay =
                playerCamera.ViewportPointToRay(
                    new Vector3(0.5f, 0.5f, 0f)
                );

            if (!Physics.Raycast(
                    interactionRay,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            BerryBush bush =
                hit.collider.GetComponentInParent<BerryBush>();

            if (bush == null)
            {
                return;
            }

            bush.TryCollect(berryInventory);
        }
    }
}
