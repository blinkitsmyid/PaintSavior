using UnityEngine;

using Game.Animals.Core;

namespace Game.Sheep.AI
{
    /// <summary>
    /// Переводит состояния и реакции овцы в универсальные команды визуального представления животного.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Paint Savior/Sheep/Sheep Animation Bridge")]
    public sealed class SheepAnimationBridge : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Компонент логики овцы, который сообщает о состояниях и реакциях.")]
        [SerializeField] private SheepBrain sheepBrain;

        [Tooltip("Универсальный компонент, передающий логические команды Animator.")]
        [SerializeField] private AnimalAnimationPresenter animationPresenter;

        [Header("Movement Animation")]
        [Tooltip("Нормализованная скорость для состояния Wandering и движения к потребности.")]
        [SerializeField, Range(0f, 1f)] private float walkAnimationSpeed = 0.5f;

        [Tooltip("Нормализованная скорость для испуганного бегства.")]
        [SerializeField, Range(0f, 1f)] private float runAnimationSpeed = 1f;

        /// <summary>
        /// Находит компоненты овцы, если они не назначены вручную в Inspector.
        /// </summary>
        private void Awake()
        {
            if (sheepBrain == null)
            {
                sheepBrain =
                    GetComponent<SheepBrain>();
            }

            if (animationPresenter == null)
            {
                animationPresenter =
                    GetComponent<AnimalAnimationPresenter>();
            }
        }

        /// <summary>
        /// Подписывается на смену состояния, дружелюбия и успешное поглаживание овцы.
        /// </summary>
        private void OnEnable()
        {
            if (sheepBrain == null)
            {
                return;
            }

            sheepBrain.StateChanged +=
                OnStateChanged;
            sheepBrain.FriendlyChanged +=
                OnFriendlyChanged;
            sheepBrain.PetPerformed +=
                OnPetPerformed;
        }

        /// <summary>
        /// Синхронизирует визуальное состояние после запуска всех компонентов овцы.
        /// </summary>
        private void Start()
        {
            RefreshFromBrain();
        }

        /// <summary>
        /// Отписывается от событий овцы при отключении компонента.
        /// </summary>
        private void OnDisable()
        {
            if (sheepBrain == null)
            {
                return;
            }

            sheepBrain.StateChanged -=
                OnStateChanged;
            sheepBrain.FriendlyChanged -=
                OnFriendlyChanged;
            sheepBrain.PetPerformed -=
                OnPetPerformed;
        }

        /// <summary>
        /// Переводит новое состояние FSM в действие и скорость визуального представления.
        /// </summary>
        private void OnStateChanged(
            SheepStateId previousState,
            SheepStateId nextState)
        {
            ApplyState(nextState);
            ApplyMood();
        }

        /// <summary>
        /// Обновляет настроение представления после изменения дружелюбия овцы.
        /// </summary>
        private void OnFriendlyChanged(
            bool isFriendly)
        {
            ApplyMood();
        }

        /// <summary>
        /// Запускает короткую визуальную реакцию на успешное поглаживание.
        /// </summary>
        private void OnPetPerformed()
        {
            if (animationPresenter == null)
            {
                return;
            }

            animationPresenter.SetMood(
                AnimalAnimationMood.Friendly
            );
            animationPresenter.PlayPetReaction();
        }

        /// <summary>
        /// Синхронизирует действие и настроение с текущими данными SheepBrain.
        /// </summary>
        private void RefreshFromBrain()
        {
            if (sheepBrain == null)
            {
                return;
            }

            ApplyState(
                sheepBrain.CurrentStateId
            );
            ApplyMood();
        }

        /// <summary>
        /// Применяет к presenter визуальное действие и нормализованную скорость для состояния овцы.
        /// </summary>
        private void ApplyState(
            SheepStateId state)
        {
            if (animationPresenter == null)
            {
                return;
            }

            AnimalAnimationAction action =
                AnimalAnimationAction.None;
            float speed = 0f;

            switch (state)
            {
                case SheepStateId.Wandering:
                case SheepStateId.SeekingWater:
                    speed = walkAnimationSpeed;
                    break;
                case SheepStateId.Eating:
                    action = AnimalAnimationAction.Eat;
                    break;
                case SheepStateId.Drinking:
                    action = AnimalAnimationAction.Drink;
                    break;
                case SheepStateId.Resting:
                    action = AnimalAnimationAction.Rest;
                    break;
                case SheepStateId.ScaredRun:
                    speed = runAnimationSpeed;
                    break;
                case SheepStateId.Washing:
                    action = AnimalAnimationAction.Wash;
                    break;
                case SheepStateId.Shearing:
                    action = AnimalAnimationAction.Shear;
                    break;
            }

            animationPresenter.SetMoveSpeed(speed);
            animationPresenter.SetAction(action);
        }

        /// <summary>
        /// Применяет настроение овцы с приоритетом испуга над дружелюбием.
        /// </summary>
        private void ApplyMood()
        {
            if (animationPresenter == null || sheepBrain == null)
            {
                return;
            }

            if (sheepBrain.IsScared)
            {
                animationPresenter.SetMood(
                    AnimalAnimationMood.Scared
                );
                return;
            }

            animationPresenter.SetMood(
                sheepBrain.IsFriendly
                    ? AnimalAnimationMood.Friendly
                    : AnimalAnimationMood.Calm
            );
        }
    }
}
