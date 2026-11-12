using UnityEngine;
using UnityEngine.Rendering;

using Game.Sheep.AI;

namespace Game.Paint
{
    [RequireComponent(typeof(SheepPaintController))]
    public sealed class SheepPaintRenderer : MonoBehaviour
    {
        private const string CoverageStampShaderName =
            "Hidden/Game/PaintCoverageStamp";

        private const string PaintShaderName =
            "Game/Paint/SheepPaint";

        private static readonly int PaintBlobMaskId =
            Shader.PropertyToID("_PaintBlobMask");

        private static readonly int PaintCoverageTextureId =
            Shader.PropertyToID("_PaintCoverageTexture");

        private static readonly int PaintSpreadTextureId =
            Shader.PropertyToID("_PaintSpreadTexture");

        private static readonly int PaintSpreadProgressId =
            Shader.PropertyToID("_PaintSpreadProgress");

        private static readonly int StampDataId =
            Shader.PropertyToID("_StampData");

        private static readonly int StampColorId =
            Shader.PropertyToID("_StampColor");

        [Header("Paint Mask")]
        [SerializeField] private int paintTextureSize = 256;

        private SheepPaintController _paintController;
        private SheepPaintCoverage _paintCoverage;
        private SheepWoolGrowth _woolGrowth;
        private MaterialPropertyBlock _propertyBlock;
        private Renderer _renderer;
        private Material _stampMaterial;
        private RenderTexture _paintTexture;
        private RenderTexture _paintScratchTexture;
        private bool _coverageReadbackInFlight;
        private bool _coverageReadbackQueued;
        private bool _isDestroying;

        /// <summary>
        /// Возвращает фактический размер текстур, используемый Renderer для маски покраски.
        /// </summary>
        public int PaintTextureSize =>
            Mathf.Max(32, paintTextureSize);

        /// <summary>
        /// Находит зависимости и создаёт ресурсы постоянной UV-маски овцы.
        /// </summary>
        private void Awake()
        {
            _paintController =
                GetComponent<SheepPaintController>();

            _paintCoverage =
                GetComponent<SheepPaintCoverage>();

            _woolGrowth =
                GetComponent<SheepWoolGrowth>();

            _renderer =
                FindActivePaintRenderer();

            _propertyBlock =
                new MaterialPropertyBlock();

            if (_renderer == null)
            {
                Debug.LogWarning(
                    $"{name}: paint surface Renderer is missing."
                );

                return;
            }

            CreateStampMaterial();
            CreatePaintTextures();
        }

        /// <summary>
        /// Подписывается на добавление и удаление пятен для изменения UV-маски.
        /// </summary>
        private void OnEnable()
        {
            if (_paintController == null)
            {
                return;
            }

            _paintController.BlobCreated += OnBlobCreated;
            _paintController.BlobRemoved += OnBlobRemoved;

            if (_woolGrowth != null)
            {
                _woolGrowth.StageChanged += OnWoolStageChanged;
            }
        }

        /// <summary>
        /// Отписывается от событий контроллера при отключении рендерера.
        /// </summary>
        private void OnDisable()
        {
            if (_paintController != null)
            {
                _paintController.BlobCreated -= OnBlobCreated;
                _paintController.BlobRemoved -= OnBlobRemoved;
            }

            if (_woolGrowth != null)
            {
                _woolGrowth.StageChanged -= OnWoolStageChanged;
            }
        }

        /// <summary>
        /// Восстанавливает маску из текущего глобального списка пятен после запуска объекта.
        /// </summary>
        private void Start()
        {
            RefreshActiveRenderer();
            RebuildPaintTexture();
        }

        /// <summary>
        /// Обновляет ссылку на активную визуальную модель с шейдером покраски.
        /// </summary>
        private void RefreshActiveRenderer()
        {
            Renderer activeRenderer =
                FindActivePaintRenderer();

            if (activeRenderer == null)
            {
                Debug.LogWarning(
                    $"{name}: active renderer with shader " +
                    $"'{PaintShaderName}' is missing.",
                    this
                );
                return;
            }

            _renderer = activeRenderer;
            UpdateStampMaterialMask();
        }

        /// <summary>
        /// Находит активный Renderer, у которого хотя бы один материал использует шейдер покраски.
        /// </summary>
        private Renderer FindActivePaintRenderer()
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer candidate = renderers[i];

