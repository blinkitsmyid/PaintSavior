using UnityEngine;

namespace Game.Sheep.AI
{
    [DisallowMultipleComponent]
    public sealed class SheepNeedPoint : MonoBehaviour
    {
        [SerializeField] private SheepNeedType needType;
        [SerializeField] private Transform interactionPoint;

        public SheepNeedType NeedType => needType;

        public Vector3 Position =>
            interactionPoint != null
                ? interactionPoint.position
                : transform.position;
    }
}
