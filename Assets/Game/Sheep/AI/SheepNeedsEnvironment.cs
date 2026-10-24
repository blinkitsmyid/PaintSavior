using UnityEngine;

namespace Game.Sheep.AI
{
    [DisallowMultipleComponent]
    public sealed class SheepNeedsEnvironment : MonoBehaviour
    {
        [SerializeField] private SheepNeedPoint[] points;

        public bool HasAvailablePoint(SheepNeedType need)
        {
            return CountAvailablePoints(need) > 0;
        }

        public bool TryGetRandomPoint(
            SheepNeedType need,
            out SheepNeedPoint point)
        {
            point = null;

            int availableCount = CountAvailablePoints(need);

            if (availableCount == 0)
            {
                return false;
            }

            int selectedIndex = Random.Range(0, availableCount);

            for (int index = 0; index < points.Length; index++)
            {
                SheepNeedPoint candidate = points[index];

                if (!IsAvailableFor(candidate, need))
                {
                    continue;
                }

                if (selectedIndex-- == 0)
                {
                    point = candidate;
                    return true;
                }
            }

            return false;
        }

        private int CountAvailablePoints(SheepNeedType need)
        {
            if (points == null)
            {
                return 0;
            }

            int count = 0;

            for (int index = 0; index < points.Length; index++)
            {
                if (IsAvailableFor(points[index], need))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsAvailableFor(
            SheepNeedPoint point,
            SheepNeedType need)
        {
            return point != null && point.isActiveAndEnabled &&
                point.NeedType == need;
        }
    }
}
