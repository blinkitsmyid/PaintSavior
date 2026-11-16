using System.Collections.Generic;

using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// Хранит переиспользуемые экземпляры ParticleSystem для каждого ParticleEffectCue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleEffectPool : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Контейнер для неактивных экземпляров эффектов.")]
        [SerializeField] private Transform poolRoot;

        private readonly Dictionary<ParticleEffectCue, PoolBucket>
            _buckets =
            new Dictionary<ParticleEffectCue, PoolBucket>();

        private readonly Dictionary<ParticleSystem, ParticleEffectCue>
            _owners =
            new Dictionary<ParticleSystem, ParticleEffectCue>();

        /// <summary>
        /// Находит контейнер пула, если он не был назначен вручную.
        /// </summary>
        private void Awake()
        {
            if (poolRoot == null)
            {
                poolRoot = transform;
            }
        }

        /// <summary>
        /// Выдаёт свободный экземпляр Cue или создаёт его до достижения лимита.
        /// </summary>
        public bool TryRent(
            ParticleEffectCue cue,
            out ParticleSystem effect)
        {
            effect = null;

            if (cue == null || cue.EffectPrefab == null)
            {
                return false;
            }

            PoolBucket bucket =
                GetOrCreateBucket(cue);

            if (bucket.Available.Count > 0)
            {
                effect = bucket.Available.Pop();
            }
            else
            {
                if (bucket.TotalCount >= cue.MaxPoolSize)
                {
                    return false;
                }

                CreateInstance(cue, bucket);
                effect = bucket.Available.Pop();
            }

            if (effect == null)
            {
                return false;
            }

            bucket.ActiveCount++;
            effect.gameObject.SetActive(false);
            return true;
        }

        /// <summary>
        /// Останавливает экземпляр и возвращает его в свободную часть пула.
        /// </summary>
        public void Return(
            ParticleEffectCue cue,
            ParticleSystem effect)
        {
            if (cue == null || effect == null ||
                !_owners.TryGetValue(effect, out ParticleEffectCue owner) ||
                owner != cue ||
                !_buckets.TryGetValue(cue, out PoolBucket bucket))
            {
                return;
            }

            effect.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            effect.transform.SetParent(
                poolRoot,
                false);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.identity;
            effect.transform.localScale = Vector3.one;
            effect.gameObject.SetActive(false);

            bucket.ActiveCount =
                Mathf.Max(0, bucket.ActiveCount - 1);
            bucket.Available.Push(effect);
        }

        /// <summary>
        /// Создаёт контейнер состояния пула и заранее подготавливает его начальные экземпляры.
        /// </summary>
        private PoolBucket GetOrCreateBucket(
            ParticleEffectCue cue)
        {
            if (_buckets.TryGetValue(cue, out PoolBucket bucket))
            {
                return bucket;
            }

            bucket = new PoolBucket();
            _buckets.Add(cue, bucket);

            int prewarmCount =
                Mathf.Min(
                    cue.InitialPoolSize,
                    cue.MaxPoolSize);

            for (int index = 0; index < prewarmCount; index++)
            {
                CreateInstance(cue, bucket);
            }

            return bucket;
        }

        /// <summary>
        /// Создаёт один неактивный экземпляр prefab и регистрирует его владельца.
        /// </summary>
        private ParticleSystem CreateInstance(
            ParticleEffectCue cue,
            PoolBucket bucket)
        {
            ParticleSystem instance =
                Instantiate(
                    cue.EffectPrefab,
                    poolRoot);

            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            ParticleSystem.MainModule main =
                instance.main;
            main.stopAction = ParticleSystemStopAction.None;

            instance.gameObject.SetActive(false);

            bucket.TotalCount++;
            _owners.Add(instance, cue);
            bucket.Available.Push(instance);

            return instance;
        }

        /// <summary>
        /// Освобождает ссылки на состояния пула перед уничтожением объекта сервиса.
        /// </summary>
        private void OnDestroy()
        {
            _owners.Clear();
            _buckets.Clear();
        }

        /// <summary>
        /// Содержит свободные экземпляры и счётчики одного типа визуального эффекта.
        /// </summary>
        private sealed class PoolBucket
        {
            public readonly Stack<ParticleSystem> Available =
                new Stack<ParticleSystem>();

            public int TotalCount;

            public int ActiveCount;
        }
    }
}
