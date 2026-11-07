using UnityEngine;

namespace Game.Animals.Core
{
    /// <summary>
    /// Описывает запрос модуля животного на запуск конкретного поведения.
    /// </summary>
    public readonly struct AnimalBehaviorRequest
    {
        /// <summary>
        /// Создаёт заявку с идентификатором поведения, приоритетом и необязательной целью.
        /// </summary>
        public AnimalBehaviorRequest(
            string behaviorId,
            int priority,
            Transform target = null,
            float expirationTime = -1f,
            bool canInterrupt = false)
        {
            BehaviorId = behaviorId;
            Priority = priority;
            Target = target;
            ExpirationTime = expirationTime;
            CanInterrupt = canInterrupt;
        }

        /// <summary>
        /// Возвращает идентификатор поведения, например ScaredRun или Wandering.
        /// </summary>
        public string BehaviorId { get; }

        /// <summary>
        /// Возвращает приоритет заявки: большее значение означает более срочное поведение.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Возвращает цель поведения, если она требуется состоянию.
        /// </summary>
        public Transform Target { get; }

        /// <summary>
        /// Возвращает время окончания заявки или отрицательное значение для бессрочной заявки.
        /// </summary>
        public float ExpirationTime { get; }

        /// <summary>
        /// Возвращает признак, что заявка может прервать обычное текущее поведение.
        /// </summary>
        public bool CanInterrupt { get; }

        /// <summary>
        /// Проверяет, истёк ли срок действия заявки в указанное время.
        /// </summary>
        public bool IsExpired(
            float currentTime)
        {
            return ExpirationTime >= 0f &&
                currentTime > ExpirationTime;
        }

        /// <summary>
        /// Проверяет, содержит ли заявка идентификатор и остаётся ли она действительной.
        /// </summary>
        public bool IsValid(
            float currentTime)
        {
            return !string.IsNullOrWhiteSpace(BehaviorId) &&
                !IsExpired(currentTime);
        }
    }
}
