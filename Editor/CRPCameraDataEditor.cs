#if UNITY_EDITOR
namespace PowerUtilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;

    [CustomEditor(typeof(CRPCameraData))]
    public class CRPCameraDataEditor : Editor
    {
        bool isPostSettingsFoldout, isColorGradingSettingsFoldout;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var inst = target as CRPCameraData;
            DrawBlendMode(inst);

            DrawPostStackSettings();

            DrawColorGradingSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawColorGradingSettings()
        {
            var colorTextureProp = serializedObject.FindProperty(nameof(CRPCameraData.colorGradingTexture));
            EditorGUILayout.PropertyField(colorTextureProp);

            if (colorTextureProp.objectReferenceValue != null)
            {
                EditorGUILayout.LabelField("ColorGradingSettings is hidden when use ColorGradingTexture .");
                return;
            }

            var colorGradingSettingsProp = serializedObject.FindProperty(nameof(CRPCameraData.colorGradingSettings));
            EditorGUILayout.PropertyField(colorGradingSettingsProp);

            if (colorGradingSettingsProp ==null)
                return;

            EditorGUITools.DrawPropertyEditor(colorGradingSettingsProp, ref isColorGradingSettingsFoldout);
        }

        private void DrawPostStackSettings()
        {
            var postStackSettingsProp = serializedObject.FindProperty(nameof(CRPCameraData.postStackSettings));
            EditorGUILayout.PropertyField(postStackSettingsProp);

            if (postStackSettingsProp == null)
                return;

            EditorGUITools.DrawPropertyEditor(postStackSettingsProp, ref isPostSettingsFoldout);
        }

        private void DrawBlendMode(CRPCameraData inst)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CRPCameraData.finalSrcMode)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CRPCameraData.finalDstMode)));

            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("Preset Blend Modes");
                if (GUILayout.Button("Alpha Blend"))
                {
                    inst.finalSrcMode = BlendMode.SrcAlpha;
                    inst.finalDstMode =BlendMode.OneMinusSrcAlpha;
                }
                if (GUILayout.Button("Normal"))
                {
                    inst.finalSrcMode = BlendMode.One;
                    inst.finalDstMode = BlendMode.Zero;
                }
            }
            GUILayout.EndVertical();
        }
    }
}
#endif