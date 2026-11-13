using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Запускает назначенный AudioCue через AudioService от имени игрового объекта.
    /// </summary>
    [AddComponentMenu("Paint Savior/Audio/Audio Emitter")]
    public sealed class AudioEmitter : MonoBehaviour
    {
        [Header("Audio Cue")]
        [Tooltip("Звуковое событие, которое будет запускаться этим компонентом.")]
        [SerializeField]
        private AudioCue cue;

        [Header("Playback")]
        [Tooltip("Автоматически запустить событие при включении объекта.")]
        [SerializeField]
        private bool playOnEnable;

        [Tooltip("Для 3D-звука источник будет следовать за этим объектом во время воспроизведения.")]
        [SerializeField]
        private bool followEmitter = true;

        [Header("References")]
        [Tooltip("Предпочтительная явная ссылка на AudioService.")]
        [SerializeField]
        private AudioService audioService;

        /// <summary>
        /// Возвращает назначенное звуковое событие.
        /// </summary>
        public AudioCue Cue => cue;

        /// <summary>
        /// Находит AudioService по явной ссылке или один раз ищет его в сцене.
        /// </summary>
        private void Awake()
        {
            ResolveAudioService();
        }

        /// <summary>
        /// Запускает AudioCue при включении объекта, если включён соответствующий режим.
        /// </summary>
        private void OnEnable()
        {
            if (playOnEnable)
            {
                TryPlay();
            }
        }

        /// <summary>
        /// Запускает назначенный AudioCue в позиции объекта.
        /// </summary>
        public bool TryPlay()
        {
            if (cue == null || audioService == null)
            {
                return false;
            }

            Transform followTarget =
                followEmitter
                    ? transform
                    : null;

            return audioService.TryPlay(
                cue,
                transform.position,
                followTarget);
        }

        /// <summary>
        /// Находит AudioService среди родителей или один раз в активной сцене.
        /// </summary>
        private void ResolveAudioService()
        {
            if (audioService != null)
            {
                return;
            }

            audioService =
                GetComponentInParent<AudioService>();

            if (audioService == null)
            {
                audioService =
                    FindFirstObjectByType<AudioService>();
            }

            if (audioService == null)
            {
                Debug.LogWarning(
                    $"{name}: AudioService was not found for AudioEmitter.",
                    this);
            }
        }
    }
}
