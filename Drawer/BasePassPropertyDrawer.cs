#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PowerUtilities
{
    //[CustomPropertyDrawer(typeof(BasePass))]
    public class BasePassPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var basePassObj = new SerializedObject(property.objectReferenceValue);
            var iterator = basePassObj.GetIterator();

            var enterChildren = true;
            var height = 0f;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                height += EditorGUI.GetPropertyHeight(iterator);
            }
            
            var basePass = property.objectReferenceValue as BasePass;
            if (basePass.isFoldout)
                return height;
            return 18;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var basePassObj = new SerializedObject(property.objectReferenceValue);
            basePassObj.UpdateIfRequiredOrScript();
            //EditorGUI.PropertyField(position, property, true);
            var labelRect = position;
            labelRect.height =18;

            var isFoldoutProp = basePassObj.FindProperty("isFoldout");
            if (isFoldoutProp.boolValue = EditorGUI.Foldout(labelRect, isFoldoutProp.boolValue, property.objectReferenceValue.name,true))
            {
                EditorGUI.DrawRect(position, Color.red*0.2f);

                var iterator = basePassObj.GetIterator();
                var enterChildren = true;
                var propRect = position;
                
                while (iterator.NextVisible(enterChildren))
                {
                    var height = EditorGUI.GetPropertyHeight(iterator);
                    propRect.height = height;
                    propRect.y += 18;
                    
                    using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                    {
                        //EditorGUILayout.PropertyField(iterator, true);
                        EditorGUI.PropertyField(propRect, iterator);
                    }
                    enterChildren =false;
                    //EditorGUI.DrawRect(new Rect(propRect.x, propRect.y, propRect.width, propRect.height *0.8f), Color.blue*0.2f);
                }
            }
            basePassObj.ApplyModifiedProperties();
        }
    }
}
#endif