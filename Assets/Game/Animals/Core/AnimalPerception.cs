using UnityEngine;

namespace Game.Animals.Core
{
    /// <summary>
    /// Хранит наблюдаемую цель животного и выполняет простые проверки дистанции.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimalPerception : MonoBehaviour
    {
        [Header("Observed Target")]
        [SerializeField] private Transform target;

        /// <summary>
        /// Возвращает текущую наблюдаемую цель животного.
        /// </summary>
        public Transform Target => target;

        /// <summary>
        /// Показывает, назначена ли наблюдаемая цель.
        /// </summary>
        public bool HasTarget => target != null;

        /// <summary>
        /// Назначает или заменяет наблюдаемую цель животного.
        /// </summary>
        public void SetTarget(
            Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Проверяет расстояние до цели по горизонтальной плоскости.
        /// </summary>
        public bool IsTargetWithinDistance(
            float distance)
        {
            if (target == null || distance < 0f)
            {
                return false;
            }

            Vector3 offset = target.position - transform.position;
            offset.y = 0f;

            return offset.sqrMagnitude <= distance * distance;
        }
    }
}
