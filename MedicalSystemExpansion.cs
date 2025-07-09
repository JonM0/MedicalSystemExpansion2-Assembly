using System;
using System.Diagnostics;
using System.Reflection;

using HarmonyLib;

using Multiplayer.API;

using UnityEngine;

using Verse;

namespace MSE2
{
    public class MedicalSystemExpansion : Mod
    {

        public MedicalSystemExpansion(ModContentPack content) : base(content)
        {
            Instance = this;
            this.settings = GetSettings<Settings>();
            this.harmony = new Harmony("MSE2.Harmony");

            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (MP.enabled) MP.RegisterAll();

            LongEventHandler.QueueLongEvent(this.Initialize, "MSE2_LongEvent_Initialize", false, null);
        }

        private void Initialize()
        {
            AutoRecipeUserUtilities.ApplyAutoRecipeUsers();
            IncludedPartsUtilities.CacheAllStandardParents();
            IgnoreSubPartsUtilities.IgnoreAllNonCompedSubparts();
            IgnoreSubPartsUtilities.IgnoreUnsupportedSubparts();
            LimbRecipeDefGenerator.AddExtraRecipesToDefDatabase();
            IncludedPartsUtilities.PrintIncompatibleVersionsReport();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);
            settings.DoWindowContents(list);
            list.End();
        }

        public override string SettingsCategory() => "MSE2";

        public static MedicalSystemExpansion Instance { get; private set; }

        private readonly Settings settings;
        private readonly Harmony harmony;

        // settings
        public Settings.HediffHideMode HediffHideModeSetting => settings.hediffHideMode;

        public bool RemoveAllFromSegmentSetting => settings.removeAllFromSegment;

        public bool HideModuleSlotsSetting => settings.hideModuleSlots;

    }
}