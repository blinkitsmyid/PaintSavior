using UnityEngine;
using UnityEngine.InputSystem;

using Game.Items;

namespace Game.Player
{
    /// <summary>
    /// Обрабатывает выбор доступного типа ягоды клавишами 1, 2 и 3 при экипированной рогатке.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BerrySelectionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BerryInventory inventory;
        [SerializeField] private PlayerEquipment equipment;

        private PlayerInputActions _inputActions;

        /// <summary>
        /// Создаёт действия ввода и автоматически находит инвентарь и экипировку игрока.
        /// </summary>
        private void Awake()
        {
            _inputActions =
                new PlayerInputActions();

            if (inventory == null)
            {
                inventory =
                    GetComponent<BerryInventory>();
            }

            if (equipment == null)
            {
                equipment =
                    GetComponent<PlayerEquipment>();
            }
        }

        /// <summary>
        /// Включает карту ввода и подписывается на клавиши выбора ягод.
        /// </summary>
        private void OnEnable()
        {
            _inputActions.Player.SelectBerry1.performed +=
                OnSelectBerry1;

            _inputActions.Player.SelectBerry2.performed +=
                OnSelectBerry2;

            _inputActions.Player.SelectBerry3.performed +=
                OnSelectBerry3;

            _inputActions.Player.Enable();

            Debug.Log(
                $"{name}: BerrySelectionController enabled."
            );
        }

        /// <summary>
        /// Отписывается от клавиш выбора и отключает карту ввода.
        /// </summary>
        private void OnDisable()
        {
            if (_inputActions == null)
            {
                return;
            }

            _inputActions.Player.SelectBerry1.performed -=
                OnSelectBerry1;

            _inputActions.Player.SelectBerry2.performed -=
                OnSelectBerry2;

            _inputActions.Player.SelectBerry3.performed -=
                OnSelectBerry3;

            _inputActions.Player.Disable();
        }

        /// <summary>
        /// Освобождает экземпляр Input System после уничтожения компонента.
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
        /// Выбирает красный тип ягоды по клавише 1.
        /// </summary>
        private void OnSelectBerry1(
            InputAction.CallbackContext context)
        {
            Debug.Log(
                $"{name}: SelectBerry1 input received."
            );

            TrySelectBerry(BerryType.Red);
        }

        /// <summary>
        /// Выбирает синий тип ягоды по клавише 2.
        /// </summary>
        private void OnSelectBerry2(
            InputAction.CallbackContext context)
        {
            Debug.Log(
                $"{name}: SelectBerry2 input received."
            );

            TrySelectBerry(BerryType.Blue);
        }

        /// <summary>
        /// Выбирает жёлтый тип ягоды по клавише 3.
        /// </summary>
        private void OnSelectBerry3(
            InputAction.CallbackContext context)
        {
            Debug.Log(
                $"{name}: SelectBerry3 input received."
            );

            TrySelectBerry(BerryType.Yellow);
        }

        /// <summary>
        /// Передаёт выбор инвентарю только при наличии рогатки и хотя бы одной ягоды выбранного типа.
        /// </summary>
        private void TrySelectBerry(
            BerryType type)
        {
            if (inventory == null ||
                equipment == null ||
                !equipment.IsSlingshotEquipped)
            {
                Debug.LogWarning(
                    $"{name}: berry selection blocked. " +
                    $"Inventory: {inventory != null}; " +
                    $"Equipment: {equipment != null}; " +
                    $"Slingshot equipped: " +
                    $"{equipment != null && equipment.IsSlingshotEquipped}."
                );

                return;
            }

            bool selected =
                inventory.TrySelectBerry(type);

            Debug.Log(
                $"{name}: selecting {type}. " +
                $"Amount: {inventory.GetAmount(type)}; " +
                $"Selected: {inventory.SelectedBerryType}; " +
                $"Changed: {selected}."
            );
        }
    }
}
