using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerUtilities.CRP
{

    [Serializable]
    public class PipelineSettings
    {
        public bool isHdr;
        public bool useSRPBatch = true;
        public bool lightsUseLinearColor = true;
    }
}
