using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

namespace Assets.CRP.Test
{
    public class TestCameraRenderer
    {
        ScriptableRenderContext context;
        Camera camera;
        CullingResults cullingResults;
        Lazy<Material> lazyErrorMaterial = new Lazy<Material>(() => new Material(Shader.Find("Hidden/InternalErrorShader")));

        ShaderTagId[] shaderTagIds = new[] {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
        };

        ShaderTagId[] unsupportShaderTags = new[] {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };

        CommandBuffer cmd = new CommandBuffer();

        int _CameraTarget = Shader.PropertyToID("_CameraTarget");
        int _CameraTexture = Shader.PropertyToID("_CameraTexture");
        int _CameraDepthTarget = Shader.PropertyToID("_CameraDepthTarget");
        int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");

        
        public void Render(ref ScriptableRenderContext context, Camera camera)
        {
            this.context = context;
            this.camera = camera;

            cmd.name = camera.name;

            DrawSceneViewObjects();

            if (!camera.TryGetCullingParameters(out var cullingParams))
                return;

            cullingResults = context.Cull(ref cullingParams);

            Setup();
            DrawObjects();
            DrawGizmos();


            if (CanExecute(camera))
            {
                cmd.BeginSample("final blit");
                if (camera.cameraType != CameraType.Reflection)
                {
                    cmd.Blit(_CameraTarget, BuiltinRenderTextureType.CameraTarget, TestRPAsset.asset.blitMat);
                }
                cmd.EndSample("final blit");
            }

            Submit();
        }
        public void ExecuteCommand()
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        void SetupTextures()
        {
            var desc = new RenderTextureDescriptor();
            desc.width = camera.pixelWidth;
            desc.msaaSamples =1;
            desc.dimension = TextureDimension.Tex2D;
            desc.height = camera.pixelHeight;
            desc.colorFormat = RenderTextureFormat.ARGB32;

            cmd.GetTemporaryRT(_CameraTarget, desc);
            cmd.GetTemporaryRT(_CameraTexture, desc);
            cmd.GetTemporaryRT(_CameraDepthTexture, desc);

            desc.colorFormat = RenderTextureFormat.Depth;
            desc.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;
            cmd.GetTemporaryRT(_CameraDepthTarget, desc);

        }
        bool CanExecute(Camera c)
        {
            //return c.CompareTag("MainCamera") || c.cameraType == CameraType.SceneView;
            return c.cameraType != CameraType.Reflection;
        }
        public void Setup()
        {
            context.SetupCameraProperties(camera);

            if (CanExecute(camera))
            {
                SetupTextures();
                cmd.SetRenderTarget(_CameraTarget, (RenderTargetIdentifier)_CameraDepthTarget);
            }
            
            cmd.ClearRenderTarget(camera.clearFlags <= CameraClearFlags.Depth,
                camera.clearFlags == CameraClearFlags.Color,
                camera.clearFlags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);

            cmd.BeginSample(cmd.name);
            ExecuteCommand();
        }

        public void DrawSceneViewObjects()
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif
        }

        public void DrawObjects()
        {
            var sortingSettings = new SortingSettings(camera);
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            var drawSettings = new DrawingSettings();
            for (int i = 0; i < shaderTagIds.Length; i++)
            {
                drawSettings.SetShaderPassName(i, shaderTagIds[i]);
            }
            drawSettings.sortingSettings = sortingSettings;

            var filterSettings = new FilteringSettings(RenderQueueRange.opaque, camera.cullingMask);

            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);

            context.DrawSkybox(camera);

            if(CanExecute(camera))
                BlitTextures();

            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);


            drawSettings.overrideMaterial = lazyErrorMaterial.Value;
            for (int i = 0; i < unsupportShaderTags.Length; i++)
            {
                drawSettings.SetShaderPassName(i, unsupportShaderTags[i]);
            }
            filterSettings.renderQueueRange = RenderQueueRange.all;
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
        }

        private void BlitTextures()
        {
            cmd.Blit(_CameraTarget, _CameraTexture);
            cmd.Blit(_CameraDepthTarget, _CameraDepthTexture);

            cmd.SetRenderTarget(_CameraTarget, _CameraDepthTarget);
            ExecuteCommand();
        }

        public void DrawGizmos()
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
#endif
        }

        public void Submit()
        {
            cmd.EndSample(cmd.name);
            ExecuteCommand();
            context.Submit();
        }
    }


    public class TestRP : RenderPipeline
    {
        TestCameraRenderer renderer = new TestCameraRenderer();
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                renderer.Render(ref context, camera);
            }
        }
    }
}
