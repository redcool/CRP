#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace PowerUtilities
{
    //[CustomEditor(typeof(CRPAsset))]
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
                var isPasses = iterator.propertyPath == "passes";
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    if (isPasses)
                    {
                        isPassesFoldout = EditorGUILayout.Foldout(isPassesFoldout, iterator.displayName,true,EditorStyles.foldoutHeader);
                        if (isPassesFoldout)
                        {
                            EditorGUI.indentLevel++;
                            for (int i = 0; i < iterator.arraySize; i++)
                            {
                                var passItemProp = iterator.GetArrayElementAtIndex(i);

                                var passItemSO = new SerializedObject(passItemProp.objectReferenceValue);
                                passItemSO.UpdateIfRequiredOrScript();

                                var isPassFoldout = passItemSO.FindProperty("isFoldout");
                                var passName = passItemSO.targetObject.GetType().Name;
                                if (isPassFoldout.boolValue = EditorGUILayout.Foldout(isPassFoldout.boolValue, passName, true))
                                {
                                    EditorGUI.indentLevel++;
                                    InspectorTools.DrawDefaultInspect(passItemSO);
                                    EditorGUI.indentLevel--;
                                }
                                passItemSO.ApplyModifiedProperties();
                            }
                            EditorGUI.indentLevel--;
                        }

                    }
                    //else
                        EditorGUILayout.PropertyField(iterator, true);
                }
                enterChildren = false;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif