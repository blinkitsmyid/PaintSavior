using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Показывает текущее состояние инвентаря ягод в простом экранном отладочном представлении.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BerryInventoryDebugView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BerryInventory inventory;

        [Header("Layout")]
        [SerializeField] private Vector2 screenPosition = new Vector2(20f, 20f);
        [SerializeField, Min(12)] private int fontSize = 18;
        [SerializeField, Min(20)] private float lineHeight = 24f;
        [SerializeField, Min(100)] private float labelWidth = 240f;

        private readonly string[] _lines = new string[4];
        private GUIStyle _style;

        /// <summary>
        /// Находит инвентарь на том же объекте и создаёт стиль экранного текста.
        /// </summary>
        private void Awake()
        {
            if (inventory == null)
            {
                inventory =
                    GetComponent<BerryInventory>();
            }

            _style =
                new GUIStyle
                {
                    fontSize = fontSize,
                    richText = false
                };

            _style.normal.textColor = Color.white;
        }

        /// <summary>
        /// Подписывается на изменения количества и выбранного типа ягоды.
        /// </summary>
        private void OnEnable()
        {
            if (inventory == null)
            {
                return;
            }

            inventory.AmountChanged += OnAmountChanged;
            inventory.SelectedBerryChanged += OnSelectedBerryChanged;
            RefreshLines();
        }

        /// <summary>
        /// Отписывается от событий инвентаря при отключении отладочного представления.
        /// </summary>
        private void OnDisable()
        {
            if (inventory == null)
            {
                return;
            }

            inventory.AmountChanged -= OnAmountChanged;
            inventory.SelectedBerryChanged -= OnSelectedBerryChanged;
        }

        /// <summary>
        /// Обновляет строки после изменения количества ягод.
        /// </summary>
        private void OnAmountChanged(
            BerryType type,
            int amount,
            int capacity)
        {
            RefreshLines();
        }

        /// <summary>
        /// Обновляет строки после выбора другого типа ягоды.
        /// </summary>
        private void OnSelectedBerryChanged(
            BerryType type)
        {
            RefreshLines();
        }

        /// <summary>
        /// Формирует кэшируемые строки для отображения состояния инвентаря.
        /// </summary>
        private void RefreshLines()
        {
            if (inventory == null)
            {
                return;
            }

            _lines[0] =
                $"Selected: {inventory.SelectedBerryType}";

            _lines[1] =
                $"Red: {inventory.GetAmount(BerryType.Red)} / " +
                $"{inventory.GetCapacity(BerryType.Red)}";

            _lines[2] =
                $"Blue: {inventory.GetAmount(BerryType.Blue)} / " +
                $"{inventory.GetCapacity(BerryType.Blue)}";

            _lines[3] =
                $"Yellow: {inventory.GetAmount(BerryType.Yellow)} / " +
                $"{inventory.GetCapacity(BerryType.Yellow)}";
        }

        /// <summary>
        /// Рисует четыре строки отладочного состояния в левом верхнем углу экрана.
        /// </summary>
        private void OnGUI()
        {
            if (inventory == null || _style == null)
            {
                return;
            }

            for (int i = 0; i < _lines.Length; i++)
            {
                Rect labelRect =
                    new Rect(
                        screenPosition.x,
                        screenPosition.y + lineHeight * i,
                        labelWidth,
                        lineHeight
                    );

                GUI.Label(
                    labelRect,
                    _lines[i],
                    _style
                );
            }
        }
    }
}
