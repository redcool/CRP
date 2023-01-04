using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;



namespace PowerUtilities
{
    public partial class CRP : RenderPipeline
    {
        public const string CREATE_PASS_ASSET_MENU_ROOT = ""+nameof(CRP)+"/Passes";
        public const string CREATE_SETTINGS_ASSET_MENU_ROOT = nameof(CRP)+"Settings";

        public static CRPAsset Asset { private set; get; }
        CommandBuffer cmd = new CommandBuffer();
        List<BasePass> needCleanupList = new List<BasePass>();

        public CRP(CRPAsset asset)
        {
            Asset=asset;

            GraphicsSettings.lightsUseLinearIntensity = true;
            EditorInit();
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (!Asset || Asset.passes == null ||
                Asset.passes.Length == 0 ||
                cameras.Length == 0)
                return;

            CameraStates.cameras = cameras;

            ExecutePasses(Asset.beginPasses, ref context, cameras[0], 0);

            for (int i = 0; i < cameras.Length; i++)
            {
                CameraStates.cameraIndex = i;

                var camera = cameras[i];

                ExecutePasses(Asset.passes, ref context, camera, i);
            }
            ExecutePasses(Asset.endPasses, ref context, cameras[cameras.Length - 1], cameras.Length - 1);

            //cleanup
            Cleanup();

            cmd.Execute(ref context);
            context.Submit();
        }

        private void Cleanup()
        {
            for (int i = 0; i < needCleanupList.Count; i++)
            {
                needCleanupList[i].Cleanup();
            }
            needCleanupList.Clear();
        }

        void ExecutePasses(BasePass[] passes,ref ScriptableRenderContext context,Camera camera, int cameraId)
        {
            cmd.name = camera.name;
            cmd.BeginSampleExecute(camera.name, ref context);
            foreach (var pass in passes)
            {
                if (pass == null || pass.isSkip)
                    continue;
                
                if(pass.NeedCleanup())
                    needCleanupList.Add(pass);  

                pass.Render(ref context, camera);

                if (pass.isInterrupt)
                    break;
            }

            cmd.EndSampleExecute(camera.name, ref context);

        }

    }

}
