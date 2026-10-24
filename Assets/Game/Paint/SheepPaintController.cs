using System;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Paint
{
    public sealed class SheepPaintController : MonoBehaviour
    {
        [Header("Paint")]
        [SerializeField] private int maxBlobsPerSide = 8;
        [SerializeField] private float minBlobSize = 0.15f;
        [SerializeField] private float maxBlobSize = 0.3f;

        private readonly Dictionary<SheepSide, SidePaintState> _sides = new();
        private readonly List<PaintBlobData> _blobsInOrder = new();

        private int _nextBlobSequence = 1;

        public event Action<PaintHit, PaintBlobData> BlobCreated;
        public event Action<PaintBlobData> BlobRemoved;
        public event Action<PaintHit, Color> PaintHitReceived;
        public event Action<SheepSide, Color> SidePainted;
        public event Action AllSidesPainted;

        public IReadOnlyList<PaintBlobData> BlobsInOrder => _blobsInOrder;

        /// <summary>
        /// Инициализирует состояния сторон и подготавливает коллайдер поверхности для попаданий по UV меша.
        /// </summary>
        private void Awake()
        {
            InitializeSides();
            EnsureSurfaceCollider();
        }

        /// <summary>
        /// Находит меш поверхности и назначает его неконвексному MeshCollider для получения textureCoord.
        /// </summary>
        private void EnsureSurfaceCollider()
        {
            MeshFilter meshFilter =
                GetComponentInChildren<MeshFilter>();

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning(
                    $"{name}: paint surface MeshFilter or mesh is missing."
                );

                return;
            }

            MeshCollider meshCollider =
                meshFilter.GetComponent<MeshCollider>();

            if (meshCollider == null)
            {
                meshCollider =
                    meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.convex = false;
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = meshFilter.sharedMesh;

            BoxCollider oldPrototypeCollider =
                GetComponent<BoxCollider>();

            if (oldPrototypeCollider != null)
            {
                oldPrototypeCollider.enabled = false;
            }
        }

        /// <summary>
        /// Создаёт независимое состояние пятен для каждой стороны овцы.
        /// </summary>
        private void InitializeSides()
        {
            foreach (SheepSide side in Enum.GetValues(typeof(SheepSide)))
            {
                _sides.Add(
                    side,
                    new SidePaintState(maxBlobsPerSide)
                );
            }
        }

        /// <summary>
        /// Принимает попадание, создаёт пятно с новым глобальным порядком и уведомляет подписчиков.
        /// </summary>
        public void ApplyPaint(
            PaintHit hit,
            Color color)
        {
            PaintHitReceived?.Invoke(hit, color);

            SheepSide side = hit.Side;

            float size = UnityEngine.Random.Range(
                minBlobSize,
                maxBlobSize
            );

            float rotation = UnityEngine.Random.Range(
                0f,
                360f
            );

            int sequence = _nextBlobSequence++;

            PaintBlobData blob = new PaintBlobData(
                sequence,
                side,
                hit.WorldPosition,
                hit.Normal,
                hit.UV,
                color,
                size,
                rotation
            );

            SidePaintState state = _sides[side];

            bool wasPainted = state.IsPainted;

            bool added = state.TryAddBlob(
                blob,
                maxBlobsPerSide,
                out PaintBlobData removedFromSide
            );

            if (!added)
            {
                return;
            }

            if (removedFromSide.Sequence > 0)
            {
                RemoveFromGlobalOrder(removedFromSide.Sequence);
            }

            _blobsInOrder.Add(blob);

            Debug.Log(
                $"Painted {side} | " +
                $"Side Blobs: {state.Blobs.Count} | " +
                $"Global Blobs: {_blobsInOrder.Count} | " +
                $"Sequence: {blob.Sequence} | " +
                $"Current Color: {state.CurrentColor}"
            );

            BlobCreated?.Invoke(
                hit,
                blob
            );

            if (!wasPainted)
            {
                SidePainted?.Invoke(
                    side,
                    color
                );
            }

            if (AreAllSidesPainted())
            {
                AllSidesPainted?.Invoke();
            }
        }

        /// <summary>
        /// Возвращает самое старое ещё существующее пятно для последовательного процесса мытья.
        /// </summary>
        public bool TryGetOldestBlob(
            out PaintBlobData blob)
        {
            if (_blobsInOrder.Count == 0)
            {
                blob = default;
                return false;
            }

            blob = _blobsInOrder[0];
            return true;
        }

        /// <summary>
        /// Удаляет пятно из глобального порядка и состояния соответствующей стороны.
        /// </summary>
        public bool RemoveBlob(
            int sequence)
        {
            for (int i = 0; i < _blobsInOrder.Count; i++)
            {
                PaintBlobData blob = _blobsInOrder[i];

                if (blob.Sequence != sequence)
                {
                    continue;
                }

                _blobsInOrder.RemoveAt(i);

                if (_sides.TryGetValue(blob.Side, out SidePaintState state))
                {
                    state.RemoveBlob(sequence, out _);
                }

                BlobRemoved?.Invoke(blob);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Удаляет пятно только из глобального списка после автоматического вытеснения лимитом стороны.
        /// </summary>
        private bool RemoveFromGlobalOrder(
            int sequence)
        {
            for (int i = 0; i < _blobsInOrder.Count; i++)
            {
                if (_blobsInOrder[i].Sequence != sequence)
                {
                    continue;
                }

                PaintBlobData removedBlob = _blobsInOrder[i];
                _blobsInOrder.RemoveAt(i);
                BlobRemoved?.Invoke(removedBlob);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Возвращает состояние пятен указанной стороны овцы.
        /// </summary>
        public SidePaintState GetSideState(
            SheepSide side)
        {
            return _sides[side];
        }

        /// <summary>
        /// Проверяет, есть ли хотя бы одно пятно на указанной стороне.
        /// </summary>
        public bool IsSidePainted(
            SheepSide side)
        {
            return _sides[side].IsPainted;
        }

        /// <summary>
        /// Проверяет, окрашены ли все четыре игровые стороны овцы.
        /// </summary>
        public bool AreAllSidesPainted()
        {
            foreach (SidePaintState state in _sides.Values)
            {
                if (!state.IsPainted)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
