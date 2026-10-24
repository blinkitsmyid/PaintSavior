using UnityEngine;
using UnityEngine.AI;

namespace Game.Sheep.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SheepWanderArea : MonoBehaviour
    {
        [SerializeField] private BoxCollider areaCollider;
        [SerializeField, Min(0.01f)] private float navMeshSampleDistance = 1f;
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

        private void Awake()
        {
            ResolveCollider();
        }

        private void OnValidate()
        {
            ResolveCollider();
            navMeshSampleDistance = Mathf.Max(0.01f, navMeshSampleDistance);
        }

        public bool TryGetRandomPoint(
            Vector3 origin,
            float minimumDistance,
            int maximumAttempts,
            out Vector3 point)
        {
            point = default;

            if (areaCollider == null || maximumAttempts <= 0)
            {
                return false;
            }

            Bounds bounds = areaCollider.bounds;
            float minimumDistanceSqr = minimumDistance * minimumDistance;

            for (int attempt = 0; attempt < maximumAttempts; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                if (!NavMesh.SamplePosition(
                        candidate,
                        out NavMeshHit navMeshHit,
                        navMeshSampleDistance,
                        navMeshAreaMask))
                {
                    continue;
                }

                Vector3 offset = navMeshHit.position - origin;
                offset.y = 0f;

                if (offset.sqrMagnitude < minimumDistanceSqr)
                {
                    continue;
                }

                point = navMeshHit.position;
                return true;
            }

            return false;
        }

        public bool TryGetFleePoint(
            Vector3 origin,
            Vector3 threatPosition,
            float minimumFleeDistance,
            int maximumAttempts,
            out Vector3 point)
        {
            point = default;

            if (areaCollider == null || maximumAttempts <= 0)
            {
                return false;
            }

            Bounds bounds = areaCollider.bounds;
            float currentThreatDistance = FlatDistance(origin, threatPosition);
            float requiredThreatDistance =
                currentThreatDistance + minimumFleeDistance;

            for (int attempt = 0; attempt < maximumAttempts; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                if (!NavMesh.SamplePosition(
                        candidate,
                        out NavMeshHit navMeshHit,
                        navMeshSampleDistance,
                        navMeshAreaMask))
                {
                    continue;
                }

                if (FlatDistance(navMeshHit.position, threatPosition) <
                    requiredThreatDistance)
                {
                    continue;
                }

                if (FlatDistance(navMeshHit.position, origin) <
                    minimumFleeDistance)
                {
                    continue;
                }

                point = navMeshHit.position;
                return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (areaCollider == null)
            {
                return;
            }

            Bounds bounds = areaCollider.bounds;
            Gizmos.color = new Color(0.35f, 0.9f, 0.5f, 0.75f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private void ResolveCollider()
        {
            if (areaCollider == null)
            {
                areaCollider = GetComponent<BoxCollider>();
            }
        }

        private static float FlatDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }
    }
}
