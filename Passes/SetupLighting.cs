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
        static int _DirectionalLightCount = Shader.PropertyToID(nameof(_DirectionalLightCount));
        static int _DirectionalLightColors = Shader.PropertyToID(nameof(_DirectionalLightColors));
        static int _DirectionalLightDirections = Shader.PropertyToID(nameof(_DirectionalLightDirections));

        [Header("Light")]
        public bool updateLights;
        [Range(1, 8)] public int maxLightCount = 8;

        Vector4[] dirLightColors;
        Vector4[] dirLightDirections;

        static int _DirectionalShadowAtlas = Shader.PropertyToID(nameof(_DirectionalShadowAtlas));

        [Header("Shadow")]
        [Range(0, 8)] public int maxShadowedDirLightCount = 8;
        [Range(1, 4)] public int maxCascades = 4;
        [Range(0.001f, 1f)] public float cascadeFade;

        public int shadowedDirLightCount = 0;

        public float maxDisatance = 100;
        public int atlasSize = 1024;

        LightShadowInfo[] dirLightShadowInfos;

        public override void OnRender()
        {
            if(IsCullingResultsValid())
                SetupLights();
        }

        void SetupLights()
        {
            if (dirLightColors == null || dirLightColors.Length ==0)
            {
                dirLightColors = new Vector4[maxLightCount];
                dirLightDirections = new Vector4[maxLightCount];
            }

            var vLights = cullingResults.visibleLights;
            int i;
            for (i = 0; i < vLights.Length; i++)
            {
                var vlight = vLights[i];
                SetupLight(ref vlight, i);
                SetupShadow(vlight.light, i);

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

        public void SetupShadow(Light light, int id)
        {
            if (id >= maxShadowedDirLightCount)
                return;

            if (dirLightShadowInfos == null || dirLightShadowInfos.Length != maxShadowedDirLightCount)
            {
                dirLightShadowInfos = new LightShadowInfo[maxShadowedDirLightCount];
            }
            dirLightShadowInfos[id] = new LightShadowInfo
            {
                lightIndex = id,
                shadowBias = light.shadowBias,
                shadowNearPlane = light.shadowNearPlane,
                shadowStrength = light.shadowStrength,
                cascadeId = 0,
                shadowNormalBias = light.shadowNormalBias,
            };
            shadowedDirLightCount++;
        }

        public void RenderShadows()
        {
            Cmd.GetTemporaryRT(_DirectionalShadowAtlas, atlasSize, atlasSize, 32,
                FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

            Cmd.SetRenderTarget(_DirectionalShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            Cmd.BeginSampleExecute(nameof(RenderShadows), ref context);

            var splitCount = 1;
            var tileSize = atlasSize/splitCount;
            for (int i = 0; i < shadowedDirLightCount; i++)
            {
                RenderShadow(i, splitCount, tileSize);
            }

            Cmd.EndSampleExecute(nameof(RenderShadows), ref context);

        }

        void RenderShadow(int id, int splitCount, int tileSize)
        {
            LightShadowInfo shadowInfo = dirLightShadowInfos[id];

            var lightIndex = 0;
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightIndex);
            var tileOffset = id * maxCascades;
            var splitRatio = new Vector3();
            var cullingFactor = Mathf.Max(0, 0.8f - cascadeFade);

            for (int i = 0; i < maxCascades; i++)
            {

                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, id, splitCount, splitRatio, tileSize,
                   shadowInfo.shadowNearPlane,
                   out var viewMatrix, out var projMatrix, out var shadowSplitData);
                shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = shadowSplitData;

                var tileId = tileOffset + 1;

                Cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);
                Cmd.SetGlobalDepthBias(0, shadowInfo.shadowBias);
                ExecuteCommand();

                context.DrawShadows(ref shadowSettings);
                Cmd.SetGlobalDepthBias(0, 0);
            }
        }
    }
}
