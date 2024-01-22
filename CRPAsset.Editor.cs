#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities.CRP
{
    partial class CRPAsset
    {
        static string[] renderingLayerNames;
        static CRPAsset()
        {
            renderingLayerNames = new string[32];
            for (int i = 0; i < 31; i++)
            {
                renderingLayerNames[i] = "Layer " + (i+1);
            }
        }
        public override string[] renderingLayerMaskNames => renderingLayerNames;
#if UNITY_2021_1_OR_NEWER
        public override string[] prefixedRenderingLayerMaskNames => renderingLayerNames;
#endif
    }
}
#endif