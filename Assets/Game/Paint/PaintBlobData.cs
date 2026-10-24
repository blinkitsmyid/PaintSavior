using UnityEngine;

namespace Game.Paint
{
    public readonly struct PaintBlobData
    {
        public readonly int Sequence;
        public readonly SheepSide Side;
        public readonly Vector3 WorldPosition;
        public readonly Vector3 Normal;
        public readonly Vector2 UV;
        public readonly Color Color;
        public readonly float Size;
        public readonly float Rotation;

        /// <summary>
        /// Создаёт неизменяемую запись принятого пятна краски с его порядком нанесения и стороной овцы.
        /// </summary>
        public PaintBlobData(
            int sequence,
            SheepSide side,
            Vector3 worldPosition,
            Vector3 normal,
            Vector2 uv,
            Color color,
            float size,
            float rotation)
        {
            Sequence = sequence;
            Side = side;
            WorldPosition = worldPosition;
            Normal = normal;
            UV = uv;
            Color = color;
            Size = size;
            Rotation = rotation;
        }
    }
}
