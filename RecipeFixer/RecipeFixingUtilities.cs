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
        internal static void FixRecipe ( ThingDef thingDef, PartsIncludedInCost partsDefMod )
        {
            var allPartsList = partsDefMod.RecursivePartsInCost.ToList();

            foreach ( var part in allPartsList )
            {
                EnsureFixedRecipe( part.thingDef );
            }

            switch ( partsDefMod.fixMode )
            {
                case PartsIncludedInCost.FixMode.CostList:
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
                        }
                    }
                    break;

                case PartsIncludedInCost.FixMode.MarketValue:
                    var valueOfParts = allPartsList.Select( p => p.thingDef.BaseMarketValue * p.count ).Sum();
                    thingDef.BaseMarketValue = Math.Max( thingDef.BaseMarketValue * partsDefMod.minToLeave, thingDef.BaseMarketValue - valueOfParts );

                    break;

                default:
                    Log.Error( "" );
                    break;
            }

            partsDefMod.wasFixed = true;
        }

        internal static void EnsureFixedRecipe ( ThingDef thingDef )
        {
            var partsIncluded = thingDef.GetModExtension<PartsIncludedInCost>();
            if ( partsIncluded != null && !partsIncluded.wasFixed )
            {
                Log.Message( "[MSE2] Fixing costList of " + thingDef.defName );
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