using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEditor.Graphs;

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
        public int shadowedLightIndex;
    }

    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(SetupLighting))]
    public class SetupLighting : BasePass
    {
        public static readonly int
            _DirectionalLightCount = Shader.PropertyToID(nameof(_DirectionalLightCount)),
            _DirectionalLightColors = Shader.PropertyToID(nameof(_DirectionalLightColors)),
            _DirectionalLightDirections = Shader.PropertyToID(nameof(_DirectionalLightDirections)),

            _DirectionalShadowAtlas = Shader.PropertyToID(nameof(_DirectionalShadowAtlas)),
            _DirectionalShadowMatrices = Shader.PropertyToID(nameof(_DirectionalShadowMatrices)),
            _DirectionalLightShadowData = Shader.PropertyToID(nameof(_DirectionalLightShadowData))
            ;


        Vector4[] dirLightColors;
        Vector4[] dirLightDirections;
        Vector4[] dirLightShadowData;

        int shadowedDirLightCount = 0;
        LightShadowInfo[] dirLightShadowInfos;
        Matrix4x4[] dirLightShadowMatrices;


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

            if (dirLightColors == null || dirLightColors.Length != maxLightCount)
            {
                dirLightColors = new Vector4[maxLightCount];
                dirLightDirections = new Vector4[maxLightCount];
                dirLightShadowData = new Vector4[maxLightCount];
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
                dirLightShadowMatrices = new Matrix4x4[maxShadowedDirLightCount];

            }

            var shadowedDirLightCount = 0;

            var vLights = cullingResults.visibleLights;
            for (var i= 0 ; i < vLights.Length; i++)
            {
                var vlight = vLights[i];

                if(SetupShadowInfo(vlight.light,i, out var shadowInfo))
                {
                    shadowInfo.shadowedLightIndex = shadowedDirLightCount;
                    dirLightShadowInfos[shadowedDirLightCount++] = shadowInfo;
                }
                else
                {
                    dirLightShadowInfos[i] = shadowInfo; // restore to default
                }

                dirLightShadowData[i] = new Vector4(shadowInfo.shadowStrength, shadowInfo.shadowedLightIndex);

                if (i >= maxLightCount || shadowedDirLightCount >= maxShadowedDirLightCount)
                    break;
            }
            return shadowedDirLightCount;
        }

        public bool SetupShadowInfo(Light light, int id,out LightShadowInfo shadowInfo)
        {
            shadowInfo = default(LightShadowInfo);

            var isInvalidShadowLight =
                light.shadows == LightShadows.None ||
                light.shadowStrength <= 0 ||
                !cullingResults.GetShadowCasterBounds(id, out var bounds)
                ;

            if (!isInvalidShadowLight)
            {
                shadowInfo = new LightShadowInfo
                {
                    lightIndex = id,
                    shadowBias = light.shadowBias,
                    shadowNearPlane = light.shadowNearPlane,
                    shadowStrength = light.shadowStrength,
                    cascadeId = 0,
                    shadowNormalBias = light.shadowNormalBias,
                    shadowedLightIndex = 0
                };
            }
            return !isInvalidShadowLight;
        }

        int GetAtlasRowCount(int count) => count switch
        {
            <= 1 => 1,
            <= 4 => 2,
            <= 8 => 4,
            _ => 1
        };

        public void RenderShadows()
        {
            var sampleName = nameof(RenderShadows);
            Cmd.BeginSampleExecute(sampleName, ref context);

            var atlasSize = (int)CRP.Asset.lightSettings.atlasSize;

            Cmd.GetTemporaryRT(_DirectionalShadowAtlas, atlasSize, atlasSize, 32,
                FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

            Cmd.SetRenderTarget(_DirectionalShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            Cmd.ClearRenderTarget(true, false, Color.clear);

            var splitCount = GetAtlasRowCount(shadowedDirLightCount);
            var tileSize = atlasSize/splitCount;
            for (int i = 0; i < shadowedDirLightCount; i++)
            {
                RenderDirectionalShadow(i, splitCount, tileSize);
            }

            Cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            Cmd.SetGlobalDepthBias(0, 0);
            Cmd.SetGlobalMatrixArray(_DirectionalShadowMatrices, dirLightShadowMatrices);
            Cmd.SetGlobalVectorArray(_DirectionalLightShadowData, dirLightShadowData);

            Cmd.EndSampleExecute(sampleName, ref context);
            //Cmd.ReleaseTemporaryRT(_DirectionalShadowAtlas);
        }

        Rect GetTileRect(int id,int splitCount,int tileSize)
        {
            var offset = new Vector2(id%splitCount, id/splitCount);
            return new Rect(offset.x*tileSize, offset.y*tileSize, tileSize, tileSize);
        }

        void ConvertToShadowAtlasMatrix(ref Matrix4x4 m, Vector2 offset, float splitCount)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.SetRow(2, m.GetRow(2) * -1);
            }

            float scale = 1f / splitCount;
            var r0 = m.GetRow(0);
            var r1 = m.GetRow(1);
            var r2 = m.GetRow(2);
            var r3 = m.GetRow(3);

            OffsetScale(ref r0, r3, offset.x, scale);
            OffsetScale(ref r1, r3, offset.x, scale);
            OffsetScale(ref r2, r3, 0, 1);

            m.SetRow(0, r0);
            m.SetRow(1, r1);
            m.SetRow(2, r2);

            void OffsetScale(ref Vector4 v, Vector4 r3, float offset, float scale)
            {
                //v.x = (0.5f *(v.x + r3.x) + offset * r3.x) * scale;
                for (int i = 0; i < 4; i++)
                {
                    v[i] = (0.5f * (v[i] + r3[i]) + offset * r3[i]) * scale;
                }
            }
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


                var offset = new Vector3(id % splitCount, id/splitCount);
                var viewPortRect = new Rect(offset.x*tileSize, offset.y*tileSize, tileSize, tileSize);

                Cmd.SetViewport(viewPortRect);
                Cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                Cmd.SetGlobalDepthBias(0, shadowInfo.shadowBias);
                ExecuteCommand();

                context.DrawShadows(ref settings);
                // save 
                var worldToShadowMat = projectionMatrix * viewMatrix;
                ConvertToShadowAtlasMatrix(ref worldToShadowMat, offset, splitCount);
                dirLightShadowMatrices[id] = worldToShadowMat;
            }
        }
    }
}
