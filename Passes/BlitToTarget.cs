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
        [Header("Source")]
        public string sourceName;
        public bool isCurrentActive;

        [Header("Target")]
        public string targetName;
        public bool isDefaultCameraTarget;
        //public bool isNeedCreateTarget;

        [Header("Material")]
        public Material blitMat;
        [Range(0, 15)]
        [Tooltip("ToneMappingPass is None,use this")]
        public int pass = 0;

        [Header("ToneMapping")]
        [Tooltip("ToneMapping should apply the last blit pass")]
        public ToneMappingPass toneMappingPass;

        public override bool CanExecute()
        {
            if (camera.IsReflectionCamera())
                return false;

            var isSourceOk = isCurrentActive || !string.IsNullOrEmpty(sourceName);
            var isTargetOk = isDefaultCameraTarget || !string.IsNullOrEmpty(targetName);
            return base.CanExecute() && isSourceOk && isTargetOk;
        }


        public override void OnRender()
        {
            var sourceNameId = Shader.PropertyToID(sourceName);
            var targetNameId = Shader.PropertyToID(targetName);

            RenderTargetIdentifier sourceId = isCurrentActive ? BuiltinRenderTextureType.CurrentActive : sourceNameId;
            RenderTargetIdentifier targetId = isDefaultCameraTarget ? BuiltinRenderTextureType.CameraTarget : targetNameId;

            //if (!isDefaultCameraTarget && isNeedCreateTarget)
            //{
            //    targetId = targetNameId;
            //    Cmd.GetTemporaryRT(targetNameId, camera.pixelWidth, camera.pixelHeight);
            //}

            Blit(sourceId, targetId,blitMat,GetPassId());
        }

        bool IsApplyTone() => CRP.Asset.pipelineSettings.isHdr && toneMappingPass != ToneMappingPass.None;
        int GetPassId() => IsApplyTone() ? (int)toneMappingPass : pass;

        private void Blit(RenderTargetIdentifier sourceId, RenderTargetIdentifier targetId,Material blitMat,int pass)
        {
            if (!blitMat)
            {
                Cmd.Blit(sourceId, targetId);
            }
            else
            {
                Cmd.Blit(sourceId, targetId, blitMat, pass);
            }
        }

    }
}
