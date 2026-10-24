using System.Collections.Generic;
using UnityEngine;

namespace Game.Paint
{
    public sealed class SidePaintState
    {
        private readonly List<PaintBlobData> _blobs;

        public IReadOnlyList<PaintBlobData> Blobs => _blobs;

        public Color CurrentColor { get; private set; }

        public bool IsPainted { get; private set; }

        /// <summary>
        /// Создаёт состояние пятен для одной игровой стороны овцы с заданной начальной ёмкостью списка.
        /// </summary>
        public SidePaintState(int maxBlobCount)
        {
            _blobs = new List<PaintBlobData>(Mathf.Max(0, maxBlobCount));
        }

        /// <summary>
        /// Добавляет пятно и возвращает самое старое удалённое пятно, если был достигнут лимит стороны.
        /// </summary>
        public bool TryAddBlob(
            PaintBlobData blob,
            int maxBlobCount,
            out PaintBlobData removedBlob)
        {
            removedBlob = default;

            if (maxBlobCount <= 0)
            {
                return false;
            }

            if (_blobs.Count >= maxBlobCount)
            {
                removedBlob = _blobs[0];
                _blobs.RemoveAt(0);
            }

            _blobs.Add(blob);

            CurrentColor = blob.Color;
            IsPainted = true;

            return true;
        }

        /// <summary>
        /// Удаляет пятно по его порядковому номеру и восстанавливает цвет последнего оставшегося пятна.
        /// </summary>
        public bool RemoveBlob(
            int sequence,
            out PaintBlobData removedBlob)
        {
            for (int i = 0; i < _blobs.Count; i++)
            {
                if (_blobs[i].Sequence != sequence)
                {
                    continue;
                }

                removedBlob = _blobs[i];
                _blobs.RemoveAt(i);
                RecalculateState();
                return true;
            }

            removedBlob = default;
            return false;
        }

        /// <summary>
        /// Пересчитывает признак окрашенности и цвет самого нового оставшегося пятна.
        /// </summary>
        private void RecalculateState()
        {
            IsPainted = _blobs.Count > 0;
            CurrentColor = IsPainted
                ? _blobs[_blobs.Count - 1].Color
                : Color.clear;
        }
    }
}
