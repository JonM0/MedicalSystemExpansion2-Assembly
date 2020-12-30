using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

using Verse;

namespace MSE2.DebugTools
{
    public static class GlobalProthesisFixAction
    {
        [DebugAction( "Pawns", "Fix all prostheses in the world", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing )]
        private static void Apply ()
        {
            BackCompatibility.GlobalProthesisFix.Apply(true);
        }
    }
}
