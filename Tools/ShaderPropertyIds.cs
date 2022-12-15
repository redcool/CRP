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
            _ProjectionParams = Shader.PropertyToID(nameof(_ProjectionParams)),
            _SourceTex = Shader.PropertyToID(nameof(_SourceTex)),
            _SourceTex2 = Shader.PropertyToID(nameof(_SourceTex2)),
            _ColorAdjustments = Shader.PropertyToID(nameof(_ColorAdjustments)),
            _ColorFilter = Shader.PropertyToID(nameof(_ColorFilter)),
            _ApplyColorGrading = Shader.PropertyToID(nameof(_ApplyColorGrading)),
            _ColorAdjustHSV = Shader.PropertyToID(nameof(_ColorAdjustHSV)),
            _WhiteBalanceFactors = Shader.PropertyToID(nameof(_WhiteBalanceFactors))
            ;
    }
}
