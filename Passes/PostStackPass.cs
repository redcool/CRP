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
        }

        [Range(0.1f,1)]public float bloomPrefilterRenderScale = 1;
        public string cameraTarget = "_CameraTarget";
        public Material postStackMaterial;

        [Range(0,16)]public int maxIterates = 16;
        [Min(1)]public int minSize = 1;
        [Min(0)]public float threshold;
        [Range(0,1)]public float thresholdKnee;
        [Min(0)] public float intensity;


        public static readonly int 
            _BloomPrefilterMap = Shader.PropertyToID(nameof(_BloomPrefilterMap)),
            _SourceTex = Shader.PropertyToID(nameof(_SourceTex)),
            _BloomThreshold = Shader.PropertyToID(nameof(_BloomThreshold)),
            _BloomIntensity = Shader.PropertyToID(nameof(_BloomIntensity))
            ;
        int _CameraTarget;
        int _BloomPyramid0;

        public override void Init()
        {
            base.Init();
            _CameraTarget = Shader.PropertyToID(cameraTarget);

            _BloomPyramid0 = Shader.PropertyToID(nameof(_BloomPyramid0));
            for (int i = 1; i < 16; i++)
            {
                Shader.PropertyToID("_BloomPyramid" + i);
            }
        }

        public override bool CanExecute()
        {
            return base.CanExecute() && postStackMaterial;
        }

        public override void OnRender()
        {
            int width = (int)(camera.pixelWidth * bloomPrefilterRenderScale);
            int height =(int)( camera.pixelHeight * bloomPrefilterRenderScale);

            Cmd.GetTemporaryRT(_BloomPrefilterMap,width,height,0,FilterMode.Bilinear,RenderTextureFormat.Default);

            var thresholdVec = new Vector4();
            thresholdVec.x = Mathf.GammaToLinearSpace(threshold);
            thresholdVec.y = thresholdVec.x * thresholdKnee;
            thresholdVec.z = 2 * thresholdVec.y;
            thresholdVec.w = 0.25f / (thresholdVec.y + 1e-5f);
            thresholdVec.y -= thresholdVec.x;

            Cmd.SetGlobalVector(_BloomThreshold, thresholdVec);

            Blit(_CameraTarget, _BloomPrefilterMap, postStackMaterial, (int)Pass.PreFilter);

            var fromId = _BloomPrefilterMap;
            var toId = _BloomPyramid0;

            DownSamples(width, height, fromId, toId,out var maxId,out var maxCount);

            UpSamples(maxId, maxCount);

            Cmd.SetGlobalFloat(_BloomIntensity, intensity);
            Blit(_BloomPyramid0, _CameraTarget, postStackMaterial, (int)Pass.Combine);

            Clean(maxId,maxCount);
        }

        void DownSamples(int width,int height,int fromId, int toId,out int maxId,out int maxCount)
        {
            maxId = maxCount= 0;
            width /= 2;
            height /= 2;

            Cmd.BeginSampleExecute(nameof(DownSamples), ref context);
            for (int i = 0; i < maxIterates-1; i++)
            {
                if(width< minSize || height< minSize) break;

                Cmd.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                Blit(fromId, toId, postStackMaterial, (int)Pass.Copy);

                fromId = toId;
                toId++;

                width /= 2; height /= 2;
                maxCount = i;
            }
            maxId = fromId;
            Cmd.EndSampleExecute(nameof(DownSamples), ref context);
        }

        void UpSamples(int maxId,int maxCount)
        {
            if(maxCount < 1)
            {
                return;
            }
            var fromId = maxId;
            var toId = fromId - 1;

            Cmd.BeginSampleExecute("UpSamples", ref context);
            for (int i = maxCount; i > 0; i--)
            {
                Blit(fromId, toId, postStackMaterial, (int)Pass.Copy);
                fromId -= 1;
                toId -= 1;
            }
            Cmd.EndSampleExecute("UpSamples", ref context);
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
