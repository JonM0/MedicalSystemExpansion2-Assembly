using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MSE2
{
    internal static class AutoRecipeUserUtilities
    {
        internal static bool AutoRecipeUsersApplied { get; private set; } = false;

        internal static void ApplyAutoRecipeUsers ()
        {
            try
            {
                List<ThingDef> pawndefs = (from t in DefDatabase<ThingDef>.AllDefs
                                           where t.race != null
                                           select t).ToList();

                if ( pawndefs.NullOrEmpty() || pawndefs.Exists( pawn => pawn.race.body.AllParts.NullOrEmpty() ) )
                {
                    throw new ApplicationException( "Pawn defs and bodies are not loaded" );
                }

                foreach ( (RecipeDef recipedef, AutoRecipeUsers aru) in from d in DefDatabase<RecipeDef>.AllDefs
                                                                        where d.HasModExtension<AutoRecipeUsers>()
                                                                        select (d, d.GetModExtension<AutoRecipeUsers>()) )
                {
                    for ( int i = 0; i < pawndefs.Count; i++ )
                    {
                        ThingDef pawndef = pawndefs[i];
                        if ( !recipedef.recipeUsers.Contains( pawndef ) && // is not already present
                            aru.IsValidRace( pawndef ) && // AutoRecipeUsers allows the race
                            ((recipedef.appliedOnFixedBodyParts.NullOrEmpty() && recipedef.appliedOnFixedBodyPartGroups.NullOrEmpty()) // can be installed on any part
                            || recipedef.appliedOnFixedBodyParts.Exists( part => pawndef.race.body.AllParts.Exists( p => p.def == part ) ) // has a part of a compatible def
                            || recipedef.appliedOnFixedBodyPartGroups.Exists( group => pawndef.race.body.AllParts.Exists( p => p.IsInGroup( group ) ) )) )
                        {
                            recipedef.recipeUsers.Add( pawndef );
                        }
                    }
                }

                AutoRecipeUsersApplied = true;
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception applying AutoRecipeUsers: " + ex );
            }
        }
    }
}