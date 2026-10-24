using UnityEngine;

namespace Game.Paint
{
    public readonly struct PaintHit
    {
        public readonly SheepPaintHitZone HitZone;
        public readonly SheepSide Side;
        public readonly Vector3 WorldPosition;
        public readonly Vector3 Normal;
        public readonly Vector2 UV;
        public readonly Vector3 SourcePosition;

        public PaintHit(
            SheepPaintHitZone hitZone,
            Vector3 worldPosition,
            Vector3 normal,
            Vector2 uv)
            : this(
                hitZone != null ? hitZone.Side : SheepSide.Left,
                worldPosition,
                normal,
                uv,
                Vector3.zero,
                hitZone)
        {
        }

        public PaintHit(
            SheepSide side,
            Vector3 worldPosition,
            Vector3 normal,
            Vector2 uv,
            Vector3 sourcePosition = default)
            : this(
                side,
                worldPosition,
                normal,
                uv,
                sourcePosition,
                null)
        {
        }

        private PaintHit(
            SheepSide side,
            Vector3 worldPosition,
            Vector3 normal,
            Vector2 uv,
            Vector3 sourcePosition,
            SheepPaintHitZone hitZone)
        {
            HitZone = hitZone;
            Side = side;
            WorldPosition = worldPosition;
            Normal = normal;
            UV = uv;
            SourcePosition = sourcePosition;
        }
    }
}
