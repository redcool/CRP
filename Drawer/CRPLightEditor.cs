using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR

namespace PowerUtilities
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light),typeof(CRPAsset))]
    public class CRPLightEditor : LightEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if(!settings.lightType.hasMultipleDifferentValues &&
                settings.light.type == LightType.Spot)
            {
                settings.DrawInnerAndOuterSpotAngle();
                settings.ApplyModifiedProperties();
            }
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
            var light = settings.light;
            if (light.type == LightType.Spot)
            {
                // draw disct
                var dir = light.transform.forward * light.range;
                var endPos = light.transform.position + dir;
                float r = Mathf.Tan(light.innerSpotAngle * Mathf.Deg2Rad * 0.5f) * dir.magnitude;
                Handles.DrawWireDisc(endPos, -dir, r);

                // draw handle
                var snap = Vector3.one * 0.1f;
                var size = HandleUtility.GetHandleSize(endPos)*0.1f;
                var handlePos = endPos + light.transform.right * r;

                EditorGUI.BeginChangeCheck();
                var newPos = Handles.FreeMoveHandle(handlePos, Quaternion.identity, size, snap, Handles.CubeHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(light, $"update {light.name} spot inner angle");
                    var dist = Vector3.Distance(endPos, newPos);
                    light.innerSpotAngle = Mathf.Atan2(dist, dir.magnitude) * Mathf.Rad2Deg * 2;
                }
                
            }
        }
    }
}
#endif