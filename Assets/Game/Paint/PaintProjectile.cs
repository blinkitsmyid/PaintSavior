using UnityEngine;

namespace Game.Paint
{
    public sealed class PaintProjectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifeTime = 3f;

        [Header("Hit Detection")]
        [SerializeField] private float castRadius = 0.08f;

        private float _lifeTimer;
        private Color _color;
        private Vector3 _sourcePosition;

        private static readonly RaycastHit[] HitBuffer =
            new RaycastHit[16];

        public void Launch(
            Vector3 direction,
            Color color)
        {
            Launch(direction, color, transform.position);
        }

        public void Launch(
            Vector3 direction,
            Color color,
            Vector3 sourcePosition)
        {
            transform.forward = direction.normalized;

            _color = color;
            _sourcePosition = sourcePosition;
            _lifeTimer = lifeTime;
        }

        private void Update()
        {
            float distance =
                speed * Time.deltaTime;

            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;

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

            _lifeTimer -= Time.deltaTime;

            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

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
