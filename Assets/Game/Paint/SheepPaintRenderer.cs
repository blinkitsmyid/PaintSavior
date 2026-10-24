using UnityEngine;

namespace Game.Paint
{
    [RequireComponent(typeof(SheepPaintController))]
    public sealed class SheepPaintRenderer : MonoBehaviour
    {
        private const string CoverageStampShaderName =
            "Hidden/Game/PaintCoverageStamp";

        private static readonly int PaintBlobMaskId =
            Shader.PropertyToID("_PaintBlobMask");

        private static readonly int PaintCoverageTextureId =
            Shader.PropertyToID("_PaintCoverageTexture");

        private static readonly int StampDataId =
            Shader.PropertyToID("_StampData");

        private static readonly int StampColorId =
            Shader.PropertyToID("_StampColor");

        [Header("Paint Mask")]
        [SerializeField] private int paintTextureSize = 256;

        private SheepPaintController _paintController;
        private MaterialPropertyBlock _propertyBlock;
        private Renderer _renderer;
        private Material _stampMaterial;
        private RenderTexture _paintTexture;
        private RenderTexture _paintScratchTexture;

        /// <summary>
        /// Находит зависимости и создаёт ресурсы постоянной UV-маски овцы.
        /// </summary>
        private void Awake()
        {
            _paintController =
                GetComponent<SheepPaintController>();

            _renderer =
                GetComponentInChildren<Renderer>();

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
        }

        /// <summary>
        /// Отписывается от событий контроллера при отключении рендерера.
        /// </summary>
        private void OnDisable()
        {
            if (_paintController == null)
            {
                return;
            }

            _paintController.BlobCreated -= OnBlobCreated;
            _paintController.BlobRemoved -= OnBlobRemoved;
        }

        /// <summary>
        /// Восстанавливает маску из текущего глобального списка пятен после запуска объекта.
        /// </summary>
        private void Start()
        {
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
            ReleasePaintTexture(_paintTexture);
            ReleasePaintTexture(_paintScratchTexture);

            if (_stampMaterial != null)
            {
                Destroy(_stampMaterial);
            }
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
