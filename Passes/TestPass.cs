using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities.CRP
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(TestPass))]
    public class TestPass : BasePass
    {
        public Material mat;

        public override void OnRender()
        {
            //var d1 = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight, RenderTextureFormat.Default, 0,0);
            //Debug.Log(d1);
            //RenderTextureDescriptor desc = d1;// 
            ////desc.SetupColorDescriptor(camera);


            var sourceId = Shader.PropertyToID("_CameraTarget");
            //Cmd.GetTemporaryRT(sourceId,camera.pixelWidth,camera.pixelHeight, 32,FilterMode.Bilinear, RenderTextureFormat.Default);
            //Cmd.GetTemporaryRT(sourceId, desc);
            //ExecuteCommand();

            Cmd.SetRenderTarget(sourceId);
            ExecuteCommand();
            context.DrawSkybox(camera);

            //Cmd.Blit(sourceId,BuiltinRenderTextureType.CameraTarget);
            Cmd.SetGlobalFloat("_ApplyColorGrading", 0);
            Cmd.BlitTriangle(sourceId, BuiltinRenderTextureType.CameraTarget, mat, 0, camera);
            //Cmd.ReleaseTemporaryRT(sourceId);
        }

    }
}
 