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
    [EarlyInit]
    [StaticConstructorOnStartup]
    public class MedicalSystemExpansion : ModBase
    {
        public override void DefsLoaded ()
        {
            base.DefsLoaded();

            IncludedPartsUtilities.CacheAllStandardParents();

            IgnoreSubPartsUtilities.IgnoreAllNonCompedSubparts();

            // add the recipes to craft the prostheses with the various configurations of parts
            foreach ( RecipeDef def in LimbRecipeDefGenerator.ImpliedLimbRecipeDefs() )
            {
                def.ResolveReferences();
                DefGenerator.AddImpliedDef<RecipeDef>( def );
                HugsLib.Utils.InjectedDefHasher.GiveShortHashToDef( def, typeof( RecipeDef ) );
            }

            // duplicate ambiguous installation surgeries
            foreach ( RecipeDef def in LimbRecipeDefGenerator.ExtraLimbSurgeryRecipeDefs() )
            {
                def.ResolveReferences();
                DefGenerator.AddImpliedDef<RecipeDef>( def );
                HugsLib.Utils.InjectedDefHasher.GiveShortHashToDef( def, typeof( RecipeDef ) );
            }

            DefDatabase<RecipeDef>.ErrorCheckAllDefs();
        }

        public override string ModIdentifier => "MSE2";
    }
}