using System;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Items;
using Game.Player;

namespace Game.Paint
{
    /// <summary>
    /// Управляет логическим прицеливанием, отменой и выстрелом выбранной ягодой.
    /// </summary>
    public sealed class PaintShooter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PaintProjectile projectilePrefab;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private BerryInventory berryInventory;

        [Header("Temporary Berry Colors")]
        [SerializeField] private Color paintColor = Color.red;
        [SerializeField] private Color blueBerryColor = Color.blue;
        [SerializeField] private Color yellowBerryColor = Color.yellow;

        [Header("Aim")]
        [SerializeField, Min(0.1f)] private float pullChargeSpeed = 2.5f;
        [SerializeField, Min(0f)] private float minShotSpeed = 12f;
        [SerializeField, Min(0f)] private float maxShotSpeed = 25f;

        private PlayerInputActions _inputActions;
        private bool _isAiming;

        public event Action UnarmedPrimaryAction;

        public bool IsAiming => _isAiming;

        public float PullStrength { get; private set; }

        public Vector3 AimDirection { get; private set; }

        /// <summary>
        /// Создаёт действия ввода и автоматически находит экипировку, камеру и инвентарь игрока.
        /// </summary>
        private void Awake()
        {
            _inputActions =
                new PlayerInputActions();

            if (equipment == null)
            {
                equipment =
                    GetComponent<PlayerEquipment>();
            }

            if (berryInventory == null)
            {
                berryInventory =
                    GetComponent<BerryInventory>();
            }
        }

        /// <summary>
        /// Подписывается на начало и завершение ЛКМ, отмену ПКМ и смену экипировки.
        /// </summary>
        private void OnEnable()
        {
            _inputActions.Player.Shoot.started +=
                OnShootStarted;

            _inputActions.Player.Shoot.performed +=
                OnShootPerformed;

            _inputActions.Player.Shoot.canceled +=
                OnShootCanceled;

            _inputActions.Player.CancelAim.performed +=
                OnCancelAim;

            _inputActions.Player.Enable();

            if (equipment != null)
            {
                equipment.ToolChanged += OnToolChanged;
            }
        }

        /// <summary>
        /// Плавно увеличивает силу натяжения, пока игрок удерживает ЛКМ и прицеливается.
        /// </summary>
        private void Update()
        {
            if (!_isAiming)
            {
                return;
            }

            PullStrength =
                Mathf.MoveTowards(
                    PullStrength,
                    1f,
                    pullChargeSpeed * Time.deltaTime
                );

            if (playerCamera != null)
            {
                AimDirection =
                    playerCamera.transform.forward.normalized;
            }
        }

        /// <summary>
        /// Отписывается от ввода и событий экипировки при отключении компонента.
        /// </summary>
        private void OnDisable()
        {
            if (_inputActions != null)
            {
                _inputActions.Player.Shoot.started -=
                    OnShootStarted;

                _inputActions.Player.Shoot.performed -=
                    OnShootPerformed;

                _inputActions.Player.Shoot.canceled -=
                    OnShootCanceled;

                _inputActions.Player.CancelAim.performed -=
                    OnCancelAim;

                _inputActions.Player.Disable();
            }

            if (equipment != null)
            {
                equipment.ToolChanged -= OnToolChanged;
            }

            _isAiming = false;
        }

        /// <summary>
        /// Освобождает созданный экземпляр Input System после уничтожения компонента.
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
        /// Начинает прицеливание, если рогатка экипирована и выбранная ягода доступна.
        /// </summary>
        private void OnShootStarted(
            InputAction.CallbackContext context)
        {
            if (equipment == null ||
                !equipment.IsSlingshotEquipped)
            {
                return;
            }

            if (berryInventory == null ||
                !berryInventory.HasBerry(
                    berryInventory.SelectedBerryType
                ))
            {
                return;
            }

            _isAiming = true;
            PullStrength = 0f;

            if (playerCamera != null)
            {
                AimDirection =
                    playerCamera.transform.forward.normalized;
            }
        }

        /// <summary>
        /// Выполняет действие без оружия при отпускании ЛКМ, сохраняя поглаживание овцы.
        /// </summary>
        private void OnShootPerformed(
            InputAction.CallbackContext context)
        {
            if (equipment == null ||
                !equipment.IsSlingshotEquipped)
            {
                UnarmedPrimaryAction?.Invoke();
            }
        }

        /// <summary>
        /// Завершает прицеливание и пытается создать снаряд при отпускании ЛКМ.
        /// </summary>
        private void OnShootCanceled(
            InputAction.CallbackContext context)
        {
            if (!_isAiming)
            {
                return;
            }

            float shotPullStrength =
                PullStrength;

            _isAiming = false;
            PullStrength = 0f;
            TryShootSelectedBerry(shotPullStrength);
        }

        /// <summary>
        /// Отменяет текущее прицеливание по нажатию ПКМ без расхода ягоды.
        /// </summary>
        private void OnCancelAim(
            InputAction.CallbackContext context)
        {
            _isAiming = false;
            PullStrength = 0f;
        }

        /// <summary>
        /// Прерывает прицеливание, если игрок убрал рогатку из рук.
        /// </summary>
        private void OnToolChanged(
            EquippedTool tool)
        {
            if (tool != EquippedTool.Slingshot)
            {
                _isAiming = false;
                PullStrength = 0f;
            }
        }

        /// <summary>
        /// Проверяет боезапас, расходует одну ягоду и создаёт снаряд со скоростью от степени натяжения.
        /// </summary>
        private bool TryShootSelectedBerry(
            float pullStrength)
        {
            if (playerCamera == null ||
                projectilePrefab == null ||
                projectileSpawnPoint == null ||
                berryInventory == null)
            {
                return false;
            }

            BerryType berryType =
                berryInventory.SelectedBerryType;

            if (!berryInventory.TryConsumeBerry(berryType))
            {
                return false;
            }

            float shotSpeed =
                GetShotSpeed(pullStrength);

            PaintProjectile projectile =
                Instantiate(
                    projectilePrefab,
                    projectileSpawnPoint.position,
                    Quaternion.identity
                );

            projectile.Launch(
                playerCamera.transform.forward,
                GetBerryColor(berryType),
                transform.position,
                shotSpeed
            );

            return true;
        }

        /// <summary>
        /// Преобразует нормализованную силу натяжения в скорость полёта снаряда.
        /// </summary>
        private float GetShotSpeed(
            float pullStrength)
        {
            float safeMaxSpeed =
                Mathf.Max(
                    minShotSpeed,
                    maxShotSpeed
                );

            return Mathf.Lerp(
                minShotSpeed,
                safeMaxSpeed,
                Mathf.Clamp01(pullStrength)
            );
        }

        /// <summary>
        /// Возвращает временный цвет снаряда для выбранного типа ягоды.
        /// </summary>
        private Color GetBerryColor(
            BerryType berryType)
        {
            return berryType switch
            {
                BerryType.Blue => blueBerryColor,
                BerryType.Yellow => yellowBerryColor,
                _ => paintColor
            };
        }
    }
}
