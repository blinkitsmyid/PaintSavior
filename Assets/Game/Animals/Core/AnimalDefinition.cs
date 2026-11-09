using UnityEngine;

namespace Game.Animals.Core
{
    /// <summary>
    /// Хранит базовые настраиваемые параметры конкретного вида животного.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AnimalDefinition",
        menuName = "Game/Animals/Animal Definition"
    )]
    public sealed class AnimalDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Animal";

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float walkSpeed = 1.2f;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.2f;

        [Header("Idle and Wandering")]
        [SerializeField, Min(0f)] private float minimumIdleDuration = 1.5f;
        [SerializeField, Min(0f)] private float maximumIdleDuration = 4f;
        [SerializeField, Min(0f)] private float minimumWanderDuration = 2f;
        [SerializeField, Min(0f)] private float maximumWanderDuration = 5f;
        [SerializeField, Min(0f)] private float minimumWanderDistance = 1f;
        [SerializeField, Min(1)] private int maximumWanderPointAttempts = 8;

        [Header("Needs")]
        [SerializeField, Range(0f, 1f)] private float needChoiceChance = 0.65f;
        [SerializeField, Min(0f)] private float eatingDuration = 3f;
        [SerializeField, Min(0f)] private float drinkingDuration = 3f;
        [SerializeField, Min(0f)] private float restingDuration = 4f;

        [Header("Player Reaction")]
        [SerializeField, Min(0.1f)] private float friendlyDistance = 3f;
        [SerializeField, Min(0.1f)] private float petDistance = 2.5f;
        [SerializeField, Min(0.1f)] private float scaredDuration = 5f;
        [SerializeField, Min(0.1f)] private float minimumFleeDistance = 3f;

        [Header("Contamination")]
        [SerializeField, Min(1)] private int hitsBeforeWash = 3;
        [SerializeField, Min(0f)] private float washBlobDuration = 1f;

        [Header("Shearing")]
        [SerializeField, Min(1)] private int woolClumpCount = 4;
        [SerializeField, Min(0f)] private float shearingDuration = 2f;

        [Header("Behavior Priorities")]
        [SerializeField, Min(0)] private int shearPriority = 100;
        [SerializeField, Min(0)] private int washPriority = 90;
        [SerializeField, Min(0)] private int scaredPriority = 80;
        [SerializeField, Min(0)] private int interactionPriority = 70;
        [SerializeField, Min(0)] private int needPriority = 40;
        [SerializeField, Min(0)] private int wanderingPriority = 20;
        [SerializeField, Min(0)] private int idlePriority = 10;

        /// <summary>
        /// Возвращает отображаемое имя вида животного.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Возвращает скорость ходьбы из конфигурации животного.
        /// </summary>
        public float WalkSpeed => walkSpeed;

        /// <summary>
        /// Возвращает дистанцию остановки перед целью.
        /// </summary>
        public float ArrivalDistance => arrivalDistance;

        /// <summary>
        /// Возвращает минимальную длительность состояния Idle.
        /// </summary>
        public float MinimumIdleDuration => minimumIdleDuration;

        /// <summary>
        /// Возвращает максимальную длительность состояния Idle.
        /// </summary>
        public float MaximumIdleDuration => maximumIdleDuration;

        /// <summary>
        /// Возвращает минимальную длительность случайного блуждания.
        /// </summary>
        public float MinimumWanderDuration => minimumWanderDuration;

        /// <summary>
        /// Возвращает максимальную длительность случайного блуждания.
        /// </summary>
        public float MaximumWanderDuration => maximumWanderDuration;

        /// <summary>
        /// Возвращает минимальную дистанцию от текущей позиции до новой точки блуждания.
        /// </summary>
        public float MinimumWanderDistance => minimumWanderDistance;

        /// <summary>
        /// Возвращает количество попыток поиска точки блуждания.
        /// </summary>
        public int MaximumWanderPointAttempts => maximumWanderPointAttempts;

        /// <summary>
        /// Возвращает вероятность выбора потребности после состояния Idle.
        /// </summary>
        public float NeedChoiceChance => needChoiceChance;

        /// <summary>
        /// Возвращает длительность состояния Eating.
        /// </summary>
        public float EatingDuration => eatingDuration;

        /// <summary>
        /// Возвращает длительность состояния Drinking.
        /// </summary>
        public float DrinkingDuration => drinkingDuration;

        /// <summary>
        /// Возвращает длительность состояния Resting.
        /// </summary>
        public float RestingDuration => restingDuration;

        /// <summary>
        /// Возвращает дистанцию, на которой животное реагирует дружелюбно.
        /// </summary>
        public float FriendlyDistance => friendlyDistance;

        /// <summary>
        /// Возвращает максимальную дистанцию для поглаживания.
        /// </summary>
        public float PetDistance => petDistance;

        /// <summary>
        /// Возвращает длительность испуганного состояния.
        /// </summary>
        public float ScaredDuration => scaredDuration;

        /// <summary>
        /// Возвращает минимальное расстояние, на которое животное убегает от угрозы.
        /// </summary>
        public float MinimumFleeDistance => minimumFleeDistance;

        /// <summary>
        /// Возвращает количество попаданий до появления намерения мыться.
        /// </summary>
        public int HitsBeforeWash => hitsBeforeWash;

        /// <summary>
        /// Возвращает время удаления одного пятна во время мытья.
        /// </summary>
        public float WashBlobDuration => washBlobDuration;

        /// <summary>
        /// Возвращает количество клочков шерсти, создаваемых при стрижке.
        /// </summary>
        public int WoolClumpCount => Mathf.Max(1, woolClumpCount);

        /// <summary>
        /// Возвращает длительность процесса стрижки.
        /// </summary>
        public float ShearingDuration => Mathf.Max(0f, shearingDuration);

        /// <summary>
        /// Возвращает приоритет стрижки животного.
        /// </summary>
        public int ShearPriority => shearPriority;

        /// <summary>
        /// Возвращает приоритет движения к воде и мытья животного.
        /// </summary>
        public int WashPriority => washPriority;

        /// <summary>
        /// Возвращает приоритет испуганного поведения и бегства.
        /// </summary>
        public int ScaredPriority => scaredPriority;

        /// <summary>
        /// Возвращает приоритет непосредственного взаимодействия с животным.
        /// </summary>
        public int InteractionPriority => interactionPriority;

        /// <summary>
        /// Возвращает приоритет выполнения базовой потребности животного.
        /// </summary>
        public int NeedPriority => needPriority;

        /// <summary>
        /// Возвращает приоритет случайного блуждания животного.
        /// </summary>
        public int WanderingPriority => wanderingPriority;

        /// <summary>
        /// Возвращает приоритет спокойного ожидания животного.
        /// </summary>
        public int IdlePriority => idlePriority;
    }
}
