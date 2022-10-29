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
        

        CullingResults cullingResults;
        ShaderTagId[] supportLightModeTagIds;
        ShaderTagId[] unsupportLightModeTagIds;

        public static ShaderTagId[] InitShaderTagIdss(string[] shaderTags)
        {
            var tagIds = new ShaderTagId[shaderTags.Length];
            for (int i = 0; i < shaderTags.Length; i++)
            {
                tagIds[i] = new ShaderTagId(shaderTags[i]);
            }
            return tagIds;
        }

        public override void OnRender()
        {
#if UNITY_EDITOR
            if(camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif
            if(!camera.TryGetCullingParameters(out var cullingParams))
            {
                return;
            }
            cullingResults = context.Cull(ref cullingParams);

            supportLightModeTagIds = InitShaderTagIdss(supportLightModeTags);
            unsupportLightModeTagIds = InitShaderTagIdss(unsupportLightModeTags);


            var sortingSettings = new SortingSettings(camera);
            var drawSettings = new DrawingSettings();
            var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
            DrawOpaques(ref sortingSettings, ref drawSettings, ref filterSettings);

            context.DrawSkybox(camera);

            DrawTransparents(ref sortingSettings, ref drawSettings, ref filterSettings);
            DrawErrorObjects(ref sortingSettings, ref drawSettings, ref filterSettings);
            DrawGizmos();

            ExecuteCommand();
        }

        void DrawOpaques(ref SortingSettings sortSettings ,ref DrawingSettings drawSettings,ref FilteringSettings filterSettings)
        {
            sortSettings.criteria = SortingCriteria.CommonOpaque;
            
            drawSettings.sortingSettings = sortSettings;

            for (int i = 0; i < supportLightModeTagIds.Length; i++)
            {
                drawSettings.SetShaderPassName(i,supportLightModeTagIds[i]);
            }

            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            filterSettings.layerMask = camera.cullingMask;

            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
        }

        void DrawTransparents(ref SortingSettings sortSettings, ref DrawingSettings drawSettings, ref FilteringSettings filterSettings)
        {
            sortSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortSettings;

            filterSettings.layerMask = camera.cullingMask;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;

            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
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

            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
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
