using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities.CRP
{
    [CreateAssetMenu(menuName = CRP.CRP_ROOT_PATH + "/CreateRPAsset")]
    public partial class CRPAsset : RenderPipelineAsset
    {
        [Header("First camera passes")]
        public BasePass[] beginPasses;
        
        [Header("Rendering Passes")]
        public BasePass[] passes;

        [Header("End camera passes")]
        public BasePass[] endPasses;

        [Header("Light Settings")]
        public DirectionalLightSettings directionalLightSettings;
        public OtherLightSettings otherLightSettings;

        [Header("Pipeline Settings")]
        public PipelineSettings pipelineSettings;

        protected override RenderPipeline CreatePipeline()
        {
            return new CRP(this);
        }

    }
}