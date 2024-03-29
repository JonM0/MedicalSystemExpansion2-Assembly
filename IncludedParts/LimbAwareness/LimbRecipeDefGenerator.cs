﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

using Verse;
using Verse.Noise;

namespace MSE2
{
    public static class LimbRecipeDefGenerator
    {
        public static void AddExtraRecipesToDefDatabase ()
        {
            var defsToCheck = new List<RecipeDef>();


            try
            {
                // add the recipes to craft the prostheses with the various configurations of parts
                foreach ( RecipeDef def in ImpliedLimbRecipeDefs() )
                {
                    def.ResolveReferences();
                    DefGenerator.AddImpliedDef( def );
                    HugsLib.Utils.InjectedDefHasher.GiveShortHashToDef( def, typeof( RecipeDef ) );
                    defsToCheck.Add( def );
                }
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception adding ImpliedLimbRecipeDefs: " + ex );
            }

            foreach ( var def in defsToCheck )
            {
                try
                {
                    if ( def.ignoreConfigErrors )
                    {
                        continue;
                    }

                    foreach ( string item in def.ConfigErrors() )
                    {
                        Log.Error( string.Concat( "[MSE2] Config error in ", def, ": ", item ) );
                    }
                }
                catch ( Exception ex )
                {
                    Log.Error( "[MSE2] Exception in ConfigErrors() of " + def.defName + ": " + ex );
                }
            }

            try
            {
                UpdateCachedWorkTables( defsToCheck );
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception doing " + nameof( UpdateCachedWorkTables ) + ": " + ex );
            }
        }

        internal static IEnumerable<RecipeDef> ImpliedLimbRecipeDefs ()
        {
            return from ThingDef thingDef in DefDatabase<ThingDef>.AllDefs
                   where thingDef.GetCompProperties<CompProperties_IncludedChildParts>() != null
                   where thingDef.costList != null
                   from RecipeDef recipe in LimbCreationDef( thingDef )
                   select recipe;
        }

        internal static (List<IngredientCount>, float) AllIngredientsWorkForVersion ( ThingDef thingDef, ProsthesisVersion version )
        {
            _ = thingDef ?? throw new ArgumentNullException( nameof( thingDef ) );
            _ = version ?? throw new ArgumentNullException( nameof( version ) );

            if ( thingDef.costList == null )
            {
                Log.Error( "[MSE2] Tried to create list of ingredients for " + thingDef.label + ", which can not be crafted" );
                return (null, 0);
            }

            float work = 0f;

            // all the actual segments the part will include
            List<ThingDefCountClass> allParts = new()
            {
                new ThingDefCountClass( thingDef, 1 )
            };

            // add the rest grouped by thingdef
            allParts.AddRange( version.AllParts.GroupBy( t => t ).Select( g => new ThingDefCountClass( g.Key, g.Count() ) ) );

            // the ingredients to make the segments, not grouped by thingdef
            List<ThingDefCountClass> intermediateIngredients = new();
            foreach ( ThingDefCountClass partCount in allParts )
            {
                int recipeProductCount = partCount.thingDef.recipeMaker.productCount;

                // add up work
                work += partCount.thingDef.GetStatValueAbstract( StatDefOf.WorkToMake ) * partCount.count / recipeProductCount;

                // add up parts
                if ( partCount.thingDef.costList != null )
                {
                    foreach ( ThingDefCountClass intIngredientCount in partCount.thingDef.costList )
                    {
                        // add it's ingredients, adjusted by how many are crafted and rounded up
                        intermediateIngredients.Add( new ThingDefCountClass( intIngredientCount.thingDef, (intIngredientCount.count * partCount.count - 1) / recipeProductCount + 1 ) );
                    }
                }
                else
                {
                    // piece is uncraftable, add it to the list
                    intermediateIngredients.Add( partCount );
                }
            }

            List<IngredientCount> ingredients = new();
            foreach ( ThingDefCountClass thingDefCountClass in from i in intermediateIngredients
                                                               group i by i.thingDef into g
                                                               select new ThingDefCountClass( g.Key, g.Select( tdcc => tdcc.count ).Sum() ) )
            {
                IngredientCount ingredientCount = new();
                ingredientCount.SetBaseCount( (float)(thingDefCountClass.count) );
                ingredientCount.filter.SetAllow( thingDefCountClass.thingDef, true );
                ingredients.Add( ingredientCount );
            }

            return (ingredients, work);
        }

