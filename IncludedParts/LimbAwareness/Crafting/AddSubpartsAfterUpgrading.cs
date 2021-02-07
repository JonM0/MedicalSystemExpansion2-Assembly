using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( GenRecipe ) )]
    [HarmonyPatch( nameof( GenRecipe.MakeRecipeProducts ) )]
    internal static class AddSubpartsAfterUpgrading
    {
        // this will check if the recipe is an upgrade of a prosthesis (ingredient and product have CompIncludedChildParts) and try to make them similar

        [HarmonyPostfix]
        internal static void AddSubparts ( ref IEnumerable<Thing> __result, List<Thing> ingredients )
        {
            Thing ingredientToUpgrade = ingredients.Find( t => t.TryGetComp<CompIncludedChildParts>() != null );

            if ( ingredientToUpgrade != null )
            {
                List<Thing> products = __result.ToList();

                Thing productToInitialize = products.Find( t => t.TryGetComp<CompIncludedChildParts>() != null );
                if ( productToInitialize != null )
                {
                    //productToInitialize.TryGetComp<CompIncludedChildParts>().InitializeFromSimilar( ingredientToUpgrade.TryGetComp<CompIncludedChildParts>() );
                    Log.Error( "[MSE2] Prosthetic upgrading is not supported." );
                }

                //__result = products;
            }
        }
    }
}