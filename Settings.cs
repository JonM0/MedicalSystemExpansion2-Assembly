using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MSE2
{
    public class Settings : ModSettings
    {
        public void DoWindowContents(Listing_Standard list)
        {
            if (list.ButtonTextLabeled("HediffHideModeSetting_Title".Translate(), HediffHideModeLabel(hediffHideMode), tooltip: "HediffHideModeSetting_Description".Translate()))
            {
                List<FloatMenuOption> hideOptions = new();
                foreach (HediffHideMode option in Enum.GetValues(typeof(HediffHideMode)))
                    hideOptions.Add(new(HediffHideModeLabel(option), () => hediffHideMode = option));
                Find.WindowStack.Add(new FloatMenu(hideOptions));
            }
            list.CheckboxLabeled("HideModuleSlotsSetting_Title".Translate(), ref hideModuleSlots, "HideModuleSlotsSetting_Description".Translate());
            list.CheckboxLabeled("RemoveAllFromSegmentSetting_Title".Translate(), ref removeAllFromSegment, "RemoveAllFromSegmentSetting_Description".Translate());
        }

        private string HediffHideModeLabel(HediffHideMode value)
        {
            return value switch
            {
                HediffHideMode.Always => "HediffHideModeSetting_Always".Translate(),
                HediffHideMode.Never => "HediffHideModeSetting_Never".Translate(),
                HediffHideMode.Clean => "HediffHideModeSetting_Clean".Translate(),
                HediffHideMode.CleanOrModules => "HediffHideModeSetting_CleanOrModules".Translate(),
                _ => value.ToString(),
            };
        }

        public enum HediffHideMode { Always, Never, Clean, CleanOrModules }

        public HediffHideMode hediffHideMode;
        public bool removeAllFromSegment;
        public bool hideModuleSlots;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hediffHideMode, "hediffHideMode", HediffHideMode.Clean);
            Scribe_Values.Look(ref removeAllFromSegment, "removeAllFromSegment", false);
            Scribe_Values.Look(ref hideModuleSlots, "hideModuleSlots", false);
        }
    }
}