        internal static IEnumerable<RecipeDef> LimbCreationDef ( ThingDef prosthesisDef )
        {
            CompProperties_IncludedChildParts comp = prosthesisDef.GetCompProperties<CompProperties_IncludedChildParts>();

            // relabel original recipe
            RecipeDef originalRecipe = DefDatabase<RecipeDef>.GetNamed( "Make_" + prosthesisDef.defName );
            if ( originalRecipe != null )
            {
                originalRecipe.label = "RecipeMakeSegment".Translate( prosthesisDef.label );
                if ( originalRecipe.modExtensions == null ) originalRecipe.modExtensions = new List<DefModExtension>();
                originalRecipe.modExtensions.Add( new TargetLimb( comp.SegmentVersion ) );
            }

            int versionCounter = 0; // version counter

            // generate limb specific crafting recipes
            foreach ( ProsthesisVersion version in comp.SupportedVersions.Where( v => v.Parts.Any() ) )
            {
                RecipeDef recipeDef = null;
                try
                {
                    recipeDef = new RecipeDef();

                    RecipeMakerProperties recipeMaker = prosthesisDef.recipeMaker;

                    // set all text
                    string limbComparison = version.Label;

                    recipeDef.defName = "Make_" + prosthesisDef.defName + "_v" + (versionCounter++);
                    recipeDef.label = (limbComparison == "LimbComplete".Translate() ?
                        "RecipeMakeForLimbNoComparison" : "RecipeMakeForLimb").Translate( prosthesisDef.label, limbComparison );
                    recipeDef.jobString = "RecipeMakeForLimbJobString".Translate( prosthesisDef.label, limbComparison );
                    recipeDef.description = "RecipeMakeForLimbDescription".Translate( limbComparison, prosthesisDef.label, comp.GetRacesForVersion( version ).ToCommaList( true ) );
                    recipeDef.descriptionHyperlinks = originalRecipe.descriptionHyperlinks;

                    // copy other values
                    recipeDef.modContentPack = prosthesisDef.modContentPack;
                    recipeDef.workSpeedStat = recipeMaker.workSpeedStat;
                    recipeDef.efficiencyStat = recipeMaker.efficiencyStat;
                    recipeDef.defaultIngredientFilter = recipeMaker.defaultIngredientFilter;
                    recipeDef.products.Add( new ThingDefCountClass( prosthesisDef, recipeMaker.productCount ) );
                    recipeDef.targetCountAdjustment = recipeMaker.targetCountAdjustment;
                    recipeDef.skillRequirements = recipeMaker.skillRequirements.ListFullCopyOrNull<SkillRequirement>();
                    recipeDef.workSkill = recipeMaker.workSkill;
                    recipeDef.workSkillLearnFactor = recipeMaker.workSkillLearnPerTick;
                    recipeDef.requiredGiverWorkType = recipeMaker.requiredGiverWorkType;
                    recipeDef.unfinishedThingDef = recipeMaker.unfinishedThingDef;
                    recipeDef.recipeUsers = originalRecipe.AllRecipeUsers.ToList();
                    recipeDef.effectWorking = recipeMaker.effectWorking;
                    recipeDef.soundWorking = recipeMaker.soundWorking;
                    recipeDef.researchPrerequisite = recipeMaker.researchPrerequisite;
                    recipeDef.researchPrerequisites = recipeMaker.researchPrerequisites;
                    recipeDef.factionPrerequisiteTags = recipeMaker.factionPrerequisiteTags;

                    // set ingredients and work based on combined subparts
                    (recipeDef.ingredients, recipeDef.workAmount) = AllIngredientsWorkForVersion( prosthesisDef, version );

                    // add specific limb modextension
                    if ( recipeDef.modExtensions == null ) recipeDef.modExtensions = new List<DefModExtension>();
                    recipeDef.modExtensions.Add( new TargetLimb( version ) );

                }
                catch ( Exception ex )
                {
                    Log.Error( "[MSE2] Failed to generate crafting recipe for " + prosthesisDef.defName + ", " + version.Label + ". Exception: " + ex );
                    continue;
                }
                if ( (!recipeDef?.ingredients.NullOrEmpty()) ?? false )
                    yield return recipeDef;
            }
        }

        internal static void UpdateCachedWorkTables ( List<RecipeDef> recipeDefs )
        {
            FieldInfo ThingDef_allRecipesCached = typeof( ThingDef ).GetField( "allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic );

            foreach ( var thingDef in DefDatabase<ThingDef>.AllDefs.Where( d => d.IsWorkTable ) )
            {
                List<RecipeDef> cachedRecipes = (List<RecipeDef>)ThingDef_allRecipesCached.GetValue( thingDef );

                if ( cachedRecipes != null )
                {
                    foreach ( var recipeDef in recipeDefs )
                    {
                        if ( recipeDef.recipeUsers != null && recipeDef.recipeUsers.Contains( thingDef ) && !cachedRecipes.Contains( recipeDef ) )
                        {
                            int insertionindex;

                            if ( recipeDef.addsHediff != null )
                            {
                                insertionindex = cachedRecipes.FindLastIndex( r => r.addsHediff == recipeDef.addsHediff );
                            }
                            else if ( recipeDef.ProducedThingDef != null )
                            {
                                insertionindex = cachedRecipes.FindLastIndex( r => r.ProducedThingDef == recipeDef.ProducedThingDef );
                            }
                            else
                            {
                                insertionindex = -1;
                            }

                            if ( insertionindex == -1 )
                            {
                                cachedRecipes.Add( recipeDef );
                            }
                            else
                            {
                                cachedRecipes.Insert( insertionindex + 1, recipeDef );
                            }
                        }
                    }
                }
            }
        }
    }
}