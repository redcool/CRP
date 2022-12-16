using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    public static class ShaderPropertyIds
    {
        public static int 
            // post stack temporary textures
            _SourceTex = Shader.PropertyToID(nameof(_SourceTex)),
            _SourceTex2 = Shader.PropertyToID(nameof(_SourceTex2)),

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
            _SMHRange = Shader.PropertyToID(nameof(_SMHRange))
            ;
    }
}
