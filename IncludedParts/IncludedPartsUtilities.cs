using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using RimWorld.QuestGen;

using UnityEngine;

using Verse;

namespace MSE2
{
    public static class IncludedPartsUtilities
    {
        /// <summary>
        /// For each of the hediffs in the game, adds it to its children's standard parents cache (as mapped by CompProperties_IncludedChildParts of the thingdef)
        /// </summary>
        public static void CacheAllStandardParents ()
        {
            foreach ( HediffDef def in DefDatabase<HediffDef>.AllDefs )
            {
                try
                {
                    def.CacheParentOfChildren();
                }
                catch ( Exception ex )
                {
                    Log.Error( "[MSE2] Exception applying CacheParentOfChildren to " + def.defName + ": " + ex );
                }
            }
        }

        public static void PrintIncompatibleVersionsReport ()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {

                StringBuilder builder = new StringBuilder();
                int errors = 0;


                foreach ( var comp in DefDatabase<ThingDef>.AllDefs.Select( d => d.GetCompProperties<CompProperties_IncludedChildParts>() ).Where( p => p != null ) )
                {
                    if ( comp.IncompatibleLimbsReport( builder ) )
                    {
                        errors++;
                    }
                }

                stopwatch.Stop();

                builder.AppendLine().AppendLine().Append( "elapsed time " ).Append( stopwatch.Elapsed );

                if ( errors > 0 )
                {
                    Log.Warning( "[MSE2] " + errors + " prostheses with limb incompatibilities detected: \n" + builder );
                }

            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception doing " + nameof( PrintIncompatibleVersionsReport ) + ": " + ex );
            }
            finally
            {
                stopwatch.Stop();
            }

        }


        private static void CacheParentOfChildren ( this HediffDef parent )
        {
            CompProperties_IncludedChildParts comp = parent.spawnThingOnRemoved?.GetCompProperties<CompProperties_IncludedChildParts>(); // comp on the corresponding ThingDef
            if ( comp != null && !comp.standardChildren.NullOrEmpty() ) // if it has standard children
            {
                foreach ( HediffDef def in from d in DefDatabase<HediffDef>.AllDefs
                                           where d.spawnThingOnRemoved != null
                                           where comp.standardChildren.Contains( d.spawnThingOnRemoved )
                                           select d )
                {
                    def.AddStandardParent( parent ); // add it to the hediffdefs corresponding to the standard children
                }
            }
        }

        private static void AddStandardParent ( this HediffDef hediffDef, HediffDef parent )
        {
            if ( !hediffDef.HasModExtension<MSE_cachedStandardParents>() )
            {
                if ( hediffDef.modExtensions == null ) hediffDef.modExtensions = new List<DefModExtension>();

                hediffDef.modExtensions.Add( new MSE_cachedStandardParents() );
            }

            hediffDef.GetModExtension<MSE_cachedStandardParents>().Add( parent );
        }

        /// <summary>
        /// Checks if the parent part of the hediff has a hediff that is a standard parent of the given one
        /// </summary>
        public static bool IsParentStandard ( this Hediff hediff )
        {
            MSE_cachedStandardParents modExt = hediff.def.GetModExtension<MSE_cachedStandardParents>();

            return modExt != null && hediff.Part != null && hediff.Part.parent != null
                && hediff.pawn.health.hediffSet.hediffs.Any( h => h.Part == hediff.Part.parent && modExt.Contains( h.def ) );
        }

        public static IEnumerable<RecipeDef> SurgeryToInstall ( ThingDef thing )
        {
            if ( DefDatabase<RecipeDef>.AllDefsListForReading.NullOrEmpty() )
            {
                throw new ApplicationException( "[MSE2] Tried to find SurgeryToInstall before DefDatabase was loaded. ThingDef: " + thing.defName );
            }

            if ( !AutoRecipeUserUtilities.AutoRecipeUsersApplied )
            {
                throw new ApplicationException( "[MSE2] Tried to find SurgeryToInstall before AutoRecipeUsers were applied. ThingDef: " + thing.defName );
            }

            return DefDatabase<RecipeDef>.AllDefs.Where( d => d.IsSurgery && d.IsIngredient( thing ) );
        }

