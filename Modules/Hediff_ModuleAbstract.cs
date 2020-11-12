using System.Linq;

using Verse;
using Verse.AI;

namespace MSE2
{
    public abstract class Hediff_ModuleAbstract : Hediff_Implant
    {
        public override void PostAdd ( DamageInfo? dinfo )
        {
            base.PostAdd( dinfo );

            // find a module holder comp in the same bodypart
            this.moduleHolderComp =
                (from c in this.pawn.health.hediffSet.GetAllComps()
                 where c is HediffComp_ModuleHolder
                 && c.parent.Part == this.Part
                 let mh = (HediffComp_ModuleHolder)c
                 where mh.currentModules < mh.Props.maxModules
                 select mh)
                .First();

            this.moduleHolderDiff = this.moduleHolderComp.parent; // store hediff as you can't save the comp
        }

        public HediffComp_ModuleHolder ModuleHolder
        {
            get
            {
                // try to cache it
                if ( this.moduleHolderComp == null )
                {
                    this.moduleHolderComp = this.moduleHolderDiff?.TryGetComp<HediffComp_ModuleHolder>();
                    // if still null call error
                    if ( this.moduleHolderComp == null )
                    {
                        Log.Error( "[MSE] Null ModuleHolder for module " + this.Label );
                    }
                }

                return this.moduleHolderComp;
            }
        }

        public override void ExposeData ()
        {
            base.ExposeData();

            Scribe_References.Look( ref this.moduleHolderDiff, "moduleHolder" ); // need to save the diff as the comp is not referenceable

            if ( Scribe.mode == LoadSaveMode.PostLoadInit && this.moduleHolderDiff == null )
            {
                Log.Error( "[MSE2] " + this.Label + " on " + this.pawn.Name + " has null holder after loading, removing.", false );
                this.pawn.health.hediffSet.hediffs.Remove( this );
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

        private HediffWithComps moduleHolderDiff = null;
        private HediffComp_ModuleHolder moduleHolderComp = null;
    }
}