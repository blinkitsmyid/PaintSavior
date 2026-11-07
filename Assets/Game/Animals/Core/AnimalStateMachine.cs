using System;

namespace Game.Animals.Core
{
    /// <summary>
    /// Управляет жизненным циклом и последовательными переходами состояний животного.
    /// </summary>
    public sealed class AnimalStateMachine
    {
        /// <summary>
        /// Идентификатор состояния до запуска автомата.
        /// </summary>
        public const string NoneStateId = "None";

        private IAnimalState _currentState;

        /// <summary>
        /// Возвращает текущее состояние или null, если автомат остановлен.
        /// </summary>
        public IAnimalState CurrentState => _currentState;

        /// <summary>
        /// Возвращает идентификатор текущего состояния.
        /// </summary>
        public string CurrentStateId =>
            _currentState?.Id ?? NoneStateId;

        /// <summary>
        /// Показывает, запущен ли автомат состояний.
        /// </summary>
        public bool IsRunning => _currentState != null;

        /// <summary>
        /// Сообщает о завершённом переходе между состояниями.
        /// </summary>
        public event Action<string, string> StateChanged;

        /// <summary>
        /// Запускает автомат с указанным начальным состоянием.
        /// </summary>
        public void Start(
            IAnimalState initialState)
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
            _currentState.Enter();

            StateChanged?.Invoke(
                NoneStateId,
                CurrentStateId
            );
        }

        /// <summary>
        /// Завершает текущее состояние и переключает автомат на новое состояние.
        /// </summary>
        public bool TransitionTo(
            IAnimalState nextState)
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

            string previousStateId =
                CurrentStateId;

            _currentState.Exit();
            _currentState = nextState;
            _currentState.Enter();

            StateChanged?.Invoke(
                previousStateId,
                CurrentStateId
            );

            return true;
        }

        /// <summary>
        /// Передаёт прошедшее время текущему состоянию.
        /// </summary>
        public void Tick(
            float deltaTime)
        {
            _currentState?.Tick(deltaTime);
        }

        /// <summary>
        /// Вызывает выход текущего состояния и останавливает автомат.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            _currentState.Exit();
            _currentState = null;
        }
    }
}
