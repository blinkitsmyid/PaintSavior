using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Описывает объект, который может временно попадать в Render Graph-маску обводки.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OutlineTarget : MonoBehaviour
    {
        [Header("Renderers")]
        [Tooltip("Renderer объекта и его дочерних визуальных частей, которые должны получать обводку.")]
        [SerializeField]
        private Renderer[] targetRenderers;

        [Header("Outline Layer")]
        [Tooltip("Выберите ровно один слой, который назначен OutlineFeature в Renderer Data.")]
        [SerializeField]
        private LayerMask outlineLayerMask;

        private int[] _originalLayers;
        private bool _isHighlighted;
        private bool _hasValidOutlineLayer;

        /// <summary>
        /// Показывает, переведены ли Renderer цели на слой обводки.
        /// </summary>
        public bool IsHighlighted => _isHighlighted;

        /// <summary>
        /// Находит Renderer цели и сохраняет их исходные слои до первого наведения.
        /// </summary>
        private void Awake()
        {
            targetRenderers = ResolveTargetRenderers();
            _originalLayers = new int[targetRenderers.Length];

            for (int index = 0; index < targetRenderers.Length; index++)
            {
                Renderer targetRenderer = targetRenderers[index];
                _originalLayers[index] =
                    targetRenderer == null
                        ? -1
                        : targetRenderer.gameObject.layer;
            }

            _hasValidOutlineLayer = TryGetOutlineLayer(out _);
        }

        /// <summary>
        /// Возвращает Renderer из Inspector или автоматически собирает Renderer дочерних объектов.
        /// </summary>
        private Renderer[] ResolveTargetRenderers()
        {
            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                return targetRenderers;
            }

            return GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>
        /// Включает или выключает участие цели в экранной маске обводки.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted)
            {
                return;
            }

            if (highlighted)
            {
                if (!_hasValidOutlineLayer || !TryGetOutlineLayer(out int outlineLayer))
                {
                    Debug.LogError(
                        $"{name}: назначьте ровно один слой в Outline Layer Mask.",
                        this);
                    return;
                }

                ApplyOutlineLayer(outlineLayer);
            }
            else
            {
                RestoreOriginalLayers();
            }

            _isHighlighted = highlighted;
        }

        /// <summary>
        /// Переводит Renderer цели на слой, который отрисовывается OutlineFeature.
        /// </summary>
        private void ApplyOutlineLayer(int outlineLayer)
        {
            for (int index = 0; index < targetRenderers.Length; index++)
            {
                Renderer targetRenderer = targetRenderers[index];
                if (targetRenderer != null)
                {
                    targetRenderer.gameObject.layer = outlineLayer;
                }
            }
        }

        /// <summary>
        /// Возвращает Renderer цели на слои, которые были сохранены при запуске.
        /// </summary>
        private void RestoreOriginalLayers()
        {
            for (int index = 0; index < targetRenderers.Length; index++)
            {
                Renderer targetRenderer = targetRenderers[index];
                int originalLayer = _originalLayers[index];

                if (targetRenderer != null && originalLayer >= 0)
                {
                    targetRenderer.gameObject.layer = originalLayer;
                }
            }
        }

        /// <summary>
        /// Проверяет, что в маске выбран ровно один слой, и возвращает его индекс.
        /// </summary>
        private bool TryGetOutlineLayer(out int outlineLayer)
        {
            int mask = outlineLayerMask.value;
            outlineLayer = -1;

            if (mask == 0 || (mask & (mask - 1)) != 0)
            {
                return false;
            }

            for (int layer = 0; layer < 32; layer++)
            {
                if ((mask & (1 << layer)) != 0)
                {
                    outlineLayer = layer;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Возвращает Renderer цели на исходные слои перед отключением компонента.
        /// </summary>
        private void OnDisable()
        {
            if (_isHighlighted)
            {
                RestoreOriginalLayers();
                _isHighlighted = false;
            }
        }
    }
}
