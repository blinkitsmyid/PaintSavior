using UnityEngine;

using Game.Paint;

namespace Game.Player
{
    /// <summary>
    /// Отображает натяжение рогатки через перемещение обоймы и две резинки LineRenderer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SlingshotAimView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PaintShooter shooter;
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private Transform pouchRoot;
        [SerializeField] private Transform leftForkPoint;
        [SerializeField] private Transform rightForkPoint;
        [SerializeField] private Transform leftPouchPoint;
        [SerializeField] private Transform rightPouchPoint;
        [SerializeField] private LineRenderer leftBand;
        [SerializeField] private LineRenderer rightBand;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float maxPullDistance = 0.35f;
        [SerializeField, Min(0.1f)] private float followSpeed = 14f;
        [SerializeField, Min(0.1f)] private float returnSpeed = 22f;

        private Vector3 _restLocalPosition;
        private Vector3 _restWorldPosition;
        private bool _hasLocalRestPosition;

        /// <summary>
        /// Находит контроллеры игрока, сохраняет положение обоймы и подготавливает две линии резинки.
        /// </summary>
        private void Awake()
        {
            if (shooter == null)
            {
                shooter =
                    GetComponent<PaintShooter>();
            }

            if (equipment == null)
            {
                equipment =
                    GetComponent<PlayerEquipment>();
            }

            if (pouchRoot != null)
            {
                _restWorldPosition =
                    pouchRoot.position;

                if (pouchRoot.parent != null)
                {
                    _restLocalPosition =
                        pouchRoot.localPosition;

                    _hasLocalRestPosition = true;
                }
            }

            PrepareBand(leftBand);
            PrepareBand(rightBand);
            SetBandsEnabled(false);
        }

        /// <summary>
        /// Плавно перемещает обойму при натяжении и возвращает её в исходное положение после отпускания ЛКМ.
        /// </summary>
        private void Update()
        {
            if (pouchRoot == null)
            {
                return;
            }

            bool isAiming =
                shooter != null &&
                equipment != null &&
                equipment.IsSlingshotEquipped &&
                shooter.IsAiming;

            Vector3 restPosition =
                GetRestWorldPosition();

            Vector3 targetPosition =
                restPosition;

            if (isAiming)
            {
                Vector3 aimDirection =
                    shooter.AimDirection.sqrMagnitude > 0.0001f
                        ? shooter.AimDirection.normalized
                        : transform.forward;

                targetPosition =
                    restPosition -
                    aimDirection *
                    (maxPullDistance * shooter.PullStrength);
            }

            float speed =
                isAiming
                    ? followSpeed
                    : returnSpeed;

            pouchRoot.position =
                Vector3.Lerp(
                    pouchRoot.position,
                    targetPosition,
                    1f - Mathf.Exp(-speed * Time.deltaTime)
                );

            bool isReturning =
                Vector3.Distance(
                    pouchRoot.position,
                    restPosition
                ) > 0.001f;

            SetBandsEnabled(isAiming || isReturning);
            UpdateBandPositions();
        }

        /// <summary>
        /// Настраивает LineRenderer как мировую линию из двух точек.
        /// </summary>
        private void PrepareBand(
            LineRenderer band)
        {
            if (band == null)
            {
                return;
            }

            band.useWorldSpace = true;
            band.positionCount = 2;
        }

        /// <summary>
        /// Обновляет концы резинок по соответствующим точкам основания и обоймы.
        /// </summary>
        private void UpdateBandPositions()
        {
            if (leftBand != null &&
                leftForkPoint != null &&
                leftPouchPoint != null)
            {
                leftBand.SetPosition(
                    0,
                    leftForkPoint.position
                );

                leftBand.SetPosition(
                    1,
                    leftPouchPoint.position
                );
            }

            if (rightBand != null &&
                rightForkPoint != null &&
                rightPouchPoint != null)
            {
                rightBand.SetPosition(
                    0,
                    rightForkPoint.position
                );

                rightBand.SetPosition(
                    1,
                    rightPouchPoint.position
                );
            }
        }

        /// <summary>
        /// Включает или отключает обе резинки одновременно.
        /// </summary>
        private void SetBandsEnabled(
            bool enabled)
        {
            if (leftBand != null)
            {
                leftBand.enabled = enabled;
            }

            if (rightBand != null)
            {
                rightBand.enabled = enabled;
            }
        }

        /// <summary>
        /// Возвращает актуальную мировую точку покоя обоймы с учётом перемещения игрока.
        /// </summary>
        private Vector3 GetRestWorldPosition()
        {
            if (pouchRoot == null)
            {
                return _restWorldPosition;
            }

            if (_hasLocalRestPosition &&
                pouchRoot.parent != null)
            {
                return pouchRoot.parent.TransformPoint(
                    _restLocalPosition
                );
            }

            return _restWorldPosition;
        }
    }
}
