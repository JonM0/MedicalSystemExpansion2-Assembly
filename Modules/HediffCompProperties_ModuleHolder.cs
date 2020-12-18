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
            this.lazyCompatibleModulesStat = new Lazy<StatDrawEntry>( CompatibleModulesStatFactory );
        }

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
            yield return lazyCompatibleModulesStat.Value;
        }

        private readonly Lazy<StatDrawEntry> lazyCompatibleModulesStat;

        private StatDrawEntry CompatibleModulesStatFactory ()
        {
            var destinations = IncludedPartsUtilities.SurgeryToInstall( this.parentDef ).SelectMany( s => s.appliedOnFixedBodyParts ).ToList();

            var compat =
                (from r in DefDatabase<RecipeDef>.AllDefs
                 where typeof( Recipe_InstallModule ).IsAssignableFrom( r.workerClass )
                     && (r.appliedOnFixedBodyParts.Count == 0 || r.appliedOnFixedBodyParts.Exists( destinations.Contains ))
                     && r.addsHediff?.spawnThingOnRemoved != null
                 select r.addsHediff.spawnThingOnRemoved).ToList();

            compat.RemoveDuplicates();

            return new StatDrawEntry(
                StatCategoryDefOf.Basics,
                "ModuleHolder_Compatible_Label".Translate(),
                compat.Count.ToString(),
                "ModuleHolder_Compatible_ReportText".Translate(),
                4060,
                null,
                compat.Select( m => new Dialog_InfoCard.Hyperlink( m ) ) );
        }

        public int maxModules = 1;
    }
}