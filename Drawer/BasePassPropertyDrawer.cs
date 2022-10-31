#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace PowerUtilities
{
    //[CustomPropertyDrawer(typeof(BasePass))]
    public class BasePassPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);

            var basePassObj = new SerializedObject(property.objectReferenceValue);
            if (basePassObj.FindProperty("isFoldout").boolValue)
                return 18 * 4;
            return 18;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
            return;
            var basePassObj = new SerializedObject(property.objectReferenceValue);

            EditorGUI.PropertyField(position, property, true);
        }
    }
}
#endif