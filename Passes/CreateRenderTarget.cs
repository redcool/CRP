using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+ nameof(CreateRenderTarget))]
    public class CreateRenderTarget : BasePass
    {
        [Header(nameof(CreateRenderTarget))]
        public string[] targetNames = new[] { "_CameraTarget" };
        [Tooltip("rt has depth buffer?")]
        public bool isTargetHasDepth;

        [Header("Depth Target")]
        public string[] depthTargetNames = new[] { "_CameraDepthTarget" };

        int[] targetIds;
        int[] depthIds;

        [Range(0.1f,2f)]
        public float renderScale = 1;

        public override bool CanExecute()
        {
            var isok = base.CanExecute();
            return isok;
        }

        public override void OnRender()
        {
            RenderingTools.RenderTargetNameToInt(targetNames, ref targetIds);
            Cmd.CreateTargets(camera, targetIds, renderScale,isTargetHasDepth);

            RenderingTools.RenderTargetNameToInt(depthTargetNames, ref depthIds);
            Cmd.CreateDepthTargets(camera, depthIds, renderScale);
        }
    }
}
