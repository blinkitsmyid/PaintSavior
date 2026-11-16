using System.Collections.Generic;

using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// Запускает ParticleEffectCue и возвращает завершившиеся эффекты в общий пул.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleEffectPool))]
    public sealed class ParticleEffectService : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Пул экземпляров ParticleSystem, используемый сервисом.")]
        [SerializeField] private ParticleEffectPool effectPool;

        [Tooltip("Контейнер для активных экземпляров визуальных эффектов.")]
        [SerializeField] private Transform effectRoot;

        private readonly List<ActiveEffect> _activeEffects =
            new List<ActiveEffect>(32);

        private readonly HashSet<ParticleEffectCue> _missingCueWarnings =
            new HashSet<ParticleEffectCue>();

        /// <summary>
        /// Хранит эффект, его Cue и момент, после которого экземпляр можно вернуть в пул.
        /// </summary>
        private struct ActiveEffect
        {
            public ParticleSystem Effect;
            public ParticleEffectCue Cue;
            public float ReleaseTime;

            /// <summary>
            /// Создаёт запись активного экземпляра эффекта.
            /// </summary>
            public ActiveEffect(
                ParticleSystem effect,
                ParticleEffectCue cue,
                float releaseTime)
            {
                Effect = effect;
                Cue = cue;
                ReleaseTime = releaseTime;
            }
        }

        /// <summary>
        /// Находит связанные компоненты и подготавливает контейнер активных эффектов.
        /// </summary>
        private void Awake()
        {
            if (effectPool == null)
            {
                effectPool = GetComponent<ParticleEffectPool>();
            }

            if (effectRoot == null)
            {
                effectRoot = transform;
            }

            if (effectPool == null)
            {
                Debug.LogError(
                    $"{name}: ParticleEffectService requires ParticleEffectPool.",
                    this);
            }
        }

        /// <summary>
        /// Возвращает завершившиеся экземпляры в пул без создания новых объектов.
        /// </summary>
        private void Update()
        {
            ReleaseExpiredEffects();
        }

        /// <summary>
        /// Запускает визуальный эффект в мировой позиции без передачи игрового цвета.
        /// </summary>
        public ParticleSystem Play(
            ParticleEffectCue cue,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            return Play(
                cue,
                worldPosition,
                worldRotation,
                Color.white);
        }

        /// <summary>
        /// Запускает визуальный эффект в мировой позиции и передаёт ему цвет события, если это разрешено Cue.
        /// </summary>
        public ParticleSystem Play(
            ParticleEffectCue cue,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Color requestedColor)
        {
            return PlayInternal(
                cue,
                worldPosition,
                worldRotation,
                requestedColor,
                null);
        }

        /// <summary>
        /// Запускает эффект как дочерний объект цели и удерживает его активным до явной остановки.
        /// </summary>
        public ParticleSystem PlayAttached(
            ParticleEffectCue cue,
            Transform target)
        {
            if (target == null)
            {
                LogMissingCue(cue);
                return null;
            }

            return PlayInternal(
                cue,
                target.position,
                target.rotation,
                Color.white,
                target);
        }

        /// <summary>
        /// Создаёт или выдаёт из пула эффект, настраивает его привязку и запускает ParticleSystem.
        /// </summary>
        private ParticleSystem PlayInternal(
            ParticleEffectCue cue,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Color requestedColor,
            Transform followTarget)
        {
            if (cue == null || cue.EffectPrefab == null || effectPool == null)
            {
                LogMissingCue(cue);
                return null;
            }

            if (!effectPool.TryRent(cue, out ParticleSystem effect))
            {
                return null;
            }

            Transform parent =
                followTarget != null
                    ? followTarget
                    : effectRoot;

            effect.transform.SetParent(
                parent,
                false);

            if (followTarget != null)
            {
                effect.transform.localPosition = Vector3.zero;
                effect.transform.localRotation = Quaternion.identity;
            }
            else
            {
                effect.transform.SetPositionAndRotation(
                    worldPosition,
                    worldRotation);
            }

            effect.transform.localScale =
                Vector3.one * cue.Scale;

            ApplyRequestedColor(
                effect,
                cue,
                requestedColor);

            effect.Clear(true);
            effect.gameObject.SetActive(true);
            effect.Play(true);

            float releaseTime =
                followTarget != null
                    ? float.PositiveInfinity
                    : Time.time + ResolveDuration(cue, effect);

            _activeEffects.Add(
                new ActiveEffect(
                    effect,
                    cue,
                    releaseTime));

            return effect;
        }

        /// <summary>
        /// Немедленно останавливает эффект и возвращает его экземпляр в пул.
        /// </summary>
        public void Stop(
            ParticleSystem effect)
        {
            if (effect == null)
            {
                return;
            }

            for (int index = _activeEffects.Count - 1; index >= 0; index--)
            {
                if (_activeEffects[index].Effect == effect)
                {
                    ReleaseAt(index);
                    return;
                }
            }
        }

        /// <summary>
        /// Передаёт цвет в корневой ParticleSystem только для Cue с цветовым режимом UseRequestedColor.
        /// </summary>
        private static void ApplyRequestedColor(
            ParticleSystem effect,
            ParticleEffectCue cue,
            Color requestedColor)
        {
            if (cue.ColorMode != ParticleEffectColorMode.UseRequestedColor)
            {
                return;
            }

            ParticleSystem.MainModule main =
                effect.main;

            main.startColor = requestedColor;
        }

        /// <summary>
        /// Вычисляет резервное время жизни эффекта из Cue или настроек ParticleSystem.
        /// </summary>
        private static float ResolveDuration(
            ParticleEffectCue cue,
            ParticleSystem effect)
        {
            if (cue.Duration > 0f)
            {
                return cue.Duration;
            }

            ParticleSystem.MainModule main =
                effect.main;

            float maximumStartLifetime =
                main.startLifetime.mode == ParticleSystemCurveMode.Constant
                    ? main.startLifetime.constant
                    : main.startLifetime.constantMax;

            return Mathf.Max(
                0.01f,
                main.duration + maximumStartLifetime);
        }

        /// <summary>
        /// Возвращает в пул эффекты с истёкшим временем или завершившимся ParticleSystem.
        /// </summary>
        private void ReleaseExpiredEffects()
        {
            for (int index = _activeEffects.Count - 1; index >= 0; index--)
            {
                ActiveEffect activeEffect =
                    _activeEffects[index];

                bool isMissing =
                    activeEffect.Effect == null;

                bool durationExpired =
                    Time.time >= activeEffect.ReleaseTime;

                bool particleSystemFinished =
                    !isMissing &&
                    !activeEffect.Effect.IsAlive(true);

                if (isMissing || durationExpired || particleSystemFinished)
                {
                    ReleaseAt(index);
                }
            }
        }

        /// <summary>
        /// Возвращает один экземпляр в пул и удаляет его запись без сдвига всего списка.
        /// </summary>
        private void ReleaseAt(
            int index)
        {
            ActiveEffect activeEffect =
                _activeEffects[index];

            if (activeEffect.Effect != null)
            {
                effectPool.Return(
                    activeEffect.Cue,
                    activeEffect.Effect);
            }

            int lastIndex =
                _activeEffects.Count - 1;

            _activeEffects[index] =
                _activeEffects[lastIndex];
            _activeEffects.RemoveAt(lastIndex);
        }

        /// <summary>
        /// Возвращает все активные эффекты в пул перед отключением сервиса.
        /// </summary>
        private void OnDisable()
        {
            for (int index = _activeEffects.Count - 1; index >= 0; index--)
            {
                ReleaseAt(index);
            }
        }

        /// <summary>
        /// Выводит предупреждение об отсутствующем Cue или prefab только один раз для каждого Cue.
        /// </summary>
        private void LogMissingCue(
            ParticleEffectCue cue)
        {
            if (cue != null && _missingCueWarnings.Contains(cue))
            {
                return;
            }

            if (cue != null)
            {
                _missingCueWarnings.Add(cue);
            }

            Debug.LogWarning(
                $"{name}: ParticleEffectCue, Effect Prefab or ParticleEffectPool is not assigned.",
                this);
        }
    }
}
