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
        public bool isCameraTarget;

        [Header("Material")]
        public Material blitMat;
        public int pass = 0;

        public override string PassName() => nameof(BlitToTarget);

        public override void OnRender()
        {
            RenderTargetIdentifier source = isCurrentActive ? BuiltinRenderTextureType.CurrentActive : sourceName;
            RenderTargetIdentifier target = isCameraTarget ? BuiltinRenderTextureType.CameraTarget : targetName;
            //BeginSample("Blit Target");
            if (!blitMat)
            {
                Cmd.Blit(source, target);
            }
            else
            {
                Cmd.Blit(source, target, blitMat, pass);
            }

            //ExecuteCommand();
            //EndSample("Blit Target");
        }
    }
}
