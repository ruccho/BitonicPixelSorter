using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ruccho.Utilities
{
    public class BitonicPixelSorter : MonoBehaviour
    {
        [SerializeField] private bool useAsImageEffect = true;
        [SerializeField] private ComputeShader shader = default;
        [SerializeField] private bool direction = true;
        [SerializeField] private bool ascending = true;
        [SerializeField, Range(0f, 1f)] private float thresholdMin = 0.4f;
        [SerializeField, Range(0f, 1f)] private float thresholdMax = 0.6f;

        private RenderTexture metaTex = default;
        private RenderTexture sortTex = default;

        private int metaPassIndex = default;
        private int sortPassIndex = default;

        private readonly int k_direction = Shader.PropertyToID("direction");
        private readonly int k_thresholdMin = Shader.PropertyToID("thresholdMin");
        private readonly int k_thresholdMax = Shader.PropertyToID("thresholdMax");
        private readonly int k_srcTex = Shader.PropertyToID("srcTex");
        private readonly int k_metaTex = Shader.PropertyToID("metaTex");
        private readonly int k_srcMetaTex = Shader.PropertyToID("srcMetaTex");
        private readonly int k_sortTex = Shader.PropertyToID("sortTex");
        private readonly int k_ordering = Shader.PropertyToID("ordering");
        private readonly int k_MaxLevels = Shader.PropertyToID("maxLevels");

        private bool isInitialized = false;

        private void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;
            
            metaPassIndex = shader.FindKernel("MetaPass");
            sortPassIndex = shader.FindKernel("SortPass");
        }
        
        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (useAsImageEffect)
            {
                Execute(src, dest);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        public void Execute(Texture src, RenderTexture dst)
        {
            Initialize();
            
            int width = src.width;
            int height = src.height;
            int size = direction ? width : height;
            int lines = direction ? height : width;

            if (size >= 2048)
            {
                Debug.LogError("[BitonicPixelSorter] Size of source texture must be smaller than 2048.");
                Graphics.Blit(src, dst);
                return;
            }

            EnsureBufferTextureSize(ref metaTex, width, height, RenderTextureFormat.RGFloat);
            EnsureBufferTextureSize(ref sortTex, width, height, RenderTextureFormat.ARGB32);

            shader.SetBool(k_direction, direction);
            shader.SetFloat(k_thresholdMin, thresholdMin);
            shader.SetFloat(k_thresholdMax, thresholdMax);
            
            shader.SetTexture(metaPassIndex, k_srcTex, src);
            shader.SetTexture(metaPassIndex, k_metaTex, metaTex);
            
            shader.GetKernelThreadGroupSizes(metaPassIndex, out uint metaGroupX, out uint metaGroupY, out uint metaGroupZ);
            uint metaGroupSize = metaGroupX * metaGroupY * metaGroupZ;
            int metaDispatchCount = Mathf.CeilToInt((float)lines * 2 / metaGroupSize);
            
            shader.Dispatch(metaPassIndex, metaDispatchCount, 1, 1);
            
            Graphics.Blit(src, sortTex);
            
            shader.SetTexture(sortPassIndex, k_srcMetaTex, metaTex);
            shader.SetTexture(sortPassIndex, k_sortTex, sortTex);
            shader.SetBool(k_ordering, ascending);

            int maxLevel = Mathf.CeilToInt(Mathf.Log(size, 2));
            
            shader.SetInt(k_MaxLevels, maxLevel);
            
            shader.Dispatch(sortPassIndex, lines, 1, 1);
            
            Graphics.Blit(sortTex, dst);
        }

        private static void EnsureBufferTextureSize(ref RenderTexture tex, int width, int height, RenderTextureFormat format)
        {
            if (!tex || tex.width != width || tex.height != height)
            {
                tex = new RenderTexture(width, height, 0, format);
                tex.enableRandomWrite = true;
                tex.Create();
                Debug.Log("[BitonicPixelSorter] RenderTexture created.");
            }
        }
    }
}