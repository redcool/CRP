using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    public static class UnityShaderPropertyId
    {
        public static int 
            _ProjectionParams = Shader.PropertyToID(nameof(_ProjectionParams));
    }
}
