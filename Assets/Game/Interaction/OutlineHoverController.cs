using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Находит интерактивную цель лучом из центра камеры и управляет её обводкой.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OutlineHoverController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Камера, из центра которой выполняется проверка наведения.")]
        [SerializeField]
        private Camera playerCamera;

        [Header("Raycast")]
        [Tooltip("Слои коллайдеров, по которым может проходить луч наведения.")]
        [SerializeField]
        private LayerMask hoverMask = ~0;

        [Tooltip("Максимальная дистанция обнаружения объекта.")]
        [SerializeField, Min(0.1f)]
        private float hoverDistance = 5f;

        [Tooltip("Определяет, должны ли триггер-коллайдеры участвовать в наведении.")]
        [SerializeField]
        private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        private OutlineTarget _currentTarget;

        /// <summary>
        /// Находит камеру игрока, если она не была назначена в Inspector.
        /// </summary>
        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        /// <summary>
        /// Обновляет цель наведения один раз за кадр.
        /// </summary>
        private void Update()
        {
            OutlineTarget nextTarget = FindTargetUnderCrosshair();

            if (nextTarget == _currentTarget)
            {
                return;
            }

            ChangeTarget(nextTarget);
        }

        /// <summary>
        /// Выполняет луч из центра камеры и возвращает ближайшую OutlineTarget.
        /// </summary>
        private OutlineTarget FindTargetUnderCrosshair()
        {
            if (playerCamera == null)
            {
                return null;
            }

            Ray hoverRay = playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));

            if (!Physics.Raycast(
                    hoverRay,
                    out RaycastHit hit,
                    hoverDistance,
                    hoverMask,
                    triggerInteraction))
            {
                return null;
            }

            return hit.collider.GetComponentInParent<OutlineTarget>();
        }

        /// <summary>
        /// Выключает старую цель и включает новую цель наведения.
        /// </summary>
        private void ChangeTarget(OutlineTarget nextTarget)
        {
            if (_currentTarget != null)
            {
                _currentTarget.SetHighlighted(false);
            }

            _currentTarget = nextTarget;

            if (_currentTarget != null)
            {
                _currentTarget.SetHighlighted(true);
            }
        }

        /// <summary>
        /// Ограничивает дистанцию наведения положительным значением.
        /// </summary>
        private void OnValidate()
        {
            hoverDistance = Mathf.Max(0.1f, hoverDistance);
        }

        /// <summary>
        /// Возвращает активную цель на исходный слой перед отключением контроллера.
        /// </summary>
        private void OnDisable()
        {
            if (_currentTarget == null)
            {
                return;
            }

            _currentTarget.SetHighlighted(false);
            _currentTarget = null;
        }
    }
}
