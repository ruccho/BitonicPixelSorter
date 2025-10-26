using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if BPS_URP_17
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Ruccho.Utilities
{
    public sealed class BitonicPixelSortingFeature : ScriptableRendererFeature
    {
        [SerializeField] private ComputeShader shader;
        [SerializeField] private bool direction = true;
        [SerializeField] private bool ascending = true;
        [SerializeField] [Range(0f, 1f)] private float thresholdMin = 0.4f;
        [SerializeField] [Range(0f, 1f)] private float thresholdMax = 0.6f;

        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
        private BitonicPixelSortingPass _pass;

        public override void Create()
        {
            _pass = new BitonicPixelSortingPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _pass.renderPassEvent = passEvent;
            _pass.Shader = shader;
            _pass.Direction = direction;
            _pass.Ascending = ascending;
            _pass.ThresholdMin = thresholdMin;
            _pass.ThresholdMax = thresholdMax;

            renderer.EnqueuePass(_pass);
        }

        private class BitonicPixelSortingPass : ScriptableRenderPass
        {
            private const string Tag = nameof(BitonicPixelSortingPass);

            private static readonly int PropDirection = UnityEngine.Shader.PropertyToID("direction");
            private static readonly int PropMaxLevels = UnityEngine.Shader.PropertyToID("maxLevels");
            private static readonly int PropMetaTex = UnityEngine.Shader.PropertyToID("metaTex");
            private static readonly int PropOrdering = UnityEngine.Shader.PropertyToID("ordering");
            private static readonly int PropSortTex = UnityEngine.Shader.PropertyToID("sortTex");
            private static readonly int PropSrcMetaTex = UnityEngine.Shader.PropertyToID("srcMetaTex");
            private static readonly int PropSrcTex = UnityEngine.Shader.PropertyToID("srcTex");
            private static readonly int PropThresholdMax = UnityEngine.Shader.PropertyToID("thresholdMax");
            private static readonly int PropThresholdMin = UnityEngine.Shader.PropertyToID("thresholdMin");
            public bool Ascending = true;
            public bool Direction = true;

            public ComputeShader Shader;
            public float ThresholdMax = 0.6f;
            public float ThresholdMin = 0.4f;

#if BPS_URP_17
            [Obsolete(
                "This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.",
                false)]
#endif
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get(Tag);

                var metaPassIndex = Shader.FindKernel("MetaPass");
                var sortPassIndex = Shader.FindKernel("SortPass");

                var desc = renderingData.cameraData.cameraTargetDescriptor;


                var width = desc.width;
                var height = desc.height;
                var size = Direction ? width : height;
                var lines = Direction ? height : width;

                var metaWidth = Direction ? width / 2 : width;
                var metaHeight = Direction ? height : height / 2;

                if (size >= 2048)
                {
                    Debug.LogError("[BitonicPixelSorter] Size of source texture must be smaller than 2048.");
                    return;
                }

                var renderer = renderingData.cameraData.renderer;
                var src = renderer.cameraColorTargetHandle;

                cmd.GetTemporaryRT(PropMetaTex, metaWidth, metaHeight, 0, FilterMode.Point, RenderTextureFormat.RInt,
                    RenderTextureReadWrite.Default, 1, true);
                cmd.GetTemporaryRT(PropSortTex, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default, 1, true);

                cmd.SetComputeIntParam(Shader, PropDirection, Direction ? 1 : 0);
                cmd.SetComputeFloatParam(Shader, PropThresholdMin, ThresholdMin);
                cmd.SetComputeFloatParam(Shader, PropThresholdMax, ThresholdMax);

                cmd.SetComputeTextureParam(Shader, metaPassIndex, PropSrcTex, src);
                cmd.SetComputeTextureParam(Shader, metaPassIndex, PropMetaTex, new RenderTargetIdentifier(PropMetaTex));

                Shader.GetKernelThreadGroupSizes(metaPassIndex, out var metaGroupX, out var metaGroupY,
                    out var metaGroupZ);
                var metaGroupSize = metaGroupX * metaGroupY * metaGroupZ;
                var metaDispatchCount = Mathf.CeilToInt((float)lines * 2 / metaGroupSize);

                cmd.DispatchCompute(Shader, metaPassIndex, metaDispatchCount, 1, 1);

                cmd.Blit(src, new RenderTargetIdentifier(PropSortTex));

                cmd.SetComputeTextureParam(Shader, sortPassIndex, PropSrcMetaTex,
                    new RenderTargetIdentifier(PropMetaTex));
                cmd.SetComputeTextureParam(Shader, sortPassIndex, PropSortTex, new RenderTargetIdentifier(PropSortTex));

                cmd.SetComputeIntParam(Shader, PropOrdering, Ascending ? 1 : 0);

                var maxLevel = Mathf.CeilToInt(Mathf.Log(size, 2));

                cmd.SetComputeIntParam(Shader, PropMaxLevels, maxLevel);

                cmd.DispatchCompute(Shader, sortPassIndex, lines, 1, 1);

                cmd.Blit(new RenderTargetIdentifier(PropSortTex), src);

                context.ExecuteCommandBuffer(cmd);
                cmd.ReleaseTemporaryRT(PropMetaTex);
                cmd.ReleaseTemporaryRT(PropSortTex);
                CommandBufferPool.Release(cmd);
            }

#if BPS_URP_17

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourcesData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                var desc = cameraData.cameraTargetDescriptor;

                var width = desc.width;
                var height = desc.height;
                var size = Direction ? width : height;
                var lines = Direction ? height : width;

                var metaWidth = Direction ? width / 2 : width;
                var metaHeight = Direction ? height : height / 2;

                var metaTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
                    new RenderTextureDescriptor(metaWidth, metaHeight, RenderTextureFormat.RInt)
                    {
                        enableRandomWrite = true
                    },
                    "_BpsMetaTex", false);
                var sortTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
                    new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32)
                    {
                        enableRandomWrite = true
                    },
                    "_BpsSortTex", false);

                var cameraTarget = resourcesData.activeColorTexture;

                var source = cameraTarget;

                using (var builder =
                       renderGraph.AddComputePass<MetaPassData>("BitonicPixelSorter MetaPass", out var passData,
                           profilingSampler))
                {
                    builder.UseTexture(source);
                    builder.UseTexture(metaTex, AccessFlags.WriteAll);

                    passData.Shader = Shader;
                    passData.Direction = Direction;
                    passData.Lines = lines;
                    passData.ThresholdMax = ThresholdMax;
                    passData.ThresholdMin = ThresholdMin;
                    passData.SrcTex = source;
                    passData.MetaTex = metaTex;

                    builder.SetRenderFunc<MetaPassData>(static (passData, context) =>
                    {
                        var cmd = context.cmd;
                        var shader = passData.Shader;
                        var direction = passData.Direction;
                        var lines = passData.Lines;
                        var thresholdMax = passData.ThresholdMax;
                        var thresholdMin = passData.ThresholdMin;

                        var metaPassIndex = shader.FindKernel("MetaPass");

                        cmd.SetComputeIntParam(shader, PropDirection, direction ? 1 : 0);
                        cmd.SetComputeFloatParam(shader, PropThresholdMin, thresholdMin);
                        cmd.SetComputeFloatParam(shader, PropThresholdMax, thresholdMax);

                        cmd.SetComputeTextureParam(shader, metaPassIndex, PropSrcTex, passData.SrcTex);
                        cmd.SetComputeTextureParam(shader, metaPassIndex, PropMetaTex, passData.MetaTex);

                        shader.GetKernelThreadGroupSizes(metaPassIndex, out var metaGroupX, out var metaGroupY,
                            out var metaGroupZ);
                        var metaGroupSize = metaGroupX * metaGroupY * metaGroupZ;
                        var metaDispatchCount = Mathf.CeilToInt((float)lines * 2 / metaGroupSize);

                        cmd.DispatchCompute(shader, metaPassIndex, metaDispatchCount, 1, 1);
                    });
                }

                using (var builder =
                       renderGraph.AddRasterRenderPass<BlitPassData>("BitonicPixelSorter Source Blit", out var passData,
                           profilingSampler))
                {
                    builder.UseTexture(source);
                    builder.SetRenderAttachment(sortTex, 0, AccessFlags.WriteAll);

                    passData.Source = source;

                    builder.SetRenderFunc<BlitPassData>(static (passData, context) =>
                    {
                        Blitter.BlitTexture(context.cmd, passData.Source, new Vector4(1f, 1f, 0f, 0f), 0f, false);
                    });
                }

                using (var builder =
                       renderGraph.AddComputePass<SortPassData>("BitonicPixelSorter SortPass", out var passData,
                           profilingSampler))
                {
                    builder.UseTexture(metaTex);
                    builder.UseTexture(sortTex, AccessFlags.ReadWrite);

                    passData.Shader = Shader;
                    passData.Ascending = Ascending;
                    passData.Direction = Direction;
                    passData.Lines = lines;
                    passData.Size = size;
                    passData.ThresholdMax = ThresholdMax;
                    passData.ThresholdMin = ThresholdMin;
                    passData.MetaTex = metaTex;
                    passData.SortTex = sortTex;

                    builder.SetRenderFunc<SortPassData>(static (passData, context) =>
                    {
                        var cmd = context.cmd;
                        var shader = passData.Shader;
                        var direction = passData.Direction;
                        var ascending = passData.Ascending;
                        var lines = passData.Lines;
                        var size = passData.Size;
                        var thresholdMax = passData.ThresholdMax;
                        var thresholdMin = passData.ThresholdMin;

                        var sortPassIndex = shader.FindKernel("SortPass");

                        cmd.SetComputeIntParam(shader, PropDirection, direction ? 1 : 0);
                        cmd.SetComputeFloatParam(shader, PropThresholdMin, thresholdMin);
                        cmd.SetComputeFloatParam(shader, PropThresholdMax, thresholdMax);

                        cmd.SetComputeTextureParam(shader, sortPassIndex, PropSrcMetaTex, passData.MetaTex);
                        cmd.SetComputeTextureParam(shader, sortPassIndex, PropSortTex, passData.SortTex);

                        cmd.SetComputeIntParam(shader, PropOrdering, ascending ? 1 : 0);

                        var maxLevel = Mathf.CeilToInt(Mathf.Log(size, 2));

                        cmd.SetComputeIntParam(shader, PropMaxLevels, maxLevel);

                        cmd.DispatchCompute(shader, sortPassIndex, lines, 1, 1);
                    });
                }

                using (var builder =
                       renderGraph.AddRasterRenderPass<BlitPassData>("BitonicPixelSorter Destination Blit",
                           out var passData,
                           profilingSampler))
                {
                    builder.UseTexture(sortTex);
                    builder.SetRenderAttachment(source, 0, AccessFlags.WriteAll);

                    passData.Source = sortTex;

                    builder.SetRenderFunc<BlitPassData>(static (passData, context) =>
                    {
                        Blitter.BlitTexture(context.cmd, passData.Source, new Vector4(1f, 1f, 0f, 0f), 0f, false);
                    });
                }
            }

            private class MetaPassData
            {
                public bool Direction;
                public int Lines;
                public TextureHandle MetaTex;
                public ComputeShader Shader;

                public TextureHandle SrcTex;
                public float ThresholdMax;
                public float ThresholdMin;
            }

            private class SortPassData
            {
                public bool Ascending;
                public bool Direction;
                public int Lines;

                public TextureHandle MetaTex;
                public ComputeShader Shader;
                public int Size;
                public TextureHandle SortTex;
                public float ThresholdMax;
                public float ThresholdMin;
            }

            private class BlitPassData
            {
                public TextureHandle Source;
            }

#endif
        }
    }
}