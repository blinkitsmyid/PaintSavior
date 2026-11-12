using System;

using UnityEngine;

namespace Game.Paint
{
    /// <summary>
    /// Хранит процент покрытия овцы и проверяет достижение настроенного порога.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SheepPaintCoverage : MonoBehaviour
    {
        [Header("Coverage")]
        [SerializeField, Range(0f, 1f)] private float coverageThreshold = 0.8f;
        [SerializeField, Range(0f, 1f)] private float currentCoverage;

        private bool _thresholdWasReached;

        /// <summary>
        /// Сообщает о первом переходе покрытия через настроенный порог.
        /// </summary>
        public event Action<float> CoverageThresholdReached;

        /// <summary>
        /// Возвращает текущее нормализованное покрытие поверхности овцы.
        /// </summary>
        public float CurrentCoverage => currentCoverage;

        /// <summary>
        /// Возвращает нормализованное значение порога покрытия.
        /// </summary>
        public float CoverageThreshold => coverageThreshold;

        /// <summary>
        /// Показывает, был ли достигнут порог покрытия с момента последнего сброса.
        /// </summary>
        public bool IsThresholdReached => _thresholdWasReached;

        /// <summary>
        /// Обновляет процент покрытия и уведомляет подписчиков при первом достижении порога.
        /// </summary>
        public void SetCoverage(float normalizedCoverage)
        {
            currentCoverage =
                Mathf.Clamp01(normalizedCoverage);

            bool thresholdReached =
                currentCoverage >= coverageThreshold;

            if (_thresholdWasReached || !thresholdReached)
            {
                return;
            }

            _thresholdWasReached = true;
            CoverageThresholdReached?.Invoke(currentCoverage);
        }

        /// <summary>
        /// Сбрасывает процент покрытия и возможность повторного срабатывания события.
        /// </summary>
        public void ResetCoverage()
        {
            currentCoverage = 0f;
            _thresholdWasReached = false;
        }

        /// <summary>
        /// Ограничивает значения, изменённые в Inspector, допустимым диапазоном.
        /// </summary>
        private void OnValidate()
        {
            coverageThreshold =
                Mathf.Clamp01(coverageThreshold);

            currentCoverage =
                Mathf.Clamp01(currentCoverage);

            _thresholdWasReached =
                currentCoverage >= coverageThreshold;
        }
    }
}
