using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.RecipeFixer
{
    [HarmonyPatch( typeof( DefGenerator ) )]
    [HarmonyPatch( nameof( DefGenerator.GenerateImpliedDefs_PreResolve ) )]
    internal static class FixRecipesPatch
    {
        [HarmonyPrefix]
        private static bool PreFix ()
        {
            FixRecipes();
            return true;
        }

        private static void FixRecipes ()
        {
            DeepProfiler.Start( "[MSE2] Fix costlists of ThingDefs with PartsIncludedInCost modextension." );
            try
            {
                RecipeFixingUtilities.FixAllCostLists();
            }
            finally
            {
                DeepProfiler.End();
            }
        }
    }
}