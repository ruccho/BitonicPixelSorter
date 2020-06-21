
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Ruccho.Utilities
{
    public class PixelSortingBitonic : MonoBehaviour
    {
        [SerializeField] private bool useAsImageEffect = true;
        [SerializeField] private ComputeShader bitonicSortShader = default;

        [SerializeField] private float thresholdMin = 0.2f;
        [SerializeField] private float thresholdMax = 0.8f;

        public bool UseAsImageEffect
        {
            get => useAsImageEffect;
            set => useAsImageEffect = value;
        }

        public float ThresholdMin
        {
            get => thresholdMin;
            set => thresholdMin = value;
        }

        public float ThresholdMax
        {
            get => thresholdMax;
            set => thresholdMax = value;
        }

        const int BLOCK_SIZE = 1024;
        const int KERNEL_ID_BITONICSORT = 0;
        const int KERNEL_ID_THRESHOLDMASK = 1;

        private RenderTexture sortTex;
        private RenderTexture thresholdTex;

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

        private void Execute(Texture src, RenderTexture dest)
        {
            //Prepare buffers
            int w = src.width;
            int h = src.height;
            if (!sortTex || (sortTex.width != w || sortTex.height != h))
            {
                sortTex = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBFloat);
                sortTex.enableRandomWrite = true;
                sortTex.filterMode = FilterMode.Point;
                sortTex.Create();
            }
            
            if (!thresholdTex || (thresholdTex.width != w || thresholdTex.height != h))
            {
                thresholdTex = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBFloat);
                thresholdTex.enableRandomWrite = true;
                thresholdTex.filterMode = FilterMode.Point;
                thresholdTex.Create();
            }

            //Copy source texture to buffer texture for sorting
            Graphics.Blit(src, sortTex);

            //Copy source texture to buffer texture for applying threshold
            Graphics.Blit(src, thresholdTex);

            ComputeShader shader = bitonicSortShader;

            //Pass #1 - mark areas that meets the threshold
            
            shader.SetTexture(KERNEL_ID_THRESHOLDMASK, "dstTex", thresholdTex);

            shader.SetInt("_Width", (int) w);
            shader.SetInt("_Height", (int) h);
            shader.SetFloat("_ThresholdMin", thresholdMin);
            shader.SetFloat("_ThresholdMax", thresholdMax);

            shader.Dispatch(KERNEL_ID_THRESHOLDMASK, 1, h, 1);


            //Pass #2 - sort
            
            shader.SetTexture(KERNEL_ID_BITONICSORT, "dstTex", sortTex);
            shader.SetTexture(KERNEL_ID_BITONICSORT, "thrTex", thresholdTex);

            int maxLevel = (int) Mathf.Pow(2f, Mathf.CeilToInt(Mathf.Log(w, 2f)));

            shader.SetInt("_LevelMax", maxLevel);
            
            shader.Dispatch(KERNEL_ID_BITONICSORT, 1, h, 1);
            
            /*for (uint level = 2; level <= maxLevel; level <<= 1)
            {
                shader.SetInt("_Level", (int) level);
                shader.SetInt("_LevelMax", maxLevel);
                shader.SetInt("_Width",  w);
                shader.SetInt("_Height",  h);

                shader.Dispatch(KERNEL_ID_BITONICSORT, 1, h, 1);
            }*/

            Graphics.Blit(sortTex, dest);
        }

    }
}