using UnityEngine;

namespace Game.Paint
{
    public enum SheepSide
    {
        Left,
        Right,
        Back,
        Tail
    }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class SheepPaintHitZone : MonoBehaviour
    {
        [SerializeField] private SheepSide side;

        private BoxCollider _collider;

        public SheepSide Side => side;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();
        }

        public Vector2 GetPaintUV(Vector3 worldPosition)
        {
            Vector3 localPosition =
                transform.InverseTransformPoint(
                    worldPosition
                );

            Vector3 center =
                _collider.center;

            Vector3 size =
                _collider.size;

            float u;
            float v;

            switch (side)
            {
                case SheepSide.Left:
                case SheepSide.Right:

                    u =
                        Mathf.InverseLerp(
                            center.z - size.z * 0.5f,
                            center.z + size.z * 0.5f,
                            localPosition.z
                        );

                    v =
                        Mathf.InverseLerp(
                            center.y - size.y * 0.5f,
                            center.y + size.y * 0.5f,
                            localPosition.y
                        );

                    break;

                case SheepSide.Back:
                case SheepSide.Tail:

                    u =
                        Mathf.InverseLerp(
                            center.x - size.x * 0.5f,
                            center.x + size.x * 0.5f,
                            localPosition.x
                        );

                    v =
                        Mathf.InverseLerp(
                            center.y - size.y * 0.5f,
                            center.y + size.y * 0.5f, 
                            localPosition.y
                        );

                    break;

                default:
                    return Vector2.zero;
            }

            return new Vector2(
                Mathf.Clamp01(u),
                Mathf.Clamp01(v)
            );
        }
    }
}