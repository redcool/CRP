using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace PowerUtilities
{

    public struct LightShadowInfo
    {
        public int lightIndex;
        public float shadowBias;
        public float shadowNearPlane;
        public float shadowStrength;
        public int cascadeId;
        public float shadowNormalBias;
    }

    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(SetupLighting))]
    public class SetupLighting : BasePass
    {
        public static readonly int _DirectionalLightCount = Shader.PropertyToID(nameof(_DirectionalLightCount));
        public static readonly int _DirectionalLightColors = Shader.PropertyToID(nameof(_DirectionalLightColors));
        public static readonly int _DirectionalLightDirections = Shader.PropertyToID(nameof(_DirectionalLightDirections));

        public static readonly int _DirectionalShadowAtlas = Shader.PropertyToID(nameof(_DirectionalShadowAtlas));

        Vector4[] dirLightColors;
        Vector4[] dirLightDirections;

        int shadowedDirLightCount = 0;
        LightShadowInfo[] dirLightShadowInfos;


        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            SetupLights();

            shadowedDirLightCount = SetupShadows();
            RenderShadows();
        }

        void SetupLights()
        {
            var maxLightCount = CRP.Asset.lightSettings.maxLightCount;

            if (dirLightColors == null || dirLightColors.Length ==0)
            {
                dirLightColors = new Vector4[maxLightCount];
                dirLightDirections = new Vector4[maxLightCount];
            }

            var vLights = cullingResults.visibleLights;
            int i = 0;
            for (; i < vLights.Length; i++)
            {
                var vlight = vLights[i];
                SetupLight(ref vlight, i);

                if (i >= maxLightCount)
                    break;
            }

            Cmd.SetGlobalVectorArray(_DirectionalLightDirections, dirLightDirections);
            Cmd.SetGlobalVectorArray(_DirectionalLightColors, dirLightColors);
            Cmd.SetGlobalInt(_DirectionalLightCount, i);
        }

        void SetupLight(ref VisibleLight vlight, int id)
        {
            dirLightColors[id] = vlight.finalColor.linear;
            dirLightDirections[id] = -vlight.localToWorldMatrix.GetColumn(2);
        }

        int SetupShadows()
        {
            var maxLightCount = CRP.Asset.lightSettings.maxLightCount;
            var maxShadowedDirLightCount = CRP.Asset.lightSettings.maxShadowedDirLightCount;

            if (dirLightShadowInfos == null || dirLightShadowInfos.Length != maxShadowedDirLightCount)
            {
                dirLightShadowInfos = new LightShadowInfo[maxShadowedDirLightCount];
            }

            var shadowedDirLightCount = 0;

            var vLights = cullingResults.visibleLights;
            for (var i= 0 ; i < vLights.Length; i++)
            {
                var vlight = vLights[i];
                if(SetupShadowInfo(vlight.light,i, out var shadowInfo))
                {
                    dirLightShadowInfos[shadowedDirLightCount++] = shadowInfo;
                }

                if (i >= maxLightCount || shadowedDirLightCount >= maxShadowedDirLightCount)
                    break;
            }
            return shadowedDirLightCount;
        }

        public bool SetupShadowInfo(Light light, int id,out LightShadowInfo shadowInfo)
        {
            var isNotValidShadowLight =
                light.shadows == LightShadows.None ||
                light.shadowStrength <= 0 ||
                !cullingResults.GetShadowCasterBounds(id, out var bounds)
                ;

            shadowInfo = new LightShadowInfo
            {
                lightIndex = id,
                shadowBias = light.shadowBias,
                shadowNearPlane = light.shadowNearPlane,
                shadowStrength = light.shadowStrength,
                cascadeId = 0,
                shadowNormalBias = light.shadowNormalBias,
            };
            return !isNotValidShadowLight;
        }

        public void RenderShadows()
        {
            var sampleName = nameof(RenderShadows);
            Cmd.BeginSampleExecute(sampleName, ref context);

            var atlasSize = (int)CRP.Asset.lightSettings.atlasSize;

            Cmd.GetTemporaryRT(_DirectionalShadowAtlas, atlasSize, atlasSize, 32,
                FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

            Cmd.SetRenderTarget(_DirectionalShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            Cmd.ClearRenderTarget(true, false, Color.clear);

            var splitCount = 1;
            var tileSize = atlasSize/splitCount;
            for (int i = 0; i < shadowedDirLightCount; i++)
            {
                RenderDirectionalShadow(i, splitCount, tileSize);
            }

            Cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            Cmd.SetGlobalDepthBias(0, 0);

            Cmd.EndSampleExecute(sampleName, ref context);
            Cmd.ReleaseTemporaryRT(_DirectionalShadowAtlas);
        }

        void RenderDirectionalShadow(int id, int splitCount, int tileSize)
        {
            LightShadowInfo shadowInfo = dirLightShadowInfos[id];
            var settings = new ShadowDrawingSettings(cullingResults, shadowInfo.lightIndex);

            var splitRatio = Vector3.zero;
            var cascadeCount = CRP.Asset.lightSettings.maxCascades;

            for (int i = 0; i < cascadeCount; i++)
            {
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    shadowInfo.lightIndex,
                    i,
                    cascadeCount,
                    splitRatio,
                    tileSize,
                    shadowInfo.shadowNearPlane,
                    out var viewMatrix,
                    out var projectionMatrix,
                    out var shadowSplitData
                    );

                settings.splitData = shadowSplitData;
                Cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                Cmd.SetGlobalDepthBias(0, shadowInfo.shadowBias);
                ExecuteCommand();

                context.DrawShadows(ref settings);
            }
        }
    }
}
