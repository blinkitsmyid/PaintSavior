using Game.Animals.Core;

using UnityEngine;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Формирует заявку на поглаживание овцы по команде игрока.
    /// </summary>
    public sealed class SheepPetBehaviorSource : IAnimalBehaviorRequestSource
    {
        /// <summary>
        /// Возвращает идентификатор поведения поглаживания.
        /// </summary>
        public const string BehaviorId = "Pet";

        private readonly int _priority;
        private Transform _actor;
        private bool _hasPendingRequest;

        /// <summary>
        /// Создаёт источник поглаживания с заданным приоритетом взаимодействия.
        /// </summary>
        public SheepPetBehaviorSource(
            int priority)
        {
            _priority = priority;
        }

        /// <summary>
        /// Возвращает стабильное имя источника поглаживания.
        /// </summary>
        public string SourceId => "Sheep.Pet";

        /// <summary>
        /// Создаёт заявку на поглаживание от указанного игрока.
        /// </summary>
        public void Request(
            Transform actor)
        {
            _actor = actor;
            _hasPendingRequest = actor != null;
        }

        /// <summary>
        /// Возвращает заявку Pet и разрешает ей прервать текущее поведение овцы.
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
                _priority,
                _actor,
                -1f,
                true
            );
            _actor = null;
            return true;
        }
    }
}
