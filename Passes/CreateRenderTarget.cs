using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/CreateRenderTarget")]
    public class CreateRenderTarget : BasePass
    {
        public string[] targetNames = new[] { "_CameraTarget" };

        public int[] targetIds;

        public override void OnRender()
        {
            targetIds = targetNames.Where((item)=>!string.IsNullOrEmpty(item))
            .Select(item => Shader.PropertyToID(item))
            .ToArray();

            var desc = new RenderTextureDescriptor();
            desc.width = (int)(camera.pixelWidth * CRP.asset.renderScale);
            desc.height = (int)(camera.pixelHeight * CRP.asset.renderScale);
            desc.msaaSamples = 1;
            desc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;

            desc.colorFormat = RenderTextureFormat.ARGB32;
            desc.depthBufferBits = 32;

            BeginSample(nameof(CreateRenderTarget));

            foreach (var item in targetIds)
            {
                Cmd.GetTemporaryRT(item, desc);
            }

            EndSample(nameof(CreateRenderTarget));
            ExecuteCommand();
        }
    }
}
