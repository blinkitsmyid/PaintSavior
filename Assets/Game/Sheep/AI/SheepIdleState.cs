using System;

namespace Game.Sheep.AI
{
    internal sealed class SheepIdleState : ISheepState
    {
        private readonly Func<float> _durationProvider;
        private readonly Func<bool> _tryStartWandering;

        private float _remainingTime;

        public SheepStateId Id => SheepStateId.Idle;

        public SheepIdleState(
            Func<float> durationProvider,
            Func<bool> tryStartWandering)
        {
            _durationProvider = durationProvider ??
                throw new ArgumentNullException(nameof(durationProvider));
            _tryStartWandering = tryStartWandering ??
                throw new ArgumentNullException(nameof(tryStartWandering));
        }

        public void Enter()
        {
            _remainingTime = _durationProvider();
        }

        public void Tick(float deltaTime)
        {
            _remainingTime -= deltaTime;

            if (_remainingTime > 0f)
            {
                return;
            }

            if (_tryStartWandering())
            {
                return;
            }

            // If the NavMesh or the wander area is not ready yet, stay calm
            // and try again after the next idle interval instead of retrying
            // every frame.
            _remainingTime = _durationProvider();
        }

        public void Exit()
        {
        }
    }
}
