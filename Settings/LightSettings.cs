﻿using System;
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
        public enum FilterMode { 
            PCF2,PCF3,PCF5,PCF7
        }
        public enum CascadeBlendMode
        {
            Hard,Soft,Dither
        }

        [Header("Lights")]
        [Range(1, 8)] public int maxDirLightCount = 8;
        [Range(0,64)]public int maxOtherLightCount = 64;

        [Header("Light Shadow")]
        [Range(0, 8)] public int maxShadowedDirLightCount = 8;
        [Range(1, 4)] public int maxCascades = 4;
        
        [Range(0,1)]public float cascadeRatio1=0.1f,cascadeRatio2=0.25f, cascadeRatio3=0.5f;
        public Vector3 CascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
        [Range(0.001f, 1f)] public float cascadeFade = 0.2f;

        public TextureSize atlasSize = TextureSize._1k;
        [Min(0.001f)]public float maxShadowDistance = 100;
        [Range(0.001f, 1)] public float distanceFade = 0.1f;

        public FilterMode filterMode;
        public CascadeBlendMode cascadeBlendMode;
    }

}