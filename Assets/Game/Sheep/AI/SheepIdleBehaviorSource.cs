using Game.Animals.Core;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Формирует одноразовую заявку на спокойное ожидание овцы.
    /// </summary>
    public sealed class SheepIdleBehaviorSource : IAnimalBehaviorRequestSource
    {
        /// <summary>
        /// Возвращает идентификатор состояния спокойного ожидания.
        /// </summary>
        public const string BehaviorId = "Idle";

        private readonly int _priority;
        private bool _hasPendingRequest;

        /// <summary>
        /// Создаёт источник ожидания с заданным приоритетом заявки.
        /// </summary>
        public SheepIdleBehaviorSource(
            int priority)
        {
            _priority = priority;
        }

        /// <summary>
        /// Возвращает стабильное имя источника спокойного ожидания.
        /// </summary>
        public string SourceId => "Sheep.Idle";

        /// <summary>
        /// Создаёт одноразовую заявку на спокойное ожидание.
        /// </summary>
        public void Request()
        {
            _hasPendingRequest = true;
        }

        /// <summary>
        /// Возвращает заявку Idle и снимает её после рассмотрения арбитром.
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

            _hasPendingRequest = false;
            request = new AnimalBehaviorRequest(
                BehaviorId,
                _priority
            );
            return true;
        }
    }
}
