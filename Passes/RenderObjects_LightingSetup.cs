using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    /// <summary>
    /// RenderObjects Light setup
    /// </summary>
    public partial class RenderObjects
    {
        const string LightingSetup = nameof(LightingSetup);
        static int _DirectionalLightCount = Shader.PropertyToID(nameof(_DirectionalLightCount));
        static int _DirectionalLightColors = Shader.PropertyToID(nameof(_DirectionalLightColors));
        static int _DirectionalLightDirections = Shader.PropertyToID(nameof(_DirectionalLightDirections));

        [Header("Light")]
        public bool updateLights;
        [Range(1,8)]public int maxLightCount = 8;

        Vector4[] dirLightColors;
        Vector4[] dirLightDirections;

        public void SetupLighting()
        {
            Cmd.BeginSample(LightingSetup);
            SetupLights();
            Cmd.EndSampleExecute(LightingSetup,ref context);
        }

        void SetupLights()
        {
            if(dirLightColors == null || dirLightColors.Length ==0)
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

                if (i >= maxLightCount)
                    break;
            }

            Cmd.SetGlobalVectorArray(_DirectionalLightDirections, dirLightDirections);
            Cmd.SetGlobalVectorArray(_DirectionalLightColors, dirLightColors);
            Cmd.SetGlobalInt(_DirectionalLightCount, i);
        }

        private void SetupLight(ref VisibleLight vlight,int id)
        {
            dirLightColors[id] = vlight.finalColor.linear;
            dirLightDirections[id] = -vlight.localToWorldMatrix.GetColumn(2);
        }
    }
}
