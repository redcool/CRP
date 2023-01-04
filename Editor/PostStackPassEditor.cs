#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace PowerUtilities
{
    [CustomEditor(typeof(PostStackPass))]
    public class PostStackPassEditor :Editor
    {
        bool isSettingsFoldout;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            DrawSettings();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettings()
        {
            var settingsProp = serializedObject.FindProperty("postStackSettings");
            EditorGUILayout.PropertyField(settingsProp);
            if (settingsProp == null)
                return;

            EditorGUITools.DrawPropertyEditor(settingsProp,ref isSettingsFoldout);
        }


    }
}
#endif