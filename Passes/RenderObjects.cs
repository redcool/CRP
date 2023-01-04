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
    public class RenderObjects : BasePass
    {
        [Header("Render Options")]
        [Tooltip("draw opaque objects(render queue <= 2000)")]
        public bool drawOpaques = true;

        [Tooltip("draw skybox (render queue = 2450")]
        public bool drawSkybox = true;
        
        [Tooltip("draw transparent objects (render queue >= 3000)")]
        public bool drawTransparents = true;

        [Tooltip("draw unsupport object use InternalErrorShader")]
        public bool drawUnsupportObjects=true;

        [Tooltip("draw gizmos in unity editor")]
        public bool drawGizmos=true;

        [Tooltip("supported shaders (assigned by shader pass tags LightMode)")]
        public string[] supportLightModeTags = new[] {
            "SRPDefaultUnlit",
            "UniversalForward",
            "UniversalForwardOnly"
        };

        [Tooltip("unsupported shaders (assigned by shader pass tags LightMode)")]
        public string[] unsupportLightModeTags = new[] { 
        "Always",
        "ForwardBase",
        "PrepassBase"
        };

        [Header("Batch Options")]
        public bool enableDynamicBatch;
        public bool enableInstanced;
        public bool enableSRPBatch = true;

        [Tooltip("assign data for single instance")]
        public PerObjectData perObjectData = PerObjectData.None;

        //CullingResults cullingResults;
        ShaderTagId[] supportLightModeTagIds;
        ShaderTagId[] unsupportLightModeTagIds;

        public static void SetupDrawingSettings(ref DrawingSettings drawSettings,
            ShaderTagId[] supportLightModeTagIds,
            bool enableDynamicBatch,
            bool enableInstanced,
            bool enableSRPBatch,
            PerObjectData perObjectData)
        {
            drawSettings.enableDynamicBatching = enableDynamicBatch;
            drawSettings.enableInstancing = enableInstanced;
            GraphicsSettings.useScriptableRenderPipelineBatching = enableSRPBatch;
            drawSettings.perObjectData = perObjectData;

            for (int i = 0; i < supportLightModeTagIds.Length; i++)
            {
                drawSettings.SetShaderPassName(i, supportLightModeTagIds[i]);
            }
        }



        public override void OnRender()
        {
            if (IsCullingResultsValid())
                StartRenderObjects();
        }

        private void StartRenderObjects()
        {
            RenderingTools.ShaderTagNameToId(supportLightModeTags, ref supportLightModeTagIds);
            RenderingTools.ShaderTagNameToId(unsupportLightModeTags, ref unsupportLightModeTagIds);

            var sortingSettings = new SortingSettings(camera);
            var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
            var drawSettings = new DrawingSettings();

            SetupDrawingSettings(ref drawSettings, supportLightModeTagIds, enableDynamicBatch, enableInstanced, enableSRPBatch,perObjectData);

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
