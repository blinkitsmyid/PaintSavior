using System;

using UnityEngine;

namespace Game.Sheep.AI
{
    [DisallowMultipleComponent]
    public sealed class SheepWoolGrowth : MonoBehaviour
    {
        [Flags]
        private enum CompletedNeeds
        {
            None = 0,
            Eating = 1 << 0,
            Drinking = 1 << 1,
            Resting = 1 << 2,
            All = Eating | Drinking | Resting
        }

        [Header("Initial State")]
        [SerializeField] private WoolGrowthStage initialStage =
            WoolGrowthStage.Shaved;

        [Header("Wool Visuals")]
        [SerializeField] private GameObject shavedWoolVisual;
        [SerializeField] private GameObject mediumWoolVisual;
        [SerializeField] private GameObject fullWoolVisual;

        [Header("Debug")]
        [SerializeField] private bool logGrowthProgress = true;

        private CompletedNeeds _completedNeeds;

        public WoolGrowthStage CurrentStage { get; private set; }

        public bool IsFullyGrown => CurrentStage == WoolGrowthStage.Full;

        public event Action<SheepNeedType> NeedCompleted;
        public event Action<WoolGrowthStage, WoolGrowthStage> StageChanged;

        private void Awake()
        {
            CurrentStage = initialStage;
            ApplyVisual();
        }

        public bool IsNeedCompleted(SheepNeedType need)
        {
            return (_completedNeeds & ToFlag(need)) != 0;
        }

        public bool CanCompleteNeed(SheepNeedType need)
        {
            return !IsNeedCompleted(need);
        }

        public bool TryCompleteNeed(SheepNeedType need)
        {
            if (!CanCompleteNeed(need))
            {
                return false;
            }

            _completedNeeds |= ToFlag(need);

            if (logGrowthProgress)
            {
                Debug.Log(
                    $"{name}: completed {need} " +
                    $"({GetCompletedNeedCount()}/3).",
                    this
                );
            }

            NeedCompleted?.Invoke(need);

            if (_completedNeeds != CompletedNeeds.All)
            {
                return true;
            }

            if (!IsFullyGrown)
            {
                WoolGrowthStage previousStage = CurrentStage;
                CurrentStage++;
                _completedNeeds = CompletedNeeds.None;

                ApplyVisual();

                if (logGrowthProgress)
                {
                    Debug.Log(
                        $"{name}: wool stage changed " +
                        $"{previousStage} -> {CurrentStage}.",
                        this
                    );
                }

                StageChanged?.Invoke(previousStage, CurrentStage);
            }
            else
            {
                // Full blocks further growth, not normal animal needs.
                // Start another need cycle without changing the stage.
                _completedNeeds = CompletedNeeds.None;
            }

            return true;
        }

        public void ResetToShaved()
        {
            WoolGrowthStage previousStage = CurrentStage;

            CurrentStage = WoolGrowthStage.Shaved;
            _completedNeeds = CompletedNeeds.None;
            ApplyVisual();

            if (previousStage != CurrentStage)
            {
                StageChanged?.Invoke(previousStage, CurrentStage);
            }
        }

        private void ApplyVisual()
        {
            SetVisualActive(
                shavedWoolVisual,
                CurrentStage == WoolGrowthStage.Shaved
            );
            SetVisualActive(
                mediumWoolVisual,
                CurrentStage == WoolGrowthStage.Medium
            );
            SetVisualActive(
                fullWoolVisual,
                CurrentStage == WoolGrowthStage.Full
            );
        }

        private static CompletedNeeds ToFlag(SheepNeedType need)
        {
            return need switch
            {
                SheepNeedType.Eating => CompletedNeeds.Eating,
                SheepNeedType.Drinking => CompletedNeeds.Drinking,
                SheepNeedType.Resting => CompletedNeeds.Resting,
                _ => throw new ArgumentOutOfRangeException(nameof(need), need, null)
            };
        }

        private int GetCompletedNeedCount()
        {
            int count = 0;

            if ((_completedNeeds & CompletedNeeds.Eating) != 0)
            {
                count++;
            }

            if ((_completedNeeds & CompletedNeeds.Drinking) != 0)
            {
                count++;
            }

            if ((_completedNeeds & CompletedNeeds.Resting) != 0)
            {
                count++;
            }

            return count;
        }

        private static void SetVisualActive(GameObject visual, bool isActive)
        {
            if (visual != null)
            {
                visual.SetActive(isActive);
            }
        }
    }
}
