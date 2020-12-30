﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

using HarmonyLib;

namespace MSE2.BackCompatibility
{
    [HarmonyPatch(typeof(Verse.BackCompatibility))]
    [HarmonyPatch(nameof(Verse.BackCompatibility.PostLoadSavegame))]
    internal static class PostLoad
    {
        [HarmonyPostfix]
        internal static void FixProstheses()
        {
            if ( ScribeMetaHeaderUtility.loadedModIdsList != null && !ScribeMetaHeaderUtility.loadedModIdsList.Contains( "mse2.core" ) )
            {
                Log.Message( "[MSE2] Detected savegame created without MSE2, applying compatibility fixes." );

                Log.Message( string.Join( ", ", ScribeMetaHeaderUtility.loadedModIdsList ) );

                GlobalProthesisFix.Apply();
            }
        }
    }
}
