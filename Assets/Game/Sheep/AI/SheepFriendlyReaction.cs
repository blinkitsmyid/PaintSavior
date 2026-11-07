using Game.Animals.Core;

using UnityEngine;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Проверяет условия автоматической дружелюбной реакции овцы на игрока рядом.
    /// </summary>
    public sealed class SheepFriendlyReaction
    {
        private readonly Transform _owner;
        private readonly AnimalPerception _perception;
        private readonly float _friendlyDistance;

        /// <summary>
        /// Создаёт модуль дружелюбной реакции с источником восприятия и радиусом проверки.
        /// </summary>
        public SheepFriendlyReaction(
            Transform owner,
            AnimalPerception perception,
            float friendlyDistance)
        {
            _owner = owner;
            _perception = perception;
            _friendlyDistance = Mathf.Max(0f, friendlyDistance);
        }

        /// <summary>
        /// Возвращает игрока, который находится в зоне дружелюбной реакции овцы.
        /// </summary>
        public bool TryGetNearbyTarget(
            Transform fallbackTarget,
            bool isScared,
            bool isFriendly,
            bool requiresPettingAfterScare,
            out Transform target)
        {
            target = null;

            if (isScared || isFriendly || requiresPettingAfterScare)
            {
                return false;
            }

            if (_perception != null)
            {
                if (!_perception.HasTarget ||
                    !_perception.IsTargetWithinDistance(_friendlyDistance))
                {
                    return false;
                }

                target = _perception.Target;
                return target != null;
            }

            if (_owner == null || fallbackTarget == null)
            {
                return false;
            }

            Vector3 offset = fallbackTarget.position - _owner.position;
            offset.y = 0f;

            if (offset.sqrMagnitude >
                _friendlyDistance * _friendlyDistance)
            {
                return false;
            }

            target = fallbackTarget;
            return true;
        }
    }
}
