﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    public static class RenderingTools
    {
        public const string errorShaderName = "Hidden/InternalErrorShader";
        static Material errorMaterial;

        public static Material ErrorMaterial
        {
            get
            {
                if (!errorMaterial)
                    errorMaterial = new Material(Shader.Find(errorShaderName));
                return errorMaterial;
            }
        }

        public static void RenderTargetNameToIdentifier(string[] names, ref RenderTargetIdentifier[] ids)
        {
            if (names == null)
            {
                return;
            }

            ids = names.Where(name => !string.IsNullOrEmpty(name)).
                Select(name => new RenderTargetIdentifier(name)).
                ToArray();
        }
    }
}
