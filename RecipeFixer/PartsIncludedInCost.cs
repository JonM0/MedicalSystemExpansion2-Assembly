using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

namespace MSE2.RecipeFixer
{
    internal class PartsIncludedInCost : DefModExtension
    {
        internal List<ThingDefCountClass> partsInCost;

        internal float minToLeave = 0;

        internal bool wasFixed = false;

        internal FixMode fixMode = FixMode.CostList;

        public override IEnumerable<string> ConfigErrors ()
        {
            if ( minToLeave < 0 || minToLeave > 1 )
            {
                yield return "[MSE2] minToLeave has to be between 0. and 1.";
            }

            if ( !wasFixed )
            {
                yield return "[MSE2] part still has cost to fix";
            }
        }

        internal IEnumerable<ThingDefCountClass> RecursivePartsInCost
        {
            get
            {
                if ( this.partsInCost != null )
                {
                    for ( int i = 0; i < partsInCost.Count; i++ )
                    {
                        var part = partsInCost[i];
                        yield return part;

                        var rec = part.thingDef.GetModExtension<PartsIncludedInCost>();
                        if ( rec != null )
                            foreach ( var subpart in rec.RecursivePartsInCost )
                            {
                                yield return subpart;
                            }
                    }
                }
            }
        }

        internal enum FixMode
        {
            CostList,
            MarketValue,
        }
    }
}