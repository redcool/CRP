﻿using System;
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

        public static CRPAsset asset;

        public CRP(CRPAsset asset)
        {
            CRP.asset=asset;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (!asset || asset.passes == null || asset.passes.Length == 0)
                return;

            PassTools.cameras = cameras;

            ExecutePasses(asset.beginPasses, ref context, cameras[0], 0);

            //foreach (var camera in cameras)
            for (int i = 0; i < cameras.Length; i++)
            {
                PassTools.cameraIndex = i;

                var camera = cameras[i];
                ExecutePasses(asset.passes, ref context, camera, i);
            }
            ExecutePasses(asset.endPasses, ref context, cameras[cameras.Length-1], cameras.Length-1);

            context.Submit();
        }

        void ExecutePasses(BasePass[] passes,ref ScriptableRenderContext context,Camera camera, int cameraId)
        {
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

        }

    }

}
