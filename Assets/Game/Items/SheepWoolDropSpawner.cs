using System.Collections;

using UnityEngine;

using Game.Audio;
using Game.Paint;
using Game.Sheep.AI;

namespace Game.Items
{
    /// <summary>
    /// Создаёт клочки шерсти после стрижки и передаёт им текущие цвета окрашенных сторон овцы.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SheepWoolDropSpawner : MonoBehaviour
    {
        private static readonly SheepSide[] SideOrder =
        {
            SheepSide.Left,
            SheepSide.Right,
            SheepSide.Back,
            SheepSide.Tail
        };

        [Header("References")]
        [SerializeField] private WoolClumpPickup woolClumpPrefab;
        [SerializeField] private Transform dropOrigin;

        [Header("Spawn")]
        [SerializeField, Min(0f)] private float horizontalSpawnRadius = 0.35f;
        [SerializeField] private LayerMask groundLayerMask = ~0;
        [SerializeField, Min(0f)] private float groundProbeHeight = 2f;
        [SerializeField, Min(0f)] private float groundProbeDistance = 10f;
        [SerializeField, Min(0f)] private float landingOffset = 0.03f;
        [SerializeField, Min(0f)] private float jumpHeight = 0.35f;
        [SerializeField, Min(0f)] private float jumpDuration = 0.45f;
        [SerializeField, Range(0f, 1f)] private float randomRotationAmount = 1f;

        [Header("Audio")]
        [Tooltip("Необязательный звук появления и падения одного клочка шерсти.")]
        [SerializeField] private AudioCue woolDropCue;
        [Tooltip("Общий сервис воспроизведения звуков. Если не задан, ищется один раз при создании клочка.")]
        [SerializeField] private AudioService audioService;

        [Header("Debug")]
        [SerializeField] private bool logSpawn;

        private SheepWoolGrowth _woolGrowth;
        private SheepPaintController _paintController;
        private readonly RaycastHit[] _groundHits =
            new RaycastHit[16];

        /// <summary>
        /// Находит компоненты овцы, необходимые для определения стадии и цветов шерсти.
        /// </summary>
        private void Awake()
        {
            _woolGrowth =
                GetComponent<SheepWoolGrowth>();

            _paintController =
                GetComponent<SheepPaintController>();

            if (dropOrigin == null)
            {
                dropOrigin = transform;
            }
        }

        /// <summary>
        /// Создаёт настроенное количество клочков из текущей стадии шерсти до её сброса.
        /// </summary>
        public void SpawnCurrentWool()
        {
            if (_woolGrowth == null ||
                woolClumpPrefab == null)
            {
                LogMissingReferences();
                return;
            }

            WoolGrowthStage stage =
                _woolGrowth.CurrentStage;

            int clumpCount =
                _woolGrowth.GetWoolClumpCount(stage);

            for (int i = 0; i < clumpCount; i++)
            {
                SpawnClump(
                    stage,
                    GetClumpColor(i)
                );
            }

            if (logSpawn && clumpCount > 0)
            {
                Debug.Log(
                    $"{name}: spawned {clumpCount} wool clump(s) " +
                    $"from stage {stage}.",
                    this
                );
            }
        }

        /// <summary>
        /// Создаёт один клочок в небольшой случайной области рядом с точкой сброса.
        /// </summary>
        private void SpawnClump(
            WoolGrowthStage stage,
            Color color)
        {
            Vector2 randomOffset =
                Random.insideUnitCircle * horizontalSpawnRadius;

            Vector3 targetProbePosition =
                dropOrigin.position +
                dropOrigin.right * randomOffset.x +
                dropOrigin.forward * randomOffset.y;

            bool hasStartGround =
                TryFindGroundPosition(
                    dropOrigin.position,
                    out Vector3 startPosition
                );

            bool hasTargetGround =
                TryFindGroundPosition(
                    targetProbePosition,
                    out Vector3 targetPosition
                );

            if (!hasStartGround)
            {
                startPosition = dropOrigin.position;
            }

            if (!hasTargetGround)
            {
                targetPosition = targetProbePosition;
            }

            Quaternion spawnRotation =
                Quaternion.Slerp(
                    dropOrigin.rotation,
                    Random.rotation,
                    randomRotationAmount
                );

            WoolClumpPickup clump =
                Instantiate(
                    woolClumpPrefab,
                    startPosition,
                    spawnRotation
                );

            clump.Initialize(
                stage,
                color
            );

            PlayWoolDropAudio(clump.transform);

            StartCoroutine(
                AnimateClumpJump(
                    clump.transform,
                    startPosition,
                    targetPosition,
                    hasStartGround && hasTargetGround
                )
            );

            if ((!hasStartGround || !hasTargetGround) && logSpawn)
            {
                Debug.LogWarning(
                    $"{name}: ground was not found for a wool clump. " +
                    "Check Ground Layer Mask and Plane collider.",
                    this
                );
            }
        }

        /// <summary>
        /// Воспроизводит звук для созданного клочка и прикрепляет источник к его движению.
        /// </summary>
        private void PlayWoolDropAudio(
            Transform clumpTransform)
        {
            if (woolDropCue == null ||
                clumpTransform == null)
            {
                return;
            }

            ResolveAudioService();

            if (audioService == null)
            {
                return;
            }

            audioService.TryPlay(
                woolDropCue,
                clumpTransform.position,
                clumpTransform
            );
        }

        /// <summary>
        /// Находит AudioService в родителях или сцене, если он не задан вручную.
        /// </summary>
        private void ResolveAudioService()
        {
            if (audioService != null)
            {
                return;
            }

            audioService = GetComponentInParent<AudioService>();

            if (audioService == null)
            {
                audioService = FindFirstObjectByType<AudioService>();
            }
        }

        /// <summary>
        /// Перемещает клочок по короткой дуге от овцы к точке на поверхности.
        /// </summary>
        private IEnumerator AnimateClumpJump(
            Transform clumpTransform,
            Vector3 startPosition,
            Vector3 targetPosition,
            bool hasGroundPoints)
        {
            if (clumpTransform == null)
            {
                yield break;
            }

            if (!hasGroundPoints || jumpDuration <= 0f)
            {
                clumpTransform.position = targetPosition;
                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < jumpDuration)
            {
                float progress =
                    elapsedTime / jumpDuration;

                Vector3 position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        progress
                    );

                position +=
                    Vector3.up *
                    (Mathf.Sin(progress * Mathf.PI) * jumpHeight);

                clumpTransform.position = position;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            clumpTransform.position = targetPosition;
        }

        /// <summary>
        /// Находит ближайшую подходящую поверхность под указанной точкой и возвращает позицию с небольшим отступом.
        /// </summary>
        private bool TryFindGroundPosition(
            Vector3 probePosition,
            out Vector3 groundPosition)
        {
            Vector3 rayOrigin =
                probePosition + Vector3.up * groundProbeHeight;

            int hitCount =
                Physics.RaycastNonAlloc(
                    rayOrigin,
                    Vector3.down,
                    _groundHits,
                    groundProbeDistance,
                    groundLayerMask,
                    QueryTriggerInteraction.Ignore
                );

            float closestDistance = float.PositiveInfinity;
            bool foundGround = false;
            groundPosition = probePosition;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit =
                    _groundHits[i];

                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hit.distance;
                groundPosition =
                    hit.point + hit.normal * landingOffset;
                foundGround = true;
            }

            return foundGround;
        }

