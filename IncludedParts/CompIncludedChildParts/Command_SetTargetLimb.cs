using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Verse;

namespace MSE2
{
    public partial class CompIncludedChildParts
    {
        private class Command_SetTargetLimb : Command
        {
            private readonly CompIncludedChildParts comp;

            public Command_SetTargetLimb ( CompIncludedChildParts comp )
            {
                this.comp = comp;

                // use same icon as thing it belongs to
                this.icon = comp.parent.def.uiIcon;
                this.iconAngle = comp.parent.def.uiIconAngle;

                this.defaultLabel = "Command_SetTargetLimb_Label".Translate();
                this.defaultDesc = "Command_SetTargetLimb_Description".Translate();
            }

            public override void ProcessInput ( Event ev )
            {
                base.ProcessInput( ev );
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                foreach ( LimbConfiguration possibleTarget in this.comp.Props.InstallationDestinations.Where( t => t != this.comp.TargetLimb ) )
                {
                    options.Add( new FloatMenuOption(
                        this.comp.Props.LimbLabeller.GetComparisonForLimb( possibleTarget ).CapitalizeFirst(),
                        () => // click action
                        {
                            this.comp.TargetLimb = possibleTarget;
                        }
                        ) );
                }

                // Option to set to no target
                if ( this.comp.targetLimb != null )
                    options.Add( new FloatMenuOption(
                        "Command_SetTargetLimb_NoTarget".Translate(),
                        () => // click action
                        {
                            this.comp.TargetLimb = null;
                        }
                        ) );

                Find.WindowStack.Add( new FloatMenu( options ) );
            }
        }
    }
}