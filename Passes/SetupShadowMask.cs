using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerUtilities
{
    public enum ShadowMaskMode
    {
        None = 0,ShadowMask=1,ShadowMaskDistance=2
    }

    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/" + nameof(SetupShadowMask))]
    public class SetupShadowMask : BasePass
    {
        readonly string[] SHADOW_MASK_KEYS = {"_SHADOW_MASK", "_SHADOW_MASK_DISTANCE" };

        [Header(nameof(SetupShadowMask))]
        public ShadowMaskMode shadowMaskMode;
        int lastId;
        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            int maskModeId = (int)shadowMaskMode -1;
            if (lastId == maskModeId)
                return;
            lastId = maskModeId;

            //off all
            if (maskModeId < 0)
            {
                QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
                Cmd.SetShaderKeyords(false, SHADOW_MASK_KEYS);
                return;
            }

            // on by conditions
            for (int i = 0; i < SHADOW_MASK_KEYS.Length; i++)
            {
                Cmd.SetShaderKeyords(i == maskModeId, SHADOW_MASK_KEYS[i]);
            }
            // sync QualitySettings
            QualitySettings.shadowmaskMode = (ShadowmaskMode)maskModeId;
        }
    }
}
