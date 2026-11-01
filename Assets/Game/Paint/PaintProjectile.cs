using UnityEngine;

namespace Game.Paint
{
    public sealed class PaintProjectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifeTime = 3f;
        [SerializeField, Min(0f)] private float gravityMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float maxFlightDistance = 50f;

        [Header("Hit Detection")]
        [SerializeField] private float castRadius = 0.08f;

        private float _lifeTimer;
        private float _currentSpeed;
        private Vector3 _velocity;
        private Vector3 _launchPosition;
        private Color _color;
        private Vector3 _sourcePosition;

        private static readonly RaycastHit[] HitBuffer =
            new RaycastHit[16];

        /// <summary>
        /// Запускает снаряд с его настроенной в Inspector скоростью.
        /// </summary>
        public void Launch(
            Vector3 direction,
            Color color)
        {
            Launch(
                direction,
                color,
                transform.position,
                speed
            );
        }

        /// <summary>
        /// Запускает снаряд с указанной позицией источника и стандартной скоростью prefab.
        /// </summary>
        public void Launch(
            Vector3 direction,
            Color color,
            Vector3 sourcePosition)
        {
            Launch(
                direction,
                color,
                sourcePosition,
                speed
            );
        }

        /// <summary>
        /// Запускает снаряд с указанной скоростью, цветом и позицией источника попадания.
        /// </summary>
        public void Launch(
            Vector3 direction,
            Color color,
            Vector3 sourcePosition,
            float launchSpeed)
        {
            Vector3 safeDirection =
                direction.sqrMagnitude > 0.0001f
                    ? direction.normalized
                    : transform.forward;

            transform.forward = safeDirection;

            _currentSpeed =
                Mathf.Max(0f, launchSpeed);
            _velocity =
                safeDirection * _currentSpeed;
            _launchPosition =
                transform.position;
            _color = color;
            _sourcePosition = sourcePosition;
            _lifeTimer = lifeTime;
        }

        /// <summary>
        /// Перемещает снаряд, проверяет столкновение и удаляет его после истечения времени жизни.
        /// </summary>
        private void Update()
        {
            float deltaTime =
                Time.deltaTime;

            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 nextVelocity =
                _velocity +
                Physics.gravity *
                gravityMultiplier *
                deltaTime;

            Vector3 displacement =
                (_velocity + nextVelocity) *
                (0.5f * deltaTime);

            float distance =
                displacement.magnitude;

            Vector3 origin = transform.position;
            Vector3 direction =
                distance > 0.0001f
                    ? displacement / distance
                    : transform.forward;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                castRadius,
                direction,
                HitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide
            );

            if (TryGetPaintSurfaceHit(hitCount, out RaycastHit hit))
            {
                HandleHit(hit);
                return;
            }

            transform.position += direction * distance;
            _velocity = nextVelocity;

            if (_velocity.sqrMagnitude > 0.0001f)
            {
                transform.forward =
                    _velocity.normalized;
            }

            _lifeTimer -= deltaTime;

            bool exceededLifeTime =
                _lifeTimer <= 0f;

            bool exceededFlightDistance =
                (transform.position - _launchPosition).sqrMagnitude >=
                maxFlightDistance * maxFlightDistance;

            if (exceededLifeTime || exceededFlightDistance)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Передаёт подтверждённое попадание контроллеру покраски овцы и удаляет снаряд.
        /// </summary>
        private void HandleHit(RaycastHit hit)
        {
            SheepPaintController controller =
                hit.collider.GetComponentInParent<SheepPaintController>();

            if (controller == null || hit.collider is not MeshCollider)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 paintUV = hit.textureCoord;

            PaintHit paintHit = new PaintHit(
                SheepSide.Left,
                hit.point,
                hit.normal,
                paintUV,
                _sourcePosition
            );

            Debug.Log(
                $"Hit: {paintHit.Side} | " +
                $"World: {hit.point} | " +
                $"UV: {paintUV}"
            );

            controller.ApplyPaint(
                paintHit,
                _color
            );

            Destroy(gameObject);
        }

        /// <summary>
        /// Находит ближайшую MeshCollider, связанную с поверхностью покраски овцы.
        /// </summary>
        private static bool TryGetPaintSurfaceHit(
            int hitCount,
            out RaycastHit surfaceHit)
        {
            surfaceHit = default;
            float nearestDistance = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = HitBuffer[i];

                if (candidate.collider is not MeshCollider)
                {
                    continue;
                }

                if (candidate.collider.GetComponentInParent<SheepPaintController>() == null)
                {
                    continue;
                }

                if (candidate.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = candidate.distance;
                surfaceHit = candidate;
                found = true;
            }

            return found;
        }
    }
}
