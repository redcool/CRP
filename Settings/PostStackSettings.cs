using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PowerUtilities.PostStackPass;
using UnityEngine;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_SETTINGS_ASSET_MENU_ROOT+"/"+nameof(PostStackSettings))]
    public class PostStackSettings : ScriptableObject
    {
        [Header("Bloom")]
        [Range(0.1f, 1)] public float bloomPrefilterRenderScale = 1;

        public const int MAX_ITERATORS = 16;
        [Range(0, MAX_ITERATORS)] public int maxIterates = 3;
        [Min(2)] public int minSize = 2;

        [Min(0)] public float threshold = 0.5f;
        [Range(0, 1)] public float thresholdKnee = 0.3f;
        [Range(0.1f, 100)] public float maxLuma = 100;

        [Header("Bloom Intensity")]
        public bool isHdr = true;
        public BloomMode bloomMode = BloomMode.Scatter;
        [Min(0)] public float intensity = 1;

        [Tooltip("Scatter Mode use this value")]
        [Range(0.005f, 0.99f)] public float scatter = 0.9f;

        public bool useGaussianBlur = true;
        public bool isCombineBicubicFilter;

        public Material PostStackMaterial => MaterialCacheTools.GetMaterial("Hidden/CRP/PostStack");
    }
}
