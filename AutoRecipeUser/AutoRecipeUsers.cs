using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

using Verse;

namespace MSE2
{
    internal class AutoRecipeUsers : DefModExtension
    {
        private FloatRange bodySizeRange = FloatRange.One;
        private List<ThingDef> neverInclude;
        private Intelligence minIntelligence = Intelligence.Animal;
        private Intelligence maxIntelligence = Intelligence.Humanlike;

        internal bool IsValidRace ( ThingDef pawnDef )
        {
            RaceProperties race = pawnDef.race;

            return race != null
                && this.bodySizeRange.IncludesEpsilon( race.baseBodySize )
                && this.minIntelligence <= race.intelligence
                && this.maxIntelligence >= race.intelligence
                && (this.neverInclude == null || !this.neverInclude.Contains( pawnDef ));
        }
    }
}