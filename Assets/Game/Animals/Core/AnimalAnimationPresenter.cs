using UnityEngine;

namespace Game.Animals.Core
{
    /// <summary>
    /// Описывает неподвижное действие, которое может отображать животное.
    /// </summary>
    public enum AnimalAnimationAction
    {
        None = 0,
        Eat = 1,
        Drink = 2,
        Rest = 3,
        Wash = 4,
        Shear = 5
    }

    /// <summary>
    /// Описывает настроение, которое используется визуальным представлением животного.
    /// </summary>
    public enum AnimalAnimationMood
    {
        Calm = 0,
        Friendly = 1,
        Scared = 2
    }

    /// <summary>
    /// Хранит логическое состояние анимации животного и передаёт его Animator при наличии контроллера.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Paint Savior/Animals/Animal Animation Presenter")]
    public sealed class AnimalAnimationPresenter : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Animator модели животного. Если поле пустое, компонент ищет Animator среди дочерних объектов.")]
        [SerializeField] private Animator animator;

        [Header("Animator Parameters")]
        [Tooltip("Имя float-параметра скорости движения.")]
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";

        [Tooltip("Имя int-параметра текущего действия.")]
        [SerializeField] private string actionParameter = "Action";

        [Tooltip("Имя int-параметра текущего настроения.")]
        [SerializeField] private string moodParameter = "Mood";

        [Tooltip("Имя trigger-параметра реакции на поглаживание.")]
        [SerializeField] private string petTriggerParameter = "Pet";

        private int _moveSpeedParameterId;
        private int _actionParameterId;
        private int _moodParameterId;
        private int _petTriggerParameterId;

        private bool _hasMoveSpeedParameter;
        private bool _hasActionParameter;
        private bool _hasMoodParameter;
        private bool _hasPetTriggerParameter;

        /// <summary>
        /// Возвращает Animator, который используется представлением животного.
        /// </summary>
        public Animator Animator => animator;

        /// <summary>
        /// Показывает, найден ли Animator с назначенным контроллером.
        /// </summary>
        public bool HasAnimator =>
            animator != null &&
            animator.runtimeAnimatorController != null;

        /// <summary>
        /// Возвращает последнее установленное значение нормализованной скорости движения.
        /// </summary>
        public float CurrentMoveSpeed { get; private set; }

        /// <summary>
        /// Возвращает последнее установленное действие животного.
        /// </summary>
        public AnimalAnimationAction CurrentAction { get; private set; }

        /// <summary>
        /// Возвращает последнее установленное настроение животного.
        /// </summary>
        public AnimalAnimationMood CurrentMood { get; private set; }

        /// <summary>
        /// Находит Animator модели и подготавливает идентификаторы его параметров.
        /// </summary>
        private void Awake()
        {
            ResolveAnimator();
            RefreshParameterCache();
            ResetLogicalState();
        }

        /// <summary>
        /// Обновляет ссылки и проверяет параметры Animator после изменения настроек в Inspector.
        /// </summary>
        private void OnValidate()
        {
            moveSpeedParameter =
                moveSpeedParameter?.Trim() ?? string.Empty;
            actionParameter =
                actionParameter?.Trim() ?? string.Empty;
            moodParameter =
                moodParameter?.Trim() ?? string.Empty;
            petTriggerParameter =
                petTriggerParameter?.Trim() ?? string.Empty;

            ResolveAnimator();
            RefreshParameterCache();
        }

        /// <summary>
        /// Устанавливает нормализованную скорость движения и передаёт её Animator.
        /// </summary>
        public void SetMoveSpeed(
            float normalizedSpeed)
        {
            CurrentMoveSpeed =
                Mathf.Clamp01(normalizedSpeed);

            if (!HasAnimator || !_hasMoveSpeedParameter)
            {
                return;
            }

            animator.SetFloat(
                _moveSpeedParameterId,
                CurrentMoveSpeed
            );
        }

        /// <summary>
        /// Устанавливает текущее действие и передаёт его Animator как значение int-параметра.
        /// </summary>
        public void SetAction(
            AnimalAnimationAction action)
        {
            CurrentAction = action;

            if (!HasAnimator || !_hasActionParameter)
            {
                return;
            }

            animator.SetInteger(
                _actionParameterId,
                (int)CurrentAction
            );
        }

        /// <summary>
        /// Устанавливает текущее настроение и передаёт его Animator как значение int-параметра.
        /// </summary>
        public void SetMood(
            AnimalAnimationMood mood)
        {
            CurrentMood = mood;

            if (!HasAnimator || !_hasMoodParameter)
            {
                return;
            }

            animator.SetInteger(
                _moodParameterId,
                (int)CurrentMood
            );
        }

        /// <summary>
        /// Запускает краткую реакцию на поглаживание, если соответствующий trigger есть в Animator.
        /// </summary>
        public void PlayPetReaction()
        {
            if (!HasAnimator || !_hasPetTriggerParameter)
            {
                return;
            }

            animator.SetTrigger(
                _petTriggerParameterId
            );
        }

        /// <summary>
        /// Возвращает логическое представление животного в нейтральное состояние.
        /// </summary>
        public void ResetLogicalState()
        {
            CurrentMoveSpeed = 0f;
            CurrentAction = AnimalAnimationAction.None;
            CurrentMood = AnimalAnimationMood.Calm;

            if (!HasAnimator)
            {
                return;
            }

            if (_hasMoveSpeedParameter)
            {
                animator.SetFloat(
                    _moveSpeedParameterId,
                    CurrentMoveSpeed
                );
            }

            if (_hasActionParameter)
            {
                animator.SetInteger(
                    _actionParameterId,
                    (int)CurrentAction
                );
            }

            if (_hasMoodParameter)
            {
                animator.SetInteger(
                    _moodParameterId,
                    (int)CurrentMood
                );
            }
        }

        /// <summary>
        /// Находит Animator на текущем объекте или среди его дочерних объектов.
        /// </summary>
        private void ResolveAnimator()
        {
            if (animator != null)
            {
                return;
            }

            animator =
                GetComponentInChildren<Animator>(true);
        }

        /// <summary>
        /// Кэширует идентификаторы и наличие параметров Animator, чтобы не искать их при каждом обновлении состояния.
        /// </summary>
        private void RefreshParameterCache()
        {
            _moveSpeedParameterId =
                Animator.StringToHash(moveSpeedParameter);
            _actionParameterId =
                Animator.StringToHash(actionParameter);
            _moodParameterId =
                Animator.StringToHash(moodParameter);
            _petTriggerParameterId =
                Animator.StringToHash(petTriggerParameter);

            _hasMoveSpeedParameter =
                HasParameter(
                    moveSpeedParameter,
                    AnimatorControllerParameterType.Float
                );
            _hasActionParameter =
                HasParameter(
                    actionParameter,
                    AnimatorControllerParameterType.Int
                );
            _hasMoodParameter =
                HasParameter(
                    moodParameter,
                    AnimatorControllerParameterType.Int
                );
            _hasPetTriggerParameter =
                HasParameter(
                    petTriggerParameter,
                    AnimatorControllerParameterType.Trigger
                );
        }

        /// <summary>
        /// Проверяет наличие параметра нужного типа в назначенном Animator Controller.
        /// </summary>
        private bool HasParameter(
            string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            if (!HasAnimator || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            AnimatorControllerParameter[] parameters =
                animator.parameters;

            for (int index = 0; index < parameters.Length; index++)
            {
                AnimatorControllerParameter parameter =
                    parameters[index];

                if (parameter.name == parameterName &&
                    parameter.type == parameterType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
