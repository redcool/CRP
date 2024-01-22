using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities.CRP
{

    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(BlitToTarget))]
    public class BlitToTarget : BasePass
    {
        [Header(nameof(BlitToTarget))]

        [Header("Source")]
        [Tooltip("set empty will use CurrentActive")]
        public string sourceName;

        [Header("Target")]
        [Tooltip("set empty will use CameraTarget")]
        public string targetName;

        [Header("Material")]
        public Material blitMat;

        [Range(0, 15)]
        public int pass = 0;
        public bool isApplyColorGrading;

        public override bool CanExecute()
        {
            if (camera.IsReflectionCamera())
                return false;
            if (!blitMat)
                return false;

            return base.CanExecute();
        }


        public override void OnRender()
        {
            var isCurrentActive = string.IsNullOrEmpty(sourceName);
            var isCameraTarget = string.IsNullOrEmpty(targetName);
            RenderTargetIdentifier sourceId = isCurrentActive ? (RenderTargetIdentifier)BuiltinRenderTextureType.CurrentActive : Shader.PropertyToID(sourceName);
            RenderTargetIdentifier targetId = isCameraTarget ? (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget : Shader.PropertyToID(targetName);


            var finalSrcMode = BlendMode.One;
            var finalDstMode = BlendMode.Zero;
            var cameraData = camera.GetComponent<CRPCameraData>();
            if (cameraData)
            {
                finalSrcMode = cameraData.finalSrcMode;
                finalDstMode = cameraData.finalDstMode;
            }

            Cmd.SetGlobalFloat(SetupColorGradingLUT._ApplyColorGrading, isApplyColorGrading ? 1 : 0);
            Cmd.SetGlobalFloat(ShaderPropertyIds._LinearToGamma, 0);
            Cmd.BlitTriangle(sourceId, targetId, blitMat, pass, camera, finalSrcMode, finalDstMode);
        }

    }
}
