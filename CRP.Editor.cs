using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace PowerUtilities.CRP
{
    public partial class CRP
    {
        partial void EditorInit();

#if UNITY_EDITOR
        partial void EditorInit()
        {
            //Lightmapping.SetDelegate(lightsDelegate);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Lightmapping.ResetDelegate();
        }
        static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] requests, NativeArray<LightDataGI> lightsOutput) => {
            var dataGI = new LightDataGI();
            for (int i = 0; i < requests.Length; i++)
            {
                var light = requests[i];

                switch (light.type)
                {
                    case UnityEngine.LightType.Directional:
                        var dirLight = new DirectionalLight();
                        LightmapperUtils.Extract(light, ref dirLight);
                        dataGI.Init(ref dirLight);
                        break;
                    case UnityEngine.LightType.Point:
                        var pointLight = new PointLight();
                        LightmapperUtils.Extract(light, ref pointLight);
                        dataGI.Init(ref pointLight);
                        break;

                    case UnityEngine.LightType.Spot:
                        var spotLight = new SpotLight();
                        LightmapperUtils.Extract(light, ref spotLight);
                        dataGI.Init(ref spotLight);
                        break;
                    case UnityEngine.LightType.Area:
                        var areaLight = new RectangleLight();
                        areaLight.mode = LightMode.Baked;
                        LightmapperUtils.Extract(light, ref areaLight);
                        dataGI.Init(ref areaLight);
                        break;
                    default:
                        dataGI.InitNoBake(light.GetInstanceID());
                        break;
                }
                dataGI.falloff = FalloffType.InverseSquared;
                lightsOutput[i] = dataGI;
            }
        };
#endif
    }
}
