using System;

using UnityEngine;

namespace Game.Sheep.AI
{
    internal sealed class SheepScaredState : ISheepState
    {
        private readonly SheepLocomotion _locomotion;
        private readonly SheepWanderArea _wanderArea;
        private readonly Func<float> _durationProvider;
        private readonly float _minimumFleeDistance;
        private readonly int _maximumPointAttempts;
        private readonly Action _finishState;

        private Vector3 _threatPosition;
        private float _remainingTime;
        private bool _hasMoveOrder;

        public SheepStateId Id => SheepStateId.ScaredRun;

        public SheepScaredState(
            SheepLocomotion locomotion,
            SheepWanderArea wanderArea,
            Func<float> durationProvider,
            float minimumFleeDistance,
            int maximumPointAttempts,
            Action finishState)
        {
            _locomotion = locomotion;
            _wanderArea = wanderArea;
            _durationProvider = durationProvider ??
                throw new ArgumentNullException(nameof(durationProvider));
            _minimumFleeDistance = minimumFleeDistance;
            _maximumPointAttempts = maximumPointAttempts;
            _finishState = finishState ??
                throw new ArgumentNullException(nameof(finishState));
        }

        public void ReactToThreat(Vector3 threatPosition)
        {
            _threatPosition = threatPosition;
            _remainingTime = _durationProvider();
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
                if (!TryStartFleeing())
                {
                    _finishState();
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

            _finishState();
        }

        public void Exit()
        {
            _locomotion?.Stop();
            _hasMoveOrder = false;
        }

        private bool TryStartFleeing()
        {
            if (_locomotion == null || _wanderArea == null ||
                !_locomotion.IsReady)
            {
                return false;
            }

            if (!_wanderArea.TryGetFleePoint(
                    _locomotion.Position,
                    _threatPosition,
                    _minimumFleeDistance,
                    _maximumPointAttempts,
                    out Vector3 destination))
            {
                return false;
            }

            return _locomotion.TryMoveTo(destination);
        }
    }
}
