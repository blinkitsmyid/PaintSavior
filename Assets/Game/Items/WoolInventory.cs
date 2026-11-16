using System;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Хранит количество собранной шерсти, разделяя её по цвету.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Paint Savior/Items/Wool Inventory")]
    public sealed class WoolInventory : MonoBehaviour
    {
        private readonly Dictionary<Color32, int> _amounts =
            new Dictionary<Color32, int>();

        /// <summary>
        /// Сообщает об изменении количества шерсти конкретного цвета.
        /// </summary>
        public event Action<Color32, int> AmountChanged;

        /// <summary>
        /// Возвращает количество шерсти указанного цвета.
        /// </summary>
        public int GetAmount(
            Color color)
        {
            return GetAmount(
                (Color32)color
            );
        }

        /// <summary>
        /// Возвращает количество шерсти по нормализованному ключу Color32.
        /// </summary>
        public int GetAmount(
            Color32 color)
        {
            return _amounts.TryGetValue(
                color,
                out int amount
            )
                ? amount
                : 0;
        }

        /// <summary>
        /// Добавляет шерсть указанного цвета и сообщает новое общее количество.
        /// </summary>
        public bool TryAddWool(
            Color color,
            int amount = 1)
        {
            return TryAddWool(
                (Color32)color,
                amount
            );
        }

        /// <summary>
        /// Добавляет шерсть по ключу Color32 без ограничения вместимости.
        /// </summary>
        public bool TryAddWool(
            Color32 color,
            int amount = 1)
        {
            if (amount <= 0)
            {
                return false;
            }

            _amounts.TryGetValue(
                color,
                out int currentAmount
            );

            int newAmount =
                currentAmount + amount;

            _amounts[color] = newAmount;
            AmountChanged?.Invoke(
                color,
                newAmount
            );

            return true;
        }

        /// <summary>
        /// Проверяет, есть ли в инвентаре хотя бы один клочок указанного цвета.
        /// </summary>
        public bool HasWool(
            Color color)
        {
            return GetAmount(color) > 0;
        }

        /// <summary>
        /// Проверяет, есть ли в инвентаре хотя бы один клочок по ключу Color32.
        /// </summary>
        public bool HasWool(
            Color32 color)
        {
            return GetAmount(color) > 0;
        }

        /// <summary>
        /// Расходует шерсть указанного цвета, если её количество достаточно.
        /// </summary>
        public bool TryConsumeWool(
            Color color,
            int amount = 1)
        {
            return TryConsumeWool(
                (Color32)color,
                amount
            );
        }

        /// <summary>
        /// Расходует шерсть по ключу Color32 и удаляет пустой стек из словаря.
        /// </summary>
        public bool TryConsumeWool(
            Color32 color,
            int amount = 1)
        {
            if (amount <= 0 ||
                !_amounts.TryGetValue(
                    color,
                    out int currentAmount
                ) ||
                currentAmount < amount)
            {
                return false;
            }

            int newAmount =
                currentAmount - amount;

            if (newAmount == 0)
            {
                _amounts.Remove(color);
            }
            else
            {
                _amounts[color] = newAmount;
            }

            AmountChanged?.Invoke(
                color,
                newAmount
            );

            return true;
        }
    }
}
