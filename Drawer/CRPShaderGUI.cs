#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CRPShaderGUI : ShaderGUI
{
    const string SHADOW_CASTER = "ShadowCaster";

    Material[] materials;
    MaterialEditor materialEditor;
    MaterialProperty[] properties;
    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        this.materialEditor = editor;
        this.materials = materialEditor.targets as Material[];
        this.properties = properties;

        EditorGUI.BeginChangeCheck();
        base.OnGUI(materialEditor, properties);
        materialEditor.LightmapEmissionProperty(1);

        if (EditorGUI.EndChangeCheck())
        {
            UpdateShadowCasters();
        }
    }

    private void UpdateShadowCasters()
    {
        if (materials == null)
            return;

        foreach (Material mat in materials)
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
#endif