using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MSE2
{
    internal class SpecialThingFilterWorker_ProsthesisIncomplete : SpecialThingFilterWorker
    {
        public override bool AlwaysMatches ( ThingDef def )
        {
            return false;
        }

        public override bool CanEverMatch ( ThingDef def )
        {
            return def.GetCompProperties<CompProperties_IncludedChildParts>() != null;
        }

        public override bool Matches ( Thing t )
        {
            var comp = t.TryGetComp<CompIncludedChildParts>();

            return comp != null && !comp.IsComplete;
        }
    }
}