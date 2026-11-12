using UnityEngine;

namespace Game.Paint
{
    /// <summary>
    /// Создаёт целевую UV-текстуру, в которой поверхность получает цвет ближайшей кляксы.
    /// </summary>
    [RequireComponent(typeof(SheepPaintController))]
    [RequireComponent(typeof(SheepPaintCoverage))]
    public sealed class SheepPaintSpreadRenderer : MonoBehaviour
    {
        private const string SpreadShaderName =
            "Hidden/Game/PaintSpread";

        private const int MaxSpreadBlobs = 64;

        private static readonly int SpreadBlobCountId =
            Shader.PropertyToID("_SpreadBlobCount");

        private static readonly int SpreadBlobDataId =
            Shader.PropertyToID("_SpreadBlobData");

        private static readonly int SpreadBlobColorsId =
            Shader.PropertyToID("_SpreadBlobColors");

        [Header("References")]
        [SerializeField] private SheepPaintRenderer paintRenderer;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float spreadDuration = 1.5f;

        private SheepPaintController _paintController;
        private SheepPaintCoverage _paintCoverage;
        private Material _spreadMaterial;
        private RenderTexture _spreadTexture;
        private readonly Vector4[] _spreadBlobData =
            new Vector4[MaxSpreadBlobs];
        private readonly Vector4[] _spreadBlobColors =
            new Vector4[MaxSpreadBlobs];
        private float _spreadProgress;
        private bool _spreadTransitionActive;

        /// <summary>
        /// Возвращает созданную целевую текстуру распространения краски.
        /// </summary>
        public RenderTexture SpreadTexture => _spreadTexture;

        /// <summary>
        /// Находит зависимости и создаёт материал с целевой RenderTexture.
        /// </summary>
        private void Awake()
        {
            _paintController =
                GetComponent<SheepPaintController>();

            _paintCoverage =
                GetComponent<SheepPaintCoverage>();

            if (paintRenderer == null)
            {
                paintRenderer =
                    GetComponent<SheepPaintRenderer>();
            }

            CreateSpreadMaterial();
            CreateSpreadTexture();
        }

        /// <summary>
        /// Подписывается на изменения покрытия и списка пятен.
        /// </summary>
        private void OnEnable()
        {
            if (_paintController != null)
            {
                _paintController.BlobCreated += OnBlobCreated;
                _paintController.BlobRemoved += OnBlobRemoved;
            }

            if (_paintCoverage != null)
            {
                _paintCoverage.CoverageThresholdReached +=
                    OnCoverageThresholdReached;
            }
        }

        /// <summary>
        /// Отписывается от событий при отключении компонента.
        /// </summary>
        private void OnDisable()
        {
            if (_paintController != null)
            {
                _paintController.BlobCreated -= OnBlobCreated;
                _paintController.BlobRemoved -= OnBlobRemoved;
            }

            if (_paintCoverage != null)
            {
                _paintCoverage.CoverageThresholdReached -=
                    OnCoverageThresholdReached;
            }
        }

        /// <summary>
        /// Восстанавливает целевую карту, если порог уже был достигнут до запуска компонента.
        /// </summary>
        private void Start()
        {
            if (_paintCoverage != null &&
                _paintCoverage.IsThresholdReached)
            {
                RebuildSpreadTexture();
                StartSpreadTransition();
                return;
            }

            ResetSpreadVisual();
        }

        /// <summary>
        /// Плавно увеличивает степень распространения после достижения порога покрытия.
        /// </summary>
        private void Update()
        {
            if (!_spreadTransitionActive)
            {
                return;
            }

            if (spreadDuration <= 0f)
            {
                _spreadProgress = 1f;
            }
            else
            {
                _spreadProgress =
                    Mathf.MoveTowards(
                        _spreadProgress,
                        1f,
                        Time.deltaTime / spreadDuration
                    );
            }

            ApplySpreadVisual();

            if (_spreadProgress >= 1f)
            {
                _spreadTransitionActive = false;
            }
        }

        /// <summary>
        /// Перестраивает карту после добавления новой кляксы, если заполнение уже запущено.
        /// </summary>
        private void OnBlobCreated(
            PaintHit hit,
            PaintBlobData blob)
        {
            if (_paintCoverage == null ||
                !_paintCoverage.IsThresholdReached)
            {
                return;
            }

            RebuildSpreadTexture();
            ApplySpreadVisual();
        }

        /// <summary>
        /// Перестраивает или очищает карту после удаления кляксы.
        /// </summary>
        private void OnBlobRemoved(
            PaintBlobData blob)
        {
            if (_paintController == null ||
                _paintController.BlobsInOrder.Count == 0)
            {
                ClearSpreadTexture();
                return;
            }

            if (_paintCoverage != null &&
                _paintCoverage.IsThresholdReached)
            {
                RebuildSpreadTexture();
                ApplySpreadVisual();
            }
        }

