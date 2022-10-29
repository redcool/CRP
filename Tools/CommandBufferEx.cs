using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace PowerUtilities
{
    public static class CommandBufferEx
    {

        public static void ClearRenderTarget(this CommandBuffer cmd, Camera camera)
        {
            cmd.ClearRenderTarget(camera.clearFlags <= CameraClearFlags.Depth,
            camera.clearFlags == CameraClearFlags.Color,
            camera.clearFlags == CameraClearFlags.Color ? camera.backgroundColor : Color.clear);
        }
    }
}
