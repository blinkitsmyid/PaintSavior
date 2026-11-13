using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Определяет аудиошину, в которую будет направлен звук.
    /// </summary>
    public enum AudioBus
    {
        Master,
        Music,
        Ambience,
        SFX,
        UI
    }

    /// <summary>
    /// Определяет, должен ли звук зависеть от положения в мире.
    /// </summary>
    public enum AudioSpatialMode
    {
        TwoD,
        ThreeD
    }

    /// <summary>
    /// Хранит все настраиваемые данные одного игрового звукового события.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioCue",
        menuName = "Paint Savior/Audio/Audio Cue")]
    public sealed class AudioCue : ScriptableObject
    {
        [Header("Clips")]
        [Tooltip("Варианты клипа для случайного выбора при воспроизведении.")]
        [SerializeField]
        private AudioClip[] clips = new AudioClip[0];

        [Header("Routing")]
        [Tooltip("Аудиошина AudioMixer, в которую будет направлен звук.")]
        [SerializeField]
        private AudioBus bus = AudioBus.SFX;

        [Tooltip("2D-звук не зависит от позиции, 3D-звук затухает в пространстве.")]
        [SerializeField]
        private AudioSpatialMode spatialMode = AudioSpatialMode.ThreeD;

        [Header("Randomization")]
        [Tooltip("Минимальная громкость случайного варианта.")]
        [SerializeField, Min(0f)]
        private float volumeMin = 1f;

        [Tooltip("Максимальная громкость случайного варианта.")]
        [SerializeField, Min(0f)]
        private float volumeMax = 1f;

        [Tooltip("Минимальный Pitch случайного варианта.")]
        [SerializeField, Min(0.01f)]
        private float pitchMin = 1f;

        [Tooltip("Максимальный Pitch случайного варианта.")]
        [SerializeField, Min(0.01f)]
        private float pitchMax = 1f;

        [Header("Playback Limits")]
        [Tooltip("0 означает отсутствие лимита одновременных голосов для этого события.")]
        [SerializeField, Min(0)]
        private int maxSimultaneousVoices;

        [Tooltip("Минимальный интервал между запусками этого события в секундах.")]
        [SerializeField, Min(0f)]
        private float cooldownSeconds;

        [Tooltip("Приоритет AudioSource: меньшее значение означает больший приоритет.")]
        [SerializeField, Range(0, 256)]
        private int priority = 128;

        [Header("3D Settings")]
        [Tooltip("Расстояние, на котором 3D-звук начинает затухать.")]
        [SerializeField, Min(0.01f)]
        private float minDistance = 1f;

        [Tooltip("Расстояние, после которого 3D-звук перестаёт быть слышен.")]
        [SerializeField, Min(0.02f)]
        private float maxDistance = 20f;

        [Tooltip("Кривая затухания 3D-звука.")]
        [SerializeField]
        private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Loop and Fade")]
        [Tooltip("Зациклить клип. В основном используется музыкой и эмбиентом.")]
        [SerializeField]
        private bool loop;

        [Tooltip("Время плавного входа для loop-звуков и музыки.")]
        [SerializeField, Min(0f)]
        private float fadeInSeconds;

        [Tooltip("Время плавного выхода для loop-звуков и музыки.")]
        [SerializeField, Min(0f)]
        private float fadeOutSeconds;

        [Header("Designer Notes")]
        [Tooltip("Необязательное описание назначения звука для команды.")]
        [SerializeField, TextArea(2, 5)]
        private string designerNotes;

        /// <summary>
        /// Возвращает аудиошину события.
        /// </summary>
        public AudioBus Bus => bus;

        /// <summary>
        /// Возвращает режим пространственного воспроизведения.
        /// </summary>
        public AudioSpatialMode SpatialMode => spatialMode;

        /// <summary>
        /// Возвращает лимит одновременно звучащих экземпляров.
        /// </summary>
        public int MaxSimultaneousVoices => maxSimultaneousVoices;

        /// <summary>
        /// Возвращает минимальный интервал между запусками события.
        /// </summary>
        public float CooldownSeconds => cooldownSeconds;

        /// <summary>
        /// Возвращает приоритет источника звука.
        /// </summary>
        public int Priority => priority;

        /// <summary>
        /// Возвращает минимальную дистанцию 3D-звука.
        /// </summary>
        public float MinDistance => minDistance;

        /// <summary>
        /// Возвращает максимальную дистанцию 3D-звука.
        /// </summary>
        public float MaxDistance => maxDistance;

        /// <summary>
        /// Возвращает тип затухания 3D-звука.
        /// </summary>
        public AudioRolloffMode RolloffMode => rolloffMode;

        /// <summary>
        /// Возвращает признак зацикленного воспроизведения.
        /// </summary>
        public bool Loop => loop;

        /// <summary>
        /// Возвращает длительность плавного входа.
        /// </summary>
        public float FadeInSeconds => fadeInSeconds;

        /// <summary>
        /// Возвращает длительность плавного выхода.
        /// </summary>
        public float FadeOutSeconds => fadeOutSeconds;

        /// <summary>
        /// Выбирает случайный непустой клип из списка вариантов.
        /// </summary>
        public bool TryGetRandomClip(out AudioClip clip)
        {
            clip = null;

            if (clips == null || clips.Length == 0)
            {
                return false;
            }

            int startIndex = Random.Range(0, clips.Length);

            for (int offset = 0; offset < clips.Length; offset++)
            {
                int index = (startIndex + offset) % clips.Length;
                if (clips[index] == null)
                {
                    continue;
                }

                clip = clips[index];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Возвращает случайную громкость из настроенного диапазона.
        /// </summary>
        public float GetRandomVolume()
        {
            return Random.Range(volumeMin, volumeMax);
        }

        /// <summary>
        /// Возвращает случайный Pitch из настроенного диапазона.
        /// </summary>
        public float GetRandomPitch()
        {
            return Random.Range(pitchMin, pitchMax);
        }

        /// <summary>
        /// Исправляет несовместимые диапазоны параметров при изменении ассета в Inspector.
        /// </summary>
        private void OnValidate()
        {
            volumeMin = Mathf.Max(0f, volumeMin);
            volumeMax = Mathf.Max(volumeMin, volumeMax);
            pitchMin = Mathf.Max(0.01f, pitchMin);
            pitchMax = Mathf.Max(pitchMin, pitchMax);
            minDistance = Mathf.Max(0.01f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
        }
    }
}
