using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;
using Unity.Collections;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(SetupLighting))]
    public class SetupLighting : BasePass
    {
        [Header(nameof(SetupLighting))]
        public bool useLightPerObject;

        const string _LIGHTS_PER_OBJECT = nameof(_LIGHTS_PER_OBJECT);

        public static readonly int
            _DirectionalLightCount = Shader.PropertyToID(nameof(_DirectionalLightCount)),
            _DirectionalLightColors = Shader.PropertyToID(nameof(_DirectionalLightColors)),
            _DirectionalLightDirections = Shader.PropertyToID(nameof(_DirectionalLightDirections)),

            _OtherLightCount = Shader.PropertyToID(nameof(_OtherLightCount)),
            _OtherLightPositions = Shader.PropertyToID(nameof(_OtherLightPositions)),
            _OtherLightDirections = Shader.PropertyToID(nameof(_OtherLightDirections)),
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

            SetupLights();

            //SetupLightIndices();
        }
        void SetupLightIndices()
        {
            var maxOtherLightCount = CRP.Asset.lightSettings.maxOtherLightCount;

            int otherLightCount = 0;

            var indexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
            var vLights = cullingResults.visibleLights;
            for (int i = 0; i < vLights.Length; i++)
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

            if (useLightPerObject)
            {
                for (int i = vLights.Length - 1; i < indexMap.Length; i++)
                {
                    indexMap[i] = -1;
                }
                cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
            }
            Cmd.SetShaderKeyords(useLightPerObject, _LIGHTS_PER_OBJECT);
        }

        void SetupLights()
        {
            var maxDirLightCount = CRP.Asset.lightSettings.maxDirLightCount;
            var maxOtherLightCount = CRP.Asset.lightSettings.maxOtherLightCount;

            Init(maxDirLightCount, maxOtherLightCount);

            int dirLightCount = 0, otherLightCount = 0;

            var indexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
            var vLights = cullingResults.visibleLights;
            for (int i = 0; i < vLights.Length; i++)
            {
                var newIndex = -1;
                var vlight = vLights[i];
                switch (vlight.lightType)
                {
                    case LightType.Directional:
                        if (dirLightCount < maxDirLightCount)
                            SetupDirLight(dirLightCount++, ref vlight);
                        break;
                    case LightType.Point:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupPointLight(otherLightCount++, ref vlight);
                        }
                        break;
                    case LightType.Spot:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
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
                Cmd.SetGlobalVectorArray(_DirectionalLightDirections, dirLightDirections);
                Cmd.SetGlobalVectorArray(_DirectionalLightColors, dirLightColors);
            }

            Cmd.SetGlobalInt(_OtherLightCount, otherLightCount);
            if (otherLightCount > 0)
            {
                Cmd.SetGlobalVectorArray(_OtherLightPositions, otherLightPositions);
                Cmd.SetGlobalVectorArray(_OtherLightColors, otherLightColors);
                Cmd.SetGlobalVectorArray(_OtherLightDirections, otherLightDirections);
                Cmd.SetGlobalVectorArray(_OtherLightSpotAngles, otherLightSpotAngles);
            }
        }

        void SetupDirLight(int id,ref VisibleLight vlight)
        {
            dirLightColors[id] = vlight.finalColor.linear;
            dirLightDirections[id] = -vlight.localToWorldMatrix.GetColumn(2);
        }

        void SetupPointLight(int id, ref VisibleLight vlight)
        {
            otherLightColors[id] = vlight.finalColor;
            var pos = vlight.localToWorldMatrix.GetColumn(3);
            pos.w = 1f/(vlight.range * vlight.range + 0.0001f);
            otherLightPositions[id] = pos;
            otherLightSpotAngles[id] = new Vector4(0, 1);
        }
        void SetupSpotLight(int id, ref VisibleLight vlight)
        {
            SetupPointLight(id,ref vlight);
            otherLightDirections[id] = -vlight.localToWorldMatrix.GetColumn(2);
            var innerCos = Mathf.Cos(vlight.light.innerSpotAngle * Mathf.Deg2Rad * 0.5f);
            var outerCos = Mathf.Cos(vlight.spotAngle * Mathf.Deg2Rad * 0.5f);
            var invertRange = 1f/(innerCos - outerCos + 0.001f);
            otherLightSpotAngles[id] = new Vector4(invertRange,-outerCos * invertRange);
        }
    }
}
