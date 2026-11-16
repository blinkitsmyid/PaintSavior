using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Game.Items;
using Game.Paint;
using Game.Player;

namespace Game.UI
{
    /// <summary>
    /// Показывает панель выбора ягод во время прицеливания и обновляет её слоты по данным инвентаря.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BerrySelectionPanelUI : MonoBehaviour
    {
        /// <summary>
        /// Описывает визуальные элементы одного слота выбора ягоды.
        /// </summary>
        [System.Serializable]
        private sealed class BerrySlotView
        {
            [SerializeField] private BerryType berryType;
            [SerializeField] private GameObject slotRoot;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text amountLabel;
            [SerializeField] private Image selectionHighlight;

            public BerryType BerryType => berryType;

            public GameObject SlotRoot => slotRoot;

            public Image Icon => icon;

            public TMP_Text AmountLabel => amountLabel;

            public Image SelectionHighlight => selectionHighlight;
        }

        [Header("References")]
        [Tooltip("Корень панели, который включается только во время прицеливания.")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("RectTransform панели для позиционирования рядом с рогаткой.")]
        [SerializeField] private RectTransform panelRect;

        [Tooltip("Точка на рогатке, рядом с которой должна появляться панель.")]
        [SerializeField] private Transform slingshotUIAnchor;

        [Tooltip("Камера, используемая для перевода мировой позиции якоря в экранные координаты.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Инвентарь игрока с количествами ягод и выбранным типом.")]
        [SerializeField] private BerryInventory inventory;

        [Tooltip("Система экипировки игрока.")]
        [SerializeField] private PlayerEquipment equipment;

        [Tooltip("Контроллер прицеливания рогатки.")]
        [SerializeField] private PaintShooter shooter;

        [Header("Layout")]
        [Tooltip("Экранное смещение панели относительно якоря рогатки.")]
        [SerializeField] private Vector2 screenOffset =
            new Vector2(120f, 0f);

        [Header("Slot Views")]
        [Tooltip("Три слота в порядке, заданном их BerryType.")]
        [SerializeField] private BerrySlotView[] slots =
            new BerrySlotView[0];

        [Header("Colors")]
        [SerializeField] private Color availableIconColor = Color.white;
        [SerializeField] private Color unavailableIconColor =
            new Color(1f, 1f, 1f, 0.35f);

        private bool _isPanelVisible;

        /// <summary>
        /// Находит зависимости на объекте игрока и подготавливает RectTransform панели.
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

            if (playerCamera == null)
            {
                playerCamera =
                    Camera.main;
            }

            if (panelRect == null && panelRoot != null)
            {
                panelRect =
                    panelRoot.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Подписывается на изменения выбранной ягоды, количества и экипировки.
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
        /// Показывает начальное состояние панели после инициализации игровых данных.
        /// </summary>
        private void Start()
        {
            RefreshPanelState();
        }

        /// <summary>
        /// Проверяет состояние прицеливания и обновляет положение видимой панели.
        /// </summary>
        private void Update()
        {
            RefreshPanelState();

            if (_isPanelVisible)
            {
                UpdatePanelPosition();
            }
        }

        /// <summary>
        /// Отписывается от игровых событий при отключении UI-компонента.
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
        /// Обновляет слоты после смены выбранного типа ягоды.
        /// </summary>
        private void OnSelectedBerryChanged(
            BerryType type)
        {
            RefreshPanelState();
        }

        /// <summary>
        /// Обновляет количества после изменения инвентаря.
        /// </summary>
        private void OnBerryAmountChanged(
            BerryType type,
            int currentAmount,
            int capacity)
        {
            RefreshPanelState();
        }

        /// <summary>
        /// Обновляет видимость панели после смены экипированного инструмента.
        /// </summary>
        private void OnToolChanged(
            EquippedTool tool)
        {
            RefreshPanelState();
        }

        /// <summary>
        /// Показывает или скрывает панель и обновляет её содержимое.
        /// </summary>
        private void RefreshPanelState()
        {
            bool shouldShow =
                equipment != null &&
                equipment.IsSlingshotEquipped &&
                shooter != null &&
                shooter.IsAiming;

            _isPanelVisible = shouldShow;

            if (panelRoot != null &&
                panelRoot.activeSelf != shouldShow)
            {
                panelRoot.SetActive(shouldShow);
            }

            if (!shouldShow)
            {
                return;
            }

            UpdateSlotViews();
            UpdatePanelPosition();
        }

        /// <summary>
        /// Обновляет количество, цвет и подсветку каждого слота панели.
        /// </summary>
        private void UpdateSlotViews()
        {
            if (inventory == null || slots == null)
            {
                return;
            }

            BerryType selectedType =
                inventory.SelectedBerryType;

            for (int index = 0; index < slots.Length; index++)
            {
                BerrySlotView slot =
                    slots[index];

                if (slot == null)
                {
                    continue;
                }

                int amount =
                    inventory.GetAmount(slot.BerryType);

                if (slot.SlotRoot != null)
                {
                    slot.SlotRoot.SetActive(true);
                }

                if (slot.AmountLabel != null)
                {
                    slot.AmountLabel.text =
                        amount.ToString();
                }

                if (slot.Icon != null)
                {
                    slot.Icon.color =
                        amount > 0
                            ? availableIconColor
                            : unavailableIconColor;
                }

                if (slot.SelectionHighlight != null)
                {
                    slot.SelectionHighlight.enabled =
                        slot.BerryType == selectedType &&
                        amount > 0;
                }
            }
        }

        /// <summary>
        /// Переводит мировую позицию якоря рогатки в экранную позицию панели.
        /// </summary>
        private void UpdatePanelPosition()
        {
            if (panelRect == null ||
                slingshotUIAnchor == null ||
                playerCamera == null)
            {
                return;
            }

            Vector3 screenPosition =
                playerCamera.WorldToScreenPoint(
                    slingshotUIAnchor.position);

            panelRect.position =
                screenPosition +
                new Vector3(
                    screenOffset.x,
                    screenOffset.y,
                    0f);
        }
    }
}
