using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PowerUtilities
{
    public class CRP : RenderPipeline
    {
        public const string CREATE_PASS_ASSET_MENU_ROOT = ""+nameof(CRP)+"/Passes";

        //public CameraRenderer renderer = new CameraRenderer();
        public static CRPAsset asset;

        public BasePass[] passes;

        public CRP(CRPAsset asset, BasePass[] passes)
        {
            this.passes = passes;
            CRP.asset=asset;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (passes == null || passes.Length == 0)
                return;

            PassTools.cameras = cameras;

            //foreach (var camera in cameras)
            for (int i = 0; i < cameras.Length; i++)
            {
                PassTools.cameraIndex = i;

                var camera = cameras[i];
                BasePass.Cmd.name = camera.name;
                BasePass.Cmd.BeginSampleExecute(camera.name, ref context);
                foreach (var pass in passes)
                {
                    if (pass == null || pass.isSkip)
                        continue;

                    pass.Render(ref context, camera);

                    if (pass.isInterrupt)
                        break;
                }

                BasePass.Cmd.EndSampleExecute(camera.name, ref context);
                context.Submit();
            }
        }

    }

}
