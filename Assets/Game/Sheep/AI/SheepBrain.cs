using System;

using UnityEngine;

using Game.Animals.Core;
using Game.Paint;

namespace Game.Sheep.AI
{
    [DisallowMultipleComponent]
    public sealed class SheepBrain : MonoBehaviour, IAnimalInteractable
    {
        [Header("References")]
        [SerializeField] private AnimalDefinition definition;
        [SerializeField] private AnimalLocomotion locomotion;
        [SerializeField] private AnimalPerception perception;
        [SerializeField] private SheepWanderArea wanderArea;
        [SerializeField] private SheepNeedsEnvironment needsEnvironment;
        [SerializeField] private SheepWaterEnvironment waterEnvironment;
        [SerializeField] private SheepWoolGrowth woolGrowth;
        [SerializeField] private SheepPaintController paintController;
        [SerializeField] private Transform playerTarget;

        [Header("Idle")]
        [SerializeField, Min(0f)] private float minimumIdleDuration = 1.5f;
        [SerializeField, Min(0f)] private float maximumIdleDuration = 4f;

        [Header("Wandering")]
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
        [SerializeField, Min(0f)] private float shearingDuration = 2f;

        [Header("Debug")]
        [SerializeField] private bool logStateTransitions = true;
        [SerializeField] private bool logPlayerReactions = true;

        [Header("Testing")]
        [SerializeField] private bool allowShearingAfterAnyPaintHitForTesting;

        private SheepStateMachine _stateMachine;
        private SheepIdleState _idleState;
        private SheepWanderingState _wanderingState;
        private SheepNeedState _eatingState;
        private SheepNeedState _drinkingState;
        private SheepNeedState _restingState;
        private SheepScaredState _scaredState;
        private SheepSeekingWaterState _seekingWaterState;
        private SheepWashingState _washingState;
        private SheepShearingState _shearingState;
        private SheepScaredBehaviorSource _scaredBehaviorSource;
        private SheepSeekingWaterBehaviorSource _seekingWaterBehaviorSource;
        private SheepWanderingBehaviorSource _wanderingBehaviorSource;
        private SheepNeedBehaviorSource _needBehaviorSource;
        private SheepIdleBehaviorSource _idleBehaviorSource;
        private SheepPetBehaviorSource _petBehaviorSource;
        private SheepFriendlyReaction _friendlyReaction;
        private SheepContaminationState _contaminationState;
        private AnimalBehaviorCoordinator _behaviorCoordinator;
        private Vector3 _lastThreatPosition;
        private bool _requiresPettingAfterScare;

        public bool IsFriendly { get; private set; }
        public bool IsScared => CurrentStateId == SheepStateId.ScaredRun;

        /// <summary>
        /// Показывает, выполнены ли условия для начала стрижки овцы.
        /// </summary>
        public bool CanBeSheared =>
            !IsScared &&
            CurrentStateId != SheepStateId.SeekingWater &&
            CurrentStateId != SheepStateId.Washing &&
            CurrentStateId != SheepStateId.Shearing &&
            woolGrowth != null &&
            woolGrowth.IsFullyGrown &&
            HasRequiredPaintForShearing();

        public SheepStateId CurrentStateId =>
            _stateMachine?.CurrentStateId ?? SheepStateId.None;

        /// <summary>
        /// Проверяет, выполнено ли требование по покраске с учётом временного режима тестирования.
        /// </summary>
        private bool HasRequiredPaintForShearing()
        {
            if (paintController == null)
            {
                return false;
            }

            if (paintController.AreAllSidesPainted())
            {
                return true;
            }

            return allowShearingAfterAnyPaintHitForTesting &&
                paintController.BlobsInOrder.Count > 0;
        }

        public event Action<SheepStateId, SheepStateId> StateChanged;

