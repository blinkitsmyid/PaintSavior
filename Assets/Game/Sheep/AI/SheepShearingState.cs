using System;

using Game.Animals.Core;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Останавливает овцу на время стрижки и сообщает о завершении действия.
    /// </summary>
    internal sealed class SheepShearingState : ISheepState
    {
        private readonly AnimalLocomotion _locomotion;
        private readonly Func<float> _durationProvider;
        private readonly Action _completed;

        private float _remainingTime;
        private bool _isCompleted;

        /// <summary>
        /// Создаёт состояние стрижки с источником длительности и callback завершения.
        /// </summary>
        public SheepShearingState(
            AnimalLocomotion locomotion,
            Func<float> durationProvider,
            Action completed)
        {
            _locomotion = locomotion;
            _durationProvider = durationProvider ??
                throw new ArgumentNullException(nameof(durationProvider));
            _completed = completed ??
                throw new ArgumentNullException(nameof(completed));
        }

        /// <summary>
        /// Возвращает идентификатор состояния стрижки.
        /// </summary>
        public SheepStateId Id => SheepStateId.Shearing;

        /// <summary>
        /// Останавливает овцу и запускает отсчёт времени стрижки.
        /// </summary>
        public void Enter()
        {
            _locomotion?.Stop();
            _remainingTime = Math.Max(0f, _durationProvider());
            _isCompleted = false;
        }

        /// <summary>
        /// Удерживает овцу на месте и завершает состояние после истечения времени.
        /// </summary>
        public void Tick(
            float deltaTime)
        {
            if (_isCompleted)
            {
                return;
            }

            _locomotion?.Stop();
            _remainingTime -= Math.Max(0f, deltaTime);

            if (_remainingTime <= 0f)
            {
                Complete();
            }
        }

        /// <summary>
        /// Останавливает овцу при выходе из состояния стрижки.
        /// </summary>
        public void Exit()
        {
            _locomotion?.Stop();
        }

        /// <summary>
        /// Однократно сообщает координатору о завершении стрижки.
        /// </summary>
        private void Complete()
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            _completed();
        }
    }
}
