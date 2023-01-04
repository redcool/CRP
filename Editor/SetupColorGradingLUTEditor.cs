#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
namespace PowerUtilities
{
    [CustomEditor(typeof(SetupColorGradingLUT))]
    public class SetupColorGradingLUTEditor : Editor
    {
        bool isSettingsFoldout;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            //base.OnInspectorGUI();
            DrawDefaultInspector();
            DrawSettings();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettings()
        {
            var settingsProp = serializedObject.FindProperty("gradingSettings");
            EditorGUILayout.PropertyField(settingsProp);

            if (settingsProp.objectReferenceValue==null)
                return;

            EditorGUITools.DrawPropertyEditor(settingsProp, ref isSettingsFoldout);
        }
    }
}
#endif