using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Описывает общие звуковые действия, которые могут использовать разные животные.
    /// </summary>
    public enum AnimalAudioAction
    {
        Idle,
        Walk,
        Run,
        Eat,
        Drink,
        Rest,
        Scared,
        Friendly,
        Pet,
        Wash,
        Shear
    }

    /// <summary>
    /// Хранит звуковые события животного и передаёт их в общий AudioService.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Paint Savior/Audio/Animal Audio Presenter")]
    public sealed class AnimalAudioPresenter : MonoBehaviour
    {
        [Header("Audio Service")]
        [Tooltip("Общий сервис воспроизведения звуков. Если не задан, ищется один раз при запуске.")]
        [SerializeField]
        private AudioService audioService;

        [Tooltip("Если включено, источник звука следует за животным во время воспроизведения.")]
        [SerializeField]
        private bool followAnimal = true;

        [Header("Common Animal Actions")]
        [Tooltip("Звук спокойного состояния или короткой реакции ожидания.")]
        [SerializeField]
        private AudioCue idleCue;

        [Tooltip("Звук ходьбы животного.")]
        [SerializeField]
        private AudioCue walkCue;

        [Tooltip("Звук быстрого движения или бегства животного.")]
        [SerializeField]
        private AudioCue runCue;

        [Tooltip("Звук поедания травы или другой пищи.")]
        [SerializeField]
        private AudioCue eatCue;

        [Tooltip("Звук питья воды.")]
        [SerializeField]
        private AudioCue drinkCue;

        [Tooltip("Звук отдыха или нюханья цветов.")]
        [SerializeField]
        private AudioCue restCue;

        [Tooltip("Звук испуга или тревожной реакции.")]
        [SerializeField]
        private AudioCue scaredCue;

        [Tooltip("Звук дружелюбной реакции животного.")]
        [SerializeField]
        private AudioCue friendlyCue;

        [Tooltip("Звук реакции на поглаживание.")]
        [SerializeField]
        private AudioCue petCue;

        [Tooltip("Звук мытья или нахождения в воде.")]
        [SerializeField]
        private AudioCue washCue;

        [Tooltip("Звук стрижки.")]
        [SerializeField]
        private AudioCue shearCue;

        /// <summary>
        /// Находит AudioService, если он не был задан через Inspector.
        /// </summary>
        private void Awake()
        {
            ResolveAudioService();
        }

        /// <summary>
        /// Воспроизводит настроенный AudioCue для указанного действия животного.
        /// </summary>
        public bool TryPlay(AnimalAudioAction action)
        {
            if (audioService == null)
            {
                ResolveAudioService();
            }

            AudioCue cue = ResolveCue(action);
            if (audioService == null || cue == null)
            {
                return false;
            }

            return audioService.TryPlay(
                cue,
                transform.position,
                followAnimal ? transform : null);
        }

        /// <summary>
        /// Возвращает AudioCue, назначенный для выбранного общего действия.
        /// </summary>
        private AudioCue ResolveCue(AnimalAudioAction action)
        {
            switch (action)
            {
                case AnimalAudioAction.Idle:
                    return idleCue;
                case AnimalAudioAction.Walk:
                    return walkCue;
                case AnimalAudioAction.Run:
                    return runCue;
                case AnimalAudioAction.Eat:
                    return eatCue;
                case AnimalAudioAction.Drink:
                    return drinkCue;
                case AnimalAudioAction.Rest:
                    return restCue;
                case AnimalAudioAction.Scared:
                    return scaredCue;
                case AnimalAudioAction.Friendly:
                    return friendlyCue;
                case AnimalAudioAction.Pet:
                    return petCue;
                case AnimalAudioAction.Wash:
                    return washCue;
                case AnimalAudioAction.Shear:
                    return shearCue;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Находит общий AudioService в родителях или сцене один раз при необходимости.
        /// </summary>
        private void ResolveAudioService()
        {
            if (audioService != null)
            {
                return;
            }

            audioService = GetComponentInParent<AudioService>();
            if (audioService == null)
            {
                audioService = FindFirstObjectByType<AudioService>();
            }

            if (audioService == null)
            {
                Debug.LogWarning(
                    $"{name}: AnimalAudioPresenter не нашёл AudioService.",
                    this);
            }
        }
    }
}
