using System;

using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Тип ягоды, который может храниться в инвентаре и использоваться как боеприпас.
    /// </summary>
    public enum BerryType
    {
        Red = 0,
        Blue = 1,
        Yellow = 2,
        Count = 3
    }

    /// <summary>
    /// Хранит количество ягод каждого типа и выбранный тип боеприпаса игрока.
    /// </summary>
    public sealed class BerryInventory : MonoBehaviour
    {
        [Header("Capacities")]
        [SerializeField, Min(0)] private int redCapacity = 3;
        [SerializeField, Min(0)] private int blueCapacity = 3;
        [SerializeField, Min(0)] private int yellowCapacity = 3;

        [Header("Starting Amounts")]
        [SerializeField, Min(0)] private int startingRedAmount;
        [SerializeField, Min(0)] private int startingBlueAmount;
        [SerializeField, Min(0)] private int startingYellowAmount;

        private readonly int[] _capacities =
            new int[(int)BerryType.Count];

        private readonly int[] _amounts =
            new int[(int)BerryType.Count];

        public BerryType SelectedBerryType { get; private set; }

        public event Action<BerryType, int, int> AmountChanged;
        public event Action<BerryType> SelectedBerryChanged;

        /// <summary>
        /// Заполняет внутренние массивы лимитами и начальными количествами ягод.
        /// </summary>
        private void Awake()
        {
            _capacities[(int)BerryType.Red] =
                Mathf.Max(0, redCapacity);

            _capacities[(int)BerryType.Blue] =
                Mathf.Max(0, blueCapacity);

            _capacities[(int)BerryType.Yellow] =
                Mathf.Max(0, yellowCapacity);

            _amounts[(int)BerryType.Red] =
                Mathf.Clamp(
                    startingRedAmount,
                    0,
                    _capacities[(int)BerryType.Red]
                );

            _amounts[(int)BerryType.Blue] =
                Mathf.Clamp(
                    startingBlueAmount,
                    0,
                    _capacities[(int)BerryType.Blue]
                );

            _amounts[(int)BerryType.Yellow] =
                Mathf.Clamp(
                    startingYellowAmount,
                    0,
                    _capacities[(int)BerryType.Yellow]
                );

            SelectedBerryType = BerryType.Red;
        }

        /// <summary>
        /// Добавляет доступное количество ягод и сообщает фактически добавленное значение.
        /// </summary>
        public bool TryAddBerry(
            BerryType type,
            int amount,
            out int addedAmount)
        {
            addedAmount = 0;

            if (!IsValidType(type) || amount <= 0)
            {
                return false;
            }

            int index =
                GetIndex(type);

            int freeSpace =
                _capacities[index] - _amounts[index];

            addedAmount =
                Mathf.Min(amount, Mathf.Max(0, freeSpace));

            if (addedAmount <= 0)
            {
                return false;
            }

            _amounts[index] += addedAmount;

            AmountChanged?.Invoke(
                type,
                _amounts[index],
                _capacities[index]
            );

            return true;
        }

        /// <summary>
        /// Расходует указанное количество ягод выбранного типа, если его достаточно в инвентаре.
        /// </summary>
        public bool TryConsumeBerry(
            BerryType type,
            int amount = 1)
        {
            if (!IsValidType(type) || amount <= 0)
            {
                return false;
            }

            int index =
                GetIndex(type);

            if (_amounts[index] < amount)
            {
                return false;
            }

            _amounts[index] -= amount;

            AmountChanged?.Invoke(
                type,
                _amounts[index],
                _capacities[index]
            );

            return true;
        }

        /// <summary>
        /// Возвращает количество ягод указанного типа.
        /// </summary>
        public int GetAmount(
            BerryType type)
        {
            return IsValidType(type)
                ? _amounts[GetIndex(type)]
                : 0;
        }

        /// <summary>
        /// Возвращает максимальную вместимость указанного типа ягод.
        /// </summary>
        public int GetCapacity(
            BerryType type)
        {
            return IsValidType(type)
                ? _capacities[GetIndex(type)]
                : 0;
        }

        /// <summary>
        /// Проверяет, есть ли свободное место для указанного типа ягод.
        /// </summary>
        public bool HasFreeSpace(
            BerryType type)
        {
            return IsValidType(type) &&
                GetAmount(type) < GetCapacity(type);
        }

        /// <summary>
        /// Проверяет, есть ли хотя бы одна ягода указанного типа для выбора или выстрела.
        /// </summary>
        public bool HasBerry(
            BerryType type)
        {
            return IsValidType(type) &&
                GetAmount(type) > 0;
        }

        /// <summary>
        /// Выбирает тип ягоды только при наличии хотя бы одного доступного боеприпаса.
        /// </summary>
        public bool TrySelectBerry(
            BerryType type)
        {
            if (!HasBerry(type) || SelectedBerryType == type)
            {
                return false;
            }

            SelectedBerryType = type;
            SelectedBerryChanged?.Invoke(type);
            return true;
        }

        /// <summary>
        /// Возвращает индекс типа ягоды во внутренних массивах хранения.
        /// </summary>
        private static int GetIndex(
            BerryType type)
        {
            return (int)type;
        }

        /// <summary>
        /// Проверяет, является ли значение настоящим типом ягоды, а не служебным Count.
        /// </summary>
        private static bool IsValidType(
            BerryType type)
        {
            return type >= BerryType.Red &&
                type < BerryType.Count;
        }
    }
}
