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
            BeginSample("Test");
            
            context.DrawSkybox(camera);

            Cmd.Blit(BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.CameraTarget);

            EndSample("Test");
        }
    }
}