                if (!candidate.gameObject.activeInHierarchy ||
                    !UsesPaintShader(candidate))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        /// <summary>
        /// Проверяет, содержит ли Renderer материал с шейдером покраски.
        /// </summary>
        private static bool UsesPaintShader(
            Renderer candidate)
        {
            Material[] materials = candidate.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material != null &&
                    material.shader != null &&
                    material.shader.name == PaintShaderName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Обновляет визуальную поверхность и повторно применяет накопленные кляксы после смены шерсти.
        /// </summary>
        private void OnWoolStageChanged(
            WoolGrowthStage previousStage,
            WoolGrowthStage nextStage)
        {
            RefreshActiveRenderer();
            RebuildPaintTexture();
        }

        /// <summary>
        /// Наносит новое пятно в постоянную UV-маску.
        /// </summary>
        private void OnBlobCreated(
            PaintHit hit,
            PaintBlobData blob)
        {
            StampBlob(blob);
            ApplyPaintTextureToSurface();
            RequestCoverageReadback();
        }

        /// <summary>
        /// Пересобирает маску после удаления пятна, чтобы корректно убрать его вклад.
        /// </summary>
        private void OnBlobRemoved(
            PaintBlobData blob)
        {
            RebuildPaintTexture();
        }

        /// <summary>
        /// Создаёт материал, который наносит одно пятно поверх предыдущего состояния маски.
        /// </summary>
        private void CreateStampMaterial()
        {
            Shader stampShader =
                Shader.Find(CoverageStampShaderName);

            if (stampShader == null)
            {
                Debug.LogError(
                    $"{name}: shader '{CoverageStampShaderName}' was not found."
                );

                return;
            }

            _stampMaterial =
                new Material(stampShader)
                {
                    name = $"{name}_PaintCoverageStamp",
                    hideFlags = HideFlags.HideAndDontSave
                };

            Texture blobMask =
                _renderer.sharedMaterial != null
                    ? _renderer.sharedMaterial.GetTexture(PaintBlobMaskId)
                    : null;

            _stampMaterial.SetTexture(
                PaintBlobMaskId,
                blobMask != null
                    ? blobMask
                    : Texture2D.whiteTexture
            );
        }

        /// <summary>
        /// Обновляет текстуру формы кляксы из текущего активного материала.
        /// </summary>
        private void UpdateStampMaterialMask()
        {
            if (_stampMaterial == null || _renderer == null)
            {
                return;
            }

            Texture blobMask = null;
            Material[] materials = _renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null ||
                    !material.HasProperty(PaintBlobMaskId))
                {
                    continue;
                }

                blobMask = material.GetTexture(PaintBlobMaskId);
                break;
            }

            _stampMaterial.SetTexture(
                PaintBlobMaskId,
                blobMask != null
                    ? blobMask
                    : Texture2D.whiteTexture
            );
        }

        /// <summary>
        /// Создаёт две одинаковые текстуры для безопасного ping-pong нанесения пятен.
        /// </summary>
        private void CreatePaintTextures()
        {
            int textureSize =
                Mathf.Max(32, paintTextureSize);

            _paintTexture =
                CreatePaintTexture(
                    $"{name}_PaintMask_A",
                    textureSize
                );

            _paintScratchTexture =
                CreatePaintTexture(
                    $"{name}_PaintMask_B",
                    textureSize
                );

            ClearRenderTexture(_paintTexture);
            ClearRenderTexture(_paintScratchTexture);
        }

        /// <summary>
        /// Создаёт одну линейную ARGB-текстуру для хранения цвета и степени покрытия.
        /// </summary>
        private RenderTexture CreatePaintTexture(
            string textureName,
            int textureSize)
        {
            RenderTexture texture =
                new RenderTexture(
                    textureSize,
                    textureSize,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear
                )
                {
                    name = textureName,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };

            texture.Create();
            return texture;
        }

        /// <summary>
        /// Наносит одно пятно поверх текущей текстуры и меняет активную сторону ping-pong пары.
        /// </summary>
        private void StampBlob(
            PaintBlobData blob)
        {
            if (_stampMaterial == null ||
                _paintTexture == null ||
                _paintScratchTexture == null)
            {
                return;
            }

            _stampMaterial.SetVector(
                StampDataId,
                new Vector4(
                    blob.UV.x,
                    blob.UV.y,
                    blob.Size,
                    blob.Rotation * Mathf.Deg2Rad
                )
            );

            _stampMaterial.SetColor(
                StampColorId,
                blob.Color
            );

            Graphics.Blit(
                _paintTexture,
                _paintScratchTexture,
                _stampMaterial
            );

            SwapPaintTextures();
        }

        /// <summary>
        /// Пересобирает постоянную маску из всех пятен в глобальном порядке нанесения.
        /// </summary>
        private void RebuildPaintTexture()
        {
            if (_paintController == null ||
                _paintTexture == null ||
                _paintScratchTexture == null)
            {
                return;
            }

            ClearRenderTexture(_paintTexture);
            ClearRenderTexture(_paintScratchTexture);

            for (int i = 0; i < _paintController.BlobsInOrder.Count; i++)
            {
                StampBlob(
                    _paintController.BlobsInOrder[i]
                );
            }

            ApplyPaintTextureToSurface();
            RequestCoverageReadback();
        }