        /// <summary>
        /// Создаёт целевую карту при первом достижении порога покрытия.
        /// </summary>
        private void OnCoverageThresholdReached(
            float coverage)
        {
            RebuildSpreadTexture();
            StartSpreadTransition();
        }

        /// <summary>
        /// Создаёт материал скрытого шейдера распространения краски.
        /// </summary>
        private void CreateSpreadMaterial()
        {
            Shader spreadShader =
                Shader.Find(SpreadShaderName);

            if (spreadShader == null)
            {
                Debug.LogError(
                    $"{name}: shader '{SpreadShaderName}' was not found."
                );

                return;
            }

            _spreadMaterial =
                new Material(spreadShader)
                {
                    name = $"{name}_PaintSpread",
                    hideFlags = HideFlags.HideAndDontSave
                };
        }

        /// <summary>
        /// Создаёт целевую текстуру с тем же разрешением, что и текущая маска покраски.
        /// </summary>
        private void CreateSpreadTexture()
        {
            int textureSize =
                paintRenderer != null
                    ? paintRenderer.PaintTextureSize
                    : 256;

            _spreadTexture =
                new RenderTexture(
                    textureSize,
                    textureSize,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear
                )
                {
                    name = $"{name}_PaintSpread",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };

            _spreadTexture.Create();
            ClearSpreadTexture();
        }

        /// <summary>
        /// Передаёт кляксы в GPU и строит карту цвета ближайшей кляксы для каждого UV-пикселя.
        /// </summary>
        private void RebuildSpreadTexture()
        {
            if (_spreadMaterial == null ||
                _spreadTexture == null ||
                _paintController == null)
            {
                return;
            }

            int totalBlobCount =
                _paintController.BlobsInOrder.Count;

            if (totalBlobCount == 0)
            {
                ClearSpreadTexture();
                return;
            }

            int blobCount =
                Mathf.Min(
                    totalBlobCount,
                    MaxSpreadBlobs
                );

            int firstBlobIndex =
                totalBlobCount - blobCount;

            for (int i = 0; i < blobCount; i++)
            {
                PaintBlobData blob =
                    _paintController.BlobsInOrder[
                        firstBlobIndex + i
                    ];

                _spreadBlobData[i] =
                    new Vector4(
                        blob.UV.x,
                        blob.UV.y,
                        blob.Size,
                        0f
                    );

                _spreadBlobColors[i] =
                    new Vector4(
                        blob.Color.r,
                        blob.Color.g,
                        blob.Color.b,
                        1f
                    );
            }

            _spreadMaterial.SetInt(
                SpreadBlobCountId,
                blobCount
            );

            _spreadMaterial.SetVectorArray(
                SpreadBlobDataId,
                _spreadBlobData
            );

            _spreadMaterial.SetVectorArray(
                SpreadBlobColorsId,
                _spreadBlobColors
            );

            Graphics.Blit(
                Texture2D.blackTexture,
                _spreadTexture,
                _spreadMaterial
            );
        }

        /// <summary>
        /// Очищает целевую карту прозрачным цветом.
        /// </summary>
        private void ClearSpreadTexture()
        {
            if (_spreadTexture == null)
            {
                return;
            }

            RenderTexture previous =
                RenderTexture.active;

            RenderTexture.active =
                _spreadTexture;

            GL.Clear(
                true,
                true,
                Color.clear
            );

            RenderTexture.active = previous;

            ResetSpreadVisual();
        }

        /// <summary>
        /// Запускает переход от исходных клякс к целевой карте распространения.
        /// </summary>
        private void StartSpreadTransition()
        {
            _spreadProgress = 0f;
            _spreadTransitionActive = true;
            ApplySpreadVisual();
        }

        /// <summary>
        /// Передаёт текущую целевую карту и прогресс перехода активному Renderer овцы.
        /// </summary>
        private void ApplySpreadVisual()
        {
            if (paintRenderer == null)
            {
                return;
            }

            paintRenderer.ApplySpreadVisual(
                _spreadTexture,
                _spreadProgress
            );
        }

        /// <summary>
        /// Возвращает поверхность к отображению отдельных клякс без распространения.
        /// </summary>
        private void ResetSpreadVisual()
        {
            _spreadProgress = 0f;
            _spreadTransitionActive = false;

            if (paintRenderer != null)
            {
                paintRenderer.ResetSpreadVisual();
            }
        }

        /// <summary>
        /// Освобождает материал и RenderTexture при уничтожении компонента.
        /// </summary>
        private void OnDestroy()
        {
            if (_spreadTexture != null)
            {
                _spreadTexture.Release();
                Destroy(_spreadTexture);
            }

            if (_spreadMaterial != null)
            {
                Destroy(_spreadMaterial);
            }
        }
    }
}
