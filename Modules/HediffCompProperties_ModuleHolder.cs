using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;

using Verse;

namespace MSE2
{
    public class HediffCompProperties_ModuleHolder : HediffCompProperties, IHediffCompPropsWithStats
    {
        public HediffCompProperties_ModuleHolder ()
        {
            this.compClass = typeof( HediffComp_ModuleHolder );
        }

        [Unsaved]
        private HediffDef parentDef;

        public override IEnumerable<string> ConfigErrors ( HediffDef parentDef )
        {
            this.parentDef = parentDef;

            foreach ( string ce in base.ConfigErrors( parentDef ) ) yield return ce;

            if ( this.maxModules <= 0 )
            {
                yield return "[MSE2] Part has negative or no module slots";
            }

            yield break;
        }

        public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats ( StatRequest req )
        {
            yield return this.cachedModuleSlotsStat ??= this.ModuleSlotsStatFactory();
        }

        [Unsaved]
        private StatDrawEntry cachedModuleSlotsStat;

        private StatDrawEntry ModuleSlotsStatFactory ()
        {
            List<BodyPartDef> destinations = IncludedPartsUtilities.SurgeryToInstall( this.parentDef ).SelectMany( s => s.appliedOnFixedBodyParts ).ToList();

            List<ThingDef> compatibleModules =
                (from r in DefDatabase<RecipeDef>.AllDefs
                 where typeof( Recipe_InstallModule ).IsAssignableFrom( r.workerClass )
                     && (r.appliedOnFixedBodyParts.Count == 0 || r.appliedOnFixedBodyParts.Exists( destinations.Contains ))
                     && r.addsHediff?.spawnThingOnRemoved != null
                 select r.addsHediff.spawnThingOnRemoved).ToList();

            compatibleModules.RemoveDuplicates();

            return new StatDrawEntry(
                StatCategoryDefOf.Basics,
                "ModuleHolder_Compatible_Label".Translate(),
                this.maxModules.ToString(),
                "ModuleHolder_Compatible_ReportText".Translate(),
                4060,
                null,
                compatibleModules.Select( m => new Dialog_InfoCard.Hyperlink( m ) ) );
        }

        public int maxModules = 1;
    }
}