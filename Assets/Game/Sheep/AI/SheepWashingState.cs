using System;

using Game.Paint;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Последовательно удаляет самые старые пятна, пока овца находится в зоне воды.
    /// </summary>
    internal sealed class SheepWashingState : ISheepState
    {
        private readonly AnimalLocomotion _locomotion;
        private readonly SheepPaintController _paintController;
        private readonly Func<float> _blobDurationProvider;
        private readonly Action _finishState;

        private SheepWaterPoint _waterPoint;
        private PaintBlobData _currentBlob;
        private float _remainingBlobTime;
        private bool _isRemovingBlob;

        /// <summary>
        /// Создаёт состояние мытья с доступом к движению, воде и данным покраски.
        /// </summary>
        public SheepWashingState(
            AnimalLocomotion locomotion,
            SheepPaintController paintController,
            Func<float> blobDurationProvider,
            Action finishState)
        {
            _locomotion = locomotion;
            _paintController = paintController;
            _blobDurationProvider = blobDurationProvider ??
                throw new ArgumentNullException(nameof(blobDurationProvider));
            _finishState = finishState ??
                throw new ArgumentNullException(nameof(finishState));
        }

        /// <summary>
        /// Возвращает идентификатор состояния мытья.
        /// </summary>
        public SheepStateId Id => SheepStateId.Washing;

        /// <summary>
        /// Назначает водоём, в зоне которого выполняется мытьё.
        /// </summary>
        public void SetWaterPoint(
            SheepWaterPoint waterPoint)
        {
            _waterPoint = waterPoint;
        }

        /// <summary>
        /// Подготавливает состояние к продолжению или началу удаления пятен.
        /// </summary>
        public void Enter()
        {
            _locomotion?.Stop();
        }

        /// <summary>
        /// Продолжает текущее удаление или начинает удалять самое старое пятно в зоне воды.
        /// </summary>
        public void Tick(
            float deltaTime)
        {
            if (_paintController == null || _waterPoint == null ||
                _locomotion == null)
            {
                _finishState();
                return;
            }

            if (_isRemovingBlob)
            {
                TickCurrentBlob(deltaTime);
                return;
            }

            if (!_waterPoint.ContainsPosition(_locomotion.Position))
            {
                return;
            }

            if (!_paintController.TryGetOldestBlob(out PaintBlobData blob))
            {
                _finishState();
                return;
            }

            _currentBlob = blob;
            _remainingBlobTime = _blobDurationProvider();
            _isRemovingBlob = true;

            if (_remainingBlobTime <= 0f)
            {
                RemoveCurrentBlob();
            }
        }

        /// <summary>
        /// Останавливает движение, сохраняя активное удаление пятна для продолжения позже.
        /// </summary>
        public void Exit()
        {
            _locomotion?.Stop();
        }

        /// <summary>
        /// Уменьшает таймер текущего пятна и завершает его удаление по окончании времени.
        /// </summary>
        private void TickCurrentBlob(
            float deltaTime)
        {
            _remainingBlobTime -= deltaTime;

            if (_remainingBlobTime <= 0f)
            {
                RemoveCurrentBlob();
            }
        }

        /// <summary>
        /// Удаляет текущее пятно через контроллер и освобождает слот для следующего пятна.
        /// </summary>
        private void RemoveCurrentBlob()
        {
            _paintController.RemoveBlob(_currentBlob.Sequence);
            _currentBlob = default;
            _remainingBlobTime = 0f;
            _isRemovingBlob = false;
        }
    }
}
