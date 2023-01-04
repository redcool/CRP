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
            PreFilter,
            Copy,
            Combine,
            Horizontal,
            Vertical,

            CombineScatter,
            CombineScatterFinal,
        }
        public enum BloomMode
        {
            Add,
            Scatter
        }

        [Header("Bloom Targets")]
        public string cameraSourceName = "_CameraTarget";
        public string cameraTargetName = "_PostCameraTarget";

        [HideInInspector]public PostStackSettings defaultPostStackSettings;

        PostStackSettings PostStackSettings
        {
            get
            {
                var cameraData = camera.GetComponent<CRPCameraData>();
                if (cameraData && cameraData.postStackSettings)
                    return cameraData.postStackSettings;

                return defaultPostStackSettings;
            }
        }

        float bloomPrefilterRenderScale => PostStackSettings.bloomPrefilterRenderScale;
        Material postStackMaterial => PostStackSettings.PostStackMaterial;

        int maxIterates => PostStackSettings.maxIterates;
        int minSize => PostStackSettings.minSize;

        float threshold => PostStackSettings.threshold;
        float thresholdKnee => PostStackSettings.thresholdKnee;
        float maxLuma => PostStackSettings.maxLuma;

        bool isHdr=>PostStackSettings.isHdr;
        BloomMode bloomMode=>PostStackSettings.bloomMode;
        float intensity => PostStackSettings.intensity;
        float scatter => PostStackSettings.scatter;

        bool useGaussianBlur=>PostStackSettings.useGaussianBlur;
        bool isCombineBicubicFilter=>PostStackSettings.isCombineBicubicFilter;

        public static readonly int 
            _BloomPrefilterMap = Shader.PropertyToID(nameof(_BloomPrefilterMap)),
            _SourceTex = Shader.PropertyToID(nameof(_SourceTex)),
            _SourceTex_Texel= Shader.PropertyToID(nameof(_SourceTex_Texel)),
            _SourceTex2 = Shader.PropertyToID(nameof(_SourceTex2)),
            _BloomThreshold = Shader.PropertyToID(nameof(_BloomThreshold)),
            _BloomIntensity = Shader.PropertyToID(nameof(_BloomIntensity)),
            _BloomFinalIntensity = Shader.PropertyToID(nameof(_BloomFinalIntensity)),
            _BloomCombineBicubicFilter = Shader.PropertyToID(nameof(_BloomCombineBicubicFilter)),
            _FinalSrcMode = Shader.PropertyToID(nameof(_FinalSrcMode)),
            _FinalDstMode = Shader.PropertyToID(nameof(_FinalDstMode))
            ;

        int 
            _CameraTarget,
            _CameraSource,
            _BloomPyramid0
            ;

        void SetupShaderIDs()
        {
            _CameraTarget = Shader.PropertyToID(cameraTargetName);
            _CameraSource = Shader.PropertyToID(cameraSourceName);

            _BloomPyramid0 = Shader.PropertyToID(nameof(_BloomPyramid0));
            for (int i = 1; i < PostStackSettings.MAX_ITERATORS * 2; i++)
            {
                Shader.PropertyToID("_BloomPyramid" + i);
            }

            var colorFormat = PostStackSettings.isHdr? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            Cmd.GetTemporaryRT(_CameraTarget, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear,colorFormat);
        }


        RenderTextureFormat GetTextureFormat() => isHdr ? RenderTextureFormat.DefaultHDR: RenderTextureFormat.Default;

        public override bool IsNeedCameraCleanup() => true;
        public override void CameraCleanup()
        {
            Cmd.ReleaseTemporaryRT(_CameraTarget);
        }

        public override bool CanExecute()
        {
            return base.CanExecute() && PostStackSettings && PostStackSettings.PostStackMaterial;
        }

        public override void OnRender()
        {
            SetupShaderIDs();

            if(maxIterates == 0)
            {
                Cmd.BlitTriangle(_CameraSource, _CameraTarget, postStackMaterial, (int)Pass.Copy);
                return;
            }

            int width = (int)(camera.pixelWidth * bloomPrefilterRenderScale);
            int height = (int)(camera.pixelHeight * bloomPrefilterRenderScale);

            BloomPrefilter(width, height);
            //Cmd.BlitTriangle(_BloomPrefilterMap, _CameraTarget, postStackMaterial, (int)Pass.Copy);
            //return;
            var fromId = _BloomPrefilterMap;
            var toId = _BloomPyramid0;

            int maxCount = 0, maxId = 0;

            if (useGaussianBlur)
            {
                DownSamplesGaussian(width, height, fromId, toId, out maxId, out maxCount);
            }
            else
            {
                DownSamples(width, height, fromId, toId, out maxId, out maxCount);
            }

            int lastId = maxId;
            UpSamples(maxId, maxCount, out lastId, useGaussianBlur ? 2 : 1);

            CombineBloom(lastId);

            Clean(maxId, maxCount);
        }

        private void BloomPrefilter(int width, int height)
        {
            Cmd.GetTemporaryRT(_BloomPrefilterMap, width, height, 0, FilterMode.Bilinear, GetTextureFormat());

            var thresholdVec = new Vector4();
            thresholdVec.x = Mathf.GammaToLinearSpace(threshold);
            thresholdVec.y = thresholdVec.x * thresholdKnee;
            thresholdVec.z = 2 * thresholdVec.y;
            //thresholdVec.w = 0.25f / (thresholdVec.y + 1e-5f);
            //thresholdVec.y -= thresholdVec.x;
            thresholdVec.w = maxLuma;

            Cmd.SetGlobalVector(_BloomThreshold, thresholdVec);
            Cmd.BlitTriangle(_CameraSource, _BloomPrefilterMap, postStackMaterial, (int)Pass.PreFilter);
        }

        void CombineBloom(int lastId)
        {
            var finalPass = GetCombineFinalPassAndFinalIntensity(out var finalIntensity);

            Cmd.SetGlobalFloat(_BloomIntensity, finalIntensity);
            Cmd.SetGlobalFloat(_BloomCombineBicubicFilter, isCombineBicubicFilter ? 1 : 0);
            Cmd.SetGlobalTexture(_SourceTex2, _CameraSource);
            Cmd.BlitTriangle(lastId, _CameraTarget, postStackMaterial, (int)finalPass);
        }

        Pass GetCombineFinalPassAndFinalIntensity(out float finalIntensity)
        {
            finalIntensity = intensity;
            if (bloomMode == BloomMode.Add)
            {
                return Pass.Combine;
            }
            finalIntensity = Mathf.Min(1,intensity);
            return Pass.CombineScatterFinal;
        }

        Pass GetCombinePassAndIntensity(out float bloomIntensity)
        {
            bloomIntensity = 1;

            var combinePass = Pass.Combine;
            if (bloomMode == BloomMode.Scatter)
            {
                combinePass = Pass.CombineScatter;
                bloomIntensity = scatter;
            }

            return combinePass;
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

                Cmd.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, GetTextureFormat());
                Cmd.BlitTriangle(fromId, toId, postStackMaterial, (int)Pass.Copy);

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
                Cmd.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, GetTextureFormat());
                Cmd.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, GetTextureFormat());
                Cmd.SetGlobalVector(_SourceTex_Texel, new Vector4(1f / width, 1f / height, width, height));
                Cmd.BlitTriangle(fromId, midId, postStackMaterial, (int)Pass.Horizontal);
                Cmd.BlitTriangle(midId, toId, postStackMaterial, (int)Pass.Vertical);

                fromId = toId;
                toId += 2;

                width /= 2; height /= 2;
                maxCount ++;
            }
            maxId = fromId;
            Cmd.EndSampleExecute(nameof(DownSamplesGaussian), ref context);
        }

        void UpSamples(int maxId, int maxCount, out int lastId, int stepCount = 1)
        {
            lastId = maxId;

            // 0 is prefileter pass, no use, so 1 is last
            const int LAST_COUNT = 1;
            if (maxCount <= LAST_COUNT)
                return;

            var fromId = maxId;
            var toId = fromId - stepCount;

            Pass combinePass = GetCombinePassAndIntensity(out var bloomIntensity);
            Cmd.SetGlobalFloat(_BloomIntensity, bloomIntensity);

            Cmd.BeginSampleExecute(nameof(UpSamples), ref context);

            for (int i = maxCount; i > LAST_COUNT; i--)
            {
                Cmd.SetGlobalTexture(_SourceTex2, toId + 1);
                Cmd.BlitTriangle(fromId, toId, postStackMaterial, (int)combinePass);
                fromId -= stepCount;
                toId -= stepCount;
            }
            Cmd.EndSampleExecute(nameof(UpSamples), ref context);
            lastId = toId + stepCount;
        }


        private void Clean(int maxId,int maxCount)
        {
            Cmd.ReleaseTemporaryRT(_BloomPrefilterMap);
            for (int i = maxCount-1; i >= 0; i--)
            {
                Cmd.ReleaseTemporaryRT(maxId--);
            }
        }

    }
}
