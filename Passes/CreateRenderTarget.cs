using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+ nameof(CreateRenderTarget))]
    public class CreateRenderTarget : BasePass
    {
        public string[] targetNames = new[] { "_CameraTarget" };
        int[] targetIds;

        [Range(0.1f,2f)]
        public float renderScale = 1;

        public override void OnRender()
        {
            targetIds = targetNames.Where((item)=>!string.IsNullOrEmpty(item))
            .Select(item => Shader.PropertyToID(item))
            .ToArray();

            Cmd.CreateTargets(camera, targetIds, renderScale);
        }
    }
}
