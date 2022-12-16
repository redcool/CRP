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
        ACES,
        Neutral,
        GTTone,
    }
}
