using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities.CRP
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(SetRenderTarget))]
    public class SetRenderTarget : BasePass
    {
        [Header("Render Targets")]
        [Tooltip("set empty will set CameraTarget")]
        public string[] targetNames;
        RenderTargetIdentifier[] targetIds;

        [Tooltip("set empty will set CameraTarget")]
        public string depthTargetName;

        [Header("Clear Options")]
        [Tooltip("Clear target use camera clear settingList")]
        public bool clearTarget;

        public override bool CanExecute()
        {
            if (camera.IsReflectionCamera())
                return false;

            var isok = base.CanExecute();
            return isok;
        }

        public override void OnRender()
        {
            var isCameraTarget = (targetNames == null || targetNames.Length == 0);
            if (isCameraTarget)
            {
                targetIds = new RenderTargetIdentifier[] { BuiltinRenderTextureType.CameraTarget };
            }
            else
            {
                RenderingTools.RenderTargetNameToIdentifier(targetNames, ref targetIds);
            }

            var depthTargetId = string.IsNullOrEmpty(depthTargetName) ? BuiltinRenderTextureType.CameraTarget : new RenderTargetIdentifier(depthTargetName);
            Cmd.SetRenderTarget(targetIds, depthTargetId);


            if (clearTarget)
            {
                Cmd.ClearRenderTarget(camera);
            }
        }
    }
}
