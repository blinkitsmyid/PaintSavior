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

        [Header("Wool Clump Drops")]
        [SerializeField, Min(0)] private int shavedWoolClumpCount;
        [SerializeField, Min(0)] private int mediumWoolClumpCount = 2;
        [SerializeField, Min(0)] private int fullWoolClumpCount = 4;

        [Header("Debug")]
        [SerializeField] private bool logGrowthProgress = true;

        private CompletedNeeds _completedNeeds;

        public WoolGrowthStage CurrentStage { get; private set; }

        public bool IsFullyGrown => CurrentStage == WoolGrowthStage.Full;

        public event Action<SheepNeedType> NeedCompleted;
        public event Action<WoolGrowthStage, WoolGrowthStage> StageChanged;

        /// <summary>
        /// Возвращает настроенное количество клочков для указанной стадии шерсти.
        /// </summary>
        public int GetWoolClumpCount(WoolGrowthStage stage)
        {
            return stage switch
            {
                WoolGrowthStage.Shaved => Mathf.Max(
                    0,
                    shavedWoolClumpCount
                ),
                WoolGrowthStage.Medium => Mathf.Max(
                    0,
                    mediumWoolClumpCount
                ),
                WoolGrowthStage.Full => Mathf.Max(
                    0,
                    fullWoolClumpCount
                ),
                _ => 0
            };
        }

        /// <summary>
        /// Устанавливает начальную стадию шерсти и активирует соответствующую визуальную модель.
        /// </summary>
        private void Awake()
        {
            CurrentStage = initialStage;
            ApplyVisual();
        }

        /// <summary>
        /// Проверяет, была ли указанная потребность выполнена в текущем цикле роста.
        /// </summary>
        public bool IsNeedCompleted(SheepNeedType need)
        {
            return (_completedNeeds & ToFlag(need)) != 0;
        }

        /// <summary>
        /// Проверяет, может ли потребность быть засчитана в текущем цикле роста.
        /// </summary>
        public bool CanCompleteNeed(SheepNeedType need)
        {
            return !IsNeedCompleted(need);
        }

        /// <summary>
        /// Засчитывает потребность и при завершении полного цикла переводит шерсть на следующую стадию.
        /// </summary>
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

        /// <summary>
        /// Возвращает шерсть к бритой стадии и сбрасывает прогресс текущего цикла.
        /// </summary>
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

        /// <summary>
        /// Включает только ту визуальную модель, которая соответствует текущей стадии шерсти.
        /// </summary>
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

        /// <summary>
        /// Преобразует тип потребности в соответствующий бит внутреннего флага прогресса.
        /// </summary>
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

        /// <summary>
        /// Подсчитывает количество завершённых потребностей текущего цикла роста.
        /// </summary>
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

        /// <summary>
        /// Безопасно меняет активность визуального объекта, если ссылка на него задана.
        /// </summary>
        private static void SetVisualActive(GameObject visual, bool isActive)
        {
            if (visual != null)
            {
                visual.SetActive(isActive);
            }
        }
    }
}
