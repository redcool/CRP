#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace PowerUtilities.CRP
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

        public static Color GetTitleColor(BasePass pass)
        {
            if (pass.isEditorOnly)
                return Color.yellow;
            else if (pass.IsInterrupt())
                return Color.red;
            else if (pass.IsSkip())
                return Color.green;
            return GUI.color;
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

                    ColorField(GetTitleColor(basePass), () => {
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