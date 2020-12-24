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

            this.moduleSlots = new List<Hediff_ModuleSlot>( this.Props.maxModules );
            for ( int i = 0; i < this.Props.maxModules; i++ )
            {
                this.AddSlot();
            }
        }

        public override void CompPostPostRemoved ()
        {
            base.CompPostPostRemoved();

            if ( this.moduleAddeds != null )
            {
                for ( int i = this.moduleAddeds.Count - 1; i >= 0; i-- )
                {
                    this.parent.pawn.health.RemoveHediff( this.moduleAddeds[i] );
                }
                this.moduleAddeds = null;
            }

            if ( this.moduleSlots != null )
            {
                for ( int i = this.moduleSlots.Count - 1; i >= 0; i-- )
                {
                    this.parent.pawn.health.RemoveHediff( this.moduleSlots[i] );
                }
                this.moduleSlots = null;
            }
        }

        public void AddModule ( Hediff_ModuleAdded module, Hediff_ModuleSlot slot )
        {
            this.moduleSlots.Remove( slot );
            this.Pawn.health.RemoveHediff( slot );

            this.moduleAddeds.Add( module );
            module.ModuleHolder = this;

            if ( this.CurrentModules > this.MaxModules )
            {
                Log.Warning( "[MSE2] Added too many modules to part " + this.parent.Label );
            }
        }

        public void Notify_ModuleRemoved ( Hediff_ModuleAdded module )
        {
            this.moduleAddeds.Remove( module );

            // add a slot (unless parent hediff was just removed)
            if ( !this.Pawn.health.hediffSet.PartIsMissing( this.parent.Part ) && this.CurrentModules < this.MaxModules )
            {
                this.AddSlot();
            }
        }

        public void AddSlot ()
        {
            Hediff_ModuleSlot slot = (Hediff_ModuleSlot)this.parent.pawn.health.AddHediff( MSE_HediffDefOf.MSE_ModuleSlot, this.parent.Part );
            slot.ModuleHolder = this;
            this.moduleSlots.Add( slot );
        }

        public override void CompExposeData ()
        {
            base.CompExposeData();

            Scribe_Collections.Look( ref this.moduleSlots, "moduleSlots", LookMode.Reference );
            Scribe_Collections.Look( ref this.moduleAddeds, "moduleAddeds", LookMode.Reference );

            if ( Scribe.mode == LoadSaveMode.ResolvingCrossRefs )
            {
                // error check and remove null modules
                if ( this.moduleAddeds == null )
                {
                    Log.Warning( "[MSE2] " + this + ": null moduleAddeds after loading" );
                    this.moduleAddeds = new List<Hediff_ModuleAdded>();
                }
                else if ( this.moduleAddeds.RemoveAll( m => m == null ) > 0 )
                {
                    Log.Warning( "[MSE2] " + this + ": found null modules after loading" );
                }
                // error check and remove null slots
                if ( this.moduleSlots == null )
                {
                    Log.Warning( "[MSE2] " + this + ": null moduleSlots after loading" );
                    this.moduleSlots = new List<Hediff_ModuleSlot>();
                }
                else if ( this.moduleSlots.RemoveAll( m => m == null ) > 0 )
                {
                    Log.Warning( "[MSE2] " + this + ": found null slots after loading" );
                }

                // link holder
                foreach ( Hediff_ModuleAdded module in this.moduleAddeds )
                {
                    if ( module.ModuleHolder != null ) Log.Warning( "[MSE2] " + this + ": module " + module + " already had a ModuleHolder when loading" );
                    module.ModuleHolder = this;
                }
                foreach ( Hediff_ModuleSlot slot in this.moduleSlots )
                {
                    if ( slot.ModuleHolder != null ) Log.Warning( "[MSE2] " + this + ": slot " + slot + " already had a ModuleHolder when loading" );
                    slot.ModuleHolder = this;
                }
            }

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                // add extra slots if there arent enough (maybe def was edited)
                int missingSlots = this.MaxModules - this.CurrentModules - this.RemainingSlots;
                while ( missingSlots > 0 )
                {
                    this.AddSlot();
                    missingSlots--;
                }
                while ( missingSlots < 0 && this.RemainingSlots > 0 )
                {
                    Hediff_ModuleSlot slotToRemove = this.moduleSlots[this.RemainingSlots - 1];
                    this.Pawn.health.RemoveHediff( slotToRemove );
                    this.moduleSlots.Remove( slotToRemove );
                    missingSlots++;
                }
            }
        }

        public int MaxModules => this.Props.maxModules;
        public int CurrentModules => this.moduleAddeds.Count;
        public int RemainingSlots => this.moduleSlots.Count;

        private List<Hediff_ModuleSlot> moduleSlots = new List<Hediff_ModuleSlot>();
        private List<Hediff_ModuleAdded> moduleAddeds = new List<Hediff_ModuleAdded>();

        public override string ToString ()
        {
            return this.parent + "_ModuleHolder";
        }
    }
}