using System;

namespace Game.Sheep.AI
{
    internal sealed class SheepWanderingState : ISheepState
    {
        private readonly SheepLocomotion _locomotion;
        private readonly SheepWanderArea _wanderArea;
        private readonly Func<float> _durationProvider;
        private readonly float _minimumDestinationDistance;
        private readonly int _maximumPointAttempts;
        private readonly Action _finishWandering;

        private float _remainingTime;
        private bool _hasMoveOrder;

        public SheepStateId Id => SheepStateId.Wandering;

        public SheepWanderingState(
            SheepLocomotion locomotion,
            SheepWanderArea wanderArea,
            Func<float> durationProvider,
            float minimumDestinationDistance,
            int maximumPointAttempts,
            Action finishWandering)
        {
            _locomotion = locomotion;
            _wanderArea = wanderArea;
            _durationProvider = durationProvider ??
                throw new ArgumentNullException(nameof(durationProvider));
            _minimumDestinationDistance = minimumDestinationDistance;
            _maximumPointAttempts = maximumPointAttempts;
            _finishWandering = finishWandering ??
                throw new ArgumentNullException(nameof(finishWandering));
        }

        public void Enter()
        {
            _remainingTime = _durationProvider();
            _hasMoveOrder = false;
        }

        public void Tick(float deltaTime)
        {
            if (!_hasMoveOrder)
            {
                if (!TryStartMovement())
                {
                    _finishWandering();
                    return;
                }

                _hasMoveOrder = true;
            }

            _remainingTime -= deltaTime;

            if (_remainingTime > 0f &&
                !_locomotion.HasReachedDestination &&
                !_locomotion.HasInvalidPath)
            {
                return;
            }

            _finishWandering();
        }

        public void Exit()
        {
            _locomotion?.Stop();
            _hasMoveOrder = false;
        }

        private bool TryStartMovement()
        {
            if (_locomotion == null || _wanderArea == null ||
                !_locomotion.IsReady)
            {
                return false;
            }

            if (!_wanderArea.TryGetRandomPoint(
                    _locomotion.Position,
                    _minimumDestinationDistance,
                    _maximumPointAttempts,
                    out UnityEngine.Vector3 destination))
            {
                return false;
            }

            return _locomotion.TryMoveTo(destination);
        }
    }
}
