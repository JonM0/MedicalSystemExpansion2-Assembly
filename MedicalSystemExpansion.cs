using System;

using HugsLib;
using HugsLib.Settings;

using Verse;

namespace MSE2
{
    public class MedicalSystemExpansion : ModBase
    {
        public override void Initialize ()
        {
            base.Initialize();
            Instance = this;
        }

        public override void DefsLoaded ()
        {
#if DEBUG
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            try
            {
                base.DefsLoaded();

                AutoRecipeUserUtilities.ApplyAutoRecipeUsers();

                IncludedPartsUtilities.CacheAllStandardParents();

                IgnoreSubPartsUtilities.IgnoreAllNonCompedSubparts();

                IgnoreSubPartsUtilities.IgnoreUnsupportedSubparts();

                LimbRecipeDefGenerator.AddExtraRecipesToDefDatabase();

                IncludedPartsUtilities.PrintIncompatibleVersionsReport();

                this.SetupSettingHandles();
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception caught running DefsLoaded(): " + ex );
            }
#if DEBUG
            finally
            {
                stopwatch.Stop();
                Log.Message( "[MSE2] DefsLoaded completed in " + stopwatch.Elapsed );
            }
#endif
        }




        public override string ModIdentifier => "MSE2";

        public static MedicalSystemExpansion Instance { get; private set; }


        // settings

        private void SetupSettingHandles ()
        {
            this.hediffHideModeSetting = Settings.GetHandle( "hediffHideMode",
                "HediffHideModeSetting_Title".Translate(),
                "HediffHideModeSetting_Description".Translate(),
                HediffHideMode.Clean, null,
                "HediffHideModeSetting_" );

            this.removeAllFromSegmentSetting = Settings.GetHandle( "removeAllFromSegment",
                "RemoveAllFromSegmentSetting_Title".Translate(),
                "RemoveAllFromSegmentSetting_Description".Translate(),
                false );
        }

        public enum HediffHideMode { Always, Never, Clean }
        private SettingHandle<HediffHideMode> hediffHideModeSetting;
        public HediffHideMode HediffHideModeSetting => this.hediffHideModeSetting;


        private SettingHandle<bool> removeAllFromSegmentSetting;
        public bool RemoveAllFromSegmentSetting { get => this.removeAllFromSegmentSetting; }



    }
}