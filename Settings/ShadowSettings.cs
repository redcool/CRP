using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    [Serializable]
    public class LightSettings
    {
        public enum TextureSize
        {
            _128 = 128,
            _256 = _128*2,
            _512 = _256*2,
            _1k = _512*2,
            _2k = _1k*2,
            _4k = _2k*2,
            _8k = _4k * 2
        }

        [Header("Light")]
        [Range(1, 8)] public int maxLightCount = 8;

        [Header("Shadow")]
        [Range(0, 8)] public int maxShadowedDirLightCount = 8;
        [Range(1, 4)] public int maxCascades = 4;
        [Range(0.001f, 1f)] public float cascadeFade = 0.2f;

        public TextureSize atlasSize = TextureSize._1k;
        [Min(0.001f)]public float maxShadowDistance = 100;
    }
}
