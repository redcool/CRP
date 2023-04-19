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

        private void DrawPassesDetails(SerializedProperty propiterator)
        {
            isPassesFoldout = EditorGUILayout.Foldout(isPassesFoldout, "Passes Details", true, EditorStyles.foldoutHeader);
            if (isPassesFoldout)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < propiterator.arraySize; i++)
                {
                    var passItemProp = propiterator.GetArrayElementAtIndex(i);

                    var basePass = (passItemProp.objectReferenceValue as BasePass);
                    var passItemSO = new SerializedObject(basePass);
                    var titleColor = GetTitleColor(basePass);
                    var passName = basePass.PassName();
                    var isPassFoldout = passItemSO.FindProperty("isFoldout");
                    PassDrawer.DrawPassDetail(passItemSO, titleColor, isPassFoldout, EditorGUITools.TempContent(passName));
                }
                EditorGUI.indentLevel--;
            }
        }

    }
}
#endif