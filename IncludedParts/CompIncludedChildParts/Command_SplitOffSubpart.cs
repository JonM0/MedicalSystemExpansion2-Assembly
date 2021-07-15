using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Verse;

namespace MSE2
{
    internal class Command_SplitOffSubpart : Command
    {
        private readonly CompIncludedChildParts comp;

        public Command_SplitOffSubpart ( CompIncludedChildParts comp )
        {
            this.comp = comp;

            // use same icon as thing it belongs to
            this.icon = comp.parent.def.uiIcon;
            this.iconAngle = comp.parent.def.uiIconAngle;
            this.defaultIconColor = comp.parent.def.uiIconColor;

            this.defaultLabel = "CommandSplitOffSubpart_Label".Translate();
            this.defaultDesc = "CommandSplitOffSubpart_Description".Translate();
        }

        public override bool Visible => this.comp.AllIncludedParts.Any();

        public override void ProcessInput ( Event ev )
        {
            base.ProcessInput( ev );

            List<FloatMenuOption> list = new List<FloatMenuOption>();

            foreach ( (Thing lthing, CompIncludedChildParts lcomp) in this.comp.AllIncludedParts )
            {
                list.Add( new FloatMenuOption(
                    // name
                    lcomp != this.comp ?  // if added to other subpart specify it
                        "CommandSplitOffSubpart_RemoveFrom".Translate( lthing.Label.CapitalizeFirst(), lcomp.parent.Label ).ToString()
                        : lthing.Label.CapitalizeFirst(),
                    () => // click action
                    {
                        lcomp.RemoveAndSpawnPart( lthing );
                    },
                    // icon
                    lthing.def ) );
            }

            Find.WindowStack.Add( new FloatMenu( list ) );
        }

        protected override void DrawIcon ( Rect rect, Material buttonMat, GizmoRenderParms parms )
        {
            base.DrawIcon( rect, buttonMat, parms );

            // add minus sign in the top right of the gizmo texture
            if ( Assets.WidgetMinusSign != null )
            {
                Rect position = new Rect( rect.x + rect.width - 24f, rect.y, 24f, 24f );
                GUI.DrawTexture( position, Assets.WidgetMinusSign );
            }
            
        }
    }
}