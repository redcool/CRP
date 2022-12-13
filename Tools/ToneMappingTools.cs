using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    public enum ToneMappingPass
    {
        None=0,
        Reinhard,
        ACESFitted,
        ACESFilm,
        GTTone,
    }

    public static class ToneMappingTools
    {
        static Lazy<string[]> lazyGetToneMapperNames = new Lazy<string[]>(() => Enum.GetNames(typeof(ToneMappingPass)));
        public static string[] GetToneMapperNames() => lazyGetToneMapperNames.Value;


        public static void SetToneKeyword(this CommandBuffer cmd, ToneMappingPass toneMapper, bool isPassApplyTone)
        {
            var names = GetToneMapperNames();
            if (!isPassApplyTone)
            {
                cmd.SetShaderKeyords(false, names);
                return;
            }

            for (int i = 0; i < names.Length; i++)
            {
                cmd.SetShaderKeyords(i == (int)toneMapper, names[i]);
            }
        }
    }
}
