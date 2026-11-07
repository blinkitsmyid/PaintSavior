namespace Game.Animals.Core
{
    /// <summary>
    /// Выбирает одну наиболее приоритетную заявку поведения из последовательности предложенных заявок.
    /// </summary>
    public sealed class AnimalBehaviorArbiter
    {
        private AnimalBehaviorRequest _bestRequest;
        private bool _hasRequest;

        /// <summary>
        /// Очищает результат выбора перед сбором заявок для нового игрового кадра или цикла принятия решения.
        /// </summary>
        public void Clear()
        {
            _bestRequest = default;
            _hasRequest = false;
        }

        /// <summary>
        /// Рассматривает заявку и сохраняет её, если она действительна и важнее текущего выбранного поведения.
        /// </summary>
        public void Consider(
            AnimalBehaviorRequest request,
            float currentTime)
        {
            if (!request.IsValid(currentTime))
            {
                return;
            }

            if (!_hasRequest || IsMoreImportant(request, _bestRequest))
            {
                _bestRequest = request;
                _hasRequest = true;
            }
        }

        /// <summary>
        /// Сравнивает две действительные заявки с учётом разрешённого прерывания и приоритета.
        /// </summary>
        private bool IsMoreImportant(
            AnimalBehaviorRequest candidate,
            AnimalBehaviorRequest current)
        {
            if (candidate.CanInterrupt != current.CanInterrupt)
            {
                return candidate.CanInterrupt;
            }

            return candidate.Priority > current.Priority;
        }

        /// <summary>
        /// Возвращает выбранную заявку, если среди рассмотренных заявок была действительная.
        /// </summary>
        public bool TryGetWinner(
            out AnimalBehaviorRequest request)
        {
            request = _bestRequest;
            return _hasRequest;
        }
    }
}
