namespace Game.Animals.Core
{
    /// <summary>
    /// Определяет модуль, который может предложить животному заявку на выполнение поведения.
    /// </summary>
    public interface IAnimalBehaviorRequestSource
    {
        /// <summary>
        /// Возвращает стабильный идентификатор источника заявки для диагностики и отладки.
        /// </summary>
        string SourceId { get; }

        /// <summary>
        /// Пытается создать актуальную заявку поведения для указанного игрового времени.
        /// </summary>
        bool TryCreateRequest(
            float currentTime,
            out AnimalBehaviorRequest request);
    }
}