        /// <summary>
        /// Выбирает цвет клочка по порядку сторон, используя первый окрашенный цвет как запасной.
        /// </summary>
        private Color GetClumpColor(int clumpIndex)
        {
            Color fallbackColor =
                FindFirstPaintedColor();

            if (_paintController == null)
            {
                return fallbackColor;
            }

            SheepSide side =
                SideOrder[clumpIndex % SideOrder.Length];

            SidePaintState sideState =
                _paintController.GetSideState(side);

            return sideState != null && sideState.IsPainted
                ? sideState.CurrentColor
                : fallbackColor;
        }

        /// <summary>
        /// Находит первый доступный цвет окрашенной стороны или возвращает белый цвет по умолчанию.
        /// </summary>
        private Color FindFirstPaintedColor()
        {
            if (_paintController == null)
            {
                return Color.white;
            }

            for (int i = 0; i < SideOrder.Length; i++)
            {
                SidePaintState sideState =
                    _paintController.GetSideState(SideOrder[i]);

                if (sideState != null && sideState.IsPainted)
                {
                    return sideState.CurrentColor;
                }
            }

            return Color.white;
        }

        /// <summary>
        /// Выводит предупреждение, если спавнер не может создать клочки из-за незаполненных ссылок.
        /// </summary>
        private void LogMissingReferences()
        {
            if (_woolGrowth == null)
            {
                Debug.LogWarning(
                    $"{name}: SheepWoolGrowth is missing.",
                    this
                );
            }

            if (woolClumpPrefab == null)
            {
                Debug.LogWarning(
                    $"{name}: wool clump prefab is not assigned.",
                    this
                );
            }
        }
    }
}
