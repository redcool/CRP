using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(ReleaseRenderTarget))]
    public class ReleaseRenderTarget : BasePass
    {
        public string[] targetNames;
        int [] targetIds;

        public override string PassName() => nameof(ReleaseRenderTarget);

        public override void OnRender()
        {
            if (targetNames==null)
                return;

            Cmd.name = nameof(ReleaseRenderTarget);

            targetIds = targetNames.Where(name => !string.IsNullOrEmpty(name))
                .Select(name => Shader.PropertyToID(name))
                .ToArray();

            foreach (var item in targetIds)
            {
                Cmd.ReleaseTemporaryRT(item);
            }
        }
    }
}
