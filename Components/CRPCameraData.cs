namespace PowerUtilities
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    [DisallowMultipleComponent,RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class CRPCameraData : MonoBehaviour
    {
        [Header("Blend Mode")]
        public BlendMode finalSrcMode = BlendMode.One;
        public BlendMode finalDstMode = BlendMode.Zero;
    }
}