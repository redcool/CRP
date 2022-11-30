using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(SetRenderTarget))]
    public class SetRenderTarget : BasePass
    {
        [Header("Render Targets")]
        public string[] targetNames;
        RenderTargetIdentifier[] targetIds;

        public string depthTargetName;

        [Header("Clear Options")]
        [Tooltip("Clear target use camera clear settings")]
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
            if (targetNames == null || targetNames.Length ==0)
                return;

            RenderingTools.RenderTargetNameToIdentifier(targetNames, ref targetIds);
            var depthTargetId = string.IsNullOrEmpty(depthTargetName) ? targetIds[0] : new RenderTargetIdentifier(depthTargetName);

            Cmd.SetRenderTarget(targetIds, depthTargetId);
            if (clearTarget)
            {
                Cmd.ClearRenderTarget(camera);
            }
        }
    }
}
