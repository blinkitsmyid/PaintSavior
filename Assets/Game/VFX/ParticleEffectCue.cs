using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// Определяет, должен ли запущенный эффект получать цвет от игрового события.
    /// </summary>
    public enum ParticleEffectColorMode
    {
        None,
        UseRequestedColor
    }

    /// <summary>
    /// Хранит настройки одного переиспользуемого визуального эффекта на базе ParticleSystem.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ParticleEffectCue",
        menuName = "Paint Savior/VFX/Particle Effect Cue")]
    public sealed class ParticleEffectCue : ScriptableObject
    {
        [Header("Effect Prefab")]
        [Tooltip("Prefab с корневым ParticleSystem, который будет запускаться сервисом VFX.")]
        [SerializeField] private ParticleSystem effectPrefab;

        [Header("Playback")]
        [Tooltip("Резервная длительность эффекта до возврата объекта в пул.")]
        [SerializeField, Min(0f)] private float duration = 2f;

        [Tooltip("Масштаб, с которым эффект создаётся в мире.")]
        [SerializeField, Min(0.001f)] private float scale = 1f;

        [Tooltip("Определяет, должен ли сервис передать эффекту цвет игрового события.")]
        [SerializeField] private ParticleEffectColorMode colorMode;

        [Header("Pooling")]
        [Tooltip("Количество экземпляров, которые сервис может подготовить заранее.")]
        [SerializeField, Min(0)] private int initialPoolSize = 2;

        [Tooltip("Максимальное количество одновременно активных экземпляров эффекта.")]
        [SerializeField, Min(1)] private int maxPoolSize = 16;

        [Header("Designer Notes")]
        [Tooltip("Описание назначения эффекта и рекомендации по его настройке.")]
        [SerializeField, TextArea(2, 5)] private string designerNotes;

        /// <summary>
        /// Возвращает prefab ParticleSystem, который должен запускаться для этого эффекта.
        /// </summary>
        public ParticleSystem EffectPrefab => effectPrefab;

        /// <summary>
        /// Возвращает резервную длительность эффекта до его остановки или возврата в пул.
        /// </summary>
        public float Duration => duration;

        /// <summary>
        /// Возвращает масштаб экземпляра эффекта в мировом пространстве.
        /// </summary>
        public float Scale => scale;

        /// <summary>
        /// Возвращает режим передачи цвета от игрового события в ParticleSystem.
        /// </summary>
        public ParticleEffectColorMode ColorMode => colorMode;

        /// <summary>
        /// Возвращает рекомендуемое количество предварительно созданных экземпляров.
        /// </summary>
        public int InitialPoolSize => initialPoolSize;

        /// <summary>
        /// Возвращает максимальное количество одновременно активных экземпляров.
        /// </summary>
        public int MaxPoolSize => maxPoolSize;

        /// <summary>
        /// Возвращает пояснение дизайнера для этого визуального эффекта.
        /// </summary>
        public string DesignerNotes => designerNotes;

        /// <summary>
        /// Проверяет и исправляет диапазоны настроек перед сохранением ассета в Inspector.
        /// </summary>
        private void OnValidate()
        {
            duration = Mathf.Max(0f, duration);
            scale = Mathf.Max(0.001f, scale);
            initialPoolSize = Mathf.Max(0, initialPoolSize);
            maxPoolSize = Mathf.Max(1, maxPoolSize);
            initialPoolSize = Mathf.Min(initialPoolSize, maxPoolSize);
        }
    }
}
