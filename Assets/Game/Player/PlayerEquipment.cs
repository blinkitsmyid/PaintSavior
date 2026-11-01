using System;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// Описывает предмет, который сейчас находится в руках игрока.
    /// </summary>
    public enum EquippedTool
    {
        None = 0,
        Slingshot = 1
    }

    /// <summary>
    /// Управляет текущим предметом в руках игрока и переключением рогатки по клавише Q.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private bool startWithPaintGun;

        private PlayerInputActions _inputActions;

        public EquippedTool CurrentTool { get; private set; }

        public bool IsSlingshotEquipped =>
            CurrentTool == EquippedTool.Slingshot;

        public bool IsPaintGunEquipped =>
            IsSlingshotEquipped;

        public event Action<bool> EquipmentChanged;
        public event Action<EquippedTool> ToolChanged;

        /// <summary>
        /// Создаёт действия ввода и устанавливает начальное состояние экипировки без вызова событий.
        /// </summary>
        private void Awake()
        {
            _inputActions =
                new PlayerInputActions();

            CurrentTool =
                startWithPaintGun
                    ? EquippedTool.Slingshot
                    : EquippedTool.None;
        }

        /// <summary>
        /// Включает карту игрока и подписывается на переключение рогатки по Q.
        /// </summary>
        private void OnEnable()
        {
            _inputActions.Player.ToggleSlingshot.performed +=
                OnToggleSlingshot;

            _inputActions.Player.Enable();
        }

        /// <summary>
        /// Отписывается от Q и отключает карту игрока при выключении компонента.
        /// </summary>
        private void OnDisable()
        {
            if (_inputActions == null)
            {
                return;
            }

            _inputActions.Player.ToggleSlingshot.performed -=
                OnToggleSlingshot;

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
        /// Переключает состояние рогатки между руками игрока и пустыми руками.
        /// </summary>
        private void OnToggleSlingshot(
            InputAction.CallbackContext context)
        {
            SetSlingshotEquipped(!IsSlingshotEquipped);
        }

        /// <summary>
        /// Устанавливает, находится ли рогатка в руках игрока, и уведомляет подписчиков об изменении.
        /// </summary>
        public void SetSlingshotEquipped(
            bool equipped)
        {
            EquippedTool nextTool =
                equipped
                    ? EquippedTool.Slingshot
                    : EquippedTool.None;

            if (CurrentTool == nextTool)
            {
                return;
            }

            CurrentTool = nextTool;
            EquipmentChanged?.Invoke(IsSlingshotEquipped);
            ToolChanged?.Invoke(CurrentTool);
        }

        /// <summary>
        /// Сохраняет совместимость со старым PaintShooter, перенаправляя вызов на состояние рогатки.
        /// </summary>
        public void SetPaintGunEquipped(
            bool equipped)
        {
            SetSlingshotEquipped(equipped);
        }
    }
}
