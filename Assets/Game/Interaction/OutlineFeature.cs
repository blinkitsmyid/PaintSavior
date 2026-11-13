using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Game.Interaction
{
    /// <summary>
    /// Создаёт экранную обводку объектов выбранного слоя через Render Graph URP.
    /// </summary>
    public sealed class OutlineFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private LayerMask _outlineLayerMask;

        [SerializeField]
        private Material _silhouetteMaterial;

        [SerializeField]
        private Material _outlineMaterial;

        [SerializeField]
        private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        private OutlinePass _outlinePass;

        /// <summary>
        /// Хранит данные рендеринга маски силуэтов для Render Graph.
        /// </summary>
        private sealed class SilhouettePassData
        {
            public RendererListHandle RendererList;
        }

        /// <summary>
        /// Хранит материал полноэкранной обводки для Render Graph.
        /// </summary>
        private sealed class FullscreenPassData
        {
            public Material Material;
        }

        /// <summary>
        /// Выполняет две связанные операции: рисует маску объектов и применяет обводку к цвету камеры.
        /// </summary>
        private sealed class OutlinePass : ScriptableRenderPass
        {
            private const string SilhouetteTextureName = "_OutlineRenderTexture";

            private static readonly int SilhouetteTextureId = Shader.PropertyToID(SilhouetteTextureName);

            private readonly LayerMask _layerMask;
            private readonly Material _silhouetteMaterial;
            private readonly Material _outlineMaterial;
            private readonly List<ShaderTagId> _shaderTagIds = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("LightweightForward")
            };

            /// <summary>
            /// Создаёт проход и сохраняет его настройки и материалы.
            /// </summary>
            public OutlinePass(LayerMask layerMask, Material silhouetteMaterial, Material outlineMaterial)
            {
                _layerMask = layerMask;
                _silhouetteMaterial = silhouetteMaterial;
                _outlineMaterial = outlineMaterial;
            }

            /// <summary>
            /// Создаёт список рендереров объектов слоя с материалом силуэта.
            /// </summary>
            private RendererListHandle CreateRendererList(
                RenderGraph renderGraph,
                ContextContainer frameData)
            {
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    _shaderTagIds,
                    renderingData,
                    cameraData,
                    lightData,
                    cameraData.defaultOpaqueSortFlags);
                drawingSettings.overrideMaterial = _silhouetteMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;

                FilteringSettings filteringSettings = new FilteringSettings(
                    RenderQueueRange.opaque,
                    _layerMask);
                RendererListParams rendererListParams = new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings);

                return renderGraph.CreateRendererList(rendererListParams);
            }

            /// <summary>
            /// Рисует выбранные объекты белым цветом в текстуру маски.
            /// </summary>
            private static void ExecuteSilhouettePass(
                SilhouettePassData data,
                RasterGraphContext context)
            {
                context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1.0f, 0);
                context.cmd.DrawRendererList(data.RendererList);
            }

            /// <summary>
            /// Запускает полноэкранный шейдер, который расширяет маску и оставляет только её внешний край.
            /// </summary>
            private static void ExecuteFullscreenPass(
                FullscreenPassData data,
                RasterGraphContext context)
            {
                Blitter.BlitTexture(context.cmd, new Vector4(1.0f, 1.0f, 0.0f, 0.0f), data.Material, 0);
            }

            /// <summary>
            /// Записывает операции маски и обводки в Render Graph текущего кадра.
            /// </summary>
            public override void RecordRenderGraph(
                RenderGraph renderGraph,
                ContextContainer frameData)
            {
                if (_silhouetteMaterial == null || _outlineMaterial == null)
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureDesc colorDescriptor = resourceData.activeColorTexture.GetDescriptor(renderGraph);
                colorDescriptor.name = SilhouetteTextureName;
                colorDescriptor.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;
                colorDescriptor.depthBufferBits = DepthBits.None;
                colorDescriptor.msaaSamples = MSAASamples.None;
                colorDescriptor.filterMode = FilterMode.Point;
                colorDescriptor.wrapMode = TextureWrapMode.Clamp;
                colorDescriptor.clearBuffer = true;
                colorDescriptor.clearColor = Color.clear;

                TextureHandle silhouetteTexture = renderGraph.CreateTexture(colorDescriptor);
                RendererListHandle rendererList = CreateRendererList(renderGraph, frameData);

                if (!rendererList.IsValid())
                {
                    return;
                }

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<SilhouettePassData>(
                           "Outline Silhouette",
                           out SilhouettePassData passData))
                {
                    passData.RendererList = rendererList;
                    builder.UseRendererList(rendererList);
                    builder.SetRenderAttachment(silhouetteTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(silhouetteTexture, SilhouetteTextureId);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (SilhouettePassData data, RasterGraphContext context) =>
                        ExecuteSilhouettePass(data, context));
                }

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<FullscreenPassData>(
                           "Outline Composite",
                           out FullscreenPassData passData))
                {
                    passData.Material = _outlineMaterial;
                    builder.UseTexture(silhouetteTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (FullscreenPassData data, RasterGraphContext context) =>
                        ExecuteFullscreenPass(data, context));
                }
            }
        }

        /// <summary>
        /// Создаёт Render Graph-проход и применяет к нему настройки из Inspector.
        /// </summary>
        public override void Create()
        {
            _outlinePass = new OutlinePass(_outlineLayerMask, _silhouetteMaterial, _outlineMaterial)
            {
                renderPassEvent = _renderPassEvent
            };
        }

        /// <summary>
        /// Добавляет проход обводки в текущую камеру, если обязательные ссылки настроены.
        /// </summary>
        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (_outlinePass == null || _silhouetteMaterial == null || _outlineMaterial == null)
            {
                return;
            }

            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            renderer.EnqueuePass(_outlinePass);
        }
    }
}
