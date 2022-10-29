using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/SetRenderTarget")]
    public class SetRenderTarget : BasePass
    {

        public string[] targetNames;
        public RenderTargetIdentifier[] targetIds;

        [Header("Clear ")]
        public bool clearTarget;
        public bool clearDepth = true;
        public float depth = 1;
        public bool clearColor = true;
        public Color color = Color.clear;

        
        public override void OnRender()
        {
            if (targetNames == null || targetNames.Length ==0)
                return;

            targetIds = targetNames.Where(item=>!string.IsNullOrEmpty(item))
                .Select(name => new RenderTargetIdentifier(name))
                .Take(8)
                .ToArray();

            Cmd.SetRenderTarget(targetIds, targetIds[0]);

            if (clearTarget)
            {
                //Cmd.ClearRenderTarget(clearDepth, clearColor, color, depth);
                Cmd.ClearRenderTarget(camera);
            }
            ExecuteCommand();
        }
    }
}
