using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch]
    internal class AddSubpartsAfterCreation
    {
        // initialize for limb on newly crafted prostheses using the limb specified in the modextension of the recipedef

        private static MethodBase TargetMethod ()
        {
            return typeof( GenRecipe ).GetMethod( "PostProcessProduct", BindingFlags.NonPublic | BindingFlags.Static );
        }

        [HarmonyPostfix]
        public static void AddSubparts ( ref Thing __result, RecipeDef recipeDef )
        {
            if ( recipeDef.HasModExtension<TargetLimb>() && __result.TryGetComp<CompIncludedChildParts>() != null )
            {
                __result.TryGetComp<CompIncludedChildParts>().InitializeForLimb( recipeDef.GetModExtension<TargetLimb>().targetLimb );
            }
        }
    }
}