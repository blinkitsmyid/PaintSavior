using System.Collections.Generic;
using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Предоставляет переиспользуемые AudioSource для коротких и пространственных звуков.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioSourcePool : MonoBehaviour
    {
        [Header("Pool Size")]
        [Tooltip("Количество AudioSource, созданных заранее при запуске.")]
        [SerializeField, Min(1)]
        private int initialSize = 12;

        [Tooltip("Максимальное количество AudioSource, которое может создать пул.")]
        [SerializeField, Min(1)]
        private int maxSize = 32;

        private readonly List<AudioSource> _sources =
            new List<AudioSource>();

        /// <summary>
        /// Возвращает общее количество AudioSource в пуле.
        /// </summary>
        public int TotalCount => _sources.Count;

        /// <summary>
        /// Возвращает количество неактивных AudioSource, готовых к выдаче.
        /// </summary>
        public int AvailableCount
        {
            get
            {
                int availableCount = 0;

                for (int index = 0; index < _sources.Count; index++)
                {
                    AudioSource source = _sources[index];

                    if (source == null || !source.gameObject.activeSelf)
                    {
                        availableCount++;
                    }
                }

                return availableCount;
            }
        }

        /// <summary>
        /// Создаёт начальный набор AudioSource при запуске объекта пула.
        /// </summary>
        private void Awake()
        {
            NormalizeSettings();

            for (int index = 0; index < initialSize; index++)
            {
                CreateSource(index);
            }
        }

        /// <summary>
        /// Выдаёт свободный AudioSource или создаёт новый, если не достигнут лимит пула.
        /// </summary>
        public bool TryRent(out AudioSource source)
        {
            for (int index = 0; index < _sources.Count; index++)
            {
                AudioSource candidate = _sources[index];

                if (candidate == null)
                {
                    continue;
                }

                if (!candidate.gameObject.activeSelf || !candidate.isPlaying)
                {
                    candidate.gameObject.SetActive(true);
                    candidate.Stop();
                    source = candidate;
                    return true;
                }
            }

            if (_sources.Count >= maxSize)
            {
                source = null;
                return false;
            }

            source = CreateSource(_sources.Count);
            source.gameObject.SetActive(true);
            return true;
        }

        /// <summary>
        /// Останавливает AudioSource, очищает его временные настройки и возвращает в пул.
        /// </summary>
        public void Return(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.loop = false;
            source.volume = 1f;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.transform.SetParent(transform, false);
            source.transform.localPosition = Vector3.zero;
            source.gameObject.SetActive(false);
        }

        /// <summary>
        /// Создаёт и настраивает один дочерний AudioSource для повторного использования.
        /// </summary>
        private AudioSource CreateSource(int index)
        {
            GameObject sourceObject =
                new GameObject($"PooledAudioSource_{index:00}");

            sourceObject.transform.SetParent(transform, false);
            sourceObject.SetActive(false);

            AudioSource source =
                sourceObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.volume = 1f;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 128;

            _sources.Add(source);
            return source;
        }

        /// <summary>
        /// Нормализует настройки размера пула перед созданием AudioSource.
        /// </summary>
        private void NormalizeSettings()
        {
            initialSize = Mathf.Max(1, initialSize);
            maxSize = Mathf.Max(initialSize, maxSize);
        }

        /// <summary>
        /// Исправляет несовместимые значения размера пула в Inspector.
        /// </summary>
        private void OnValidate()
        {
            NormalizeSettings();
        }
    }
}
