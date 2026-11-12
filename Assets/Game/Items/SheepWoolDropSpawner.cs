using UnityEngine;

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
        [SerializeField, Min(0f)] private float verticalSpawnOffset = 0.15f;
        [SerializeField, Range(0f, 1f)] private float randomRotationAmount = 1f;

        [Header("Debug")]
        [SerializeField] private bool logSpawn;

        private SheepWoolGrowth _woolGrowth;
        private SheepPaintController _paintController;

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

            Vector3 spawnPosition =
                dropOrigin.position +
                dropOrigin.right * randomOffset.x +
                dropOrigin.forward * randomOffset.y +
                dropOrigin.up * verticalSpawnOffset;

            Quaternion spawnRotation =
                Quaternion.Slerp(
                    dropOrigin.rotation,
                    Random.rotation,
                    randomRotationAmount
                );

            WoolClumpPickup clump =
                Instantiate(
                    woolClumpPrefab,
                    spawnPosition,
                    spawnRotation
                );

            clump.Initialize(
                stage,
                color
            );
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
