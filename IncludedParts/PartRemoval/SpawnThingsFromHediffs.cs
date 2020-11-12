using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.HarmonyPatches
{
    public class MedicalRecipesUtility_SpawnThingsFromHediffs_Patch
    {
        [HarmonyPatch( typeof( MedicalRecipesUtility ) )]
        [HarmonyPatch( nameof( MedicalRecipesUtility.SpawnThingsFromHediffs ) )]
        internal class SpawnThingsFromHediffs
        {
            [HarmonyPrefix]
            [HarmonyPriority( Priority.Last )]
            public static bool ReplaceWithCustom ( Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map )
            {
                // Spawn every thing that can be made from the heddiffs on this part and childparts
                foreach ( Thing item in MakeThingsFromHediffs( pawn, part, pos, map ) )
                {
                    if ( map != null ) GenSpawn.Spawn( item, pos, map );
                }

                return false;
            }
        }

        /// <summary>
        /// Generates a list of all Things that can be dropped from a part and its subparts
        /// </summary>
        /// <param name="pawn">Pawn from which to check hediffs</param>
        /// <param name="part">From where to look for hediffs</param>
        /// <param name="pos">Position where to drop natural subparts</param>
        /// <param name="map">Map where to drop natural subparts</param>
        /// <returns>All Things that hediffs from part and childparts can drop, with subparts inserted into the correct parent</returns>
        public static IEnumerable<Thing> MakeThingsFromHediffs ( Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map )
        {
            // stop if the part is missing
            if ( !pawn.health.hediffSet.GetNotMissingParts().Contains( part ) )
            {
                return Enumerable.Empty<Thing>();
            }

            /// Things that can be made from all subPart hediffs
            List<Thing> subThings = new List<Thing>();

            foreach ( BodyPartRecord subPart in part.GetDirectChildParts() ) // for each subpart
            {
                if ( !MedicalRecipesUtility.IsClean( pawn, part ) ) // If parent is not clean
                {
                    MedicalRecipesUtility.SpawnNaturalPartIfClean( pawn, subPart, pos, map ); // try to make natural parts out of children
                }

                // add each thing coming from the child hediffs
                subThings.AddRange( MakeThingsFromHediffs( pawn, subPart, pos, map ) );
            }

            // for every thing makeable from hediffs on this part: add subparts if possible then return it
            List<Thing> items = new List<Thing>();
            foreach ( ThingDef spawnableFromPart in from h in pawn.health.hediffSet.hediffs
                                                    where h.def.spawnThingOnRemoved != null
                                                    where h.Part == part
                                                    select h.def.spawnThingOnRemoved ) // for every hediff on the part
            {
                Thing item = ThingMaker.MakeThing( spawnableFromPart );

                // compose if possible
                CompIncludedChildParts comp = item.TryGetComp<CompIncludedChildParts>();
                if ( comp != null )
                {
                    comp.TargetLimb = LimbConfiguration.LimbConfigForBodyPartRecord( part );
                    comp.InitializeFromList( subThings );
                }

                items.Add( item );
            }

            // merge siblings
            foreach ( Thing item in items.ToArray() )
            {
                if ( items.Contains( item ) )
                {
                    item.TryGetComp<CompIncludedChildParts>()?.AddMissingFromList( items );
                }
            }

            // return all
            return items.Concat( subThings );
        }
    }
}