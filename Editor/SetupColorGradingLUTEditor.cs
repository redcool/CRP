#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CustomEditor(typeof(SetupColorGradingLUT))]
    public class SetupColorGradingLUTEditor : Editor
    {
        bool isSettingsFoldout;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            DrawSettings();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SetupColorGradingLUT.toneMappingPass)));
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettings()
        {
            var settingsProp = serializedObject.FindProperty(nameof(SetupColorGradingLUT.defaultGradingSettings));
            EditorGUILayout.PropertyField(settingsProp);

            if (settingsProp.objectReferenceValue==null)
                return;

            EditorGUITools.DrawPropertyEditor(settingsProp, ref isSettingsFoldout);
        }
    }
}
#endif