using System;

using UnityEngine;

using Game.Paint;

namespace Game.Sheep.AI
{
    [DisallowMultipleComponent]
    public sealed class SheepBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SheepLocomotion locomotion;
        [SerializeField] private SheepWanderArea wanderArea;
        [SerializeField] private SheepNeedsEnvironment needsEnvironment;
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

        [Header("Debug")]
        [SerializeField] private bool logStateTransitions = true;
        [SerializeField] private bool logPlayerReactions = true;

        private SheepStateMachine _stateMachine;
        private SheepIdleState _idleState;
        private SheepWanderingState _wanderingState;
        private SheepNeedState _eatingState;
        private SheepNeedState _drinkingState;
        private SheepNeedState _restingState;
        private SheepScaredState _scaredState;

        public bool IsFriendly { get; private set; }
        public bool IsScared => CurrentStateId == SheepStateId.ScaredRun;

        public SheepStateId CurrentStateId =>
            _stateMachine?.CurrentStateId ?? SheepStateId.None;

        public event Action<SheepStateId, SheepStateId> StateChanged;

        private void Awake()
        {
            if (locomotion == null)
            {
                locomotion = GetComponent<SheepLocomotion>();
            }

            if (woolGrowth == null)
            {
                woolGrowth = GetComponent<SheepWoolGrowth>();
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
                minimumWanderDistance,
                maximumWanderPointAttempts,
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
                minimumFleeDistance,
                maximumWanderPointAttempts,
                ReturnToIdle
            );
        }

        private void OnEnable()
        {
            if (paintController != null)
            {
                paintController.PaintHitReceived += OnPaintHitReceived;
            }

            _stateMachine.Start(_idleState);
        }

        private void Update()
        {
            _stateMachine.Tick(Time.deltaTime);
            UpdateFriendlyState();
        }

        private void OnDisable()
        {
            if (paintController != null)
            {
                paintController.PaintHitReceived -= OnPaintHitReceived;
            }

            _stateMachine.Stop();
        }

        private void OnDestroy()
        {
            _stateMachine.StateChanged -= OnStateChanged;
        }

        public void ReturnToIdle()
        {
            _stateMachine.TransitionTo(_idleState);
        }

        public bool TryPet(Transform player)
        {
            if (!IsScared || player == null)
            {
                return false;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > petDistance * petDistance)
            {
                return false;
            }

            playerTarget = player;
            SetFriendly(true, "petting");
            ReturnToIdle();
            return true;
        }

        private bool TryStartWandering()
        {
            if (locomotion == null || wanderArea == null ||
                !locomotion.IsReady)
            {
                return false;
            }

            return _stateMachine.TransitionTo(_wanderingState);
        }

        private bool TryStartCalmBehaviour()
        {
            bool shouldTryNeed = woolGrowth != null &&
                needsEnvironment != null &&
                UnityEngine.Random.value <= needChoiceChance;

            if (shouldTryNeed && TryStartAvailableNeed())
            {
                return true;
            }

            if (TryStartWandering())
            {
                return true;
            }

            return !shouldTryNeed && TryStartAvailableNeed();
        }

        private bool TryStartAvailableNeed()
        {
            if (locomotion == null || needsEnvironment == null ||
                woolGrowth == null || !locomotion.IsReady)
            {
                return false;
            }

            int firstNeed = UnityEngine.Random.Range(0, 3);

            for (int offset = 0; offset < 3; offset++)
            {
                SheepNeedType need = (SheepNeedType)((firstNeed + offset) % 3);

                if (!woolGrowth.CanCompleteNeed(need) ||
                    !needsEnvironment.HasAvailablePoint(need))
                {
                    continue;
                }

                return _stateMachine.TransitionTo(GetStateForNeed(need));
            }

            return false;
        }

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

        private float GetRandomIdleDuration()
        {
            return UnityEngine.Random.Range(
                minimumIdleDuration,
                maximumIdleDuration
            );
        }

        private float GetRandomWanderDuration()
        {
            return UnityEngine.Random.Range(
                minimumWanderDuration,
                maximumWanderDuration
            );
        }

        private float GetEatingDuration()
        {
            return eatingDuration;
        }

        private float GetDrinkingDuration()
        {
            return drinkingDuration;
        }

        private float GetRestingDuration()
        {
            return restingDuration;
        }

        private float GetScaredDuration()
        {
            return scaredDuration;
        }

        private void OnPaintHitReceived(PaintHit hit, Color color)
        {
            SetFriendly(false, "paint hit");
            _scaredState.ReactToThreat(hit.SourcePosition);
            _stateMachine.TransitionTo(_scaredState);
        }

        private void UpdateFriendlyState()
        {
            if (IsScared || IsFriendly || playerTarget == null)
            {
                return;
            }

            Vector3 offset = playerTarget.position - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= friendlyDistance * friendlyDistance)
            {
                SetFriendly(true, "игрок рядом");
            }
        }

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
        }

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
