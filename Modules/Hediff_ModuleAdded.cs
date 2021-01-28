using Verse;

namespace MSE2
{
    public class Hediff_ModuleAdded : Hediff_ModuleAbstract
    {
        public override void PostAdd ( DamageInfo? dinfo )
        {
            base.PostAdd( dinfo );

            Hediff_ModuleSlot slot = (Hediff_ModuleSlot)this.pawn.health.hediffSet.hediffs.Find( h => h.Part == this.Part && h is Hediff_ModuleSlot );

            if ( slot != null )
            {
                slot.InstallModule( this );
            }
            else
            {
                Log.Error( "[MSE2] " + this + " could not find a slot to install into" );
            }
        }

        public override void PostRemoved ()
        {
            this.ModuleHolder?.Notify_ModuleRemoved( this );
            base.PostRemoved();
        }

        public override void ExposeData ()
        {
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                if ( this.ModuleHolder == null )
                {
                    Hediff_ModuleSlot slot = (Hediff_ModuleSlot)this.pawn.health.hediffSet.hediffs.Find( h => h.Part == this.Part && h is Hediff_ModuleSlot );
                    slot?.InstallModule( this );
                }
            }

            base.ExposeData();
        }

        public override string ToString ()
        {
            return "ModuleAdded_" + this.def.defName + "_" + this.Part;
        }        
    }
}