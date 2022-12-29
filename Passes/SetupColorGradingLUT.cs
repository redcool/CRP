using PowerUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace PowerUtilities
{
    public enum ToneMappingPass
    {
        None = 0,
        Reinhard,
        ACESFitted,
        ACESFilm,
        ACES,
        Neutral,
        GTTone,
    }

    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/" + nameof(SetupColorGradingLUT))]
    public class SetupColorGradingLUT : BasePass
    {
        [Header("Color Grading")]
        public ColorGradingSettings gradingSettings;

        [Header("ToneMapping")]
        [Tooltip("ToneMapping should apply the last blit pass")]
        public ToneMappingPass toneMappingPass;

        static Lazy<Material> lazyColorGradingMaterial = new Lazy<Material>(() => new Material(Shader.Find("CRP/Utils/ColorGrading")));
        public static readonly int
            _ApplyColorGrading = Shader.PropertyToID(nameof(_ApplyColorGrading)),
            // color adjustments
            _ColorAdjustments = Shader.PropertyToID(nameof(_ColorAdjustments)),
            _ColorFilter = Shader.PropertyToID(nameof(_ColorFilter)),
            _ColorAdjustHSV = Shader.PropertyToID(nameof(_ColorAdjustHSV)),
            // white balance
            _WhiteBalanceFactors = Shader.PropertyToID(nameof(_WhiteBalanceFactors)),
            //split toning
            _SplitToningShadows = Shader.PropertyToID(nameof(_SplitToningShadows)),
            _SplitToningHighlights = Shader.PropertyToID(nameof(_SplitToningHighlights)),
            //channel mixer
            _ChannelMixerRed = Shader.PropertyToID(nameof(_ChannelMixerRed)),
            _ChannelMixerGreen = Shader.PropertyToID(nameof(_ChannelMixerGreen)),
            _ChannelMixerBlue = Shader.PropertyToID(nameof(_ChannelMixerBlue)),
            //shadows midtones highlights
            _SMHShadows = Shader.PropertyToID(nameof(_SMHShadows)),
            _SMHMidtones = Shader.PropertyToID(nameof(_SMHMidtones)),
            _SMHHighlights = Shader.PropertyToID(nameof(_SMHHighlights)),
            _SMHRange = Shader.PropertyToID(nameof(_SMHRange)),

            //LUT
            _ColorGradingLUT = Shader.PropertyToID(nameof(_ColorGradingLUT)),
            _ColorGradingLUTParams = Shader.PropertyToID(nameof(_ColorGradingLUTParams)),
            _ColorGradingUseLogC = Shader.PropertyToID(nameof(_ColorGradingUseLogC))
            ;
        static bool isCreated;
        public override void OnRender()
        {
            if (isCreated)
                return;

            isCreated = true;
            SetupColorGradingParams(Cmd);

            SetupLUT(Cmd, gradingSettings.isColorGradingUseLogC, lazyColorGradingMaterial.Value, GetPassId());
        }

        public override bool NeedCleanup() => true;
        public override void Cleanup()
        {
            Cmd.ReleaseTemporaryRT(_ColorGradingLUT);
            isCreated = false;
        }

        bool IsApplyTone() => CRP.Asset.pipelineSettings.isHdr && toneMappingPass != ToneMappingPass.None;
        int GetPassId() => IsApplyTone() ? (int)toneMappingPass : 0;

        public void SetupLUT(CommandBuffer Cmd, bool useLogC,Material mat, int pass)
        {
            var lutHeight = (int)gradingSettings.colorLUTResolution;
            var lutWidth = lutHeight * lutHeight;
            Cmd.GetTemporaryRT(_ColorGradingLUT, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);

            Cmd.SetGlobalFloat(_ColorGradingUseLogC, useLogC ? 1 : 0);
            Cmd.SetGlobalVector(_ColorGradingLUTParams, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1)));
            Cmd.BlitTriangle(BuiltinRenderTextureType.None, _ColorGradingLUT, mat, pass);

            ExecuteCommand();
            // update _ColorGradingLUTParams, then can ApplyColorGradingLUT
            Cmd.SetGlobalVector(_ColorGradingLUTParams, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1));
        }

        public void SetupColorGradingParams(CommandBuffer cmd)
        {
            var colorAdjust = gradingSettings.colorAdjust;
            cmd.SetGlobalVector(_ColorAdjustments, new Vector4(
                Mathf.Pow(2, colorAdjust.exposure),
                colorAdjust.contrast + 1
                ));

            cmd.SetGlobalVector(_ColorFilter, colorAdjust.colorFilter);

            cmd.SetGlobalVector(_ColorAdjustHSV, colorAdjust.GetHSV());

            cmd.SetGlobalVector(_WhiteBalanceFactors, gradingSettings.whiteBalance.GetFactors());

            var splitToning = gradingSettings.splitToning;
            cmd.SetGlobalColor(_SplitToningShadows, new Color(splitToning.shadows.r, splitToning.shadows.g, splitToning.shadows.b, splitToning.balance));
            cmd.SetGlobalColor(_SplitToningHighlights, splitToning.hightlights);

            var channelMixer = gradingSettings.channelMixer;
            cmd.SetGlobalVector(_ChannelMixerRed, channelMixer.red);
            cmd.SetGlobalVector(_ChannelMixerGreen, channelMixer.green);
            cmd.SetGlobalVector(_ChannelMixerBlue, channelMixer.blue);

            var smh = gradingSettings.shadowsMidtonesHighlights;
            cmd.SetGlobalColor(_SMHShadows, smh.shadows.linear);
            cmd.SetGlobalColor(_SMHMidtones, smh.midtones.linear);
            cmd.SetGlobalColor(_SMHHighlights, smh.highlights.linear);
            cmd.SetGlobalVector(_SMHRange, new Vector4(smh.shadowStart, smh.shadowsEnd, smh.highlightsStart, smh.highlightsEnd));
        }
    }
}
