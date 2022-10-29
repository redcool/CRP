using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName =CRP.CREATE_PASS_ASSET_MENU_ROOT+"/"+nameof(SetupCameraProperties))]
    public class SetupCameraProperties : BasePass
    {
        public override void OnRender()
        {
            context.SetupCameraProperties(camera);
        }
    }
}
