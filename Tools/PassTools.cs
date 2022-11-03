using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    public static class PassTools
    {
        public static Camera[] cameras;
        public static int cameraIndex;
        public static bool IsFinalCamera() => cameras.Length -1 == cameraIndex;
    }
}
