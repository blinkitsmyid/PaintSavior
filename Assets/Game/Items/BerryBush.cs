using UnityEngine;

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
            if (collectionAudioSource != null &&
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
    }
}
