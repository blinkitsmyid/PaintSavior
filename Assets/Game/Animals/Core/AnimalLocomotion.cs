using UnityEngine;
using UnityEngine.AI;

namespace Game.Animals.Core
{
    /// <summary>
    /// Управляет перемещением животного через NavMeshAgent.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class AnimalLocomotion : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AnimalDefinition definition;

        [SerializeField] private NavMeshAgent agent;

        [Header("Walking")]
        [SerializeField, Min(0.01f)] private float walkSpeed = 1.2f;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.2f;

        /// <summary>
        /// Возвращает текущую мировую позицию животного.
        /// </summary>
        public Vector3 Position => transform.position;

        /// <summary>
        /// Показывает, готов ли NavMeshAgent принимать команды движения.
        /// </summary>
        public bool IsReady =>
            agent != null && agent.enabled && agent.isOnNavMesh;

        /// <summary>
        /// Показывает, достигло ли животное текущей точки назначения.
        /// </summary>
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

        /// <summary>
        /// Показывает, что текущий путь животного недействителен.
        /// </summary>
        public bool HasInvalidPath =>
            IsReady && !agent.pathPending &&
            agent.pathStatus == NavMeshPathStatus.PathInvalid;

        /// <summary>
        /// Находит NavMeshAgent на объекте животного при запуске.
        /// </summary>
        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
        }

        /// <summary>
        /// Проверяет диапазоны параметров движения в редакторе.
        /// </summary>
        private void OnValidate()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            walkSpeed = Mathf.Max(0.01f, walkSpeed);
            arrivalDistance = Mathf.Max(0.01f, arrivalDistance);
        }

        /// <summary>
        /// Отправляет животное к указанной точке на NavMesh.
        /// </summary>
        public bool TryMoveTo(
            Vector3 destination)
        {
            if (!IsReady)
            {
                return false;
            }

            agent.speed = definition != null
                ? definition.WalkSpeed
                : walkSpeed;
            agent.stoppingDistance = definition != null
                ? definition.ArrivalDistance
                : arrivalDistance;
            agent.isStopped = false;

            return agent.SetDestination(destination);
        }

        /// <summary>
        /// Останавливает животное и сбрасывает текущий путь.
        /// </summary>
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
