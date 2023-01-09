using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    public static class MaterialCacheTools
    {
        static Dictionary<string,Material> matDict = new Dictionary<string, Material>();

        public static Material GetMaterial(string path)
        {
            if(matDict.TryGetValue(path,out var mat))
            {
                if(!mat)
                    return matDict[path] = new Material(Shader.Find(path));

                return mat;
            }
            return matDict[path] = new Material(Shader.Find(path));
        }
    }
}
