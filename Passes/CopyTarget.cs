using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities.CRP
{
    [CreateAssetMenu(menuName = CRP.CREATE_EDITOR_PASS_ASSET_ROOT+"/"+nameof(CopyTarget))]
    public class CopyTarget : BasePass
    {
        [Header(nameof(CopyTarget))]
        public string sourceName;
        public string outputPath = "/target.png";
        public int width = 1024, height = 1024;

        public bool isLinearToGamma;

        [Header("Operations")]
        public bool isRunOnce;

        public RenderTexture tempRT;

        Material BlitMaterial => MaterialCacheTool.GetMaterial("CRP/Utils/CopyColor");
        public override bool CanExecute()
        {
            isEditorOnly= true;

            return
                !string.IsNullOrEmpty(sourceName) &&
                base.CanExecute() &&
                isRunOnce
                ;
        }


        private void OnDestroy()
        {
            DestroyImmediate(tempRT);
            Debug.Log("destroy");
        }

        public override void OnRender()
        {
            isRunOnce= false;

            if (TextureTools.IsNeedCreateTexture(tempRT, width, height))
            {
                //var format = CRP.Asset.pipelineSettings.isHdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                tempRT = new RenderTexture(width, height, 0, RenderTextureFormat.Default, 0);
            }

            Cmd.SetGlobalFloat(SetupColorGradingLUT._ApplyColorGrading, 0);
            Cmd.SetGlobalFloat(ShaderPropertyIds._LinearToGamma, isLinearToGamma ? 1 : 0);
            Cmd.BlitTriangle(sourceName, tempRT, BlitMaterial, 0);
            
            var path = Application.dataPath + outputPath;
            if (SystemInfo.supportsAsyncGPUReadback)
            {
                GPUTools.AsyncGPUReadRenderTexture(tempRT, 4, bytes =>
                {
                    File.Delete(path);
                    File.WriteAllBytes(path, bytes);
                });
            }
            else
            {
                var tex = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
                GPUTools.ReadRenderTexture(tempRT, ref tex);

                File.Delete(path);
                File.WriteAllBytes(path, tex.EncodeToPNG());

                DestroyImmediate(tex);
            }
        }
    }
}
