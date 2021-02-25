using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Verse;

using RimWorld;



namespace MSE2.BackCompatibility
{
    [HarmonyPatch( typeof( HediffSet ), nameof( HediffSet.ExposeData ) )]
    internal static class HediffClassLoading
    {
        // make sure that hediffs are the type required by the def, reinstall them if they are not

        [HarmonyPostfix]
        internal static void ReinstallWrongClassed ( HediffSet __instance )
        {
            if ( Scribe.mode == LoadSaveMode.ResolvingCrossRefs )
            {
                var brokenHediffs = __instance.hediffs.FindAll( h => h.GetType() != h.def.hediffClass );
                if ( brokenHediffs.Count > 0 )
                {
                    Log.Warning( "Reinstalling Hediffs with wrong type: " + string.Join( ", ", brokenHediffs ) );
                    __instance.hediffs.RemoveAll( brokenHediffs.Contains );

                    __instance.DirtyCache();

                    foreach ( var bh in brokenHediffs )
                    {
                        __instance.AddDirect( HediffMaker.MakeHediff( bh.def, bh.pawn, bh.Part ) );
                    }
                }
            }
        }
    }
}
