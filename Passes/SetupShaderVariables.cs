using PowerUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities.CRP
{
    [Serializable]
    public class ShaderVariable
    {
        public string name;
        public ShaderVariableTypes type;

        [Header("Values")]
        public Texture textureValue;
        public Vector4 vectorValue;
        public float floatValue;

        public bool IsValid() => !string.IsNullOrEmpty(name) &&
            (type == ShaderVariableTypes.Texture && !textureValue)
            ;
    }

    public enum ShaderVariableTypes
    {
        Float,Vector,Texture
    }
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/" + nameof(SetupShaderVariables))]
    public class SetupShaderVariables : BasePass
    {
        [Header("Shader variables")]
        public ShaderVariable[] variables; 

        public override void OnRender()
        {
            if(variables== null || variables.Length == 0) return;

            foreach (var item in variables)
            {
                if(!item.IsValid())
                    continue;

                switch (item.type)
                {
                    case ShaderVariableTypes.Float:
                        Cmd.SetGlobalFloat(item.name, item.floatValue); break;
                    case ShaderVariableTypes.Vector:
                        Cmd.SetGlobalVector(item.name, item.vectorValue); break;
                    case ShaderVariableTypes.Texture:
                        Cmd.SetGlobalTexture(item.name, item.textureValue); break;
                }
            }
        }
    }
}
