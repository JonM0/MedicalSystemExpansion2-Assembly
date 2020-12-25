using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MSE2
{
    public class ProsthesisVersionSegment : ProsthesisVersion
    {
        public ProsthesisVersionSegment ( CompProperties_IncludedChildParts compProp ) : base( compProp )
        {
            this.parts = new List<(ThingDef thingDef, ProsthesisVersion version)>();
            this.limbConfigurations = new List<LimbConfiguration>();
        }

        public override string Label => "LimbSegment".Translate().CapitalizeFirst();

        public override bool TryAddLimbConfig ( LimbConfiguration limbConfiguration )
        {
            return false;
        }
    }
}
