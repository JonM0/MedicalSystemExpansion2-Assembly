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
    [HarmonyPatch( typeof( ThingDef ) )]
    [HarmonyPatch( nameof( ThingDef.AllRecipes ), MethodType.Getter )]
    internal static class SortThingDefAllRecipes
    {
        [HarmonyPrefix]
        internal static bool CheckIfDirty ( List<RecipeDef> ___allRecipesCached, ref bool __state )
        {
            __state = ___allRecipesCached == null;

            return true;
        }

        [HarmonyPostfix]
        internal static void SortList ( List<RecipeDef> __result, bool __state )
        {
            if ( __state && __result.Count > 0 )
            {
                RecipeDef[] resCopy = __result.ToArray();

                __result.Clear();

                __result.AddRange(
                    from r in resCopy
                    group r by r.addsHediff into rg
                    from r in rg
                    group r by r.ProducedThingDef into rg
                    from r in rg
                    select r );
            }
        }
    }
}