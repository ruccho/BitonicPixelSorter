using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        private BitonicPixelSortingPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new BitonicPixelSortingPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.renderPassEvent = passEvent;
            m_ScriptablePass.shader = shader;
            m_ScriptablePass.direction = direction;
            m_ScriptablePass.ascending = ascending;
            m_ScriptablePass.thresholdMin = thresholdMin;
            m_ScriptablePass.thresholdMax = thresholdMax;

            renderer.EnqueuePass(m_ScriptablePass);
        }

        private class BitonicPixelSortingPass : ScriptableRenderPass
        {
            private const string Tag = nameof(BitonicPixelSortingPass);

            private readonly int k_direction = Shader.PropertyToID("direction");
            private readonly int k_MaxLevels = Shader.PropertyToID("maxLevels");
            private readonly int k_metaTex = Shader.PropertyToID("metaTex");
            private readonly int k_ordering = Shader.PropertyToID("ordering");
            private readonly int k_sortTex = Shader.PropertyToID("sortTex");
            private readonly int k_srcMetaTex = Shader.PropertyToID("srcMetaTex");
            private readonly int k_srcTex = Shader.PropertyToID("srcTex");
            private readonly int k_thresholdMax = Shader.PropertyToID("thresholdMax");
            private readonly int k_thresholdMin = Shader.PropertyToID("thresholdMin");
            public bool ascending = true;
            public bool direction = true;

            public ComputeShader shader;
            public float thresholdMax = 0.6f;
            public float thresholdMin = 0.4f;

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get(Tag);

                var metaPassIndex = shader.FindKernel("MetaPass");
                var sortPassIndex = shader.FindKernel("SortPass");

                var desc = renderingData.cameraData.cameraTargetDescriptor;


                var width = desc.width;
                var height = desc.height;
                var size = direction ? width : height;
                var lines = direction ? height : width;

                if (size >= 2048)
                {
                    Debug.LogError("[BitonicPixelSorter] Size of source texture must be smaller than 2048.");
                    return;
                }

                var renderer = renderingData.cameraData.renderer;
                var src = renderer.cameraColorTargetHandle;

                cmd.GetTemporaryRT(k_metaTex, width, height, 0, FilterMode.Point, RenderTextureFormat.RInt,
                    RenderTextureReadWrite.Default, 1, true);
                cmd.GetTemporaryRT(k_sortTex, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default, 1, true);

                cmd.SetComputeIntParam(shader, k_direction, direction ? 1 : 0);
                cmd.SetComputeFloatParam(shader, k_thresholdMin, thresholdMin);
                cmd.SetComputeFloatParam(shader, k_thresholdMax, thresholdMax);

                cmd.SetComputeTextureParam(shader, metaPassIndex, k_srcTex, src);
                cmd.SetComputeTextureParam(shader, metaPassIndex, k_metaTex, new RenderTargetIdentifier(k_metaTex));

                shader.GetKernelThreadGroupSizes(metaPassIndex, out var metaGroupX, out var metaGroupY,
                    out var metaGroupZ);
                var metaGroupSize = metaGroupX * metaGroupY * metaGroupZ;
                var metaDispatchCount = Mathf.CeilToInt((float)lines * 2 / metaGroupSize);

                cmd.DispatchCompute(shader, metaPassIndex, metaDispatchCount, 1, 1);

                cmd.Blit(src, new RenderTargetIdentifier(k_sortTex));

                cmd.SetComputeTextureParam(shader, sortPassIndex, k_srcMetaTex, new RenderTargetIdentifier(k_metaTex));
                cmd.SetComputeTextureParam(shader, sortPassIndex, k_sortTex, new RenderTargetIdentifier(k_sortTex));

                cmd.SetComputeIntParam(shader, k_ordering, ascending ? 1 : 0);

                var maxLevel = Mathf.CeilToInt(Mathf.Log(size, 2));

                cmd.SetComputeIntParam(shader, k_MaxLevels, maxLevel);

                cmd.DispatchCompute(shader, sortPassIndex, lines, 1, 1);

                cmd.Blit(new RenderTargetIdentifier(k_sortTex), src);

                context.ExecuteCommandBuffer(cmd);
                cmd.ReleaseTemporaryRT(k_metaTex);
                cmd.ReleaseTemporaryRT(k_sortTex);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}