using System;
using System.Collections.Generic;
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
            base.DefsLoaded();

            AutoRecipeUserUtilities.ApplyAutoRecipeUsers();

            IncludedPartsUtilities.CacheAllStandardParents();

            IgnoreSubPartsUtilities.IgnoreAllNonCompedSubparts();

            LimbRecipeDefGenerator.AddExtraRecipesToDefDatabase();
        }

        public override string ModIdentifier => "MSE2";

        public static MedicalSystemExpansion Instance { get; private set; }
    }
}