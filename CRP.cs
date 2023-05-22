using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;



namespace PowerUtilities.CRP
{
    public partial class CRP : RenderPipeline
    {
        public const string CRP_ROOT_PATH = "PowerUtilities/" + nameof(CRP);
        public const string CREATE_PASS_ASSET_MENU_ROOT = CRP_ROOT_PATH + "/Passes";
        public const string CREATE_SETTINGS_ASSET_MENU_ROOT = CRP_ROOT_PATH + "/Settings";
        public const string CREATE_EDITOR_PASS_ASSET_ROOT = CREATE_PASS_ASSET_MENU_ROOT + "/EditorPasses";

        public static CRPAsset Asset { private set; get; }
        CommandBuffer cmd = new CommandBuffer();

        List<BasePass> pipelineCleanupList = new List<BasePass>();
        List<BasePass> cameraCleanupList = new List<BasePass>();

        public CRP(CRPAsset asset)
        {
            Asset = asset;

            GraphicsSettings.useScriptableRenderPipelineBatching = asset.pipelineSettings.useSRPBatch;
            GraphicsSettings.lightsUseLinearIntensity = asset.pipelineSettings.lightsUseLinearColor;
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
                //camera cleanup
                CameraCleanup();
            }
            ExecutePasses(Asset.endPasses, ref context, cameras[cameras.Length - 1], cameras.Length - 1);

            //cleanup
            PipelineCleanup();

            cmd.Execute(ref context);
            context.Submit();
        }

        private void CameraCleanup()
        {
            for (int i = 0; i < cameraCleanupList.Count; i++)
            {
                cameraCleanupList[i].CameraCleanup();
            }
            cameraCleanupList.Clear();
        }

        private void PipelineCleanup()
        {
            for (int i = 0; i < pipelineCleanupList.Count; i++)
            {
                pipelineCleanupList[i].PipelineCleanup();
            }
            pipelineCleanupList.Clear();
        }

        void ExecutePasses(BasePass[] passes, ref ScriptableRenderContext context, Camera camera, int cameraId)
        {
            cmd.name = camera.name;
            cmd.BeginSampleExecute(camera.name, ref context);
            foreach (var pass in passes)
            {
                if (pass == null || pass.IsSkip())
                    continue;

                if (pass.IsNeedPipelineCleanup())
                    pipelineCleanupList.Add(pass);

                if (pass.IsNeedCameraCleanup())
                    cameraCleanupList.Add(pass);

                pass.Render(ref context, camera);

                if (pass.IsInterrupt())
                    break;
            }

            cmd.EndSampleExecute(camera.name, ref context);

        }

    }

}
