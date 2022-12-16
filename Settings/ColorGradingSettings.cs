using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [Serializable]
    public class ColorAdjustSettings
    {
        public float exposure;

        [ColorUsage(false, true)] public Color colorFilter = Color.white;

        [Range(-2, 2)]
        public float contrast;

        [Header("HSV adjust")]
        public float hueScale = 1;
        [Range(-180,180)] public float hueOffset = 0;
        [Min(0)] public float saturation = 1;
        [Min(0)] public float brightness = 1;

        public Vector4 GetHSV() => new Vector4(hueScale, hueOffset/360f, saturation, brightness);
    }

    [Serializable]
    public class WhiteBalanceSettings
    {
        [Range(-100, 100)]
        public float temperature, tint;

        public Vector3 GetFactors() => ColorUtils.ColorBalanceToLMSCoeffs(temperature, tint);
    }

    [Serializable]
    public class SplitToningSettings
    {
        [ColorUsage(false)]
        public Color shadows = Color.gray, hightlights=Color.gray;

        [Range(-1,1)]
        public float balance;
    }
    [Serializable]
    public class ChannelMixerSettings
    {
        public Vector3 red = Vector3.right, green = Vector3.up, blue = Vector3.forward;
    }

    [Serializable]
    public class ShadowsMidtonesHightlightsSettings
    {
        [ColorUsage(false,true)]
        public Color shadows = Color.white, midtones = Color.white, highlights = Color.white;

        [Range(0, 2)]
        public float shadowStart = 0, shadowsEnd = 0.3f, highlightsStart = 0.55f, highlightsEnd = 1;
    }
    [Serializable]
    public class ColorGradingSettings
    {
        public enum ColorLUTResolution
        {
            _16=16,_32=32,_64=64
        }

        public ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;
        public bool isColorGradingUseLogC;

        public ColorAdjustSettings colorAdjust = new ColorAdjustSettings();
        public WhiteBalanceSettings whiteBalance = new WhiteBalanceSettings();
        public SplitToningSettings splitToning = new SplitToningSettings();
        public ChannelMixerSettings channelMixer = new ChannelMixerSettings();
        public ShadowsMidtonesHightlightsSettings shadowsMidtonesHighlights = new ShadowsMidtonesHightlightsSettings();
    }

}
