namespace Game.Animals.Core
{
    /// <summary>
    /// Собирает заявки от модулей поведения и возвращает заявку, выбранную арбитром.
    /// </summary>
    public sealed class AnimalBehaviorCoordinator
    {
        private readonly IAnimalBehaviorRequestSource[] _sources;
        private readonly AnimalBehaviorArbiter _arbiter;

        /// <summary>
        /// Создаёт координатор с заранее зарегистрированными источниками поведения.
        /// </summary>
        public AnimalBehaviorCoordinator(
            IAnimalBehaviorRequestSource[] sources)
        {
            _sources = sources ?? System.Array.Empty<IAnimalBehaviorRequestSource>();
            _arbiter = new AnimalBehaviorArbiter();
        }

        /// <summary>
        /// Собирает актуальные заявки и возвращает победившую заявку на текущий момент времени.
        /// </summary>
        public bool TryEvaluate(
            float currentTime,
            out AnimalBehaviorRequest winningRequest)
        {
            _arbiter.Clear();

            for (int index = 0; index < _sources.Length; index++)
            {
                IAnimalBehaviorRequestSource source = _sources[index];

                if (source == null ||
                    !source.TryCreateRequest(currentTime, out AnimalBehaviorRequest request))
                {
                    continue;
                }

                _arbiter.Consider(request, currentTime);
            }

            return _arbiter.TryGetWinner(out winningRequest);
        }
    }
}
