#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace PowerUtilities
{
    public static class InspectorTools
    {
        public static bool DrawDefaultInspect(SerializedObject obj)
        {
            EditorGUI.BeginChangeCheck();
            //obj.UpdateIfRequiredOrScript();
            var iterator = obj.GetIterator();

            var enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
                enterChildren = false;
            }
            //obj.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }
    }
}
#endif