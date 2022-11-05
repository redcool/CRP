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
            Submit();
        }
        public void ExecuteCommand()
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        public void Setup()
        {
            context.SetupCameraProperties(camera);
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
}
