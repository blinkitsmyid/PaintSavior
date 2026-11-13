using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Audio
{
    /// <summary>
    /// Управляет воспроизведением AudioCue через общий пул AudioSource.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Пул источников, используемый для коротких и 3D-звуков.")]
        [SerializeField]
        private AudioSourcePool sourcePool;

        [Header("Audio Mixer Groups")]
        [SerializeField]
        private AudioMixerGroup masterGroup;

        [SerializeField]
        private AudioMixerGroup musicGroup;

        [SerializeField]
        private AudioMixerGroup ambienceGroup;

        [SerializeField]
        private AudioMixerGroup sfxGroup;

        [SerializeField]
        private AudioMixerGroup uiGroup;

        private readonly List<VoiceRecord> _activeVoices =
            new List<VoiceRecord>(64);

        private readonly Dictionary<AudioCue, float> _lastPlayTimes =
            new Dictionary<AudioCue, float>(64);

        /// <summary>
        /// Хранит AudioSource и AudioCue активного воспроизведения.
        /// </summary>
        private struct VoiceRecord
        {
            public AudioSource Source;
            public AudioCue Cue;

            /// <summary>
            /// Создаёт запись активного голоса.
            /// </summary>
            public VoiceRecord(AudioSource source, AudioCue cue)
            {
                Source = source;
                Cue = cue;
            }
        }

        /// <summary>
        /// Возвращает количество активных голосов в сервисе.
        /// </summary>
        public int ActiveVoiceCount => _activeVoices.Count;

        /// <summary>
        /// Находит пул на том же объекте, если ссылка не назначена вручную.
        /// </summary>
        private void Awake()
        {
            if (sourcePool == null)
            {
                sourcePool = GetComponent<AudioSourcePool>();
            }

            if (sourcePool == null)
            {
                Debug.LogError(
                    $"{name}: AudioService requires an AudioSourcePool.",
                    this);
            }
        }

        /// <summary>
        /// Возвращает завершившиеся источники в пул и освобождает их записи.
        /// </summary>
        private void Update()
        {
            ReleaseFinishedVoices();
        }

        /// <summary>
        /// Запускает AudioCue в заданной мировой позиции или с привязкой к объекту.
        /// </summary>
        public bool TryPlay(
            AudioCue cue,
            Vector3 worldPosition,
            Transform followTarget = null)
        {
            if (cue == null || sourcePool == null)
            {
                return false;
            }

            ReleaseFinishedVoices();

            if (!CanPlay(cue))
            {
                return false;
            }

            if (!cue.TryGetRandomClip(out AudioClip clip))
            {
                return false;
            }

            if (!sourcePool.TryRent(out AudioSource source))
            {
                return false;
            }

            ConfigureSource(
                source,
                cue,
                clip,
                worldPosition,
                followTarget);

            source.Play();
            _activeVoices.Add(new VoiceRecord(source, cue));
            _lastPlayTimes[cue] = Time.unscaledTime;
            return true;
        }

        /// <summary>
        /// Останавливает все активные голоса, созданные из указанного AudioCue.
        /// </summary>
        public void Stop(AudioCue cue)
        {
            if (cue == null)
            {
                return;
            }

            for (int index = _activeVoices.Count - 1; index >= 0; index--)
            {
                if (_activeVoices[index].Cue == cue)
                {
                    ReleaseVoiceAt(index);
                }
            }

            _lastPlayTimes.Remove(cue);
        }

        /// <summary>
        /// Останавливает все активные голоса и очищает состояние сервиса.
        /// </summary>
        public void StopAll()
        {
            for (int index = _activeVoices.Count - 1; index >= 0; index--)
            {
                ReleaseVoiceAt(index);
            }

            _lastPlayTimes.Clear();
        }

        /// <summary>
        /// Возвращает, можно ли запустить событие с учётом cooldown и лимита голосов.
        /// </summary>
        private bool CanPlay(AudioCue cue)
        {
            if (_lastPlayTimes.TryGetValue(cue, out float lastPlayTime))
            {
                float elapsed = Time.unscaledTime - lastPlayTime;
                if (elapsed < cue.CooldownSeconds)
                {
                    return false;
                }
            }

            if (cue.MaxSimultaneousVoices <= 0)
            {
                return true;
            }

            int activeVoiceCount = 0;

            for (int index = 0; index < _activeVoices.Count; index++)
            {
                if (_activeVoices[index].Cue == cue)
                {
                    activeVoiceCount++;
                }
            }

            return activeVoiceCount < cue.MaxSimultaneousVoices;
        }

        /// <summary>
        /// Применяет к AudioSource настройки клипа, Mixer-группы, рандомизации и позиции.
        /// </summary>
        private void ConfigureSource(
            AudioSource source,
            AudioCue cue,
            AudioClip clip,
            Vector3 worldPosition,
            Transform followTarget)
        {
            source.clip = clip;
            source.outputAudioMixerGroup = ResolveMixerGroup(cue.Bus);
            source.volume = cue.GetRandomVolume();
            source.pitch = cue.GetRandomPitch();
            source.priority = cue.Priority;
            source.loop = cue.Loop;
            source.rolloffMode = cue.RolloffMode;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            source.spatialBlend =
                cue.SpatialMode == AudioSpatialMode.ThreeD
                    ? 1f
                    : 0f;

            if (followTarget != null)
            {
                source.transform.SetParent(followTarget, false);
                source.transform.localPosition = Vector3.zero;
                return;
            }

            source.transform.SetParent(sourcePool.transform, false);
            source.transform.position = worldPosition;
        }

        /// <summary>
        /// Возвращает Mixer-группу, соответствующую категории AudioCue.
        /// </summary>
        private AudioMixerGroup ResolveMixerGroup(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Music:
                    return musicGroup != null ? musicGroup : masterGroup;
                case AudioBus.Ambience:
                    return ambienceGroup != null ? ambienceGroup : masterGroup;
                case AudioBus.SFX:
                    return sfxGroup != null ? sfxGroup : masterGroup;
                case AudioBus.UI:
                    return uiGroup != null ? uiGroup : masterGroup;
                default:
                    return masterGroup;
            }
        }

        /// <summary>
        /// Находит и возвращает в пул источники, которые больше не воспроизводятся.
        /// </summary>
        private void ReleaseFinishedVoices()
        {
            for (int index = _activeVoices.Count - 1; index >= 0; index--)
            {
                AudioSource source = _activeVoices[index].Source;

                if (source == null || !source.isPlaying)
                {
                    ReleaseVoiceAt(index);
                }
            }
        }

        /// <summary>
        /// Возвращает один голос в пул и удаляет его запись без сдвига всего списка.
        /// </summary>
        private void ReleaseVoiceAt(int index)
        {
            VoiceRecord record = _activeVoices[index];
            sourcePool.Return(record.Source);

            int lastIndex = _activeVoices.Count - 1;
            _activeVoices[index] = _activeVoices[lastIndex];
            _activeVoices.RemoveAt(lastIndex);
        }

        /// <summary>
        /// Возвращает активные AudioSource в пул при отключении сервиса.
        /// </summary>
        private void OnDisable()
        {
            StopAll();
        }
    }
}
