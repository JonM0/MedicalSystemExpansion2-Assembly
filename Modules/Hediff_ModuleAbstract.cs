using System.Linq;

using Verse;
using Verse.AI;

namespace MSE2
{
    public abstract class Hediff_ModuleAbstract : Hediff_Implant
    {
        public HediffComp_ModuleHolder ModuleHolder
        {
            get; set;
        }

        public override void PostRemoved ()
        {
            base.PostRemoved();
            this.ModuleHolder = null;
        }

        public override void ExposeData ()
        {
            base.ExposeData();

            if ( Scribe.mode == LoadSaveMode.PostLoadInit && this.ModuleHolder == null )
            {
                Log.Error( "[MSE2] " + this.Label + " on " + this.pawn.Name + " has null holder after loading, removing." );
                this.pawn.health.RemoveHediff( this );
                if ( this.def.spawnThingOnRemoved != null && this.pawn?.Map != null && this.pawn.IsColonistPlayerControlled )
                {
                    GenPlace.TryPlaceThing( ThingMaker.MakeThing( this.def.spawnThingOnRemoved ), this.pawn.Position, this.pawn.Map, ThingPlaceMode.Near );
                }
                return;
            }
        }

        public override bool TryMergeWith ( Hediff other )
        {
            return false;
        }


    }
}