using UnityEngine;

namespace Game.Paint
{
    /// <summary>
    /// Содержит общие данные столкновения снаряда с поверхностью, принимающей краску.
    /// </summary>
    public readonly struct PaintImpactData
    {
        public readonly Vector3 WorldPosition;
        public readonly Vector3 Normal;
        public readonly Vector2 UV;
        public readonly Color Color;
        public readonly Vector3 SourcePosition;
        public readonly Collider Collider;

        /// <summary>
        /// Создаёт неизменяемое описание попадания для любого получателя краски.
        /// </summary>
        public PaintImpactData(
            Vector3 worldPosition,
            Vector3 normal,
            Vector2 uv,
            Color color,
            Vector3 sourcePosition,
            Collider collider)
        {
            WorldPosition = worldPosition;
            Normal = normal;
            UV = uv;
            Color = color;
            SourcePosition = sourcePosition;
            Collider = collider;
        }
    }
}
