using System.Collections.Generic;
using Verse;

namespace MSE2
{
    public class HediffComp_ModuleHolder : HediffComp
    {
        public HediffCompProperties_ModuleHolder Props
        {
            get
            {
                return (HediffCompProperties_ModuleHolder)this.props;
            }
        }

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
            currentModules++;

            if ( currentModules > this.Props.maxModules )
            {
                Log.Error( "[MSE2] Added too many modules to part " + this.parent.Label );
            }

            // remove last slot in list
            if ( moduleSlots.Count > 0 )
            {
                parent.pawn.health.RemoveHediff( moduleSlots[moduleSlots.Count - 1] );
            }
        }

        public void Notify_ModuleRemoved ()
        {
            currentModules--;

            // add a slot
            if ( currentModules < this.Props.maxModules )
            {
                this.parent.pawn.health.AddHediff( MSE_HediffDefOf.MSE_ModuleSlot, this.parent.Part );
            }
        }

        public override void CompPostPostRemoved ()
        {
            base.CompPostPostRemoved();

            if ( moduleSlots != null )
            {
                for ( int i = moduleSlots.Count - 1; i >= 0; i-- )
                {
                    parent.pawn.health.RemoveHediff( moduleSlots[i] );
                }
            }
        }

        protected void PostLoadInit()
        {
            // remove null slots from the list
            this.moduleSlots.RemoveAll( s => s == null );

            // add extra slots if there arent enough (maybe def was edited)
            int missingSlots = this.Props.maxModules - currentModules - moduleSlots.Count;
            for ( int i = 0; i < missingSlots; i++ )
            {
                this.parent.pawn.health.AddHediff( MSE_HediffDefOf.MSE_ModuleSlot, this.parent.Part );
            }
        }

        public override void CompExposeData ()
        {
            base.CompExposeData();

            Scribe_Values.Look( ref currentModules, "currentModules" );

            Scribe_Collections.Look( ref moduleSlots, "moduleSlots", LookMode.Reference );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                this.PostLoadInit();
            }
        }

        public int currentModules;

        public List<Hediff_ModuleSlot> moduleSlots;
    }
}