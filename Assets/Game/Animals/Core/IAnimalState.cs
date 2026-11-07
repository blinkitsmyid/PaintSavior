namespace Game.Animals.Core
{
    /// <summary>
    /// Определяет общий жизненный цикл состояния любого животного-NPC.
    /// </summary>
    public interface IAnimalState
    {
        /// <summary>
        /// Возвращает стабильный идентификатор состояния для логов и переходов.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Выполняет подготовку состояния при входе в него.
        /// </summary>
        void Enter();

        /// <summary>
        /// Обновляет поведение состояния за прошедший кадр.
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// Освобождает управление и завершает работу состояния.
        /// </summary>
        void Exit();
    }
}
