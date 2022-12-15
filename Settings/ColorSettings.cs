using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [Serializable]
    public class ColorAdjustSettings
    {
        public float exposure;

        [ColorUsage(false, true)] public Color colorFilter;

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
    public class ColorGradingSettings
    {
        public bool isApplyColorGrading;
        public ColorAdjustSettings colorAdjust = new ColorAdjustSettings();
        public WhiteBalanceSettings whiteBalance = new WhiteBalanceSettings();
    }

    public static class ColorGradingTools
    {
        public static void SetupColorGradingParams(this ColorGradingSettings colorGradingSettings,CommandBuffer cmd)
        {
            var colorAdjust = colorGradingSettings.colorAdjust;
            cmd.SetGlobalFloat(ShaderPropertyIds._ApplyColorGrading, colorGradingSettings.isApplyColorGrading ? 1 : 0);
            cmd.SetGlobalVector(ShaderPropertyIds._ColorAdjustments, new Vector4(
                Mathf.Pow(2, colorAdjust.exposure),
                colorAdjust.contrast+1
                ));

            cmd.SetGlobalVector(ShaderPropertyIds._ColorFilter, colorAdjust.colorFilter);

            cmd.SetGlobalVector(ShaderPropertyIds._ColorAdjustHSV, colorAdjust.GetHSV());

            cmd.SetGlobalVector(ShaderPropertyIds._WhiteBalanceFactors, colorGradingSettings.whiteBalance.GetFactors());
        }
    }
}