        public static IEnumerable<RecipeDef> SurgeryToInstall ( HediffDef hediffDef )
        {
            if ( DefDatabase<RecipeDef>.AllDefsListForReading.NullOrEmpty() )
            {
                throw new ApplicationException( "[MSE2] Tried to find SurgeryToInstall before DefDatabase was loaded. HediffDef: " + hediffDef.defName );
            }

            return DefDatabase<RecipeDef>.AllDefs.Where( d => d.IsSurgery && d.addsHediff == hediffDef );
        }

        public static bool HasSameStructure ( this BodyPartRecord a, BodyPartRecord b )
        {
            if ( a == b )
            {
                return true;
            }
            else
            {
                return a.def == b.def && EnumerableEqualsOutOfOrder( a.parts, b.parts, HasSameStructure );
            }
        }

        public static bool EnumerableEqualsOutOfOrder<A, B> ( IEnumerable<A> aEnu, IEnumerable<B> bEnu, Func<A, B, bool> equalityComparer )
        {
            if ( aEnu == bEnu )
            {
                return true;
            }
            if ( aEnu.EnumerableNullOrEmpty() && bEnu.EnumerableNullOrEmpty() )
            {
                return true;
            }
            if ( aEnu.EnumerableNullOrEmpty() || bEnu.EnumerableNullOrEmpty() )
            {
                return false;
            }

            foreach ( A a in aEnu )
            {
                foreach ( B b in bEnu )
                {
                    if ( equalityComparer( a, b ) && EnumerableEqualsOutOfOrder( aEnu.ExceptFirst( a ), bEnu.ExceptFirst( b ), equalityComparer ) )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static readonly Dictionary<ThingDef, List<LimbConfiguration>> cachedInstallationDestinations = new Dictionary<ThingDef, List<LimbConfiguration>>();

        public static IReadOnlyList<LimbConfiguration> InstallationDestinations ( ThingDef parentDef )
        {
            if ( cachedInstallationDestinations.TryGetValue( parentDef, out List<LimbConfiguration> val ) )
            {
                return val;
            }
            else
            {
                IEnumerable<LimbConfiguration> allFromSurgeries =
                    (from s in SurgeryToInstall( parentDef )
                     from u in s.AllRecipeUsers
                     where u.race?.body != null
                     from bpd in s.appliedOnFixedBodyParts
                     where u.race.body.AllParts.Any( bpr => bpr.def == bpd )
                     from lc in LimbConfiguration.LimbConfigsMatchingBodyAndPart( u.race.body, bpd )
                     select lc)
                     .Distinct();

                List<LimbConfiguration> newVal = allFromSurgeries.ToList();

                cachedInstallationDestinations.Add( parentDef, newVal );
                return newVal;
            }
        }

        public static IEnumerable<T> ExceptFirst<T> ( this IEnumerable<T> lhs, T rhs )
        {
            bool removed = false;
            foreach ( T t in lhs )
            {
                if ( !removed && t.Equals( rhs ) )
                {
                    removed = true;
                }
                else
                {
                    yield return t;
                }
            }
        }

        public static bool InstallationCompatibility ( IEnumerable<Thing> things, IEnumerable<(ThingDef thingDef, ProsthesisVersion version)> parts )
        {
            foreach ( (ThingDef thingDef, ProsthesisVersion version) part in parts )
            {
                foreach ( Thing thing in things )
                {
                    if ( thing.def == part.thingDef // same def
                        && (thing.TryGetComp<CompIncludedChildParts>()?.CompatibleVersions.Contains( part.version ) ?? // subparts are compatible
                        part.version == null) // has no subparts and is compatible
                        && InstallationCompatibility( things.ExceptFirst( thing ), parts.ExceptFirst( part ) ) ) // all other things check out
                    {
                        return true;
                    }
                }
            }
            return !parts.Any();
        }
    }
}