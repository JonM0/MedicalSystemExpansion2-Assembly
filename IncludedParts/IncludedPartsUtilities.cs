using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
            foreach ( var def in DefDatabase<HediffDef>.AllDefs )
            {
                def.CacheParentOfChildren();
            }
        }

        private static void CacheParentOfChildren ( this HediffDef parent )
        {
            var comp = parent.spawnThingOnRemoved?.GetCompProperties<CompProperties_IncludedChildParts>(); // comp on the corresponding ThingDef
            if ( comp != null && !comp.standardChildren.NullOrEmpty() ) // if it has standard children
            {
                foreach ( var def in from d in DefDatabase<HediffDef>.AllDefs
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
            var modExt = hediff.def.GetModExtension<MSE_cachedStandardParents>();

            return modExt != null && hediff.Part != null && hediff.Part.parent != null
                && hediff.pawn.health.hediffSet.hediffs.Any( h => h.Part == hediff.Part.parent && modExt.Contains( h.def ) );
        }

        public static IEnumerable<RecipeDef> SurgeryToInstall ( ThingDef thing )
        {
            return DefDatabase<RecipeDef>.AllDefs.Where( d => d.IsSurgery && d.IsIngredient( thing ) );
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
            where A : class
            where B : class
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

        public static IReadOnlyList<LimbConfiguration> CachedInstallationDestinations ( ThingDef parentDef )
        {
            if ( cachedInstallationDestinations.TryGetValue( parentDef, out List<LimbConfiguration> val ) )
            {
                return val;
            }
            else
            {
                List<LimbConfiguration> newVal =
                    (from s in SurgeryToInstall( parentDef )
                     from u in s.AllRecipeUsers
                     let b = u.race.body
                     from bpd in s.appliedOnFixedBodyParts
                     where b.AllParts.Any( bpr => bpr.def == bpd )
                     from lc in LimbConfiguration.LimbConfigsMatchingBodyAndPart( b, bpd )
                     select lc)
                     .Distinct()
                     .ToList();

                cachedInstallationDestinations.Add( parentDef, newVal );
                return newVal;
            }
        }

        public static IEnumerable<T> ExceptFirst<T> ( this IEnumerable<T> lhs, T rhs ) where T : class
        {
            bool removed = false;
            foreach ( T t in lhs )
            {
                if ( !removed && t == rhs )
                {
                    removed = true;
                }
                else
                {
                    yield return t;
                }
            }
        }

        public static bool InstallationCompatibility ( IEnumerable<Thing> things, IEnumerable<LimbConfiguration> limbs )
        {
            //Log.Message( "compat: " + thingDefs.Count() + " " + bodyPartRecords.Count() );

            foreach ( var limb in limbs )
            {
                foreach ( var thing in things )
                {
                    if ( (thing.TryGetComp<CompIncludedChildParts>()?.CompatibleLimbs.Contains( limb ) ?? // subparts are compatible
                        CachedInstallationDestinations( thing.def ).Contains( limb )) // has no subparts and is compatible
                        && InstallationCompatibility( things.ExceptFirst( thing ), limbs.ExceptFirst( limb ) ) ) // all other things check out
                    {
                        return true;
                    }
                }
            }
            return !limbs.Any();
        }

        //public static bool EverInstallableOn ( ThingDef thingDef, BodyPartRecord bodyPartRecord )
        //{
        //    return CachedInstallationDestinations( thingDef ).Any( bd_bpd => bd_bpd.Item1 == bodyPartRecord.body && bd_bpd.Item2 == bodyPartRecord.def );
        //}
    }
}