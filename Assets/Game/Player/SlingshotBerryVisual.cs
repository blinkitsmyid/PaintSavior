using UnityEngine;

using Game.Items;
using Game.Paint;

namespace Game.Player
{
    /// <summary>
    /// Показывает в обойме визуальную ягоду выбранного типа и синхронизирует её с экипировкой и инвентарём.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SlingshotBerryVisual : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [Header("References")]
        [Tooltip("Объект визуальной ягоды внутри BerrySocket.")]
        [SerializeField] private GameObject berryVisual;

        [Tooltip("Инвентарь игрока с выбранным типом ягоды.")]
        [SerializeField] private BerryInventory inventory;

        [Tooltip("Система экипировки, определяющая, находится ли рогатка в руках.")]
        [SerializeField] private PlayerEquipment equipment;

        [Tooltip("Контроллер прицеливания, определяющий момент показа ягоды в обойме.")]
        [SerializeField] private PaintShooter shooter;

        [Tooltip("Renderer визуальной ягоды. Если список пуст, Renderer ищутся внутри berryVisual.")]
        [SerializeField] private Renderer[] visualRenderers;

        [Header("Berry Colors")]
        [SerializeField] private Color redColor = Color.red;
        [SerializeField] private Color blueColor = Color.blue;
        [SerializeField] private Color yellowColor = Color.yellow;

        private MaterialPropertyBlock _propertyBlock;
        private bool _lastAimingState;

        /// <summary>
        /// Находит зависимости на родителях и подготавливает MaterialPropertyBlock для цвета ягоды.
        /// </summary>
        private void Awake()
        {
            if (inventory == null)
            {
                inventory =
                    GetComponentInParent<BerryInventory>();
            }

            if (equipment == null)
            {
                equipment =
                    GetComponentInParent<PlayerEquipment>();
            }

            if (shooter == null)
            {
                shooter =
                    GetComponentInParent<PaintShooter>();
            }

            if ((visualRenderers == null || visualRenderers.Length == 0) &&
                berryVisual != null)
            {
                visualRenderers =
                    berryVisual.GetComponentsInChildren<Renderer>(true);
            }

            _propertyBlock =
                new MaterialPropertyBlock();
        }

        /// <summary>
        /// Подписывается на изменение выбранной ягоды, количества боеприпасов и экипировки.
        /// </summary>
        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.SelectedBerryChanged +=
                    OnSelectedBerryChanged;

                inventory.AmountChanged +=
                    OnBerryAmountChanged;
            }

            if (equipment != null)
            {
                equipment.ToolChanged +=
                    OnToolChanged;
            }
        }

        /// <summary>
        /// Обновляет начальное состояние ягоды после инициализации инвентаря и экипировки.
        /// </summary>
        private void Start()
        {
            _lastAimingState =
                shooter != null && shooter.IsAiming;

            RefreshVisual();
        }

        /// <summary>
        /// Обновляет визуал при начале или завершении прицеливания без создания отдельного обработчика ввода.
        /// </summary>
        private void Update()
        {
            bool isAiming =
                shooter != null && shooter.IsAiming;

            if (isAiming == _lastAimingState)
            {
                return;
            }

            _lastAimingState = isAiming;
            RefreshVisual();
        }

        /// <summary>
        /// Отписывается от событий инвентаря и экипировки при отключении компонента.
        /// </summary>
        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.SelectedBerryChanged -=
                    OnSelectedBerryChanged;

                inventory.AmountChanged -=
                    OnBerryAmountChanged;
            }

            if (equipment != null)
            {
                equipment.ToolChanged -=
                    OnToolChanged;
            }
        }

        /// <summary>
        /// Обновляет визуал после смены выбранного типа ягоды.
        /// </summary>
        private void OnSelectedBerryChanged(
            BerryType type)
        {
            RefreshVisual();
        }

        /// <summary>
        /// Обновляет видимость после изменения количества ягод в инвентаре.
        /// </summary>
        private void OnBerryAmountChanged(
            BerryType type,
            int currentAmount,
            int capacity)
        {
            RefreshVisual();
        }

        /// <summary>
        /// Обновляет видимость ягоды при экипировке или убирании рогатки.
        /// </summary>
        private void OnToolChanged(
            EquippedTool tool)
        {
            RefreshVisual();
        }

        /// <summary>
        /// Показывает ягоду только при экипированной рогатке и наличии выбранного боеприпаса.
        /// </summary>
        private void RefreshVisual()
        {
            if (berryVisual == null ||
                inventory == null ||
                equipment == null)
            {
                return;
            }

            BerryType selectedType =
                inventory.SelectedBerryType;

            bool shouldShow =
                equipment.IsSlingshotEquipped &&
                shooter != null &&
                shooter.IsAiming &&
                inventory.HasBerry(selectedType);

            berryVisual.SetActive(shouldShow);

            if (!shouldShow)
            {
                return;
            }

            ApplyColor(
                GetBerryColor(selectedType));
        }

        /// <summary>
        /// Применяет цвет выбранной ягоды ко всем Renderer через MaterialPropertyBlock без создания материалов.
        /// </summary>
        private void ApplyColor(
            Color color)
        {
            if (visualRenderers == null ||
                _propertyBlock == null)
            {
                return;
            }

            for (int index = 0; index < visualRenderers.Length; index++)
            {
                Renderer visualRenderer =
                    visualRenderers[index];

                if (visualRenderer == null)
                {
                    continue;
                }

                visualRenderer.GetPropertyBlock(
                    _propertyBlock);

                _propertyBlock.SetColor(
                    BaseColorId,
                    color);

                _propertyBlock.SetColor(
                    ColorId,
                    color);

                visualRenderer.SetPropertyBlock(
                    _propertyBlock);
            }
        }

        /// <summary>
        /// Возвращает цвет, соответствующий выбранному типу ягоды.
        /// </summary>
        private Color GetBerryColor(
            BerryType type)
        {
            return type switch
            {
                BerryType.Blue => blueColor,
                BerryType.Yellow => yellowColor,
                _ => redColor
            };
        }
    }
}
