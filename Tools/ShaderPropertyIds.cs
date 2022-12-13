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
            _SourceTex2 = Shader.PropertyToID(nameof(_SourceTex2))
            ;
    }
}
