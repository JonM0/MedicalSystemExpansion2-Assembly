using System.Collections.Generic;

using Verse;

namespace MSE2
{
    public class HediffComp_ModuleHolder : HediffComp
    {
        public HediffCompProperties_ModuleHolder Props => (HediffCompProperties_ModuleHolder)this.props;

        public override void CompPostPostAdd ( DamageInfo? dinfo )
        {
            base.CompPostPostAdd( dinfo );

            this.currentModules = 0;

            this.moduleSlots = new List<Hediff_ModuleSlot>( this.Props.maxModules );
            for ( int i = 0; i < this.Props.maxModules; i++ )
            {
                this.parent.pawn.health.AddHediff( MSE_HediffDefOf.MSE_ModuleSlot, this.parent.Part );
            }
        }

        public void Notify_ModuleAdded ()
        {
            this.currentModules++;

            if ( this.currentModules > this.Props.maxModules )
            {
                Log.Error( "[MSE2] Added too many modules to part " + this.parent.Label );
            }

            // remove last slot in list
            if ( this.moduleSlots.Count > 0 )
            {
                this.parent.pawn.health.RemoveHediff( this.moduleSlots[this.moduleSlots.Count - 1] );
            }
        }

        public void Notify_ModuleRemoved ()
        {
            this.currentModules--;

            // add a slot (unless parent hediff was just removed)
            if ( this.Pawn.health.hediffSet.hediffs.Contains( this.parent ) && this.currentModules < this.Props.maxModules )
            {
                this.parent.pawn.health.AddHediff( MSE_HediffDefOf.MSE_ModuleSlot, this.parent.Part );
            }
        }

        public override void CompPostPostRemoved ()
        {
            base.CompPostPostRemoved();

            if ( this.moduleSlots != null )
            {
                for ( int i = this.moduleSlots.Count - 1; i >= 0; i-- )
                {
                    this.parent.pawn.health.RemoveHediff( this.moduleSlots[i] );
                }
            }
        }

        protected void PostLoadInit ()
        {
            // remove null slots from the list
            this.moduleSlots.RemoveAll( s => s == null );

            // add extra slots if there arent enough (maybe def was edited)
            int missingSlots = this.Props.maxModules - this.currentModules - this.moduleSlots.Count;
            for ( int i = 0; i < missingSlots; i++ )
            {
                this.parent.pawn.health.AddHediff( MSE_HediffDefOf.MSE_ModuleSlot, this.parent.Part );
            }
        }

        public override void CompExposeData ()
        {
            base.CompExposeData();

            Scribe_Values.Look( ref this.currentModules, "currentModules" );

            Scribe_Collections.Look( ref this.moduleSlots, "moduleSlots", LookMode.Reference );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                this.PostLoadInit();
            }
        }

        public int currentModules;

        public List<Hediff_ModuleSlot> moduleSlots;
    }
}