using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MSE2.RecipeFixer
{
    public static class RecipeFixingUtilities
    {
        private static void FixCostList ( ThingDef thingDef, PartsIncludedInCost partsDefMod, List<ThingDefCountClass> allPartsList )
        {
            foreach ( (ThingDef part, int totalOfPart) in from part in allPartsList // all the parts that need their cost removed from here
                                                          from cost in part.thingDef.costList // the ingredients of those parts
                                                          group (cost.thingDef, cost.count * part.count / (part.thingDef.recipeMaker?.productCount ?? 1)) by cost.thingDef into tc // group them by the thingdef
                                                          select (tc.Key, tc.Select( x => x.Item2 ).Sum()) ) // add up all the total by thingdef
            {
                // take the corresponding price item
                var originalPartCost = thingDef.costList.Find( x => x.thingDef == part );
                if ( originalPartCost != null )
                {
                    // decrement it by totalOfPart, but leave at least the minimum specified in partsDefMod rounded up
                    originalPartCost.count = Math.Max( (int)Math.Ceiling( originalPartCost.count * partsDefMod.minToLeave ), originalPartCost.count - totalOfPart );

                    if ( originalPartCost.count == 0 ) thingDef.costList.Remove( originalPartCost );
                }
            }
        }

        private static void FixWorkToMake ( ThingDef thingDef, PartsIncludedInCost partsDefMod, List<ThingDefCountClass> allPartsList )
        {
            var work = thingDef.GetStatValueAbstract( StatDefOf.WorkToMake );
            work *= partsDefMod.baseWorkFactor;
            //var temp = allPartsList.Select( p => p.thingDef.GetStatValueAbstract( StatDefOf.WorkToMake ) ).ToList();
            var workOfParts = allPartsList.Select( p => p.thingDef.GetStatValueAbstract( StatDefOf.WorkToMake ) * p.count ).Sum();
            work = Math.Max( work * partsDefMod.minToLeave, work - workOfParts );
            thingDef.SetStatBaseValue( StatDefOf.WorkToMake, work );
        }

        internal static void FixRecipe ( ThingDef thingDef, PartsIncludedInCost partsDefMod )
        {
            var allPartsList = partsDefMod.partsInCost.ToList();

            foreach ( var part in allPartsList )
            {
                EnsureFixedRecipe( part.thingDef );
            }

            Log.Message( "[MSE2] Fixing costList of " + thingDef.defName );

            switch ( partsDefMod.fixMode )
            {
            case PartsIncludedInCost.FixMode.CostList:
                FixWorkToMake( thingDef, partsDefMod, allPartsList );
                FixCostList( thingDef, partsDefMod, allPartsList );
                break;

            case PartsIncludedInCost.FixMode.MarketValue:
                var valueOfParts = allPartsList.Select( p => p.thingDef.BaseMarketValue * p.count ).Sum();
                thingDef.BaseMarketValue = Math.Max( thingDef.BaseMarketValue * partsDefMod.minToLeave, thingDef.BaseMarketValue - valueOfParts );

                break;

            default:
                Log.Error( "Unsupported FixMode" );
                break;
            }

            partsDefMod.wasFixed = true;
        }

        internal static void EnsureFixedRecipe ( ThingDef thingDef )
        {
            var partsIncluded = thingDef.GetModExtension<PartsIncludedInCost>();
            if ( partsIncluded != null && !partsIncluded.wasFixed )
            {
                FixRecipe( thingDef, partsIncluded );
            }
        }

        internal static void FixAllCostLists ()
        {
            Log.Message( DefDatabase<ThingDef>.AllDefsListForReading.Count + " thingdefs existing" );

            foreach ( var def in DefDatabase<ThingDef>.AllDefs )
            {
                EnsureFixedRecipe( def );
            }
        }
    }
}