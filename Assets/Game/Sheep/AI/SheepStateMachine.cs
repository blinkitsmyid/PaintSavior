using System;

namespace Game.Sheep.AI
{
    internal sealed class SheepStateMachine
    {
        private ISheepState _currentState;

        public SheepStateId CurrentStateId { get; private set; } =
            SheepStateId.None;

        public bool IsRunning => _currentState != null;

        public event Action<SheepStateId, SheepStateId> StateChanged;

        public void Start(ISheepState initialState)
        {
            if (initialState == null)
            {
                throw new ArgumentNullException(nameof(initialState));
            }

            if (IsRunning)
            {
                return;
            }

            _currentState = initialState;
            CurrentStateId = initialState.Id;
            _currentState.Enter();

            StateChanged?.Invoke(
                SheepStateId.None,
                CurrentStateId
            );
        }

        public bool TransitionTo(ISheepState nextState)
        {
            if (nextState == null)
            {
                throw new ArgumentNullException(nameof(nextState));
            }

            if (!IsRunning)
            {
                Start(nextState);
                return true;
            }

            if (CurrentStateId == nextState.Id)
            {
                return false;
            }

            SheepStateId previousStateId = CurrentStateId;

            _currentState.Exit();
            _currentState = nextState;
            CurrentStateId = nextState.Id;
            _currentState.Enter();

            StateChanged?.Invoke(
                previousStateId,
                CurrentStateId
            );

            return true;
        }

        public void Tick(float deltaTime)
        {
            _currentState?.Tick(deltaTime);
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            _currentState.Exit();
            _currentState = null;
            CurrentStateId = SheepStateId.None;
        }
    }
}
