using PowerUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/" + nameof(PostStackPass))]
    public class PostStackPass : BasePass
    {
        public enum Pass
        {
            PreFilter=0,
            Copy = 1,
            Combine = 2,
            Horizontal = 3,
            Vertical = 4,
        }
        [Header("Bloom")]
        [Range(0.1f,1)]public float bloomPrefilterRenderScale = 1;
        public string cameraTarget = "_CameraTarget";
        public Material postStackMaterial;

        const int MAX_ITERATORS = 16;
        [Range(0, MAX_ITERATORS)]public int maxIterates = MAX_ITERATORS;
        [Min(2)]public int minSize = 2;
        [Min(0)]public float threshold = 0.3f;
        [Range(0,1)]public float thresholdKnee = 0;
        [Min(0)] public float intensity = 10;
        public bool useGaussianBlur;
        public bool isCombineBicubicFilter;


        public static readonly int 
            _BloomPrefilterMap = Shader.PropertyToID(nameof(_BloomPrefilterMap)),
            _SourceTex = Shader.PropertyToID(nameof(_SourceTex)),
            _SourceTex_Texel= Shader.PropertyToID(nameof(_SourceTex_Texel)),
            _SourceTex2 = Shader.PropertyToID(nameof(_SourceTex2)),
            _BloomThreshold = Shader.PropertyToID(nameof(_BloomThreshold)),
            _BloomIntensity = Shader.PropertyToID(nameof(_BloomIntensity)),
            _BloomCombineBicubicFilter = Shader.PropertyToID(nameof(_BloomCombineBicubicFilter))
            ;
        int _CameraTarget;
        int _BloomPyramid0;

        public override void Init()
        {
            base.Init();
            _CameraTarget = Shader.PropertyToID(cameraTarget);

            _BloomPyramid0 = Shader.PropertyToID(nameof(_BloomPyramid0));
            for (int i = 1; i < MAX_ITERATORS * 2; i++)
            {
                Shader.PropertyToID("_BloomPyramid" + i);
            }
        }

        public override bool CanExecute()
        {
            return base.CanExecute() && postStackMaterial && maxIterates>0;
        }

        public override void OnRender()
        {
            int width = (int)(camera.pixelWidth * bloomPrefilterRenderScale);
            int height = (int)(camera.pixelHeight * bloomPrefilterRenderScale);

            Cmd.GetTemporaryRT(_BloomPrefilterMap, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);

            var thresholdVec = new Vector4();
            thresholdVec.x = Mathf.GammaToLinearSpace(threshold);
            thresholdVec.y = thresholdVec.x * thresholdKnee;
            thresholdVec.z = 2 * thresholdVec.y;
            thresholdVec.w = 0.25f / (thresholdVec.y + 1e-5f);
            thresholdVec.y -= thresholdVec.x;

            Cmd.SetGlobalVector(_BloomThreshold, thresholdVec);
            Blit(_CameraTarget, _BloomPrefilterMap, postStackMaterial, (int)Pass.PreFilter);
            //Blit(_BloomPrefilterMap, _CameraTarget, postStackMaterial, (int)Pass.Copy);
            //return;
            var fromId = _BloomPrefilterMap;
            var toId = _BloomPyramid0;

            int maxCount = 0, maxId = 0, lastId = 0;

            if (useGaussianBlur)
            {
                DownSamplesGaussian(width, height, fromId, toId, out maxId, out maxCount);
            }
            else
            {
                DownSamples(width, height, fromId, toId, out maxId, out maxCount);
            }
            //lastId = maxId;

            UpSamples(maxId, maxCount, out lastId, useGaussianBlur ? 2 : 1);

            Cmd.SetGlobalFloat(_BloomIntensity, intensity);
            Cmd.SetGlobalFloat(_BloomCombineBicubicFilter, isCombineBicubicFilter?1:0);
            Cmd.SetGlobalTexture(_SourceTex2, "_CameraTexture");
            Blit(lastId, _CameraTarget, postStackMaterial, (int)Pass.Combine);

            Clean(maxId, maxCount);
        }

        void DownSamples(int width,int height,int fromId, int toId,out int maxId,out int maxCount)
        {
            maxId = maxCount= 0;
            width /= 2;
            height /= 2;

            Cmd.BeginSampleExecute(nameof(DownSamples), ref context);
            for (int i = 0; i < maxIterates; i++)
            {
                if(width< minSize || height< minSize) 
                    break;

                Cmd.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                Blit(fromId, toId, postStackMaterial, (int)Pass.Copy);

                fromId = toId;
                toId++;

                width /= 2; height /= 2;
                maxCount++;
            }
            maxId = fromId;
            Cmd.EndSampleExecute(nameof(DownSamples), ref context);
        }


        void DownSamplesGaussian(int width, int height, int fromId, int toId, out int maxId, out int maxCount)
        {
            maxId = maxCount = 0;
            width /= 2;
            height /= 2;

            toId++;

            Cmd.BeginSampleExecute(nameof(DownSamplesGaussian), ref context);
            for (int i = 0; i < maxIterates; i++)
            {
                if (width < minSize || height < minSize)
                    break;

                var midId = toId - 1;
                Cmd.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                Cmd.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                Cmd.SetGlobalVector(_SourceTex_Texel, new Vector4(1f / width, 1f / height, width, height));
                Blit(fromId, midId, postStackMaterial, (int)Pass.Horizontal);
                Blit(midId, toId, postStackMaterial, (int)Pass.Vertical);

                fromId = toId;
                toId += 2;

                width /= 2; height /= 2;
                maxCount ++;
            }
            maxId = fromId;
            Cmd.EndSampleExecute(nameof(DownSamplesGaussian), ref context);
        }

        void UpSamples(int maxId, int maxCount, out int lastId, int stepCount=1)
        {
            lastId = maxId;
            
            // 0 is prefileter pass, no use, so 1 is last
            const int LAST_COUNT = 1;
            if (maxCount <= LAST_COUNT)
                return;

            var fromId = maxId;
            var toId = fromId - stepCount;

            Cmd.BeginSampleExecute(nameof(UpSamples), ref context);

            for (int i = maxCount; i > LAST_COUNT; i--)
            {
                Cmd.SetGlobalTexture(_SourceTex2, toId - 1);
                Blit(fromId, toId, postStackMaterial, (int)Pass.Combine);
                fromId -= stepCount;
                toId -= stepCount;
            }
            Cmd.EndSampleExecute(nameof(UpSamples), ref context);
            lastId = toId + stepCount;
        }

        private void Clean(int maxId,int maxCount)
        {
            for (int i = maxCount-1; i >= 0; i--)
            {
                Cmd.ReleaseTemporaryRT(maxId--);
            }
        }

        public void Blit(int sourceId,int targetId,Material mat, int pass)
        {
            Cmd.SetGlobalTexture(_SourceTex, sourceId);
            Cmd.SetRenderTarget(targetId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            Cmd.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Triangles, 3);
        }
    }
}
