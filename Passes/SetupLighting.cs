using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;
using Unity.Collections;

namespace PowerUtilities.CRP
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(SetupLighting))]
    public class SetupLighting : BasePass
    {
        [Header(nameof(SetupLighting))]

        [Tooltip("enable light culling per object, also need RenderObjects(Pass) enable PerObjectData(Light Indices|Light Data)")]
        public bool useLightPerObject;

        const string _LIGHTS_PER_OBJECT = nameof(_LIGHTS_PER_OBJECT);

        public static readonly int
            _DirectionalLightCount = Shader.PropertyToID(nameof(_DirectionalLightCount)),
            _DirectionalLightColors = Shader.PropertyToID(nameof(_DirectionalLightColors)),
            _DirectionalLightDirectionsAndMask = Shader.PropertyToID(nameof(_DirectionalLightDirectionsAndMask)),

            _OtherLightCount = Shader.PropertyToID(nameof(_OtherLightCount)),
            _OtherLightPositions = Shader.PropertyToID(nameof(_OtherLightPositions)),
            _OtherLightDirectionsAndMask = Shader.PropertyToID(nameof(_OtherLightDirectionsAndMask)),
            _OtherLightColors = Shader.PropertyToID(nameof(_OtherLightColors)),
            _OtherLightSpotAngles = Shader.PropertyToID(nameof(_OtherLightSpotAngles))
            ;

        Vector4[] dirLightColors;
        Vector4[] dirLightDirections;

        Vector4[] otherLightPositions;
        Vector4[] otherLightDirections;
        Vector4[] otherLightColors;
        Vector4[] otherLightSpotAngles;

        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            var renderingLayerMask = CameraData ? CameraData.renderingLayerMask : uint.MaxValue;
            SetupLights(renderingLayerMask);

            SetupLightIndices();
        }
        void SetupLightIndices()
        {
            if (!useLightPerObject)
            {
                Cmd.SetShaderKeyords(false, _LIGHTS_PER_OBJECT);
                return;
            }

            var maxOtherLightCount = CRP.Asset.otherLightSettings.maxOtherLightCount;

            int otherLightCount = 0;

            var indexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
            var vLights = cullingResults.visibleLights;
            int i;
            for (i = 0; i < vLights.Length; i++)
            {
                var newIndex = -1;
                var vlight = vLights[i];
                if (vlight.lightType == LightType.Point || vlight.lightType == LightType.Spot)
                {
                    if (otherLightCount < maxOtherLightCount)
                        newIndex = otherLightCount++;
                }

                indexMap[i] = newIndex;
            }

            for (; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }

            cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();

            Cmd.SetShaderKeyords(true, _LIGHTS_PER_OBJECT);
        }

        void SetupLights(uint renderingLayerMask=uint.MaxValue)
        {
            var maxDirLightCount = CRP.Asset.directionalLightSettings.maxDirLightCount;
            var maxOtherLightCount = CRP.Asset.otherLightSettings.maxOtherLightCount;

            Init(maxDirLightCount, maxOtherLightCount);

            int dirLightCount = 0, otherLightCount = 0;

            var vLights = cullingResults.visibleLights;
            for (int i = 0; i < vLights.Length; i++)
            {
                var vlight = vLights[i];

                if ((vlight.light.renderingLayerMask & renderingLayerMask) ==0)
                    continue;

                switch (vlight.lightType)
                {
                    case LightType.Directional:
                        if (dirLightCount < maxDirLightCount)
                            SetupDirLight(dirLightCount++, ref vlight);
                        break;
                    case LightType.Point:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            SetupPointLight(otherLightCount++, ref vlight);
                        }
                        break;
                    case LightType.Spot:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            SetupSpotLight(otherLightCount++, ref vlight);
                        }
                        break;
                }
            }

            SendLightInfo(dirLightCount, otherLightCount);
        }

        private void Init(int maxDirLightCount, int maxOtherLightCount)
        {
            if (dirLightColors == null || dirLightColors.Length != maxDirLightCount)
            {
                dirLightColors = new Vector4[maxDirLightCount];
                dirLightDirections = new Vector4[maxDirLightCount];
            }
            if (otherLightColors == null || otherLightColors.Length != maxOtherLightCount)
            {
                otherLightColors = new Vector4[maxOtherLightCount];
                otherLightDirections = new Vector4[maxOtherLightCount];
                otherLightPositions = new Vector4[maxOtherLightCount];
                otherLightSpotAngles = new Vector4[maxOtherLightCount];
            }
        }

        private void SendLightInfo(int dirLightCount, int otherLightCount)
        {
            Cmd.SetGlobalInt(_DirectionalLightCount, dirLightCount);
            if (dirLightCount > 0)
            {
                Cmd.SetGlobalVectorArray(_DirectionalLightDirectionsAndMask, dirLightDirections);
                Cmd.SetGlobalVectorArray(_DirectionalLightColors, dirLightColors);
            }

            Cmd.SetGlobalInt(_OtherLightCount, otherLightCount);
            if (otherLightCount > 0)
            {
                Cmd.SetGlobalVectorArray(_OtherLightPositions, otherLightPositions);
                Cmd.SetGlobalVectorArray(_OtherLightColors, otherLightColors);
                Cmd.SetGlobalVectorArray(_OtherLightDirectionsAndMask, otherLightDirections);
                Cmd.SetGlobalVectorArray(_OtherLightSpotAngles, otherLightSpotAngles);
            }
        }

        void SetupDirLight(int id,ref VisibleLight vlight)
        {
            dirLightColors[id] = vlight.finalColor.linear;
            dirLightDirections[id] = -vlight.localToWorldMatrix.GetColumn(2);
            dirLightDirections[id].w = vlight.light.renderingLayerMask.AsFloat();
        }

        void SetupPointLight(int id, ref VisibleLight vlight)
        {
            otherLightColors[id] = vlight.finalColor;
            var pos = vlight.localToWorldMatrix.GetColumn(3);
            pos.w = 1f/(vlight.range * vlight.range + 0.0001f);
            otherLightPositions[id] = pos;
            otherLightSpotAngles[id] = new Vector4(0, 1);

            otherLightDirections[id] = -vlight.localToWorldMatrix.GetColumn(2);
            otherLightDirections[id].w = vlight.light.renderingLayerMask.AsFloat();
        }
        void SetupSpotLight(int id, ref VisibleLight vlight)
        {
            SetupPointLight(id,ref vlight);

            var innerCos = Mathf.Cos(vlight.light.innerSpotAngle * Mathf.Deg2Rad * 0.5f);
            var outerCos = Mathf.Cos(vlight.spotAngle * Mathf.Deg2Rad * 0.5f);
            var invertRange = 1f/(innerCos - outerCos + 0.001f);
            otherLightSpotAngles[id] = new Vector4(invertRange,-outerCos * invertRange);
        }
    }
}
