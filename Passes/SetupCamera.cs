using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(SetupCamera))]
    public class SetupCamera : BasePass
    {

        public override void OnRender()
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif
            context.SetupCameraProperties(camera);
            Cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            if (camera.TryGetCullingParameters(out var cullingParams))
            {
                cullingParams.shadowDistance = Mathf.Min(CRP.Asset.directionalLightSettings.maxShadowDistance, camera.farClipPlane);
                cullingResults = context.Cull(ref cullingParams);
            }
        }
    }
}
