using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/Test/" + nameof(TestRenderOtherShadow))]
    public class TestRenderOtherShadow : BasePass
    {
        struct OtherShadowInfo
        {
            public int lightIndex;
            public float strength;
            public float bias;
            public float normalBias;
            public bool isPoint;
        }
        int _OtherShadowMap = Shader.PropertyToID(nameof(_OtherShadowMap));
        int _OtherShadowMatrices = Shader.PropertyToID(nameof(_OtherShadowMatrices));
        int _OtherShadowData = Shader.PropertyToID(nameof(_OtherShadowData));
        int _OtherLightCount = Shader.PropertyToID(nameof(_OtherLightCount));

        Matrix4x4[] otherShadowMatrices;
        Vector4[] otherShadowData;

        const int atlasSize = 1024;
        const int maxShadowedLightCount = 16;
        OtherShadowInfo[] shadowInfos = new OtherShadowInfo[maxShadowedLightCount];



        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            Cmd.GetTemporaryRT(_OtherShadowMap, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            Cmd.SetRenderTarget(_OtherShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            Cmd.ClearRenderTarget(true, false, Color.clear);

            var shadowedLightCount = SetupShadowInfos();
            RenderShadowLights(shadowedLightCount);

            Cmd.SetGlobalInt(_OtherLightCount, shadowedLightCount);
        }

        void RenderShadowLights(int shadowedLightCount)
        {
            if(otherShadowData == null || otherShadowData.Length != maxShadowedLightCount)
            {
                otherShadowData = new Vector4[maxShadowedLightCount];
                otherShadowMatrices = new Matrix4x4[maxShadowedLightCount];
            }

            var split = shadowedLightCount <=1 ? 1 : shadowedLightCount <=4 ? 2 : 4;
            var tileSize = atlasSize / split;

            for (int i = 0; i < shadowedLightCount; i++)
            {
                var shadowInfo = shadowInfos[i];

                if (shadowInfo.isPoint)
                {
                    RenderPointShadow(shadowInfo,i,split,tileSize);
                }
                else
                {

                    RenderSpotShadow(shadowInfo, i, split, tileSize);
                }
            }

            Cmd.SetGlobalDepthBias(0, 0);
            Cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        private void RenderPointShadow(OtherShadowInfo shadowInfo,int id,int split,int tileSize)
        {
            
        }

        void RenderSpotShadow(OtherShadowInfo shadowInfo, int id, int split, int tileSize)
        {
            var settings = new ShadowDrawingSettings(cullingResults, shadowInfo.lightIndex);
            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowInfo.lightIndex,
                out var viewMatrix, out var projMatrix, out var splitData);
            settings.splitData = splitData;

            var offset = new Vector2(id % split, id / split);
            var viewportRect =new Rect(offset.x * tileSize,offset.y * tileSize, tileSize, tileSize);

            Cmd.SetViewport(viewportRect);
            Cmd.SetGlobalDepthBias(0, shadowInfo.bias);
            Cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);

            ExecuteCommand();
            context.DrawShadows(ref settings);
            //
            //otherShadowMatrices[id] = TestRenderDirShadow.ToTextureMatrix(projMatrix * viewMatrix, split, offset.x, offset.y);
            otherShadowMatrices[id] = SetupShadow.ConvertToShadowAtlasMatrix(projMatrix * viewMatrix, offset, split);
            otherShadowData[id] = new Vector4(id,0);
        }

        int SetupShadowInfos()
        {
            int shadowedOtherLightCount = 0;
            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var vLight = cullingResults.visibleLights[i];
                var isPoint = vLight.lightType == LightType.Point;
                if (!(isPoint || vLight.lightType == LightType.Spot))
                    continue;
                if (vLight.light.shadowStrength <= 0 || !cullingResults.GetShadowCasterBounds(i, out var bounds))
                    continue;
                if (shadowedOtherLightCount >= maxShadowedLightCount)
                    break;

                shadowInfos[shadowedOtherLightCount] = new OtherShadowInfo
                {
                    bias = vLight.light.shadowBias,
                    normalBias = vLight.light.shadowNormalBias,
                    lightIndex = i,
                    strength = vLight.light.shadowStrength,
                    isPoint = isPoint,
                };
                shadowedOtherLightCount++;
            }

            return shadowedOtherLightCount;
        }
    }
}
