using UnityEngine;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Хранит назначенные водоёмы и предоставляет ближайший доступный водоём овце.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SheepWaterEnvironment : MonoBehaviour
    {
        [Header("Water Points")]
        [SerializeField] private SheepWaterPoint[] waterPoints;

        /// <summary>
        /// Возвращает количество зарегистрированных водоёмов.
        /// </summary>
        public int WaterPointCount =>
            waterPoints != null ? waterPoints.Length : 0;

        /// <summary>
        /// Проверяет, существует ли хотя бы один доступный водоём.
        /// </summary>
        public bool HasAvailablePoint()
        {
            if (waterPoints == null)
            {
                return false;
            }

            for (int index = 0; index < waterPoints.Length; index++)
            {
                if (waterPoints[index] != null &&
                    waterPoints[index].IsAvailable)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Находит ближайший доступный водоём без создания временных коллекций.
        /// </summary>
        public bool TryGetClosestPoint(
            Vector3 origin,
            out SheepWaterPoint closestPoint)
        {
            closestPoint = null;

            if (waterPoints == null)
            {
                return false;
            }

            float closestDistance = float.PositiveInfinity;

            for (int index = 0; index < waterPoints.Length; index++)
            {
                SheepWaterPoint candidate = waterPoints[index];

                if (candidate == null || !candidate.IsAvailable)
                {
                    continue;
                }

                float distance =
                    (candidate.ApproachPosition - origin).sqrMagnitude;

                if (distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                closestPoint = candidate;
            }

            return closestPoint != null;
        }
    }
}
