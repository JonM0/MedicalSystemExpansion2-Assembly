using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;

using Verse;

namespace MSE2
{
    internal static class IgnoreSubPartsUtilities
    {
        public static bool PartShouldBeIgnored ( this HediffSet set, BodyPartRecord bodyPart )
        {
            if ( bodyPart != null && bodyPart.parent != null )
            {
                IgnoreSubParts modExt = set.hediffs
                    .Find( h => h.Part == bodyPart.parent && h.def.HasModExtension<IgnoreSubParts>() )// added part on parent bodypartrecord
                    ?.def.GetModExtension<IgnoreSubParts>();

                return
                    (modExt != null && modExt.ignoredSubParts.Contains( bodyPart.def ))
                    || set.PartShouldBeIgnored( bodyPart.parent );
            }
            return false;
        }

        public static IEnumerable<BodyPartRecord> AllChildParts ( this BodyPartRecord bodyPart )
        {
            foreach ( BodyPartRecord p in bodyPart.parts )
            {
                yield return p;
                foreach ( BodyPartRecord p2 in p.AllChildParts() )
                    yield return p2;
            }
            yield break;
        }

        private static List<BodyPartDef> AllChildPartDefs ( this BodyPartDef bodyPartDef, IEnumerable<BodyDef> bodies = null )
        {
            List<BodyPartDef> list = new List<BodyPartDef>();

            foreach ( BodyDef bodyDef in bodies ?? DefDatabase<BodyDef>.AllDefs )
            {
                foreach ( BodyPartRecord partRecord in bodyDef.AllParts )
                {
                    if ( partRecord.def == bodyPartDef )
                    {
                        list.AddRange(
                            partRecord.AllChildParts()
                            .Select( r => r.def )
                            .Where( d => !list.Contains( d ) ) );
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Makes limb prostheses without a CompProperties_IncludedChildParts behave like they would in vanilla
        /// </summary>
        public static void IgnoreAllNonCompedSubparts ()
        {
            try
            {
                List<string> brokenMods = new List<string>();

                StringBuilder unpatchedDefs = new StringBuilder();

                foreach ( (RecipeDef recipeDef, IgnoreSubParts oldME) in
                    from r in DefDatabase<RecipeDef>.AllDefs
                    where r != null
                    where r.IsSurgery
                    where
                        // has ingredients
                        r.fixedIngredientFilter?.AllowedThingDefs != null
                        // no ingredient has a CompProperties_IncludedChildParts
                        && !r.fixedIngredientFilter.AllowedThingDefs
                            .Select( t => t.GetCompProperties<CompProperties_IncludedChildParts>() )
                            .Any( c => c != null )
                    where
                        // adds a hediff
                        r.addsHediff?.hediffClass != null
                        // of type addedpart
                        && typeof( Hediff_AddedPart ).IsAssignableFrom( r.addsHediff.hediffClass )
                    let me = r.addsHediff.GetModExtension<IgnoreSubParts>()
                    where
                        // that does not already ignore parts
                        me == null || me.ignoreAll
                    select (r, me) )
                {
                    IgnoreSubParts modExt = oldME ?? new IgnoreSubParts();

                    // add all the subparts this prosthesis could have
                    if ( recipeDef.appliedOnFixedBodyParts != null )
                        foreach ( BodyPartDef partDef in recipeDef.appliedOnFixedBodyParts )
                        {
                            if ( modExt.ignoredSubParts == null )
                                modExt.ignoredSubParts = new List<BodyPartDef>();

                            modExt.ignoredSubParts.AddRange( partDef.AllChildPartDefs( recipeDef.AllRecipeUsers.Select( ru => ru.race.body ) ) );
                        }

                    // found any and it wasnt already present
                    if ( !modExt.ignoredSubParts.NullOrEmpty() && oldME == null )
                    {
                        // add the modextension
                        if ( recipeDef.addsHediff.modExtensions == null )
                            recipeDef.addsHediff.modExtensions = new List<DefModExtension>();

                        recipeDef.addsHediff.modExtensions.Add( modExt );

                        // log only for humanlike
                        //if ( recipeDef.AllRecipeUsers.Any( td => td.race.Humanlike ) )
                        {
                            unpatchedDefs.AppendLine(
                                string.Format( "<{0}> from \"{1}\": {2}",
                                    recipeDef.addsHediff.defName,
                                    recipeDef.modContentPack?.Name ?? "???",
                                    string.Join( ", ", modExt.ignoredSubParts.Select( p => p.label ) )
                                    )
                                );

                            if ( !brokenMods.Contains( recipeDef.modContentPack?.Name ) )
                                brokenMods.Add( recipeDef.modContentPack?.Name );
                        }
                    }
                }

                if ( brokenMods.Count > 0 )
                {
                    Log.Warning( string.Format( "[MSE2] Some prostheses that have not been patched were detected in mods: {0}. They will default to vanilla behaviour.\n\n{1}", string.Join( ", ", brokenMods ), unpatchedDefs ) );
                }
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception applying IgnoreAllNonCompedSubparts: " + ex );
            }
        }
    }
}