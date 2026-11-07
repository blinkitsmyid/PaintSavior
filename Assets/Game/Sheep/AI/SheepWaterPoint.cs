using UnityEngine;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Описывает водоём, к которому животное может подойти и войти для мытья.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SheepWaterPoint : MonoBehaviour
    {
        [Header("Water Point")]
        [SerializeField] private Transform approachPoint;
        [SerializeField] private Collider washZone;
        [SerializeField, Min(0.01f)] private float zoneCheckTolerance = 0.05f;

        /// <summary>
        /// Возвращает точку, к которой животное должно подойти перед мытьём.
        /// </summary>
        public Vector3 ApproachPosition =>
            approachPoint != null
                ? approachPoint.position
                : transform.position;

        /// <summary>
        /// Возвращает признак доступного и настроенного водоёма.
        /// </summary>
        public bool IsAvailable =>
            isActiveAndEnabled && washZone != null && washZone.enabled;

        /// <summary>
        /// Проверяет, находится ли указанная мировая позиция внутри зоны мытья.
        /// </summary>
        public bool ContainsPosition(
            Vector3 worldPosition)
        {
            if (!IsAvailable)
            {
                return false;
            }

            Vector3 closestPoint = washZone.ClosestPoint(worldPosition);
            float tolerance = Mathf.Max(0.01f, zoneCheckTolerance);

            return (closestPoint - worldPosition).sqrMagnitude <=
                tolerance * tolerance;
        }

        /// <summary>
        /// Ограничивает допуск проверки зоны положительным значением в Inspector.
        /// </summary>
        private void OnValidate()
        {
            zoneCheckTolerance = Mathf.Max(0.01f, zoneCheckTolerance);
        }
    }
}
