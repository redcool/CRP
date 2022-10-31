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

        public static void ClearRenderTarget(this CommandBuffer cmd, Camera camera, float depth = 1, uint stencil = 0)
        {
            var isClearDepth = camera.clearFlags <= CameraClearFlags.Depth;
            var isClearColor = camera.clearFlags == CameraClearFlags.Color;
            var backColor = isClearColor ? camera.backgroundColor : Color.clear;
            var flags = RTClearFlags.None;
            if (isClearColor)
                flags |= RTClearFlags.Color;
            if (isClearDepth)
                flags |= RTClearFlags.DepthStencil;
            cmd.ClearRenderTarget(flags, backColor, depth, stencil);

            //cmd.ClearRenderTarget(camera.clearFlags <= CameraClearFlags.Depth,
            //camera.clearFlags == CameraClearFlags.Color,
            //camera.clearFlags == CameraClearFlags.Color ? camera.backgroundColor : Color.clear
            //);
        }

        public static void Execute(this CommandBuffer cmd,ref ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public static void BeginSampleExecute(this CommandBuffer cmd, string sampleName,ref ScriptableRenderContext context)
        {
            cmd.BeginSample(sampleName);
            cmd.Execute(ref context);
        }
        public static void EndSampleExecute(this CommandBuffer cmd,string sampleName,ref ScriptableRenderContext context)
        {
            cmd.EndSample(sampleName);
            cmd.Execute(ref context);
        }
    }
}
