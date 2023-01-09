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
        [Header(nameof(SetupColorGradingLUT))]
        public string targetName = "_ColorGradingLUT";

        [HideInInspector]public ColorGradingSettings defaultGradingSettings;

        ColorGradingSettings GradingSettings
        {
            get
            {
                var cameraData = camera.GetComponent<CRPCameraData>();
                if(cameraData && cameraData.colorGradingSettings)
                {
                    return cameraData.colorGradingSettings;
                }
                return defaultGradingSettings;
            }
        }

        [Header("ToneMapping")]
        [Tooltip("ToneMapping should apply the last blit pass")]
        [HideInInspector] public ToneMappingPass toneMappingPass;

        Material ColorGradingMaterial => MaterialCacheTools.GetMaterial("CRP/Utils/ColorGrading");

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
            _ColorGradingLUTParams = Shader.PropertyToID(nameof(_ColorGradingLUTParams)),
            _ColorGradingUseLogC = Shader.PropertyToID(nameof(_ColorGradingUseLogC))
            ;

        int targetId;
        bool hasColorGradingTexture;
        public override bool CanExecute()
        {
            return base.CanExecute() && GradingSettings!= null;
        }

        public override void OnRender()
        {

            targetId = Shader.PropertyToID(targetName);

            Cmd.SetGlobalFloat(_ColorGradingUseLogC, GradingSettings.isColorGradingUseLogC ? 1 : 0);

            var lutHeight = (int)GradingSettings.colorLUTResolution;
            var lutWidth = lutHeight * lutHeight;

            var cameraData = camera.GetComponent<CRPCameraData>();
            hasColorGradingTexture = (cameraData && cameraData.colorGradingTexture);

            if (hasColorGradingTexture)
            {
                Cmd.SetGlobalTexture(targetId, cameraData.colorGradingTexture);
                lutWidth = cameraData.colorGradingTexture.width;
                lutHeight = cameraData.colorGradingTexture.height;
            }
            else
            {
                SetupColorGradingParams(Cmd);
                SetupLUT(Cmd, ColorGradingMaterial, GetPassId(), lutWidth, lutHeight);
            }
            // update _ColorGradingLUTParams, then can ApplyColorGradingLUT
            Cmd.SetGlobalVector(_ColorGradingLUTParams, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1));
        }

        public override bool IsNeedCameraCleanup() => true;
        public override void CameraCleanup()
        {
            if(hasColorGradingTexture)
                Cmd.ReleaseTemporaryRT(targetId);
        }

        bool IsApplyTone() => CRP.Asset.pipelineSettings.isHdr && toneMappingPass != ToneMappingPass.None;
        int GetPassId() => IsApplyTone() ? (int)toneMappingPass : 0;

        public void SetupLUT(CommandBuffer Cmd,Material mat, int pass,int lutWidth,int lutHeight)
        {
            var colorFormat = CRP.Asset.pipelineSettings.isHdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            Cmd.GetTemporaryRT(targetId, lutWidth, lutHeight, 0, FilterMode.Bilinear, colorFormat);
            
            Cmd.SetGlobalVector(_ColorGradingLUTParams, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1)));
            Cmd.BlitTriangle(BuiltinRenderTextureType.None, targetId, mat, pass);
        }

        public void SetupColorGradingParams(CommandBuffer cmd)
        {
            var colorAdjust = GradingSettings.colorAdjust;
            cmd.SetGlobalVector(_ColorAdjustments, new Vector4(
                Mathf.Pow(2, colorAdjust.exposure),
                colorAdjust.contrast + 1
                ));

            cmd.SetGlobalVector(_ColorFilter, colorAdjust.colorFilter);

            cmd.SetGlobalVector(_ColorAdjustHSV, colorAdjust.GetHSV());

            cmd.SetGlobalVector(_WhiteBalanceFactors, GradingSettings.whiteBalance.GetFactors());

            var splitToning = GradingSettings.splitToning;
            cmd.SetGlobalColor(_SplitToningShadows, new Color(splitToning.shadows.r, splitToning.shadows.g, splitToning.shadows.b, splitToning.balance));
            cmd.SetGlobalColor(_SplitToningHighlights, splitToning.hightlights);

            var channelMixer = GradingSettings.channelMixer;
            cmd.SetGlobalVector(_ChannelMixerRed, channelMixer.red);
            cmd.SetGlobalVector(_ChannelMixerGreen, channelMixer.green);
            cmd.SetGlobalVector(_ChannelMixerBlue, channelMixer.blue);

            var smh = GradingSettings.shadowsMidtonesHighlights;
            cmd.SetGlobalColor(_SMHShadows, smh.shadows.linear);
            cmd.SetGlobalColor(_SMHMidtones, smh.midtones.linear);
            cmd.SetGlobalColor(_SMHHighlights, smh.highlights.linear);
            cmd.SetGlobalVector(_SMHRange, new Vector4(smh.shadowStart, smh.shadowsEnd, smh.highlightsStart, smh.highlightsEnd));
        }
    }
}
