using System;

namespace Game.Sheep.AI
{
    internal sealed class SheepNeedState : ISheepState
    {
        private readonly SheepStateId _id;
        private readonly SheepNeedType _needType;
        private readonly SheepLocomotion _locomotion;
        private readonly SheepNeedsEnvironment _environment;
        private readonly SheepWoolGrowth _woolGrowth;
        private readonly Func<float> _actionDurationProvider;
        private readonly Action _finishState;

        private bool _hasMoveOrder;
        private bool _isPerformingAction;
        private float _remainingActionTime;

        public SheepStateId Id => _id;

        public SheepNeedState(
            SheepStateId id,
            SheepNeedType needType,
            SheepLocomotion locomotion,
            SheepNeedsEnvironment environment,
            SheepWoolGrowth woolGrowth,
            Func<float> actionDurationProvider,
            Action finishState)
        {
            _id = id;
            _needType = needType;
            _locomotion = locomotion;
            _environment = environment;
            _woolGrowth = woolGrowth;
            _actionDurationProvider = actionDurationProvider ??
                throw new ArgumentNullException(nameof(actionDurationProvider));
            _finishState = finishState ??
                throw new ArgumentNullException(nameof(finishState));
        }

        public void Enter()
        {
            _hasMoveOrder = false;
            _isPerformingAction = false;
            _remainingActionTime = _actionDurationProvider();
        }

        public void Tick(float deltaTime)
        {
            if (!_hasMoveOrder)
            {
                if (!TryStartMovement())
                {
                    _finishState();
                    return;
                }

                _hasMoveOrder = true;
            }

            if (!_isPerformingAction)
            {
                if (_locomotion.HasInvalidPath)
                {
                    _finishState();
                    return;
                }

                if (!_locomotion.HasReachedDestination)
                {
                    return;
                }

                _locomotion.Stop();
                _isPerformingAction = true;
            }

            _remainingActionTime -= deltaTime;

            if (_remainingActionTime > 0f)
            {
                return;
            }

            _woolGrowth.TryCompleteNeed(_needType);
            _finishState();
        }

        public void Exit()
        {
            _locomotion?.Stop();
            _hasMoveOrder = false;
            _isPerformingAction = false;
        }

        private bool TryStartMovement()
        {
            if (_locomotion == null || _environment == null ||
                _woolGrowth == null || !_locomotion.IsReady ||
                !_woolGrowth.CanCompleteNeed(_needType))
            {
                return false;
            }

            if (!_environment.TryGetRandomPoint(_needType, out SheepNeedPoint point))
            {
                return false;
            }

            return _locomotion.TryMoveTo(point.Position);
        }
    }
}
