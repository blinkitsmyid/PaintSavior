using Game.Animals.Core;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Формирует одноразовую заявку на движение загрязнённой овцы к воде.
    /// </summary>
    public sealed class SheepSeekingWaterBehaviorSource : IAnimalBehaviorRequestSource
    {
        /// <summary>
        /// Возвращает идентификатор поведения поиска водоёма.
        /// </summary>
        public const string BehaviorId = "SeekingWater";

        private readonly SheepContaminationState _contaminationState;
        private readonly SheepWaterEnvironment _waterEnvironment;
        private readonly int _priority;
        private bool _hasPendingRequest;

        /// <summary>
        /// Создаёт источник поиска воды с состоянием загрязнения и реестром водоёмов.
        /// </summary>
        public SheepSeekingWaterBehaviorSource(
            SheepContaminationState contaminationState,
            SheepWaterEnvironment waterEnvironment,
            int priority)
        {
            _contaminationState = contaminationState;
            _waterEnvironment = waterEnvironment;
            _priority = priority;
        }

        /// <summary>
        /// Возвращает стабильное имя источника поиска воды.
        /// </summary>
        public string SourceId => "Sheep.SeekingWater";

        /// <summary>
        /// Создаёт одноразовое намерение проверить возможность движения к воде.
        /// </summary>
        public void Request()
        {
            _hasPendingRequest = true;
        }

        /// <summary>
        /// Возвращает заявку SeekingWater, если овца всё ещё хочет мыться и вода доступна.
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

            if (_contaminationState == null ||
                !_contaminationState.WantsToWash ||
                _waterEnvironment == null ||
                !_waterEnvironment.HasAvailablePoint())
            {
                return false;
            }

            request = new AnimalBehaviorRequest(
                BehaviorId,
                _priority
            );
            return true;
        }
    }
}
