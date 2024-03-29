﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

using Verse;

namespace MSE2.DebugTools
{
    public static class ApplySurgery
    {
        [DebugAction( "Pawns", "Apply surgery...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap )]
        private static void Apply ()
        {
            Find.WindowStack.Add( new Dialog_DebugOptionListLister( Options_ApplySurgery() ) );
        }

        private static List<DebugMenuOption> Options_ApplySurgery ()
        {
            List<DebugMenuOption> list = new();
            foreach ( RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs.Where( r => r.IsSurgery ) )
            {
                list.Add( new DebugMenuOption( recipe.LabelCap, DebugMenuOptionMode.Tool, delegate ()
                {
                    Pawn pawn = Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Pawn>().FirstOrDefault<Pawn>();
                    if ( pawn != null )
                    {
                        var options = Options_ApplySurgery_BodyParts( pawn, recipe );
                        if ( options.Count > 0 )
                        {
                            Find.WindowStack.Add( new Dialog_DebugOptionListLister( options ) );
                        }
                        else
                        {
                            Messages.Message( new Message( "No destination available on selected pawn", MessageTypeDefOf.RejectInput ), false );
                        }
                    }
                } ) );
            }
            return list;
        }

        private static List<DebugMenuOption> Options_ApplySurgery_BodyParts ( Pawn pawn, RecipeDef recipe )
        {
            if ( pawn == null )
            {
                throw new ArgumentNullException( nameof( pawn ) );
            }
            List<DebugMenuOption> list = new();

            if ( recipe.AllRecipeUsers.Select( ru => ru.race.body ).Contains( pawn.RaceProps.body ) )
            {
                foreach ( BodyPartRecord bpr in recipe.Worker.GetPartsToApplyOn( pawn, recipe ) )
                {
                    list.Add( new DebugMenuOption( bpr.Label, DebugMenuOptionMode.Action, delegate ()
                    {
                        recipe.Worker.ApplyOnPawn( pawn, bpr, null, null, null );
                    } ) );
                }
            }

            return list;
        }
    }
}