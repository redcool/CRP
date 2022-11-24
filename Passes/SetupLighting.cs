using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(SetupLighting))]
    public class SetupLighting : BasePass
    {
        public static readonly int
            _DirectionalLightCount = Shader.PropertyToID(nameof(_DirectionalLightCount)),
            _DirectionalLightColors = Shader.PropertyToID(nameof(_DirectionalLightColors)),
            _DirectionalLightDirections = Shader.PropertyToID(nameof(_DirectionalLightDirections))
            ;

        Vector4[] dirLightColors;
        Vector4[] dirLightDirections;

        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            SetupLights();
        }

        void SetupLights()
        {
            int maxLightCount =  CRP.Asset.lightSettings.maxLightCount;

            if (dirLightColors == null || dirLightColors.Length != maxLightCount)
            {
                dirLightColors = new Vector4[maxLightCount];
                dirLightDirections = new Vector4[maxLightCount];
            }

            var vLights = cullingResults.visibleLights;
            var count = Mathf.Min(maxLightCount, vLights.Length);
            int i = 0;
            for (; i < count; i++)
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
        
    }
}
