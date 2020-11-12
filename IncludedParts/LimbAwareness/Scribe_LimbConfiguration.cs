using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MSE2
{
    public static class Scribe_LimbConfiguration
    {
        public static void Look ( ref LimbConfiguration limbConfiguration, string label )
        {
            // Save an example of record in the limbTarget
            BodyPartRecord limbPartExample = limbConfiguration?.RecordExample;

            Scribe_BodyParts.Look( ref limbPartExample, label );

            if ( Scribe.mode == LoadSaveMode.LoadingVars )
            {
                limbConfiguration = LimbConfiguration.LimbConfigForBodyPartRecord( limbPartExample );
            }
        }
    }
}