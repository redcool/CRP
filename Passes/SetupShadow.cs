﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{

    public struct LightShadowInfo
    {
        public int lightIndex;
        public float shadowBias;
        public float shadowNearPlane;
        public float shadowStrength;
        public float shadowNormalBias;
        public int shadowedLightIndex;
        // shadow mask
        public int occlusionMaskChannel;
    }

    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/" + nameof(SetupShadow))]
    public class SetupShadow : BasePass
    {
        LightSettings lightShadowSettings => CRP.Asset.lightSettings;

        public static readonly int
            _DirectionalShadowAtlas = Shader.PropertyToID(nameof(_DirectionalShadowAtlas)),
            _DirectionalShadowMatrices = Shader.PropertyToID(nameof(_DirectionalShadowMatrices)),
            _DirectionalLightShadowData = Shader.PropertyToID(nameof(_DirectionalLightShadowData)),

            _CascadeCount = Shader.PropertyToID(nameof(_CascadeCount)),
            _CascadeCullingSpheres = Shader.PropertyToID(nameof(_CascadeCullingSpheres)),
            _CascadeData = Shader.PropertyToID(nameof(_CascadeData)),
            _ShadowDistance = Shader.PropertyToID(nameof(_ShadowDistance)),
            _ShadowDistanceFade = Shader.PropertyToID(nameof(_ShadowDistanceFade)), //{1/distance,distanceFade factor,cascadeFade factor}
            _ShadowAtlasSize = Shader.PropertyToID(nameof(_ShadowAtlasSize)),

            _OtherLightShadowData = Shader.PropertyToID(nameof(_OtherLightShadowData))
        ;

        const float MIN_SHADOW_NORMAL_BIAS = 1f;
        const int MAX_CASCADES = 4;

        readonly string[] DIR_SHADOW_FILTER_MODE_KEYWORDS = new[] {
            "_DIRECTIONAL_PCF3","_DIRECTIONAL_PCF5","_DIRECTIONAL_PCF7"
        };
        readonly string[] CASCADE_BLEND_MODE_KEYWORDS = new[]
        {
            "_CASCADE_BLEND_SOFT","_CASCADE_BLEND_DITHER"
        };

        LightShadowInfo[] dirLightShadowInfos;
        Matrix4x4[] dirLightShadowMatrices;

        Vector4[]
            dirLightShadowData,//{shadow strength,tile index, normal bias,shadow Mask channel}
            cascadeCCullingSpheres = new Vector4[MAX_CASCADES], //{xyz:sphere center, w: dist2}
            cascadeData = new Vector4[MAX_CASCADES], //{x: 1/dist2,y: filterMode Atten}
            otherLightShadowData  //{shadow strength,tile index, normal bias,shadow Mask channel}
            ;


        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            var shadowedDirLightCount = SetupDirLightShadows();
            SetupOtherLightShadows();
            //
            if(!camera.IsReflectionCamera())
                RenderDirLightShadows(shadowedDirLightCount);

            SendDirLightShadowParams();
            SetShadowKeywords();

            SendOtherLightShadowParams();
        }

        private void SetupOtherLightShadows()
        {
            var maxOtherLightCount = CRP.Asset.lightSettings.maxOtherLightCount;

            if(otherLightShadowData == null || otherLightShadowData.Length != maxOtherLightCount)
            {
                otherLightShadowData = new Vector4[MAX_CASCADES];
            }

            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var vLight = cullingResults.visibleLights[i];
                if(vLight.lightType == LightType.Point ||
                    vLight.lightType == LightType.Spot
                )
                {
                    if(SetupShadowInfo(vLight.light, i, out var shadowInfo))
                    {
                        otherLightShadowData[i] = new Vector4(shadowInfo.shadowStrength, 0, 0, shadowInfo.occlusionMaskChannel);
                    }

                }
            }
        }

        void SendOtherLightShadowParams()
        {
            Cmd.SetGlobalVectorArray(_OtherLightShadowData, otherLightShadowData);
        }

        public override bool NeedCleanup() => true;
        public override void Cleanup()
        {
            Cmd.ReleaseTemporaryRT(_DirectionalShadowAtlas);
        }

        void SetShadowKeywords()
        {
            var filterId = (int)lightShadowSettings.filterMode - 1;
            for (int i = 0; i < DIR_SHADOW_FILTER_MODE_KEYWORDS.Length; i++)
            {
                Cmd.SetShaderKeyords(i == filterId, DIR_SHADOW_FILTER_MODE_KEYWORDS[i]);
            }

            var cascadeBlendId = (int)lightShadowSettings.cascadeBlendMode - 1;
            for (int i = 0; i < CASCADE_BLEND_MODE_KEYWORDS.Length; i++)
            {
                Cmd.SetShaderKeyords(i == cascadeBlendId, CASCADE_BLEND_MODE_KEYWORDS[i]);
            }
        }

        int SetupDirLightShadows()
        {
            var maxLightCount = lightShadowSettings.maxDirLightCount;
            var maxShadowedDirLightCount = lightShadowSettings.maxShadowedDirLightCount;
            var maxCascadeCount = lightShadowSettings.maxCascades;

            if (maxShadowedDirLightCount <= 0)
                return 0;

            InitDirLights(maxLightCount, maxShadowedDirLightCount, maxCascadeCount);


            var shadowedDirLightCount = 0;

            var vLights = cullingResults.visibleLights;
            for (var i = 0; i < vLights.Length; i++)
            {
                var vlight = vLights[i];
                if (i >= maxLightCount || shadowedDirLightCount >= maxShadowedDirLightCount)
                    break;

                if (SetupShadowInfo(vlight.light, i, out var shadowInfo))
                {
                    shadowInfo.shadowedLightIndex = shadowedDirLightCount;
                    dirLightShadowInfos[shadowedDirLightCount++] = shadowInfo;
                }

                dirLightShadowData[i] = new Vector4(shadowInfo.shadowStrength,
                    shadowInfo.shadowedLightIndex * maxCascadeCount,
                    shadowInfo.shadowNormalBias + MIN_SHADOW_NORMAL_BIAS,// min : 1
                    shadowInfo.occlusionMaskChannel
                    );
            }
            return shadowedDirLightCount;
        }

        void InitDirLights(int maxLightCount, int maxShadowedDirLightCount, int maxCascadeCount)
        {
            if (dirLightShadowInfos == null || dirLightShadowInfos.Length != maxLightCount)
            {
                dirLightShadowData = new Vector4[maxLightCount];
                dirLightShadowInfos = new LightShadowInfo[maxLightCount];
            }

            if (dirLightShadowMatrices == null || dirLightShadowMatrices.Length != maxShadowedDirLightCount * maxCascadeCount)
                dirLightShadowMatrices = new Matrix4x4[maxShadowedDirLightCount * maxCascadeCount];
        }

        public bool SetupShadowInfo(Light light, int id, out LightShadowInfo shadowInfo)
        {
            shadowInfo = default;

            var isInnerBounds = cullingResults.GetShadowCasterBounds(id, out var bounds);
            var isInvalidShadowLight =
                light.shadows == LightShadows.None ||
                light.shadowStrength <= 0 ||
                !isInnerBounds
                ;

            light.HasShadowMask(out shadowInfo.occlusionMaskChannel);

            if (!isInvalidShadowLight)
            {
                var bakingOutput = light.bakingOutput;

                shadowInfo = new LightShadowInfo
                {
                    lightIndex = id,
                    shadowBias = light.shadowBias,
                    shadowNearPlane = light.shadowNearPlane,
                    shadowStrength = light.shadowStrength,
                    shadowNormalBias = light.shadowNormalBias,
                    shadowedLightIndex = 0, // will get light id outer this method
                    occlusionMaskChannel = bakingOutput.occlusionMaskChannel
            };
            }
            return !isInvalidShadowLight;
        }

        int GetAtlasRowCount(int count) => count switch
        {
            <= 1 => 1,
            <= 4 => 2,
            <= 16 => 4,
            <= 64 => 8,
            _ => 1
        };


        void SetupShadowTarget(int atlasSize)
        {
            Cmd.GetTemporaryRT(_DirectionalShadowAtlas, atlasSize, atlasSize, 32,
                FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

            Cmd.SetRenderTarget(_DirectionalShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            Cmd.ClearRenderTarget(true, false, Color.clear);
        }

        public void RenderDirLightShadows(int shadowedDirLightCount)
        {
            var atlasSize = (int)lightShadowSettings.atlasSize;
            var cascadeCount = lightShadowSettings.maxCascades;

            var sampleName = nameof(RenderDirLightShadows);
            Cmd.BeginSampleExecute(sampleName, ref context);

            SetupShadowTarget(atlasSize);

            var splitCount = GetAtlasRowCount(shadowedDirLightCount * cascadeCount);
            var tileSize = atlasSize / splitCount;
            for (int i = 0; i < shadowedDirLightCount; i++)
            {
                RenderDirectionalShadow(i, splitCount, tileSize);
            }

            Cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            Cmd.SetGlobalDepthBias(0, 0);

            Cmd.EndSampleExecute(sampleName, ref context);
        }

        private void SendDirLightShadowParams()
        {
            var maxLightCount = lightShadowSettings.maxDirLightCount;
            var maxShadowedDirLightCount = lightShadowSettings.maxShadowedDirLightCount;

            if (maxLightCount == 0 || maxShadowedDirLightCount == 0)
                return;

            var atlasSize = (int)lightShadowSettings.atlasSize;
            var cascadeCount = lightShadowSettings.maxCascades;
            var shadowDistance = lightShadowSettings.maxShadowDistance;
            var distanceFade = lightShadowSettings.distanceFade;
            var cascadeFade = lightShadowSettings.cascadeFade;

            Cmd.SetGlobalMatrixArray(_DirectionalShadowMatrices, dirLightShadowMatrices);

            Cmd.SetGlobalVectorArray(_DirectionalLightShadowData, dirLightShadowData);
            Cmd.SetGlobalInt(_CascadeCount, cascadeCount);
            Cmd.SetGlobalVectorArray(_CascadeCullingSpheres, cascadeCCullingSpheres);
            Cmd.SetGlobalVectorArray(_CascadeData, cascadeData);
            Cmd.SetGlobalFloat(_ShadowDistance, shadowDistance);

            float f = 1f - cascadeFade;
            Cmd.SetGlobalVector(_ShadowDistanceFade, new Vector4(1f / shadowDistance, 1f / distanceFade, 1f / (1f - f * f)));
            Cmd.SetGlobalVector(_ShadowAtlasSize, new Vector4(atlasSize, 1f / atlasSize));
        }

        Rect GetTileRect(int id, int splitCount, int tileSize)
        {
            var offset = new Vector2(id % splitCount, id / splitCount);
            return new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize);
        }

        void ConvertToShadowAtlasMatrix(ref Matrix4x4 m, Vector2 offset, float splitCount, int splitId)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.SetRow(2, m.GetRow(2) * -1);
            }

            float scale = 1f / splitCount;


            //var m1 = new Matrix4x4();
            //m1 = 
            //    m1.Matrix(
            //     0.5f*scale, 0, 0, (0.5f+offset.x)*scale,
            //     0, 0.5f*scale, 0, (0.5f+offset.y)*scale,
            //     0, 0, 0.5f, 0.5f,
            //     0, 0, 0, 1
            //);

            //m = m1 * m;
            //return;

            var r0 = m.GetRow(0);
            var r1 = m.GetRow(1);
            var r2 = m.GetRow(2);
            var r3 = m.GetRow(3);

            OffsetScale(ref r0, r3, offset.x, scale);
            OffsetScale(ref r1, r3, offset.y, scale);
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

        void RenderDirectionalShadow(int lightId, int splitCount, int tileSize)
        {
            LightShadowInfo shadowInfo = dirLightShadowInfos[lightId];
            var settings = new ShadowDrawingSettings(cullingResults, shadowInfo.lightIndex);

            var cascadeCount = lightShadowSettings.maxCascades;
            var tileOffset = lightId * cascadeCount;
            var splitRatio = lightShadowSettings.CascadeRatios;
            var cullingFactor = Mathf.Max(0, 0.8f - lightShadowSettings.cascadeFade);

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

                shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
                settings.splitData = shadowSplitData;

                var tileId = tileOffset + i;

                var offset = new Vector2(tileId % splitCount, tileId / splitCount);
                var viewPortRect = new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize);

                Cmd.SetViewport(viewPortRect);
                Cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                Cmd.SetGlobalDepthBias(0, shadowInfo.shadowBias);
                ExecuteCommand();

                context.DrawShadows(ref settings);
                // save 
                var worldToShadowMat = projectionMatrix * viewMatrix;
                ConvertToShadowAtlasMatrix(ref worldToShadowMat, offset, splitCount, tileId);
                dirLightShadowMatrices[tileId] = worldToShadowMat;

                if (lightId == 0)
                {
                    SetCascadeData(i, shadowSplitData.cullingSphere, tileSize);
                }
            }
        }

        private void SetCascadeData(int cascadeId, Vector4 cullingSphere, int tileSize)
        {
            float filterId = (float)lightShadowSettings.filterMode + 1;
            float texelSize = 2f * cullingSphere.w / tileSize;
            var filterSize = texelSize * filterId;

            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;

            cascadeCCullingSpheres[cascadeId] = cullingSphere;
            cascadeData[cascadeId] = new Vector4(1f / cullingSphere.w, filterSize * 1.414f);
        }
    }
}
