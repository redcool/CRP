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
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            var inst = target as CRPCameraData;
            DrawBlendModePresets(inst);

            var postStackSettingsProp = serializedObject.FindProperty("postStackSettings");
            var colorGradingSettingsProp = serializedObject.FindProperty("colorGradingSettings");
            EditorGUILayout.PropertyField(postStackSettingsProp);
            EditorGUILayout.PropertyField(colorGradingSettingsProp);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawBlendModePresets(CRPCameraData inst)
        {
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