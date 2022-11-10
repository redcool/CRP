using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/RenderObjects")]
    public partial class RenderObjects : BasePass
    {
        [Header("Render Options")]
        public bool drawOpaques = true;
        public bool drawSkybox = true;
        public bool drawTransparents = true;
        public bool drawUnsupportObjects=true;
        public bool drawGizmos=true;

        public string[] supportLightModeTags = new[] {
            "SRPDefaultUnlit",
            "UniversalForward",
            "UniversalForwardOnly"
        };
        public string[] unsupportLightModeTags = new[] { 
        "Always",
        "ForwardBase",
        "PrepassBase"
        };

        [Header("Batch Options")]
        public bool enableDynamicBatch;
        public bool enableInstanced;
        public bool enableSRPBatch = true;

        CullingResults cullingResults;
        ShaderTagId[] supportLightModeTagIds;
        ShaderTagId[] unsupportLightModeTagIds;

        public static void SetupDrawingSettings(ref DrawingSettings drawSettings,
            ShaderTagId[] supportLightModeTagIds,
            bool enableDynamicBatch,bool enableInstanced,bool enableSRPBatch)
        {
            drawSettings.enableDynamicBatching = enableDynamicBatch;
            drawSettings.enableInstancing = enableInstanced;
            GraphicsSettings.useScriptableRenderPipelineBatching = enableSRPBatch;

            for (int i = 0; i < supportLightModeTagIds.Length; i++)
            {
                drawSettings.SetShaderPassName(i, supportLightModeTagIds[i]);
            }
        }



        public override void OnRender()
        {

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif

            if (!camera.TryGetCullingParameters(out var cullingParams))
            {
                return;
            }
            cullingResults = context.Cull(ref cullingParams);

            if (updateLights)
                SetupLighting();

            StartRenderObjects();
        }

        private void StartRenderObjects()
        {
            RenderingTools.ShaderTagNameToId(supportLightModeTags, ref supportLightModeTagIds);
            RenderingTools.ShaderTagNameToId(unsupportLightModeTags, ref unsupportLightModeTagIds);

            var sortingSettings = new SortingSettings(camera);
            var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
            var drawSettings = new DrawingSettings();

            SetupDrawingSettings(ref drawSettings, supportLightModeTagIds, enableDynamicBatch, enableInstanced, enableSRPBatch);

            if (drawOpaques)
                DrawOpaques(ref sortingSettings, ref drawSettings, ref filterSettings);

            if (drawSkybox)
                context.DrawSkybox(camera);

            if (drawTransparents)
                DrawTransparents(ref sortingSettings, ref drawSettings, ref filterSettings);

            if (drawUnsupportObjects)
                DrawErrorObjects(ref sortingSettings, ref drawSettings, ref filterSettings);

            if (drawGizmos)
                DrawGizmos();
        }

        void DrawOpaques(ref SortingSettings sortSettings ,ref DrawingSettings drawSettings,ref FilteringSettings filterSettings)
        {
            sortSettings.criteria = SortingCriteria.CommonOpaque;
            
            drawSettings.sortingSettings = sortSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            filterSettings.layerMask = camera.cullingMask;

            Cmd.BeginSampleExecute(nameof(DrawOpaques),ref context);
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
            Cmd.EndSampleExecute(nameof(DrawOpaques), ref context);
        }

        void DrawTransparents(ref SortingSettings sortSettings, ref DrawingSettings drawSettings, ref FilteringSettings filterSettings)
        {
            sortSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortSettings;

            filterSettings.layerMask = camera.cullingMask;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;

            Cmd.BeginSampleExecute(nameof(DrawTransparents), ref context);
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
            Cmd.EndSampleExecute(nameof(DrawTransparents), ref context);
        }

        void DrawErrorObjects(ref SortingSettings sortSettings, ref DrawingSettings drawSettings, ref FilteringSettings filterSettings)
        {
            sortSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortSettings;

            drawSettings.overrideMaterial = RenderingTools.ErrorMaterial;
            for (int i = 0; i < unsupportLightModeTagIds.Length; i++)
            {
                drawSettings.SetShaderPassName(i, unsupportLightModeTagIds[i]);
            }
            filterSettings.layerMask = camera.cullingMask;
            filterSettings.renderQueueRange = RenderQueueRange.all;

            Cmd.BeginSampleExecute(nameof(DrawErrorObjects), ref context);
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
            Cmd.EndSampleExecute(nameof(DrawErrorObjects), ref context);
        }

        void DrawGizmos()
        {
#if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
#endif
        }

    }
}
