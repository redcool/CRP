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
        public static readonly int 
            _LinearToGamma = Shader.PropertyToID(nameof(_LinearToGamma)),
            _GammaToLinear = Shader.PropertyToID(nameof(_GammaToLinear))
            ;
    }
}
