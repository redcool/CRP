using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    public class ObjectCacheTool<T> where T : class
    {
        Dictionary<string, T> dict = new Dictionary<string, T>();
        public T Get(string key, Func<T> onNotExists)
        {
            if (dict.TryGetValue(key, out T obj))
            {
                if (onNotExists != null && obj == default)
                {
                    return dict[key] = onNotExists();
                }
                return obj;
            }
            return onNotExists != null ? dict[key] = onNotExists() : default;
        }
    }

    public static class MaterialCacheTool
    {
        static ObjectCacheTool<Material> matCacheTool = new ObjectCacheTool<Material>();
        public static Material GetMaterial(string path)=> matCacheTool.Get(path, () => new Material(Shader.Find(path)));
    }
}
