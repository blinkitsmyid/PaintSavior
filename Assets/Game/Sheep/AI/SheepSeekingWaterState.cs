using System;

using Game.Animals.Core;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Перемещает овцу к ближайшему доступному водоёму и удерживает её у точки подхода.
    /// </summary>
    internal sealed class SheepSeekingWaterState : ISheepState
    {
        private readonly AnimalLocomotion _locomotion;
        private readonly SheepWaterEnvironment _waterEnvironment;
        private readonly Action<SheepWaterPoint> _waterReached;
        private readonly Action _finishState;

        private SheepWaterPoint _waterPoint;
        private bool _hasMoveOrder;
        private bool _hasReachedWater;

        /// <summary>
        /// Создаёт состояние поиска воды с компонентом движения и реестром водоёмов.
        /// </summary>
        public SheepSeekingWaterState(
            AnimalLocomotion locomotion,
            SheepWaterEnvironment waterEnvironment,
            Action<SheepWaterPoint> waterReached,
            Action finishState)
        {
            _locomotion = locomotion;
            _waterEnvironment = waterEnvironment;
            _waterReached = waterReached ??
                throw new ArgumentNullException(nameof(waterReached));
            _finishState = finishState ??
                throw new ArgumentNullException(nameof(finishState));
        }

        /// <summary>
        /// Возвращает идентификатор состояния движения к воде.
        /// </summary>
        public SheepStateId Id => SheepStateId.SeekingWater;

        /// <summary>
        /// Возвращает выбранный водоём после начала движения.
        /// </summary>
        public SheepWaterPoint WaterPoint => _waterPoint;

        /// <summary>
        /// Возвращает признак достижения точки подхода к водоёму.
        /// </summary>
        public bool HasReachedWater => _hasReachedWater;

        /// <summary>
        /// Сбрасывает цель и подготавливает состояние к новому поиску воды.
        /// </summary>
        public void Enter()
        {
            _waterPoint = null;
            _hasMoveOrder = false;
            _hasReachedWater = false;
        }

        /// <summary>
        /// Выбирает водоём, ведёт овцу к нему и удерживает её после прибытия.
        /// </summary>
        public void Tick(
            float deltaTime)
        {
            if (!_hasMoveOrder)
            {
                if (!TryStartMovement())
                {
                    _finishState();
                }

                return;
            }

            if (_hasReachedWater)
            {
                return;
            }

            if (_locomotion == null || _locomotion.HasInvalidPath)
            {
                _finishState();
                return;
            }

            if (!_locomotion.HasReachedDestination)
            {
                return;
            }

            _locomotion.Stop();
            _hasReachedWater = true;
            _waterReached(_waterPoint);
        }

        /// <summary>
        /// Останавливает движение и очищает временную цель при выходе из состояния.
        /// </summary>
        public void Exit()
        {
            _locomotion?.Stop();
            _waterPoint = null;
            _hasMoveOrder = false;
            _hasReachedWater = false;
        }

        /// <summary>
        /// Выбирает ближайший водоём и создаёт для овцы команду движения к его точке подхода.
        /// </summary>
        private bool TryStartMovement()
        {
            if (_locomotion == null || _waterEnvironment == null ||
                !_locomotion.IsReady ||
                !_waterEnvironment.TryGetClosestPoint(
                    _locomotion.Position,
                    out SheepWaterPoint waterPoint))
            {
                return false;
            }

            if (!_locomotion.TryMoveTo(waterPoint.ApproachPosition))
            {
                return false;
            }

            _waterPoint = waterPoint;
            _hasMoveOrder = true;
            return true;
        }
    }
}
