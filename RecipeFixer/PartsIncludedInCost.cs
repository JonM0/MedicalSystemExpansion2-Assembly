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
        internal List<ThingDefCountClass> partsInCost = new List<ThingDefCountClass>();

        internal float minToLeave = 0.2f;

        internal float baseWorkFactor = 1;

        [Unsaved]
        internal bool wasFixed = false;

        internal FixMode fixMode = FixMode.CostList;

        public override IEnumerable<string> ConfigErrors ()
        {
            if ( baseWorkFactor < 0 )
            {
                yield return "[MSE2] baseWorkFactor has to be positive. Value: " + baseWorkFactor;
            }

            if ( baseWorkFactor != 1f && fixMode != FixMode.CostList )
            {
                yield return "[MSE2] baseWorkFactor does nothing when fixMode is not CostList. Value: " + baseWorkFactor;
            }

            if ( minToLeave < 0 || minToLeave > 1 )
            {
                yield return "[MSE2] minToLeave has to be between 0. and 1. Value: " + minToLeave;
            }

            if ( !wasFixed )
            {
                yield return "[MSE2] part still has cost to fix";
            }
        }

        //internal IEnumerable<ThingDefCountClass> RecursivePartsInCost
        //{
        //    get
        //    {
        //        if ( this.partsInCost != null )
        //        {
        //            for ( int i = 0; i < partsInCost.Count; i++ )
        //            {
        //                var part = partsInCost[i];
        //                yield return part;

        //                var rec = part.thingDef.GetModExtension<PartsIncludedInCost>();
        //                if ( rec != null )
        //                    foreach ( var subpart in rec.RecursivePartsInCost )
        //                    {
        //                        yield return subpart;
        //                    }
        //            }
        //        }
        //    }
        //}

        internal enum FixMode
        {
            CostList,
            MarketValue,
        }
    }
}