using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading;

using HarmonyLib;

using HugsLib;

using RimWorld;

using UnityEngine;

using UnityEngineInternal;

using Verse;

namespace MSE2
{
    public class MedicalSystemExpansion : ModBase
    {
        public override void StaticInitialize ()
        {
            base.StaticInitialize();
            Instance = this;
        }

        public override void DefsLoaded ()
        {
#if DEBUG
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            try
            {
                base.DefsLoaded();

                AutoRecipeUserUtilities.ApplyAutoRecipeUsers();

                IncludedPartsUtilities.CacheAllStandardParents();

                IgnoreSubPartsUtilities.IgnoreAllNonCompedSubparts();

                LimbRecipeDefGenerator.AddExtraRecipesToDefDatabase();
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception caught running DefsLoaded(): " + ex );
            }
#if DEBUG
            finally
            {
                stopwatch.Stop();
                Log.Message( "[MSE2] DefsLoaded completed in " + stopwatch.Elapsed );
            }
#endif
        }

        public override string ModIdentifier => "MSE2";

        public static MedicalSystemExpansion Instance { get; private set; }
    }
}