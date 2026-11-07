using Game.Animals.Core;

using UnityEngine;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Выбирает доступную потребность овцы и формирует одноразовую заявку на её выполнение.
    /// </summary>
    public sealed class SheepNeedBehaviorSource : IAnimalBehaviorRequestSource
    {
        /// <summary>
        /// Возвращает идентификатор состояния поедания травы.
        /// </summary>
        public const string EatingBehaviorId = "Eating";

        /// <summary>
        /// Возвращает идентификатор состояния питья воды.
        /// </summary>
        public const string DrinkingBehaviorId = "Drinking";

        /// <summary>
        /// Возвращает идентификатор состояния отдыха у цветов.
        /// </summary>
        public const string RestingBehaviorId = "Resting";

        private readonly SheepNeedsEnvironment _needsEnvironment;
        private readonly SheepWoolGrowth _woolGrowth;
        private readonly int _priority;
        private bool _hasPendingRequest;

        /// <summary>
        /// Создаёт источник потребностей с доступом к точкам и прогрессу шерсти овцы.
        /// </summary>
        public SheepNeedBehaviorSource(
            SheepNeedsEnvironment needsEnvironment,
            SheepWoolGrowth woolGrowth,
            int priority)
        {
            _needsEnvironment = needsEnvironment;
            _woolGrowth = woolGrowth;
            _priority = priority;
        }

        /// <summary>
        /// Возвращает стабильное имя источника потребностей.
        /// </summary>
        public string SourceId => "Sheep.Needs";

        /// <summary>
        /// Создаёт одноразовый запрос на выбор доступной потребности.
        /// </summary>
        public void Request()
        {
            _hasPendingRequest = true;
        }

        /// <summary>
        /// Выбирает случайную ещё не выполненную потребность с доступной точкой назначения.
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

            if (_needsEnvironment == null || _woolGrowth == null)
            {
                return false;
            }

            int firstNeed = Random.Range(0, 3);

            for (int offset = 0; offset < 3; offset++)
            {
                SheepNeedType need = (SheepNeedType)((firstNeed + offset) % 3);

                if (!_woolGrowth.CanCompleteNeed(need) ||
                    !_needsEnvironment.HasAvailablePoint(need))
                {
                    continue;
                }

                request = new AnimalBehaviorRequest(
                    GetBehaviorId(need),
                    _priority
                );
                return true;
            }

            return false;
        }

        /// <summary>
        /// Преобразует тип потребности в идентификатор заявки поведения.
        /// </summary>
        private string GetBehaviorId(
            SheepNeedType need)
        {
            return need switch
            {
                SheepNeedType.Eating => EatingBehaviorId,
                SheepNeedType.Drinking => DrinkingBehaviorId,
                SheepNeedType.Resting => RestingBehaviorId,
                _ => string.Empty
            };
        }
    }
}
