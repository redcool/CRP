﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities.CRP
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

    public struct OhterLightShadow
    {
        public int lightIndex;
        public float shadowBias;
        public float normalBias;
        public bool isPoint;
    }

    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/" + nameof(SetupShadow))]
    public class SetupShadow : BasePass
    {
        DirectionalLightSettings dirLightSettings => CRP.Asset.directionalLightSettings;

        public static readonly int
            _DirectionalShadowAtlas = Shader.PropertyToID(nameof(_DirectionalShadowAtlas)),
            _DirectionalShadowMatrices = Shader.PropertyToID(nameof(_DirectionalShadowMatrices)),
            _DirectionalLightShadowData = Shader.PropertyToID(nameof(_DirectionalLightShadowData)),

            _CascadeCount = Shader.PropertyToID(nameof(_CascadeCount)),
            _CascadeCullingSpheres = Shader.PropertyToID(nameof(_CascadeCullingSpheres)),
            _CascadeData = Shader.PropertyToID(nameof(_CascadeData)),
            _ShadowDistance = Shader.PropertyToID(nameof(_ShadowDistance)),
            _ShadowDistanceFade = Shader.PropertyToID(nameof(_ShadowDistanceFade)),     //{1/distance,distanceFade factor,cascadeFade factor}
            _ShadowAtlasSize = Shader.PropertyToID(nameof(_ShadowAtlasSize))
        ;

        const float MIN_SHADOW_NORMAL_BIAS = 1f;
        const int MAX_CASCADES = 4;

        readonly string[] DIR_PCF_MODE_KEYWORDS = new[] { "_DIRECTIONAL_PCF3","_DIRECTIONAL_PCF5","_DIRECTIONAL_PCF7" };
        readonly string[] CASCADE_BLEND_MODE_KEYWORDS = new[] {"_CASCADE_BLEND_SOFT","_CASCADE_BLEND_DITHER" };

        LightShadowInfo[] dirLightShadowInfos;
        Matrix4x4[] dirLightShadowMatrices;
        Vector4 shadowAtlasSize;    //{dirAtlasSize ,1/dirAtlasSize,otherAtlasSize,1/ otherAtlasSize}


        Vector4[]
            dirLightShadowData,     //{shadow strength,tile id, normal bias,shadow Mask channel}
            cascadeCCullingSpheres = new Vector4[MAX_CASCADES],     //{xyz:sphere center, w: dist2}
            cascadeData = new Vector4[MAX_CASCADES]     //{x: 1/dist2,y: pcfMode Atten}
            ;

        readonly string[] OTHER_PCF_MODE_KEYWORDS = new[] { "_OTHER_PCF3","_OTHER_PCF5","_OTHER_PCF7"}; 
        static readonly int
            _OtherShadowData = Shader.PropertyToID(nameof(_OtherShadowData)),
            _OtherShadowAtlas = Shader.PropertyToID(nameof(_OtherShadowAtlas)),
            _OtherShadowMatrices = Shader.PropertyToID(nameof(_OtherShadowMatrices)),
            _OtherShadowTiles = Shader.PropertyToID(nameof(_OtherShadowTiles))
            ;
        Vector4[]
            otherShadowData,    //{shadow strength,tile id, normal bias,shadow Mask channel}
            otherShadowTiles    //{bounds.xyz,normal bias}
            ;
        OhterLightShadow[] otherShadowInfos;
        Matrix4x4[] otherShadowMatrices;
        private bool needCleanOther;

        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            var cameraRenderingLayerMask = CameraData ? CameraData.renderingLayerMask : int.MaxValue;

            var shadowedDirLightCount = SetupDirLightShadows(cameraRenderingLayerMask);
            var shadowedOtherLightCount = SetupOtherLightShadows(cameraRenderingLayerMask);
            //
            if (!camera.IsReflectionCamera())
            {
                if (shadowedDirLightCount > 0)
                    RenderDirLightShadows(shadowedDirLightCount);
                else
                    Cmd.GetTemporaryRT(_DirectionalShadowAtlas, 1, 1, 16, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

                if (shadowedOtherLightCount > 0)
                    RenderOtherLightShadows(shadowedOtherLightCount);
                else
                    Cmd.SetGlobalTexture(_OtherShadowAtlas, _DirectionalShadowAtlas);
            }

            SendDirLightShadowParams();
            SetDirShadowKeywords();

            SendOtherLightShadowParams();
            SetOtherShadowKeywords();

            //restore
            Cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            Cmd.SetGlobalDepthBias(0, 0);

            needCleanOther = shadowedOtherLightCount > 0;
        }

        public override bool IsNeedPipelineCleanup() => true;
        public override void PipelineCleanup()
        {
            Cmd.ReleaseTemporaryRT(_DirectionalShadowAtlas);
            if (needCleanOther)
                Cmd.ReleaseTemporaryRT(_OtherShadowAtlas);
        }

        int SetupOtherLightShadows(uint cameraRenderingLayerMask = uint.MaxValue)
        {
            var maxOtherLightCount = CRP.Asset.otherLightSettings.maxOtherLightCount;
            var maxShadowedOtherLightCount = CRP.Asset.otherLightSettings.maxShadowedOtherLightCount;
            var shadowedOtherLightCount = 0;

            if (otherShadowData == null || otherShadowData.Length != maxOtherLightCount)
            {
                otherShadowData = new Vector4[maxOtherLightCount];
            }
            if (otherShadowInfos == null || otherShadowInfos.Length != maxShadowedOtherLightCount)
            {
                otherShadowInfos = new OhterLightShadow[maxShadowedOtherLightCount];
                otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];
                otherShadowTiles = new Vector4[maxShadowedOtherLightCount];
            }

            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                // set default value
                if(i < maxOtherLightCount)
                    otherShadowData[i].Set(0, -1, 0, -1);

                var vLight = cullingResults.visibleLights[i];
                if ((vLight.light.renderingLayerMask & cameraRenderingLayerMask) == 0)
                    continue;

                var isPoint = vLight.lightType == LightType.Point;
                if (!(isPoint || vLight.lightType == LightType.Spot))
                    continue;

                var shadowedCount = isPoint ? 6 : 1;
                // point light is full, try take spot light.
                if (shadowedOtherLightCount + shadowedCount >= maxShadowedOtherLightCount)
                {
                    continue;
                }
                if (shadowedOtherLightCount >= maxShadowedOtherLightCount)
                    break;

                if (SetupShadowInfo(vLight.light, i, out var shadowInfo))
                {
                    otherShadowData[i] = new Vector4(shadowInfo.shadowStrength, shadowedOtherLightCount, isPoint ? 1 : 0, shadowInfo.occlusionMaskChannel);
                    otherShadowInfos[shadowedOtherLightCount] = new OhterLightShadow
                    {
                        isPoint = isPoint,
                        lightIndex = i,
                        normalBias = vLight.light.shadowNormalBias,
                        shadowBias = vLight.light.shadowBias,
                    };

                    shadowedOtherLightCount += shadowedCount;
                }
            }

            return shadowedOtherLightCount;
        }

        void RenderOtherLightShadows(int shadowedOtherLightCount)
        {
            var atlasSize = (int)CRP.Asset.otherLightSettings.atlasSize;
            shadowAtlasSize.z = atlasSize;
            shadowAtlasSize.w = 1f / atlasSize;

            Cmd.GetTemporaryRT(_OtherShadowAtlas, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            Cmd.SetRenderTarget(_OtherShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            Cmd.ClearRenderTarget(true, false, Color.clear);

            Cmd.BeginSampleExecute(nameof(RenderOtherLightShadows),ref context);

            //int split = shadowedOtherLightCount <= 1 ? 1 : shadowedOtherLightCount <= 4 ? 2 : 4;
            var split = GetAtlasRowCount(shadowedOtherLightCount);
            var tileSize = atlasSize / split;

            for (int i = 0; i < shadowedOtherLightCount;)
            {
                var info = otherShadowInfos[i];
                if(info.isPoint)
                {
                    RenderPointShadow(i,split,tileSize);
                }
                else
                {
                    RenderSpotShadow(i,split,tileSize);
                }
                i += info.isPoint ? 6 : 1;
            }
            Cmd.EndSampleExecute(nameof(RenderOtherLightShadows), ref context);
        }

        private void RenderSpotShadow(int i, int split, int tileSize)
        {
            var otherLightSettings = CRP.Asset.otherLightSettings;

            var shadowInfo = otherShadowInfos[i];
            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowInfo.lightIndex, 
                out var viewMatrix, out var projMatrix, out var splitData);

            var settings = new ShadowDrawingSettings(cullingResults, shadowInfo.lightIndex)
            {
#if UNITY_2021_1_OR_NEWER
                objectsFilter = otherLightSettings.objectsFilter,
#endif
                useRenderingLayerMaskTest = otherLightSettings.useRenderingLayerMask,
            };
            settings.splitData = splitData;

            var offset = new Vector2(i % split, i / split);
            var viewportRect = new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize);
            Cmd.SetViewport(viewportRect);
            Cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);
            Cmd.SetGlobalDepthBias(0,shadowInfo.shadowBias);
            ExecuteCommand();

            context.DrawShadows(ref settings);

            //save 
            otherShadowMatrices[i] = ConvertToShadowAtlasMatrix(projMatrix * viewMatrix, offset, split);

            var texelSize = 2f / (tileSize * projMatrix.m00);
            float pcfMode = (float)otherLightSettings.pcfMode + 1;
            var filterSize = texelSize * pcfMode;
            var bias = shadowInfo.normalBias * filterSize * 1.414f;
            var tileScale = 1f / split;
            SetOtherTileData(i, offset, tileScale, bias);
        }

        private void RenderPointShadow(int shadowedLightId, int split, int tileSize)
        {
            var otherLightSettings = CRP.Asset.otherLightSettings;

            float pcfMode = (float)otherLightSettings.pcfMode +1;
            var shadowInfo = otherShadowInfos[shadowedLightId];
            var settings = new ShadowDrawingSettings(cullingResults, shadowInfo.lightIndex)
            {
#if UNITY_2021_1_OR_NEWER
                objectsFilter = otherLightSettings.objectsFilter,
#endif
                useRenderingLayerMaskTest = otherLightSettings.useRenderingLayerMask
            };

            var texelSize = 2 / tileSize;
            var filterSize = texelSize * pcfMode;
            var bias = shadowInfo.normalBias * filterSize * 1.414f;
            var tileScale = 1f / split;
            var fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2 - 90f;

            for (int i = 0; i < 6; i++)
            {

                cullingResults.ComputePointShadowMatricesAndCullingPrimitives(shadowInfo.lightIndex, (CubemapFace)i, fovBias,
                    out var viewMatrix, out var projMatrix, out var splitData);

                //viewMatrix.m11 *= -1;
                //viewMatrix.m12 *= -1;
                //viewMatrix.m13 *= -1;

                settings.splitData = splitData;

                var id = shadowedLightId + i;
                Vector2 offset = new Vector2(id%split,id/split);
                Rect viewportRect = new Rect(offset.x*tileSize,offset.y*tileSize,tileSize,tileSize);
                Cmd.SetViewport(viewportRect);
                Cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);
                Cmd.SetGlobalDepthBias(0, shadowInfo.shadowBias);
                ExecuteCommand();

                context.DrawShadows(ref settings);
                //save
                SetOtherTileData(id, offset, tileScale, bias);
                otherShadowMatrices[id] = ConvertToShadowAtlasMatrix(projMatrix * viewMatrix, offset, split);
            }
        }
        void SetOtherTileData(int id,Vector2 offset,float tileScale, float bias)
        {
            var border = shadowAtlasSize.w * 0.5f;
            otherShadowTiles[id] = new Vector4(
                offset.x * tileScale + border,
                offset.y * tileScale + border,
                tileScale - border - border,
                bias
                );
        }
        void SendOtherLightShadowParams()
        {
            Cmd.SetGlobalVectorArray(_OtherShadowData, otherShadowData);
            Cmd.SetGlobalMatrixArray(_OtherShadowMatrices, otherShadowMatrices);
            Cmd.SetGlobalVectorArray(_OtherShadowTiles, otherShadowTiles);
        }

        void SetDirShadowKeywords()
        {
            var filterId = (int)dirLightSettings.pcfMode - 1;
            for (int i = 0; i < DIR_PCF_MODE_KEYWORDS.Length; i++)
            {
                Cmd.SetShaderKeywords(i == filterId, DIR_PCF_MODE_KEYWORDS[i]);
            }

            var cascadeBlendId = (int)dirLightSettings.cascadeBlendMode - 1;
            for (int i = 0; i < CASCADE_BLEND_MODE_KEYWORDS.Length; i++)
            {
                Cmd.SetShaderKeywords(i == cascadeBlendId, CASCADE_BLEND_MODE_KEYWORDS[i]);
            }
        }

        void SetOtherShadowKeywords()
        {
            var filterId = (int)CRP.Asset.otherLightSettings.pcfMode - 1;
            for (int i = 0; i < OTHER_PCF_MODE_KEYWORDS.Length; i++)
            {
                Cmd.SetShaderKeywords(i == filterId, OTHER_PCF_MODE_KEYWORDS[i]);
            }
        }

        int SetupDirLightShadows(uint cameraRenderingLayerMask = uint.MaxValue)
        {
            var maxDirLightCount = dirLightSettings.maxDirLightCount;
            var maxShadowedDirLightCount = dirLightSettings.maxShadowedDirLightCount;
            var maxCascadeCount = dirLightSettings.maxCascades;

            if (maxShadowedDirLightCount <= 0)
                return 0;

            InitDirLights(maxDirLightCount, maxShadowedDirLightCount, maxCascadeCount);
            var shadowedDirLightCount = 0;
            var vLights = cullingResults.visibleLights;
            for (var i = 0; i < vLights.Length; i++)
            {
                var vlight = vLights[i];
                if (vlight.lightType != LightType.Directional)
                    continue;
                if ((vlight.light.renderingLayerMask & cameraRenderingLayerMask) == 0)
                    continue;

                if (shadowedDirLightCount >= maxShadowedDirLightCount)
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

#if UNITY_2020
        int GetAtlasRowCount(int count)
        {
            if (count <= 1) return 1;
            if (count <= 4) return 2;
            if (count <= 16) return 4;
            if(count <= 64) return 8;
            return 1;
        }
#else
        int GetAtlasRowCount(int count) => count switch
        {
            <= 1 => 1,
            <= 4 => 2,
            <= 16 => 4,
            <= 64 => 8,
            _ => 1
        };
#endif

        void SetupShadowTarget(int atlasSize)
        {
            Cmd.GetTemporaryRT(_DirectionalShadowAtlas, atlasSize, atlasSize, 32,
                FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

            Cmd.SetRenderTarget(_DirectionalShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            Cmd.ClearRenderTarget(true, false, Color.clear);
        }

        public void RenderDirLightShadows(int shadowedDirLightCount)
        {
            var atlasSize = (int)dirLightSettings.atlasSize;
            var cascadeCount = dirLightSettings.maxCascades;

            var sampleName = nameof(RenderDirLightShadows);
            Cmd.BeginSampleExecute(sampleName, ref context);

            SetupShadowTarget(atlasSize);

            var splitCount = GetAtlasRowCount(shadowedDirLightCount * cascadeCount);
            var tileSize = atlasSize / splitCount;
            for (int i = 0; i < shadowedDirLightCount; i++)
            {
                RenderDirectionalShadow(i, splitCount, tileSize);
            }

            Cmd.EndSampleExecute(sampleName, ref context);
        }

        private void SendDirLightShadowParams()
        {
            var maxLightCount = dirLightSettings.maxDirLightCount;
            var maxShadowedDirLightCount = dirLightSettings.maxShadowedDirLightCount;

            if (maxLightCount == 0 || maxShadowedDirLightCount == 0)
                return;

            var atlasSize = (int)dirLightSettings.atlasSize;
            var cascadeCount = dirLightSettings.maxCascades;
            var shadowDistance = dirLightSettings.maxShadowDistance;
            var distanceFade = dirLightSettings.distanceFade;
            var cascadeFade = dirLightSettings.cascadeFade;

            Cmd.SetGlobalMatrixArray(_DirectionalShadowMatrices, dirLightShadowMatrices);

            Cmd.SetGlobalVectorArray(_DirectionalLightShadowData, dirLightShadowData);
            Cmd.SetGlobalInt(_CascadeCount, cascadeCount);
            Cmd.SetGlobalVectorArray(_CascadeCullingSpheres, cascadeCCullingSpheres);
            Cmd.SetGlobalVectorArray(_CascadeData, cascadeData);
            Cmd.SetGlobalFloat(_ShadowDistance, shadowDistance);

            float f = 1f - cascadeFade;
            Cmd.SetGlobalVector(_ShadowDistanceFade, new Vector4(1f / shadowDistance, 1f / distanceFade, 1f / (1f - f * f)));

            shadowAtlasSize.x = atlasSize;
            shadowAtlasSize.y = 1f / atlasSize;
            Cmd.SetGlobalVector(_ShadowAtlasSize, shadowAtlasSize);
        }

        Rect GetTileRect(int id, int splitCount, int tileSize)
        {
            var offset = new Vector2(id % splitCount, id / splitCount);
            return new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize);
        }

        public static Matrix4x4 ConvertToShadowAtlasMatrix(Matrix4x4 m, Vector2 offset, float splitCount)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.SetRow(2, m.GetRow(2) * -1);
            }

            float scale = 1f / splitCount;

            /* 
            var splitRatio = 1f / splitCount;
            var objectMat = MatrixEx.Matrix(
                0.5f * splitRatio, 0, 0, 0.5f * splitRatio + splitRatio * offset.x,
                0, 0.5f * splitRatio, 0, 0.5f * splitRatio + splitRatio * offset.y,
                0, 0, 0.5f, 0.5f,
                0, 0, 0, 1
                );
            return objectMat * m;
            */

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

            return m;

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
            var settings = new ShadowDrawingSettings(cullingResults, shadowInfo.lightIndex)
            {
                useRenderingLayerMaskTest = dirLightSettings.useRenderingLayerMask,
#if UNITY_2021_1_OR_NEWER
                objectsFilter = dirLightSettings.objectsFilter
#endif
            };

            var cascadeCount = dirLightSettings.maxCascades;
            var tileOffset = lightId * cascadeCount;
            var splitRatio = dirLightSettings.CascadeRatios;
            var cullingFactor = Mathf.Max(0, 0.8f - dirLightSettings.cascadeFade);

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
                dirLightShadowMatrices[tileId] = ConvertToShadowAtlasMatrix(projectionMatrix * viewMatrix, offset, splitCount);

                if (lightId == 0)
                {
                    SetCascadeData(i, shadowSplitData.cullingSphere, tileSize);
                }
            }
        }

        private void SetCascadeData(int cascadeId, Vector4 cullingSphere, int tileSize)
        {
            float filterId = (float)dirLightSettings.pcfMode + 1;
            float texelSize = 2f * cullingSphere.w / tileSize;
            var filterSize = texelSize * filterId;

            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;

            cascadeCCullingSpheres[cascadeId] = cullingSphere;
            cascadeData[cascadeId] = new Vector4(1f / cullingSphere.w, filterSize * 1.414f);
        }
    }
}
