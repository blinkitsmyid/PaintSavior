using UnityEngine;
using UnityEngine.AI;

namespace Game.Sheep.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class SheepLocomotion : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;

        [Header("Walking")]
        [SerializeField, Min(0.01f)] private float walkSpeed = 1.2f;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.2f;

        public Vector3 Position => transform.position;

        public bool IsReady =>
            agent != null && agent.enabled && agent.isOnNavMesh;

        public bool HasReachedDestination
        {
            get
            {
                if (!IsReady || agent.pathPending)
                {
                    return false;
                }

                if (agent.remainingDistance > agent.stoppingDistance)
                {
                    return false;
                }

                return !agent.hasPath ||
                    agent.velocity.sqrMagnitude <= 0.001f;
            }
        }

        public bool HasInvalidPath =>
            IsReady && !agent.pathPending &&
            agent.pathStatus == NavMeshPathStatus.PathInvalid;

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
        }

        private void OnValidate()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            walkSpeed = Mathf.Max(0.01f, walkSpeed);
            arrivalDistance = Mathf.Max(0.01f, arrivalDistance);
        }

        public bool TryMoveTo(Vector3 destination)
        {
            if (!IsReady)
            {
                return false;
            }

            agent.speed = walkSpeed;
            agent.stoppingDistance = arrivalDistance;
            agent.isStopped = false;

            return agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (!IsReady)
            {
                return;
            }

            agent.isStopped = true;

            if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }
    }
}
