using System;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Хранит игровое состояние загрязнения овцы независимо от визуальных пятен.
    /// </summary>
    public sealed class SheepContaminationState
    {
        private readonly int _hitsBeforeWash;
        private int _totalHitCount;
        private bool _wantsToWash;

        /// <summary>
        /// Создаёт состояние загрязнения с заданным порогом попаданий до мытья.
        /// </summary>
        public SheepContaminationState(
            int hitsBeforeWash)
        {
            _hitsBeforeWash = Math.Max(1, hitsBeforeWash);
        }

        /// <summary>
        /// Возвращает настроенный порог попаданий до появления намерения мыться.
        /// </summary>
        public int HitsBeforeWash => _hitsBeforeWash;

        /// <summary>
        /// Возвращает общее количество зарегистрированных попаданий.
        /// </summary>
        public int TotalHitCount => _totalHitCount;

        /// <summary>
        /// Возвращает признак достижения порога загрязнения.
        /// </summary>
        public bool HasReachedThreshold =>
            _totalHitCount >= _hitsBeforeWash;

        /// <summary>
        /// Возвращает признак текущего намерения овцы идти к воде.
        /// </summary>
        public bool WantsToWash => _wantsToWash;

        /// <summary>
        /// Регистрирует попадание и активирует намерение мыться при достижении порога.
        /// </summary>
        public void RegisterHit()
        {
            if (_totalHitCount < int.MaxValue)
            {
                _totalHitCount++;
            }

            if (HasReachedThreshold)
            {
                _wantsToWash = true;
            }
        }

        /// <summary>
        /// Отменяет текущее намерение мыться, сохраняя счётчик попаданий и порог загрязнения.
        /// </summary>
        public void CancelWashIntent()
        {
            _wantsToWash = false;
        }
    }
}
