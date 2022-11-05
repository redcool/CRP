using Assets.CRP.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace Assets.CRP.Test
{

    [CreateAssetMenu(menuName = "Rendering/TestRPAsset")]
    public class TestRPAsset : RenderPipelineAsset
    {
        public Material blitMat;
        public static TestRPAsset asset;
        protected override RenderPipeline CreatePipeline()
        {
            asset = this;
            return new TestRP();
        }
    }
}
