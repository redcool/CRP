using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/TestPass")]
    public class TestPass : BasePass
    {
        public SetRenderTarget setRenderTargetPass;
        public override void OnRender()
        {
            context.DrawSkybox(camera);

            Cmd.BeginSampleExecute("Test",ref context);
            
            Cmd.Blit(BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.CameraTarget);
            Cmd.EndSampleExecute("Test",ref context);
        }

    }
}
 