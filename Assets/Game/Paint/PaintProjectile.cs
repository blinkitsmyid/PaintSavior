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
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField, Min(0.1f)] private float impactEffectLifetime = 2f;

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
                collisionMask,
                QueryTriggerInteraction.Ignore
            );

            if (TryGetNearestCollision(hitCount, out RaycastHit hit))
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
        /// Передаёт подтверждённое попадание найденному получателю краски и удаляет снаряд.
        /// </summary>
        private void HandleHit(RaycastHit hit)
        {
            SpawnImpactEffect(hit);

            IPaintReceiver receiver =
                hit.collider.GetComponentInParent(
                    typeof(IPaintReceiver)
                ) as IPaintReceiver;

            if (receiver == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 paintUV =
                hit.collider is MeshCollider
                    ? hit.textureCoord2
                    : Vector2.zero;

            PaintImpactData impact =
                new PaintImpactData(
                    hit.point,
                    hit.normal,
                    paintUV,
                    _color,
                    _sourcePosition,
                    hit.collider
                );

            receiver.ReceivePaint(
                impact
            );

            Destroy(gameObject);
        }

        /// <summary>
        /// Создаёт необязательный эффект столкновения и удаляет его после короткого времени жизни.
        /// </summary>
        private void SpawnImpactEffect(
            RaycastHit hit)
        {
            if (impactEffectPrefab == null)
            {
                return;
            }

            Vector3 normal =
                hit.normal.sqrMagnitude > 0.0001f
                    ? hit.normal.normalized
                    : Vector3.up;

            GameObject effect =
                Instantiate(
                    impactEffectPrefab,
                    hit.point,
                    Quaternion.LookRotation(normal)
                );

            Destroy(
                effect,
                impactEffectLifetime
            );
        }

        /// <summary>
        /// Находит ближайший подходящий Collider среди результатов SphereCast.
        /// </summary>
        private static bool TryGetNearestCollision(
            int hitCount,
            out RaycastHit surfaceHit)
        {
            surfaceHit = default;
            float nearestDistance = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = HitBuffer[i];

                if (candidate.collider == null)
                {
                    continue;
                }

                if (candidate.collider.GetComponentInParent<PaintShooter>() != null)
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
