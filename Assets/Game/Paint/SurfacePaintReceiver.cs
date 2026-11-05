using System.Collections.Generic;

using UnityEngine;

namespace Game.Paint
{
    /// <summary>
    /// Принимает попадания краской на обычной поверхности и показывает временные кляксы через ограниченный пул объектов.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurfacePaintReceiver : MonoBehaviour, IPaintReceiver
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [Header("Blob")]
        [SerializeField] private GameObject blobPrefab;
        [SerializeField] private Transform blobParent;
        [SerializeField, Min(1)] private int maxActiveBlobs = 16;
        [SerializeField, Min(0.1f)] private float blobLifetime = 10f;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.005f;
        [SerializeField, Min(0.001f)] private float minBlobSize = 0.15f;
        [SerializeField, Min(0.001f)] private float maxBlobSize = 0.3f;

        private readonly List<SurfaceBlobInstance> _blobPool = new();
        private bool _missingPrefabWarningShown;

        /// <summary>
        /// Находит контейнер клякс, если он не был назначен через Inspector.
        /// </summary>
        private void Awake()
        {
            if (blobParent == null)
            {
                blobParent = transform;
            }
        }

        /// <summary>
        /// Освобождает просроченные кляксы и возвращает их в пул.
        /// </summary>
        private void Update()
        {
            float currentTime =
                Time.time;

            for (int i = 0; i < _blobPool.Count; i++)
            {
                SurfaceBlobInstance instance =
                    _blobPool[i];

                if (!instance.IsActive ||
                    currentTime < instance.ExpirationTime)
                {
                    continue;
                }

                instance.Deactivate();
            }
        }

        /// <summary>
        /// Создаёт или переиспользует кляксу в точке столкновения с цветом полученного попадания.
        /// </summary>
        public void ReceivePaint(
            PaintImpactData impact)
        {
            if (blobPrefab == null)
            {
                LogMissingPrefabWarning();
                return;
            }

            SurfaceBlobInstance instance =
                GetAvailableBlob();

            if (instance == null)
            {
                return;
            }

            Vector3 normal =
                impact.Normal.sqrMagnitude > 0.0001f
                    ? impact.Normal.normalized
                    : Vector3.up;

            Quaternion surfaceRotation =
                Quaternion.LookRotation(-normal);

            Quaternion randomRotation =
                Quaternion.AngleAxis(
                    Random.Range(0f, 360f),
                    Vector3.forward
                );

            float size =
                Random.Range(
                    Mathf.Min(minBlobSize, maxBlobSize),
                    Mathf.Max(minBlobSize, maxBlobSize)
                );

            instance.Activate(
                impact.WorldPosition + normal * surfaceOffset,
                surfaceRotation * randomRotation,
                size,
                impact.Color,
                Time.time + blobLifetime
            );
        }

        /// <summary>
        /// Возвращает свободную кляксу, вытесняя самую старую при заполнении пула.
        /// </summary>
        private SurfaceBlobInstance GetAvailableBlob()
        {
            for (int i = 0; i < _blobPool.Count; i++)
            {
                if (!_blobPool[i].IsActive)
                {
                    return _blobPool[i];
                }
            }

            if (_blobPool.Count < Mathf.Max(1, maxActiveBlobs))
            {
                SurfaceBlobInstance newInstance =
                    CreateBlobInstance();

                if (newInstance != null)
                {
                    _blobPool.Add(newInstance);
                }

                return newInstance;
            }

            SurfaceBlobInstance oldest =
                _blobPool[0];

            for (int i = 1; i < _blobPool.Count; i++)
            {
                if (_blobPool[i].ExpirationTime <
                    oldest.ExpirationTime)
                {
                    oldest = _blobPool[i];
                }
            }

            oldest.Deactivate();
            return oldest;
        }

        /// <summary>
        /// Создаёт один неактивный экземпляр кляксы и кэширует его Renderer-компоненты.
        /// </summary>
        private SurfaceBlobInstance CreateBlobInstance()
        {
            GameObject instance =
                Instantiate(
                    blobPrefab,
                    blobParent
                );

            return new SurfaceBlobInstance(instance);
        }

        /// <summary>
        /// Показывает предупреждение об отсутствующем prefab только один раз.
        /// </summary>
        private void LogMissingPrefabWarning()
        {
            if (_missingPrefabWarningShown)
            {
                return;
            }

            _missingPrefabWarningShown = true;

            Debug.LogWarning(
                $"{name}: SurfacePaintReceiver blob prefab is not assigned."
            );
        }

        /// <summary>
        /// Отключает все созданные кляксы перед уничтожением получателя.
        /// </summary>
        private void OnDestroy()
        {
            for (int i = 0; i < _blobPool.Count; i++)
            {
                _blobPool[i].Destroy();
            }

            _blobPool.Clear();
        }

        /// <summary>
        /// Хранит один переиспользуемый объект кляксы и его MaterialPropertyBlock.
        /// </summary>
        private sealed class SurfaceBlobInstance
        {
            private readonly GameObject _gameObject;
            private readonly Renderer[] _renderers;
            private readonly MaterialPropertyBlock _propertyBlock;

            public bool IsActive { get; private set; }

            public float ExpirationTime { get; private set; }

            /// <summary>
            /// Создаёт запись пула и кэширует Renderer-компоненты prefab-объекта.
            /// </summary>
            public SurfaceBlobInstance(
                GameObject gameObject)
            {
                _gameObject = gameObject;
                _renderers =
                    gameObject.GetComponentsInChildren<Renderer>(true);
                _propertyBlock =
                    new MaterialPropertyBlock();

                IsActive = false;
                ExpirationTime = 0f;
                _gameObject.SetActive(false);
            }

            /// <summary>
            /// Размещает кляксу, устанавливает её цвет и включает объект на заданное время.
            /// </summary>
            public void Activate(
                Vector3 position,
                Quaternion rotation,
                float size,
                Color color,
                float expirationTime)
            {
                _gameObject.transform.SetPositionAndRotation(
                    position,
                    rotation
                );

                _gameObject.transform.localScale =
                    Vector3.one * size;

                for (int i = 0; i < _renderers.Length; i++)
                {
                    Renderer renderer =
                        _renderers[i];

                    renderer.GetPropertyBlock(
                        _propertyBlock
                    );

                    _propertyBlock.SetColor(
                        BaseColorId,
                        color
                    );

                    _propertyBlock.SetColor(
                        ColorId,
                        color
                    );

                    renderer.SetPropertyBlock(
                        _propertyBlock
                    );
                }

                ExpirationTime = expirationTime;
                IsActive = true;
                _gameObject.SetActive(true);
            }

            /// <summary>
            /// Возвращает объект кляксы в неактивное состояние пула.
            /// </summary>
            public void Deactivate()
            {
                IsActive = false;
                _gameObject.SetActive(false);
            }

            /// <summary>
            /// Уничтожает объект кляксы при уничтожении владельца пула.
            /// </summary>
            public void Destroy()
            {
                if (_gameObject != null)
                {
                    Object.Destroy(_gameObject);
                }
            }
        }
    }
}
