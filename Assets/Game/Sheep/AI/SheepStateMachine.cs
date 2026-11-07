using System;

using Game.Animals.Core;

namespace Game.Sheep.AI
{
    internal sealed class SheepStateMachine
    {
        private readonly AnimalStateMachine _animalStateMachine =
            new AnimalStateMachine();

        /// <summary>
        /// Возвращает идентификатор текущего состояния овцы.
        /// </summary>
        public SheepStateId CurrentStateId { get; private set; } =
            SheepStateId.None;

        /// <summary>
        /// Показывает, запущен ли автомат состояний овцы.
        /// </summary>
        public bool IsRunning => _animalStateMachine.IsRunning;

        /// <summary>
        /// Сообщает о завершённом переходе между состояниями овцы.
        /// </summary>
        public event Action<SheepStateId, SheepStateId> StateChanged;

        /// <summary>
        /// Запускает автомат с указанным начальным состоянием овцы.
        /// </summary>
        public void Start(
            ISheepState initialState)
        {
            if (initialState == null)
            {
                throw new ArgumentNullException(nameof(initialState));
            }

            if (IsRunning)
            {
                return;
            }

            _animalStateMachine.Start(
                new SheepStateAdapter(initialState)
            );

            CurrentStateId = initialState.Id;

            StateChanged?.Invoke(
                SheepStateId.None,
                CurrentStateId
            );
        }

        /// <summary>
        /// Завершает текущее состояние и переключает овцу на новое состояние.
        /// </summary>
        public bool TransitionTo(
            ISheepState nextState)
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

            bool transitioned = _animalStateMachine.TransitionTo(
                new SheepStateAdapter(nextState)
            );

            if (!transitioned)
            {
                return false;
            }

            CurrentStateId = nextState.Id;

            StateChanged?.Invoke(
                previousStateId,
                CurrentStateId
            );

            return true;
        }

        /// <summary>
        /// Передаёт прошедшее время текущему состоянию овцы.
        /// </summary>
        public void Tick(
            float deltaTime)
        {
            _animalStateMachine.Tick(deltaTime);
        }

        /// <summary>
        /// Вызывает выход текущего состояния и останавливает автомат овцы.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            _animalStateMachine.Stop();
            CurrentStateId = SheepStateId.None;
        }

        /// <summary>
        /// Адаптирует существующее состояние овцы к общему контракту животных.
        /// </summary>
        private sealed class SheepStateAdapter : IAnimalState
        {
            private readonly ISheepState _state;
            private readonly string _id;

            /// <summary>
            /// Создаёт адаптер вокруг состояния овцы.
            /// </summary>
            public SheepStateAdapter(
                ISheepState state)
            {
                _state = state ??
                    throw new ArgumentNullException(nameof(state));
                _id = state.Id.ToString();
            }

            /// <summary>
            /// Возвращает строковый идентификатор состояния.
            /// </summary>
            public string Id => _id;

            /// <summary>
            /// Передаёт состоянию событие входа.
            /// </summary>
            public void Enter()
            {
                _state.Enter();
            }

            /// <summary>
            /// Передаёт состоянию обновление поведения.
            /// </summary>
            public void Tick(
                float deltaTime)
            {
                _state.Tick(deltaTime);
            }

            /// <summary>
            /// Передаёт состоянию событие выхода.
            /// </summary>
            public void Exit()
            {
                _state.Exit();
            }
        }
    }
}
