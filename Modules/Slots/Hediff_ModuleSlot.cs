using System.Runtime.CompilerServices;
using Verse;

namespace MSE2
{
    public class Hediff_ModuleSlot : Hediff_ModuleAbstract
    {
        public override void PostAdd ( DamageInfo? dinfo )
        {
            base.PostAdd( dinfo );

            this.ModuleHolder.moduleSlots.Add( this );
        }

        public override void PostRemoved ()
        {
            base.PostRemoved();

            this.ModuleHolder.moduleSlots.Remove( this );
        }

        public override void ExposeData ()
        {
            base.ExposeData();

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                // remove if not in the holder's list
                if ( !this.ModuleHolder.moduleSlots.Contains( this ) )
                {
                    this.pawn.health.RemoveHediff( this );
                }
            }
        }
    }
}