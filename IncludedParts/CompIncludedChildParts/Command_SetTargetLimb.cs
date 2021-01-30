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
                this.defaultIconColor = comp.parent.def.uiIconColor;

                this.defaultLabel = "Command_SetTargetLimb_Label".Translate();
                this.defaultDesc = "Command_SetTargetLimb_Description".Translate();
            }

            public override void ProcessInput ( Event ev )
            {
                base.ProcessInput( ev );
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                foreach ( ProsthesisVersion possibleTarget in this.comp.Props.SupportedVersions.Except( this.comp.TargetVersion ) )
                {
                    options.Add( new FloatMenuOption(
                        possibleTarget.Label,
                        () => // click action
                        {
                            this.comp.TargetVersion = possibleTarget;
                        }
                        ) );
                }

                Find.WindowStack.Add( new FloatMenu( options ) );
            }
        }
    }
}