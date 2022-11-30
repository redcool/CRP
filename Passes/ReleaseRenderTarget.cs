using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(ReleaseRenderTarget))]
    public class ReleaseRenderTarget : BasePass
    {
        public string[] targetNames = new[] { "_CameraTarget"} ;
        int [] targetIds;

        public override bool CanExecute()
        {
            if (camera.IsReflectionCamera())
                return false;

            return base.CanExecute();
        }

        public override void OnRender()
        {
            if (targetNames==null)
                return;

            RenderingTools.RenderTargetNameToInt(targetNames, ref targetIds);

            foreach (var item in targetIds)
            {
                Cmd.ReleaseTemporaryRT(item);
            }
        }
    }
}
