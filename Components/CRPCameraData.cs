namespace PowerUtilities.CRP
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

        [Header("Camera PostStack Settings")]
        [HideInInspector] public PostStackSettings postStackSettings;

        [Header("Color Grading LUT Generation")]
        [Tooltip("When not empty,do not generate colorGrading LUT.")]
        public Texture colorGradingTexture;

        [HideInInspector] public ColorGradingSettings colorGradingSettings;

        [Header("Camera rendering layer")]
        [RenderingLayerMask]
        public uint renderingLayerMask = uint.MaxValue;
    }
}