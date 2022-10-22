using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace MSE2
{
    class Bill_MedicalLimbAware : Bill_Medical
    {

        public Bill_MedicalLimbAware ()
        {
        }

        public Bill_MedicalLimbAware ( RecipeDef recipe, List<Thing> uniqueIngredients)
            : base( recipe, uniqueIngredients )
        {
        }

        private bool allowIncomplete = false;
        public bool AllowIncomplete { get => allowIncomplete; set => allowIncomplete = value; }

        protected override void DoConfigInterface ( Rect rect, Color baseColor )
        {
            base.DoConfigInterface( rect, baseColor );

            Rect newRect = new( rect.xMax - 51f, rect.yMax - 24f, 24f, 24f );

            TooltipHandler.TipRegion( newRect, this.AllowIncomplete ? "Bill_MedicalLimbAware_AllowIncomplete".Translate() : "Bill_MedicalLimbAware_OnlyComplete".Translate() );
            if ( Widgets.ButtonImage( newRect, this.AllowIncomplete ? Assets.WidgetPartial : Assets.WidgetComplete ) )
            {
                this.AllowIncomplete = !this.AllowIncomplete;
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        public override void ExposeData ()
        {
            Scribe_Values.Look( ref this.allowIncomplete, "allowIncomplete" );
            base.ExposeData();
        }

    }
}