        /// <summary>
        /// Находит зависимости овцы и создаёт экземпляры её состояний.
        /// </summary>
        private void Awake()
        {
            if (locomotion == null)
            {
                locomotion = GetComponent<AnimalLocomotion>();
            }

            if (perception == null)
            {
                perception = GetComponent<AnimalPerception>();
            }

            if (perception != null && playerTarget != null)
            {
                perception.SetTarget(playerTarget);
            }

            if (woolGrowth == null)
            {
                woolGrowth = GetComponent<SheepWoolGrowth>();
            }

            if (waterEnvironment == null)
            {
                waterEnvironment = GetComponent<SheepWaterEnvironment>();
            }

            if (paintController == null)
            {
                paintController = GetComponent<SheepPaintController>();
            }

            _stateMachine = new SheepStateMachine();
            _stateMachine.StateChanged += OnStateChanged;

            _idleState = new SheepIdleState(
                GetRandomIdleDuration,
                TryStartCalmBehaviour
            );
            _wanderingState = new SheepWanderingState(
                locomotion,
                wanderArea,
                GetRandomWanderDuration,
                GetMinimumWanderDistance(),
                GetMaximumWanderPointAttempts(),
                ReturnToIdle
            );
            _eatingState = CreateNeedState(
                SheepStateId.Eating,
                SheepNeedType.Eating,
                GetEatingDuration
            );
            _drinkingState = CreateNeedState(
                SheepStateId.Drinking,
                SheepNeedType.Drinking,
                GetDrinkingDuration
            );
            _restingState = CreateNeedState(
                SheepStateId.Resting,
                SheepNeedType.Resting,
                GetRestingDuration
            );
            _scaredState = new SheepScaredState(
                locomotion,
                wanderArea,
                GetScaredDuration,
                GetMinimumFleeDistance(),
                GetMaximumWanderPointAttempts(),
                ReturnToIdle
            );
            _seekingWaterState = new SheepSeekingWaterState(
                locomotion,
                waterEnvironment,
                OnWaterReached,
                ReturnToIdle
            );
            _washingState = new SheepWashingState(
                locomotion,
                paintController,
                GetWashBlobDuration,
                FinishWashing
            );
            _shearingState = new SheepShearingState(
                locomotion,
                GetShearingDuration,
                FinishShearing
            );
            _contaminationState = new SheepContaminationState(
                GetHitsBeforeWash()
            );

            _scaredBehaviorSource = new SheepScaredBehaviorSource(
                GetScaredPriority()
            );
            _seekingWaterBehaviorSource = new SheepSeekingWaterBehaviorSource(
                _contaminationState,
                waterEnvironment,
                GetWashPriority()
            );
            _wanderingBehaviorSource = new SheepWanderingBehaviorSource(
                GetWanderingPriority()
            );
            _needBehaviorSource = new SheepNeedBehaviorSource(
                needsEnvironment,
                woolGrowth,
                GetNeedPriority()
            );
            _idleBehaviorSource = new SheepIdleBehaviorSource(
                GetIdlePriority()
            );
            _petBehaviorSource = new SheepPetBehaviorSource(
                GetInteractionPriority()
            );
            _friendlyReaction = new SheepFriendlyReaction(
                transform,
                perception,
                GetFriendlyDistance()
            );
            _behaviorCoordinator = new AnimalBehaviorCoordinator(
                new IAnimalBehaviorRequestSource[]
                {
                    _scaredBehaviorSource,
                    _seekingWaterBehaviorSource,
                    _needBehaviorSource,
                    _wanderingBehaviorSource,
                    _idleBehaviorSource,
                    _petBehaviorSource
                }
            );
        }

        /// <summary>
        /// Подписывается на попадания краской и запускает начальное состояние овцы.
        /// </summary>
        private void OnEnable()
        {
            if (paintController != null)
            {
                paintController.PaintHitReceived += OnPaintHitReceived;
                paintController.RefreshSurfaceCollider();
            }

            if (woolGrowth != null)
            {
                woolGrowth.StageChanged += OnWoolStageChanged;
            }

            _stateMachine.Start(_idleState);
        }

        /// <summary>
        /// Обновляет FSM овцы и проверяет её спокойную реакцию на игрока.
        /// </summary>
        private void Update()
        {
            TryProcessBehaviorRequests(out _);
            _stateMachine.Tick(Time.deltaTime);
            UpdateFriendlyState();
        }

        /// <summary>
        /// Отписывается от событий и останавливает FSM при отключении объекта.
        /// </summary>
        private void OnDisable()
        {
            if (paintController != null)
            {
                paintController.PaintHitReceived -= OnPaintHitReceived;
            }

            if (woolGrowth != null)
            {
                woolGrowth.StageChanged -= OnWoolStageChanged;
            }

            _stateMachine.Stop();
        }

        /// <summary>
        /// Освобождает подписку на события FSM перед уничтожением объекта.
        /// </summary>
        private void OnDestroy()
        {
            _stateMachine.StateChanged -= OnStateChanged;
        }

        /// <summary>
        /// Возвращает овцу в состояние спокойного ожидания.
        /// </summary>
        public void ReturnToIdle()
        {
            _stateMachine.TransitionTo(_idleState);
        }

