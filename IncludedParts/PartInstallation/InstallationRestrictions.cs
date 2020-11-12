using System.Collections.Generic;
using System.Linq;

using Verse;

namespace MSE2
{
    public class InstallationRestrictions : DefModExtension
    {
        private readonly List<HediffDef> whitelist;
        private readonly bool onlyOnNatural = false;

        public bool CompatibleWithPart ( BodyPartRecord part, HediffSet hediffSet )
        {
            return (this.whitelist == null || (from h in hediffSet.hediffs // no whitelist or there is a whitelisted hediff on the part
                                               where h.Part == part
                                               where this.whitelist.Contains( h.def )
                                               select h).Any())
                && !(this.onlyOnNatural && hediffSet.HasDirectlyAddedPartFor( part )); // there is no added part when onlyOnNatural
        }

        public override IEnumerable<string> ConfigErrors ()
        {
            foreach ( string e in base.ConfigErrors() ) yield return e;

            if ( this.whitelist == null && this.onlyOnNatural == false )
                yield return "[MSE] all InstallationRestrictions are null";

            yield break;
        }
    }
}