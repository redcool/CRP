using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = nameof(CRP)+"/CreateRPAsset")]
    public class CRPAsset : RenderPipelineAsset
    {

        public float renderScale = 1;

        public BasePass[] passes;
        protected override RenderPipeline CreatePipeline()
        {
            return new CRP(this, passes);
        }

    }
}