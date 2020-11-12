using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

using Verse;

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
            if ( this.baseWorkFactor < 0 )
            {
                yield return "[MSE2] baseWorkFactor has to be positive. Value: " + this.baseWorkFactor;
            }

            if ( this.baseWorkFactor != 1f && this.fixMode != FixMode.CostList )
            {
                yield return "[MSE2] baseWorkFactor does nothing when fixMode is not CostList. Value: " + this.baseWorkFactor;
            }

            if ( this.minToLeave < 0 || this.minToLeave > 1 )
            {
                yield return "[MSE2] minToLeave has to be between 0. and 1. Value: " + this.minToLeave;
            }

            if ( !this.wasFixed )
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