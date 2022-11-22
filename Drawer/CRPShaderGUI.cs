#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CRPShaderGUI : ShaderGUI
{
    const string SHADOW_CASTER = "ShadowCaster";
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUI.BeginChangeCheck();
        base.OnGUI(materialEditor, properties);
        materialEditor.LightmapEmissionProperty(1);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Material mat in materialEditor.targets)
            {
                var _ShadowMode = FindProperty("_ShadowMode", properties);
                if (_ShadowMode != null)
                {
                    mat.SetShaderPassEnabled(SHADOW_CASTER, (int)_ShadowMode.floatValue != 0);
                }
                mat.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            } 

        }
    }
}
#endif