        /// <summary>
        /// Передаёт текущую текстуру покрытия в материал поверхности через MaterialPropertyBlock.
        /// </summary>
        private void ApplyPaintTextureToSurface()
        {
            if (_renderer == null || _paintTexture == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(
                _propertyBlock
            );

            _propertyBlock.SetTexture(
                PaintCoverageTextureId,
                _paintTexture
            );

            _renderer.SetPropertyBlock(
                _propertyBlock
            );
        }

        /// <summary>
        /// Передаёт целевую карту и степень её смешивания в активный материал овцы.
        /// </summary>
        public void ApplySpreadVisual(
            Texture spreadTexture,
            float progress)
        {
            if (_renderer == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(
                _propertyBlock
            );

            _propertyBlock.SetTexture(
                PaintSpreadTextureId,
                spreadTexture
            );

            _propertyBlock.SetFloat(
                PaintSpreadProgressId,
                Mathf.Clamp01(progress)
            );

            _renderer.SetPropertyBlock(
                _propertyBlock
            );
        }

        /// <summary>
        /// Отключает визуальное распространение и возвращает отображение к отдельным кляксам.
        /// </summary>
        public void ResetSpreadVisual()
        {
            ApplySpreadVisual(
                Texture2D.blackTexture,
                0f
            );
        }

        /// <summary>
        /// Очищает RenderTexture прозрачным цветом и восстанавливает предыдущую активную текстуру.
        /// </summary>
        private void ClearRenderTexture(
            RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }

            RenderTexture previous =
                RenderTexture.active;

            RenderTexture.active = texture;
            GL.Clear(
                true,
                true,
                Color.clear
            );
            RenderTexture.active = previous;
        }

        /// <summary>
        /// Меняет местами исходную и промежуточную текстуры маски.
        /// </summary>
        private void SwapPaintTextures()
        {
            RenderTexture temporary =
                _paintTexture;

            _paintTexture =
                _paintScratchTexture;

            _paintScratchTexture =
                temporary;
        }

        /// <summary>
        /// Освобождает графические ресурсы маски и временного материала при уничтожении объекта.
        /// </summary>
        private void OnDestroy()
        {
            _isDestroying = true;

            ReleasePaintTexture(_paintTexture);
            ReleasePaintTexture(_paintScratchTexture);

            if (_stampMaterial != null)
            {
                Destroy(_stampMaterial);
            }

        }

        /// <summary>
        /// Запрашивает асинхронное чтение альфа-канала текущей маски покрытия.
        /// </summary>
        private void RequestCoverageReadback()
        {
            if (_paintCoverage == null ||
                _paintTexture == null ||
                _isDestroying)
            {
                return;
            }

            if (_coverageReadbackInFlight)
            {
                _coverageReadbackQueued = true;
                return;
            }

            _coverageReadbackInFlight = true;

            AsyncGPUReadback.Request(
                _paintTexture,
                0,
                TextureFormat.RGBA32,
                OnCoverageReadbackCompleted
            );
        }

        /// <summary>
        /// Обрабатывает результат чтения маски и передаёт рассчитанное покрытие компоненту состояния.
        /// </summary>
        private void OnCoverageReadbackCompleted(
            AsyncGPUReadbackRequest request)
        {
            _coverageReadbackInFlight = false;

            if (!_isDestroying &&
                !request.hasError &&
                _paintCoverage != null)
            {
                var data =
                    request.GetData<byte>();

                float coverage =
                    CalculateCoverage(data);

                _paintCoverage.SetCoverage(coverage);
            }

            if (!_coverageReadbackQueued || _isDestroying)
            {
                _coverageReadbackQueued = false;
                return;
            }

            _coverageReadbackQueued = false;
            RequestCoverageReadback();
        }

        /// <summary>
        /// Вычисляет долю пикселей с ненулевым альфа-каналом в RGBA32-маске.
        /// </summary>
        private static float CalculateCoverage(
            Unity.Collections.NativeArray<byte> data)
        {
            const int BytesPerPixel = 4;
            const byte AlphaCutoff = 2;

            int pixelCount =
                data.Length / BytesPerPixel;

            if (pixelCount == 0)
            {
                return 0f;
            }

            int coveredPixelCount = 0;

            for (int i = 3; i < data.Length; i += BytesPerPixel)
            {
                if (data[i] > AlphaCutoff)
                {
                    coveredPixelCount++;
                }
            }

            return coveredPixelCount /
                (float)pixelCount;
        }

        /// <summary>
        /// Освобождает одну RenderTexture, если она была создана.
        /// </summary>
        private void ReleasePaintTexture(
            RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }

            texture.Release();
            Destroy(texture);
        }
    }
}
