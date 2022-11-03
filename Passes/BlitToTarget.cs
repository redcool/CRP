using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(BlitToTarget))]
    public class BlitToTarget : BasePass
    {
        [Header("Blit Options")]
        [Tooltip("Blit run after last camera")]
        public bool isFinalBlit = true;

        [Tooltip("After blit, restore cameraTarget to SourceName")]
        public bool isRestoreCameraTarget;

        [Header("Source")]
        public string sourceName;
        public bool isCurrentActive;

        [Header("Target")]
        public string targetName;
        public bool isDefaultCameraTarget;
        public bool isNeedCreateTarget;

        [Header("Material")]
        public Material blitMat;
        [Range(0, 15)]
        public int pass = 0;

        public override bool CanExecute()
        {
            var isSourceOk = isCurrentActive || !string.IsNullOrEmpty(sourceName);
            var isTargetOk = isDefaultCameraTarget || !string.IsNullOrEmpty(targetName);
            var isFinalOk = !isFinalBlit || (isFinalBlit && PassTools.IsFinalCamera());
            return isSourceOk &&  isTargetOk && isFinalOk;
        }

        public override string PassName() => isFinalBlit ? "FinalBlit" : base.PassName();

        public override void OnRender()
        {
            var sourceNameId = Shader.PropertyToID(sourceName);
            var targetNameId = Shader.PropertyToID(targetName);
            
            RenderTargetIdentifier sourceId = isCurrentActive ? BuiltinRenderTextureType.CurrentActive : sourceNameId;
            RenderTargetIdentifier targetId = isDefaultCameraTarget ? BuiltinRenderTextureType.CameraTarget : targetNameId;

            if(!isDefaultCameraTarget && isNeedCreateTarget)
            {
                targetId = targetNameId;
                Cmd.GetTemporaryRT(targetNameId, camera.pixelWidth, camera.pixelHeight);
            }

            if (!blitMat)
            {
                Cmd.Blit(sourceId, targetId);
            }
            else
            {
                Cmd.Blit(sourceId, targetId, blitMat, pass);
            }

            if (isRestoreCameraTarget)
            {
                Cmd.SetRenderTarget(sourceNameId);
            }

        }
    }
}
