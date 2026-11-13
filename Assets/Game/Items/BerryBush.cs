using UnityEngine;

using Game.Audio;

namespace Game.Items
{
    /// <summary>
    /// Представляет куст, который добавляет ягоды в инвентарь игрока без изменения своего внешнего вида.
    /// </summary>
    public sealed class BerryBush : MonoBehaviour
    {
        [Header("Berry")]
        [SerializeField] private BerryType berryType = BerryType.Red;
        [SerializeField, Min(1)] private int berriesPerCollection = 1;

        [Header("Collection Feedback")]
        [SerializeField] private AudioSource collectionAudioSource;
        [SerializeField] private AudioClip collectionAudioClip;
        [SerializeField] private ParticleSystem leafParticles;

        [Header("Audio Service")]
        [Tooltip("Необязательный AudioCue для нового общего аудиосервиса.")]
        [SerializeField] private AudioCue collectionCue;
        [Tooltip("Общий сервис воспроизведения звуков. Если не задан, ищется один раз при успешном сборе.")]
        [SerializeField] private AudioService audioService;

        public BerryType Type => berryType;

        /// <summary>
        /// Находит AudioSource на кусте, если источник не был назначен через Inspector.
        /// </summary>
        private void Awake()
        {
            if (collectionAudioSource == null)
            {
                collectionAudioSource =
                    GetComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Пытается добавить ягоды этого куста в инвентарь и запускает обратную связь только при успешном сборе.
        /// </summary>
        public bool TryCollect(
            BerryInventory inventory)
        {
            if (inventory == null)
            {
                return false;
            }

            bool collected =
                inventory.TryAddBerry(
                    berryType,
                    Mathf.Max(1, berriesPerCollection),
                    out int addedAmount
                );

            if (!collected || addedAmount <= 0)
            {
                return false;
            }

            PlayCollectionFeedback();
            return true;
        }

        /// <summary>
        /// Проигрывает звук на самом кусте и запускает частицы листвы после успешного сбора.
        /// </summary>
        private void PlayCollectionFeedback()
        {
            if (collectionCue != null)
            {
                PlayCollectionAudio();
            }
            else if (collectionAudioSource != null &&
                collectionAudioClip != null)
            {
                collectionAudioSource.PlayOneShot(
                    collectionAudioClip
                );
            }

            if (leafParticles != null)
            {
                leafParticles.Play(true);
            }
        }

        /// <summary>
        /// Воспроизводит новый AudioCue сбора ягод через общий AudioService.
        /// </summary>
        private void PlayCollectionAudio()
        {
            ResolveAudioService();

            if (audioService == null)
            {
                return;
            }

            audioService.TryPlay(
                collectionCue,
                transform.position,
                transform
            );
        }

        /// <summary>
        /// Находит AudioService в родителях или сцене, если он не задан вручную.
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
        }
    }
}
