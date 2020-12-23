﻿using System;
using System.Collections.Generic;
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
                    DefGenerator.AddImpliedDef<RecipeDef>( def );
                    HugsLib.Utils.InjectedDefHasher.GiveShortHashToDef( def, typeof( RecipeDef ) );
                    defsToCheck.Add( def );
                }
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception adding ImpliedLimbRecipeDefs: " + ex );
            }

            try
            {
                // duplicate ambiguous installation surgeries
                foreach ( RecipeDef def in ExtraLimbSurgeryRecipeDefs() )
                {
                    def.ResolveReferences();
                    DefGenerator.AddImpliedDef<RecipeDef>( def );
                    HugsLib.Utils.InjectedDefHasher.GiveShortHashToDef( def, typeof( RecipeDef ) );
                    defsToCheck.Add( def );
                }
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception adding ExtraLimbSurgeryRecipeDefs: " + ex );
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
                        Log.Error( string.Concat( "Config error in ", def, ": ", item ) );
                    }
                }
                catch ( Exception ex )
                {
                    Log.Error( "[MSE2] Exception in ConfigErrors() of " + def.defName + ": " + ex );
                }
            }
        }

        internal static IEnumerable<RecipeDef> ExtraLimbSurgeryRecipeDefs ()
        {
            List<LimbConfiguration> tmplimbsItCanTargetList = new List<LimbConfiguration>();

            // iterate over thingDefs with child parts
            foreach ( (ThingDef thingDef, CompProperties_IncludedChildParts comp) in
                DefDatabase<ThingDef>.AllDefs
                .Select( t => (t, t.GetCompProperties<CompProperties_IncludedChildParts>()) )
                .Where( c => c.Item2 != null ) )
            {
                // iterate over the surgeries to install them
                foreach ( RecipeDef surgery in IncludedPartsUtilities.SurgeryToInstall( thingDef ).ToArray() )
                {
                    // take the limbs that the thing can be installed on that this recipe targets
                    tmplimbsItCanTargetList.Clear();
                    tmplimbsItCanTargetList.AddRange(
                        comp.InstallationDestinations
                        .Where(
                            l =>
                            surgery.appliedOnFixedBodyParts.Contains( l.PartDef )
                            && surgery.AllRecipeUsers.Any( ru => l.Bodies.Contains( ru.race.body ) )
                        )
                    );

                    int count = 0;
                    foreach ( LimbConfiguration limb in tmplimbsItCanTargetList )
                    {
                        if ( count == 0 )
                        {
                            // put the first limb on the preexisting surgery
                            if ( surgery.modExtensions == null ) surgery.modExtensions = new List<DefModExtension>();

                            surgery.modExtensions.Add( new TargetLimb( limb ) );
                        }
                        else
                        {
                            // clone the surgery for the other limbs
                            RecipeDef surgeryClone = (RecipeDef)typeof( RecipeDef ).GetMethod( "MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance ).Invoke( surgery, new object[0] );
                            if ( surgeryClone == surgery ) Log.Error( "WTF" );

                            surgeryClone.defName = string.Copy( surgery.defName ) + count;

                            surgeryClone.label = string.Copy( surgery.label )/* + " " + count*/;

                            surgeryClone.modExtensions = new List<DefModExtension>( surgery.modExtensions );
                            surgeryClone.modExtensions.Remove( surgery.GetModExtension<TargetLimb>() );
                            surgeryClone.modExtensions.Add( new TargetLimb( limb ) );

                            typeof( RecipeDef ).GetField( "workerInt", BindingFlags.NonPublic | BindingFlags.Instance ).SetValue( surgeryClone, null );
                            typeof( RecipeDef ).GetField( "workerCounterInt", BindingFlags.NonPublic | BindingFlags.Instance ).SetValue( surgeryClone, null );
                            typeof( RecipeDef ).GetField( "ingredientValueGetterInt", BindingFlags.NonPublic | BindingFlags.Instance ).SetValue( surgeryClone, null );

                            surgeryClone.shortHash = 0;

                            yield return surgeryClone;
                        }
                        count++;
                    }

                    //if ( count > 1 )
                    //{
                    //    surgery.label += " 0";
                    //}
                }
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

        internal static (List<IngredientCount>, float) AllIngredientsWorkForLimb ( ThingDef thingDef, LimbConfiguration limb )
        {
            //Log.Message( "Calculating ingredients of " + thingDef.defName );

            if ( thingDef.costList == null )
            {
                Log.Error( "[MSE2] Tried to create list of ingredients for " + thingDef.label + ", which can not be crafted" );
                return (null, 0);
            }

            float work = 0f;

            // all the actual segments the part will include
            List<ThingDefCountClass> allParts = new List<ThingDefCountClass>();
            allParts.Add( new ThingDefCountClass( thingDef, 1 ) );
            CompProperties_IncludedChildParts comp = thingDef.GetCompProperties<CompProperties_IncludedChildParts>();
            if ( comp != null )
            {
                // add the rest grouped by thingdef
                allParts.AddRange( comp.AllPartsForLimb( limb ).GroupBy( t => t ).Select( g => new ThingDefCountClass( g.Key, g.Count() ) ) );
            }

            // the ingredients to make the segments, not grouped by thingdef
            List<ThingDefCountClass> intermediateIngredients = new List<ThingDefCountClass>();
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

            List<IngredientCount> ingredients = new List<IngredientCount>();
            foreach ( ThingDefCountClass thingDefCountClass in from i in intermediateIngredients
                                                               group i by i.thingDef into g
                                                               select new ThingDefCountClass( g.Key, g.Select( tdcc => tdcc.count ).Sum() ) )
            {
                IngredientCount ingredientCount = new IngredientCount();
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
            }

            // generate limb specific crafting recipes
            foreach ( LimbConfiguration limb in comp.InstallationDestinations )
            {
                RecipeDef recipeDef = new RecipeDef();

                RecipeMakerProperties recipeMaker = prosthesisDef.recipeMaker;

                // set all text
                string limbComparison = comp.LabelComparisonForLimb( limb );

                recipeDef.defName = "Make_" + prosthesisDef.defName + "_" + limb.UniqueName;
                recipeDef.label = (limbComparison == "LimbComplete".Translate() ?
                    "RecipeMakeForLimbNoComparison" : "RecipeMakeForLimb").Translate( prosthesisDef.label, limbComparison );
                recipeDef.jobString = "RecipeMakeForLimbJobString".Translate( prosthesisDef.label, limbComparison );
                recipeDef.description = "RecipeMakeForLimbDescription".Translate( limbComparison, prosthesisDef.label );
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
                recipeDef.recipeUsers = recipeMaker.recipeUsers.ListFullCopyOrNull<ThingDef>();
                recipeDef.effectWorking = recipeMaker.effectWorking;
                recipeDef.soundWorking = recipeMaker.soundWorking;
                recipeDef.researchPrerequisite = recipeMaker.researchPrerequisite;
                recipeDef.researchPrerequisites = recipeMaker.researchPrerequisites;
                recipeDef.factionPrerequisiteTags = recipeMaker.factionPrerequisiteTags;

                // set ingredients and work based on combined subparts
                (recipeDef.ingredients, recipeDef.workAmount) = AllIngredientsWorkForLimb( prosthesisDef, limb );

                // add specific limb modextension
                if ( recipeDef.modExtensions == null ) recipeDef.modExtensions = new List<DefModExtension>();
                recipeDef.modExtensions.Add( new TargetLimb( limb ) );

                if ( !recipeDef.ingredients.NullOrEmpty() )
                    yield return recipeDef;
            }
        }
    }
}