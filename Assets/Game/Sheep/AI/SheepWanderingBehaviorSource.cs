using Game.Animals.Core;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Формирует одноразовую заявку на случайное блуждание овцы.
    /// </summary>
    public sealed class SheepWanderingBehaviorSource : IAnimalBehaviorRequestSource
    {
        /// <summary>
        /// Возвращает идентификатор поведения, которое запускает этот источник.
        /// </summary>
        public const string BehaviorId = "Wandering";

        private readonly int _priority;
        private bool _hasPendingRequest;

        /// <summary>
        /// Создаёт источник блуждания с заданным приоритетом заявки.
        /// </summary>
        public SheepWanderingBehaviorSource(
            int priority)
        {
            _priority = priority;
        }

        /// <summary>
        /// Возвращает стабильное имя источника блуждания.
        /// </summary>
        public string SourceId => "Sheep.Wandering";

        /// <summary>
        /// Создаёт одноразовую заявку на начало блуждания.
        /// </summary>
        public void Request()
        {
            _hasPendingRequest = true;
        }

        /// <summary>
        /// Возвращает заявку Wandering и снимает её после рассмотрения арбитром.
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
