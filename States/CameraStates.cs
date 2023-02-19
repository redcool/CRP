using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities.CRP
{
    public static class CameraStates
    {
        public static Camera[] cameras = new Camera[] { } ;
        public static int cameraIndex;
        public static bool IsFinalCamera() => cameras.Length -1 == cameraIndex;

    }
}
