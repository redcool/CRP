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
    [CustomEditor(typeof(CRPAsset))]
    public class CRPAssetEditor : Editor
    {

        public bool isPassesFoldout;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    EditorGUILayout.PropertyField(iterator, true);


                    var isPasses = iterator.propertyPath == "passes";
                    if(isPasses)
                        DrawPassesDetails(iterator);
                }
                enterChildren = false;
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPassesDetails(SerializedProperty iterator)
        {
            isPassesFoldout = EditorGUILayout.Foldout(isPassesFoldout, "Passes Details", true, EditorStyles.foldoutHeader);
            if (isPassesFoldout)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < iterator.arraySize; i++)
                {
                    var passItemProp = iterator.GetArrayElementAtIndex(i);

                    var basePass = (passItemProp.objectReferenceValue as BasePass);
                    var passItemSO = new SerializedObject(basePass);
                    passItemSO.UpdateIfRequiredOrScript();

                    var isPassFoldout = passItemSO.FindProperty("isFoldout");
                    var passName = basePass.PassName();

                    var titleColor = basePass.isInterrupt ? Color.red : basePass.isSkip ? Color.green : GUI.color;

                    ColorField(titleColor, () => {
                        isPassFoldout.boolValue = EditorGUILayout.Foldout(isPassFoldout.boolValue, passName, true);
                    });

                    if (isPassFoldout.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        GUILayout.BeginVertical("Box");
                        InspectorTools.DrawDefaultInspect(passItemSO);
                        GUILayout.EndVertical();
                        EditorGUI.indentLevel--;
                    }

                    passItemSO.ApplyModifiedProperties();
                }
                EditorGUI.indentLevel--;
            }
        }

        public static void ColorField(Color c, Action onDraw)
        {
            var lastColor = GUI.color;
            GUI.color = c;
            onDraw();
            GUI.color = lastColor;
        }
    }
}
#endif