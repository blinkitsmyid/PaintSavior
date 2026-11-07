using Game.Animals.Core;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Формирует заявку на испуганное поведение овцы после получения угрозы.
    /// </summary>
    public sealed class SheepScaredBehaviorSource : IAnimalBehaviorRequestSource
    {
        /// <summary>
        /// Возвращает идентификатор поведения, которое запускает этот источник.
        /// </summary>
        public const string BehaviorId = "ScaredRun";

        private readonly int _priority;
        private bool _hasPendingRequest;

        /// <summary>
        /// Создаёт источник испуга с заданным приоритетом заявки.
        /// </summary>
        public SheepScaredBehaviorSource(
            int priority)
        {
            _priority = priority;
        }

        /// <summary>
        /// Возвращает стабильное имя источника испуганного поведения.
        /// </summary>
        public string SourceId => "Sheep.Scared";

        /// <summary>
        /// Помечает источник как ожидающий обработки новой угрозы.
        /// </summary>
        public void Request()
        {
            _hasPendingRequest = true;
        }

        /// <summary>
        /// Сбрасывает заявку после того, как координатор передал её овце.
        /// </summary>
        public void Consume()
        {
            _hasPendingRequest = false;
        }

        /// <summary>
        /// Возвращает заявку ScaredRun, если после попадания есть необработанная угроза.
        /// </summary>
        public bool TryCreateRequest(
            float currentTime,
            out AnimalBehaviorRequest request)
        {
            request = default;

            if (!_hasPendingRequest)
            {
                return false;
            }

            request = new AnimalBehaviorRequest(
                BehaviorId,
                _priority,
                null,
                -1f,
                true
            );
            return true;
        }
    }
}