        /// <summary>
        /// Принимает универсальную команду взаимодействия и направляет её к логике овцы.
        /// </summary>
        public bool TryInteract(
            AnimalInteractionType interactionType,
            Transform actor)
        {
            if (interactionType == AnimalInteractionType.Pet)
            {
                return TryPet(actor);
            }

            if (interactionType == AnimalInteractionType.Shear)
            {
                return TryRequestShearing();
            }

            return false;
        }

        /// <summary>
        /// Проверяет допуск к стрижке и сообщает результат без запуска самой стрижки.
        /// </summary>
        private bool TryRequestShearing()
        {
            if (!CanBeSheared)
            {
                Debug.Log(
                    $"{name}: shearing denied.",
                    this
                );
                return false;
            }

            Debug.Log(
                $"{name}: shearing allowed.",
                this
            );

            _stateMachine.TransitionTo(_shearingState);
            return true;
        }

        /// <summary>
        /// Проверяет возможность поглаживания и возвращает напуганную овцу к дружелюбию.
        /// </summary>
        public bool TryPet(Transform player)
        {
            if (!IsScared || player == null)
            {
                return false;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;

            float configuredPetDistance = GetPetDistance();

            if (offset.sqrMagnitude >
                configuredPetDistance * configuredPetDistance)
            {
                return false;
            }

            playerTarget = player;

            if (perception != null)
            {
                perception.SetTarget(player);
            }

            _petBehaviorSource.Request(player);

            if (!TryProcessBehaviorRequests(
                    out AnimalBehaviorRequest request))
            {
                return false;
            }

            return request.BehaviorId == SheepPetBehaviorSource.BehaviorId;
        }

        /// <summary>
        /// Пытается запустить случайное блуждание по доступной области.
        /// </summary>
        private bool TryStartWandering()
        {
            if (locomotion == null || wanderArea == null ||
                !locomotion.IsReady)
            {
                return false;
            }

            _wanderingBehaviorSource.Request();
            return TryProcessBehaviorRequests(out _);
        }

        /// <summary>
        /// Пытается запустить движение овцы к ближайшему доступному водоёму.
        /// </summary>
        private bool TryStartSeekingWater()
        {
            if (_contaminationState == null ||
                !_contaminationState.WantsToWash ||
                locomotion == null || !locomotion.IsReady ||
                waterEnvironment == null)
            {
                return false;
            }

            _seekingWaterBehaviorSource.Request();
            return TryProcessBehaviorRequests(out _);
        }

        /// <summary>
        /// Передаёт достигнутый водоём состоянию мытья и переводит овцу к воде.
        /// </summary>
        private void OnWaterReached(
            SheepWaterPoint waterPoint)
        {
            _washingState.SetWaterPoint(waterPoint);
            _stateMachine.TransitionTo(_washingState);
        }

        /// <summary>
        /// Завершает мытьё, отменяет намерение идти к воде и возвращает овцу к обычной жизни.
        /// </summary>
        private void FinishWashing()
        {
            _contaminationState.CancelWashIntent();
            ReturnToIdle();
        }

        /// <summary>
        /// Завершает состояние стрижки и возвращает овцу к обычному поведению.
        /// </summary>
        private void FinishShearing()
        {
            woolGrowth?.ResetToShaved();
            _contaminationState.CancelWashIntent();
            paintController?.ClearAllBlobs();
            ReturnToIdle();
        }

        /// <summary>
        /// Выбирает между потребностью, блужданием и повторным ожиданием.
        /// </summary>
        private bool TryStartCalmBehaviour()
        {
            bool shouldTryNeed = woolGrowth != null &&
                needsEnvironment != null &&
                UnityEngine.Random.value <= GetNeedChoiceChance();

            if (_contaminationState.WantsToWash &&
                TryStartSeekingWater())
            {
                return true;
            }

            if (shouldTryNeed && TryStartAvailableNeed())
            {
                return true;
            }

            if (TryStartWandering())
            {
                return true;
            }

            if (!shouldTryNeed && TryStartAvailableNeed())
            {
                return true;
            }

            _idleBehaviorSource.Request();
            TryProcessBehaviorRequests(out _);
            return false;
        }

        /// <summary>
        /// Ищет ещё не выполненную потребность с доступной точкой назначения.
        /// </summary>
        private bool TryStartAvailableNeed()
        {
            if (locomotion == null || needsEnvironment == null ||
                woolGrowth == null || !locomotion.IsReady)
            {
                return false;
            }

            _needBehaviorSource.Request();
            return TryProcessBehaviorRequests(out _);
        }

        /// <summary>
        /// Создаёт состояние выполнения указанной потребности овцы.
        /// </summary>
        private SheepNeedState CreateNeedState(
            SheepStateId stateId,
            SheepNeedType needType,
            Func<float> actionDurationProvider)
        {
            return new SheepNeedState(
                stateId,
                needType,
                locomotion,
                needsEnvironment,
                woolGrowth,
                actionDurationProvider,
                ReturnToIdle
            );
        }

        /// <summary>
        /// Возвращает заранее созданное состояние для типа потребности.
        /// </summary>
        private SheepNeedState GetStateForNeed(SheepNeedType need)
        {
            return need switch
            {
                SheepNeedType.Eating => _eatingState,
                SheepNeedType.Drinking => _drinkingState,
                SheepNeedType.Resting => _restingState,
                _ => throw new ArgumentOutOfRangeException(nameof(need), need, null)
            };
        }

        /// <summary>
        /// Возвращает случайную длительность состояния Idle.
        /// </summary>
        private float GetRandomIdleDuration()
        {
            return UnityEngine.Random.Range(
                GetMinimumIdleDuration(),
                GetMaximumIdleDuration()
            );
        }

        /// <summary>
        /// Возвращает случайную длительность состояния Wandering.
        /// </summary>
        private float GetRandomWanderDuration()
        {
            return UnityEngine.Random.Range(
                GetMinimumWanderDuration(),
                GetMaximumWanderDuration()
            );
        }

        /// <summary>
        /// Возвращает минимальную длительность Idle из конфигурации или резервного поля.
        /// </summary>
        private float GetMinimumIdleDuration()
        {
            return definition != null
                ? definition.MinimumIdleDuration
                : minimumIdleDuration;
        }

        /// <summary>
        /// Возвращает максимальную длительность Idle из конфигурации или резервного поля.
        /// </summary>
        private float GetMaximumIdleDuration()
        {
            return definition != null
                ? definition.MaximumIdleDuration
                : maximumIdleDuration;
        }

        /// <summary>
        /// Возвращает минимальную длительность блуждания из конфигурации или резервного поля.
        /// </summary>
        private float GetMinimumWanderDuration()
        {
            return definition != null
                ? definition.MinimumWanderDuration
                : minimumWanderDuration;
        }

        /// <summary>
        /// Возвращает максимальную длительность блуждания из конфигурации или резервного поля.
        /// </summary>
        private float GetMaximumWanderDuration()
        {
            return definition != null
                ? definition.MaximumWanderDuration
                : maximumWanderDuration;
        }

        /// <summary>
        /// Возвращает минимальную дистанцию блуждания из конфигурации или резервного поля.
        /// </summary>
        private float GetMinimumWanderDistance()
        {
            return definition != null
                ? definition.MinimumWanderDistance
                : minimumWanderDistance;
        }

        /// <summary>
        /// Возвращает лимит попыток поиска точки из конфигурации или резервного поля.
        /// </summary>
        private int GetMaximumWanderPointAttempts()
        {
            return definition != null
                ? definition.MaximumWanderPointAttempts
                : maximumWanderPointAttempts;
        }

        /// <summary>
        /// Возвращает настроенную длительность поедания травы.
        /// </summary>
        private float GetEatingDuration()
        {
            return definition != null
                ? definition.EatingDuration
                : eatingDuration;
        }

        /// <summary>
        /// Возвращает настроенную длительность питья воды.
        /// </summary>
        private float GetDrinkingDuration()
        {
            return definition != null
                ? definition.DrinkingDuration
                : drinkingDuration;
        }

        /// <summary>
        /// Возвращает настроенную длительность отдыха у цветов.
        /// </summary>
        private float GetRestingDuration()
        {
            return definition != null
                ? definition.RestingDuration
                : restingDuration;
        }

        /// <summary>
        /// Возвращает вероятность выбора потребности из конфигурации или резервного поля.
        /// </summary>
        private float GetNeedChoiceChance()
        {
            return definition != null
                ? definition.NeedChoiceChance
                : needChoiceChance;
        }

        /// <summary>
        /// Возвращает настроенную длительность испуга.
        /// </summary>
        private float GetScaredDuration()
        {
            return definition != null
                ? definition.ScaredDuration
                : scaredDuration;
        }

        /// <summary>
        /// Возвращает приоритет испуганного поведения из конфигурации или резервное значение.
        /// </summary>
        private int GetScaredPriority()
        {
            return definition != null
                ? definition.ScaredPriority
                : 80;
        }

        /// <summary>
        /// Возвращает приоритет блуждания из конфигурации или резервное значение.
        /// </summary>
        private int GetWanderingPriority()
        {
            return definition != null
                ? definition.WanderingPriority
                : 20;
        }

        /// <summary>
        /// Возвращает приоритет потребностей из конфигурации или резервное значение.
        /// </summary>
        private int GetNeedPriority()
        {
            return definition != null
                ? definition.NeedPriority
                : 40;
        }

        /// <summary>
        /// Возвращает приоритет мытья из конфигурации или резервное значение.
        /// </summary>
        private int GetWashPriority()
        {
            return definition != null
                ? definition.WashPriority
                : 90;
        }

        /// <summary>
        /// Возвращает приоритет спокойного ожидания из конфигурации или резервное значение.
        /// </summary>
        private int GetIdlePriority()
        {
            return definition != null
                ? definition.IdlePriority
                : 10;
        }

        /// <summary>
        /// Возвращает дистанцию дружелюбной реакции из конфигурации или резервного поля.
        /// </summary>
        private float GetFriendlyDistance()
        {
            return definition != null
                ? definition.FriendlyDistance
                : friendlyDistance;
        }

        /// <summary>
        /// Возвращает дистанцию поглаживания из конфигурации или резервного поля.
        /// </summary>
        private float GetPetDistance()
        {
            return definition != null
                ? definition.PetDistance
                : petDistance;
        }

        /// <summary>
        /// Возвращает минимальную дистанцию бегства из конфигурации или резервного поля.
        /// </summary>
        private float GetMinimumFleeDistance()
        {
            return definition != null
                ? definition.MinimumFleeDistance
                : minimumFleeDistance;
        }

        /// <summary>
        /// Возвращает порог попаданий до мытья из конфигурации или резервного поля.
        /// </summary>
        private int GetHitsBeforeWash()
        {
            return definition != null
                ? definition.HitsBeforeWash
                : hitsBeforeWash;
        }

        /// <summary>
        /// Возвращает длительность удаления пятна из конфигурации или резервного поля.
        /// </summary>
        private float GetWashBlobDuration()
        {
            return definition != null
                ? definition.WashBlobDuration
                : washBlobDuration;
        }

        /// <summary>
        /// Возвращает длительность стрижки из конфигурации или резервного поля.
        /// </summary>
        private float GetShearingDuration()
        {
            return definition != null
                ? definition.ShearingDuration
                : shearingDuration;
        }

        /// <summary>
        /// Возвращает приоритет интерактивной команды из конфигурации или резервное значение.
        /// </summary>
        private int GetInteractionPriority()
        {
            return definition != null
                ? definition.InteractionPriority
                : 70;
        }

        /// <summary>
        /// Переводит овцу в испуганное состояние после попадания краской.
        /// </summary>
        private void OnPaintHitReceived(PaintHit hit, Color color)
        {
            _contaminationState.RegisterHit();
            _requiresPettingAfterScare = true;
            _lastThreatPosition = hit.SourcePosition;
            SetFriendly(false, "paint hit");
            _scaredBehaviorSource.Request();
        }

        /// <summary>
        /// Обновляет меш поверхности покраски после смены визуальной стадии шерсти.
        /// </summary>
        private void OnWoolStageChanged(
            WoolGrowthStage previousStage,
            WoolGrowthStage nextStage)
        {
            paintController?.RefreshSurfaceCollider();
        }

        /// <summary>
        /// Передаёт победившие заявки поведения в соответствующие состояния овцы.
        /// </summary>
        private bool TryProcessBehaviorRequests(
            out AnimalBehaviorRequest processedRequest)
        {
            processedRequest = default;

            if (_behaviorCoordinator == null ||
                !_behaviorCoordinator.TryEvaluate(
                    Time.time,
                    out AnimalBehaviorRequest request))
            {
                return false;
            }

            processedRequest = request;

            if (request.BehaviorId == SheepScaredBehaviorSource.BehaviorId)
            {
                _scaredBehaviorSource.Consume();
                _scaredState.ReactToThreat(_lastThreatPosition);
                _stateMachine.TransitionTo(_scaredState);
                return true;
            }

            if (request.BehaviorId == SheepWanderingBehaviorSource.BehaviorId)
            {
                return _stateMachine.TransitionTo(_wanderingState);
            }

            if (request.BehaviorId == SheepSeekingWaterBehaviorSource.BehaviorId)
            {
                return _stateMachine.TransitionTo(_seekingWaterState);
            }

            if (request.BehaviorId == SheepNeedBehaviorSource.EatingBehaviorId)
            {
                return _stateMachine.TransitionTo(_eatingState);
            }

            if (request.BehaviorId == SheepNeedBehaviorSource.DrinkingBehaviorId)
            {
                return _stateMachine.TransitionTo(_drinkingState);
            }

            if (request.BehaviorId == SheepNeedBehaviorSource.RestingBehaviorId)
            {
                return _stateMachine.TransitionTo(_restingState);
            }

            if (request.BehaviorId == SheepIdleBehaviorSource.BehaviorId)
            {
                if (CurrentStateId == SheepStateId.Idle)
                {
                    return false;
                }

                return _stateMachine.TransitionTo(_idleState);
            }

            if (request.BehaviorId == SheepPetBehaviorSource.BehaviorId)
            {
                if (request.Target != null)
                {
                    playerTarget = request.Target;

                    if (perception != null)
                    {
                        perception.SetTarget(request.Target);
                    }
                }

                _contaminationState.CancelWashIntent();
                _requiresPettingAfterScare = false;
                SetFriendly(true, "petting");
                ReturnToIdle();

                if (logPlayerReactions)
                {
                    Debug.Log(
                        $"{name}: petting succeeded; friendly = {IsFriendly}.",
                        this
                    );
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Проверяет приближение игрока и включает дружелюбную реакцию овцы.
        /// </summary>
        private void UpdateFriendlyState()
        {
            if (!_friendlyReaction.TryGetNearbyTarget(
                    playerTarget,
                    IsScared,
                    IsFriendly,
                    _requiresPettingAfterScare,
                    out Transform target))
            {
                return;
            }

            playerTarget = target;
            SetFriendly(true, "игрок рядом");
        }

        /// <summary>
        /// Изменяет дружелюбный статус и при необходимости записывает причину изменения.
        /// </summary>
        private void SetFriendly(bool friendly, string reason)
        {
            if (IsFriendly == friendly)
            {
                return;
            }

            IsFriendly = friendly;

            if (logPlayerReactions)
            {
                Debug.Log(
                    $"{name}: friendly = {IsFriendly} ({reason}).",
                    this
                );
            }
        }

        /// <summary>
        /// Ограничивает настройки поведения овцы допустимыми значениями в Inspector.
        /// </summary>
        private void OnValidate()
        {
            minimumIdleDuration = Mathf.Max(0f, minimumIdleDuration);
            maximumIdleDuration = Mathf.Max(
                minimumIdleDuration,
                maximumIdleDuration
            );
            minimumWanderDuration = Mathf.Max(0f, minimumWanderDuration);
            maximumWanderDuration = Mathf.Max(
                minimumWanderDuration,
                maximumWanderDuration
            );
            minimumWanderDistance = Mathf.Max(0f, minimumWanderDistance);
            maximumWanderPointAttempts = Mathf.Max(
                1,
                maximumWanderPointAttempts
            );
            needChoiceChance = Mathf.Clamp01(needChoiceChance);
            eatingDuration = Mathf.Max(0f, eatingDuration);
            drinkingDuration = Mathf.Max(0f, drinkingDuration);
            restingDuration = Mathf.Max(0f, restingDuration);
            friendlyDistance = Mathf.Max(0.1f, friendlyDistance);
            petDistance = Mathf.Max(0.1f, petDistance);
            scaredDuration = Mathf.Max(0.1f, scaredDuration);
            minimumFleeDistance = Mathf.Max(0.1f, minimumFleeDistance);
            hitsBeforeWash = Mathf.Max(1, hitsBeforeWash);
            washBlobDuration = Mathf.Max(0f, washBlobDuration);
            shearingDuration = Mathf.Max(0f, shearingDuration);
        }

        /// <summary>
        /// Передаёт изменение состояния внешним подписчикам и выводит отладочный лог.
        /// </summary>
        private void OnStateChanged(
            SheepStateId previousState,
            SheepStateId nextState)
        {
            if (logStateTransitions)
            {
                Debug.Log(
                    $"{name}: {previousState} -> {nextState}",
                    this
                );
            }

            StateChanged?.Invoke(previousState, nextState);
        }
    }
}
