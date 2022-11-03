using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/SetRenderTarget")]
    public class SetRenderTarget : BasePass
    {

        [Header("Apply Camera Props")]
        public bool isSetupCameraProperties = true;

        [Header("Render Targets")]
        public string[] targetNames;
        RenderTargetIdentifier[] targetIds;

        [Header("Clear Options")]
        [Tooltip("Clear target use camera clear settings")]
        public bool clearTarget;

        public override void OnRender()
        {
            if (isSetupCameraProperties)
            {
                context.SetupCameraProperties(camera);
            }

            if (targetNames == null || targetNames.Length ==0)
                return;

            RenderingTools.RenderTargetNameToIdentifier(targetNames, ref targetIds);

            Cmd.SetRenderTarget(targetIds, targetIds[0]);

            if (clearTarget)
            {
                Cmd.ClearRenderTarget(camera);
            }
        }
    }
}
