using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( HealthCardUtility ) )]
    [HarmonyPatch( nameof( HealthCardUtility.GetPawnCapacityTip ) )]
    internal static class SortAndGroupPawnCapacityTip
    {
        private static readonly StringBuilder internalStringBuilder = new StringBuilder();

        [HarmonyPostfix]
        [HarmonyPriority( Priority.VeryLow )]
        private static void SortAndGroup ( ref string __result )
        {
            string[] lines = __result.Split( '\n' );

            if ( lines.Length > 5 ) // has more than 1 impactor (+3 starting lines and +1 empty line at the end)
            {
                // copy starting 3 lines
                StringBuilder newResult = internalStringBuilder.Clear()
                    .AppendLine( lines[0] )
                    .AppendLine( lines[1] )
                    .AppendLine( lines[2] );

                Array.Sort( lines, 3, lines.Length - 4 ); // sort impactors

                string prev = lines[3];
                int count = 1;

                // loop from second impactor to last impactor
                for ( int i = 4; i < lines.Length - 1; i++ )
                {
                    string line = lines[i];

                    if ( prev != line )
                    {
                        newResult.Append( prev );
                        if ( count > 1 )
                        {
                            newResult.Append( " (x" ).Append( count ).Append( ')' );
                        }
                        newResult.AppendLine();

                        count = 1;
                    }
                    else
                    {
                        count++;
                    }
                    prev = line;
                }

                newResult.Append( prev );
                if ( count > 1 )
                {
                    newResult.Append( " (x" ).Append( count ).Append( ')' );
                }
                newResult.AppendLine();

                __result = newResult.ToString();
            }
        }
    }
}