using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace MSE2
{
    public partial class CompIncludedChildParts : ThingComp
    {
        private CompIncludedChildParts holderComp;
        private CompIncludedChildParts TopHolderComp => holderComp == null ? this : this.holderComp.TopHolderComp;
        private Map Map => this.TopHolderComp.parent.Map;
        private IntVec3 Position => this.TopHolderComp.parent.Position;

        public override void Initialize ( CompProperties props )
        {
            base.Initialize( props );

            // Create the needed command gizmos
            this.command_SetTargetLimb = new Command_SetTargetLimb( this );
            this.command_AddExistingSubpart = new Command_AddExistingSubpart( this );
            this.command_SplitOffSubpart = new Command_SplitOffSubpart( this );
        }

        public CompProperties_IncludedChildParts Props
        {
            get
            {
                return this.props as CompProperties_IncludedChildParts;
            }
        }

        // Save / Load

        private void PostLoadInitialization ()
        {
            // init the list if it comes up null (loading a map created without MSE2)
            if ( this.IncludedParts == null )
            {
                this.IncludedParts = new List<Thing>();
                Log.Warning( "[MSE2] Included parts was null during loading." );
            }

            // set the holder comp of the included parts
            for ( int i = 0; i < this.IncludedParts.Count; i++ )
            {
                var comp = this.IncludedParts[i].TryGetComp<CompIncludedChildParts>();
                if ( comp != null )
                {
                    comp.holderComp = this;
                }
            }
        }

        public override void PostExposeData ()
        {
            base.PostExposeData();

            // Deep save the included Things
            Scribe_Collections.Look( ref this.childPartsIncluded, "childPartsIncluded", LookMode.Deep );
            Scribe_LimbConfiguration.Look( ref this.targetLimb, "targetLimb" );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit ) this.PostLoadInitialization();
        }

        /// <summary>
        /// Resets the cache for MissingParts, MissingValue and strings
        /// </summary>
        private void DirtyCache ()
        {
            this.cachedCompatibleLimbs.valid = false;
            this.cachedMissingParts.valid = false;
            this.cachedTransformLabelString = null;
            this.cachedInspectString = null;

            this.holderComp?.DirtyCache();
        }

        // gizmos for merging and splitting

        private Command_SetTargetLimb command_SetTargetLimb;
        private Command_AddExistingSubpart command_AddExistingSubpart;
        private Command_SplitOffSubpart command_SplitOffSubpart;

        public override IEnumerable<Gizmo> CompGetGizmosExtra ()
        {
            yield return this.command_SetTargetLimb;
            yield return this.command_AddExistingSubpart;
            yield return this.command_SplitOffSubpart;

            foreach ( var g in base.CompGetGizmosExtra() ) yield return g;

            yield break;
        }

        public override bool AllowStackWith ( Thing other )
        {
            return false;
        }

        #region PartHandling

        private readonly List<Thing> tmpThingList = new List<Thing>();

        #region Included parts

        private List<Thing> childPartsIncluded = new List<Thing>();

        public List<Thing> IncludedParts
        {
            get => this.childPartsIncluded;
            set
            {
                this.childPartsIncluded.Clear();
                if ( value != null ) this.childPartsIncluded.AddRange( value );
                this.DirtyCache();
            }
        }

        public IEnumerable<CompIncludedChildParts> IncludedPartComps =>
            this.IncludedParts.Select( p => p.TryGetComp<CompIncludedChildParts>() ).Where( c => c != null );

        private IEnumerable<(Thing, LimbConfiguration)> IncludedPartsLimbs
        {
            get
            {
                tmpThingList.Clear();
                tmpThingList.AddRange( this.IncludedParts );

                foreach ( (ThingDef def, LimbConfiguration limb) in this.Props.StandardPartsForLimb( this.TargetLimb ) )
                {
                    var thing = tmpThingList.Find( t => t.def == def && (t.TryGetComp<CompIncludedChildParts>() == null || t.TryGetComp<CompIncludedChildParts>().TargetLimb == limb) );

                    if ( thing != null )
                    {
                        tmpThingList.Remove( thing );
                        yield return (thing, limb);
                    }
                }
            }
        }

        public void AddPart ( Thing part )
        {
            if ( !this.StandardParts.Contains( part.def ) && !this.Props.alwaysInclude.Contains( part.def ) )
            {
                Log.Error( part.Label + " is not a valid subpart for " + this.parent.Label );
                return;
            }

            CompIncludedChildParts partComp = part.TryGetComp<CompIncludedChildParts>();

            // prioritize matches of both def and target part
            (ThingDef, LimbConfiguration) target = this.MissingParts.Find( p => p.Item1 == part.def && (partComp == null || partComp.TargetLimb == p.Item2) );
            // fallback to just matching the def
            if ( target.Item1 == null ) target = this.MissingParts.Find( p => p.Item1 == part.def );

            // found a match (part is actually missing)
            if ( target.Item1 != null )
            {
                this.childPartsIncluded.Add( part );
                this.DirtyCache();

                if ( partComp != null )
                {
                    partComp.TargetLimb = target.Item2;
                    partComp.holderComp = this;
                }

                if ( part.Spawned )
                {
                    part.DeSpawn();
                }
            }
        }

        public void RemoveAndSpawnPart ( Thing part, IntVec3 position, Map map )
        {
            if ( !this.IncludedParts.Contains( part ) )
            {
                Log.Error( "[MSE2] Tried to remove " + part.Label + " from " + this.parent.Label + " while it wasn't actually included." );
                return;
            }

            this.childPartsIncluded.Remove( part );
            this.DirtyCache();

            CompIncludedChildParts partComp = part.TryGetComp<CompIncludedChildParts>();
            if ( partComp != null )
            {
                partComp.holderComp = null;
            }

            GenPlace.TryPlaceThing( part, position, map, ThingPlaceMode.Near );
        }

        public void RemoveAndSpawnPart ( Thing part )
        {
            this.RemoveAndSpawnPart( part, this.Position, this.Map );
        }

        #endregion Included parts

        #region Target Limb

        private LimbConfiguration targetLimb;

        public LimbConfiguration TargetLimb
        {
            get
            {
                return targetLimb;
            }
            set
            {
                if ( value != null && !this.Props.EverInstallableOn( value ) )
                {
                    Log.Error( string.Format( "Tried to set invalid target limb ({0}) on {1}", value.UniqueName, this.parent.Label ) );
                }

                this.targetLimb = value;
                this.UpdateTargetLimbOrRemoveIncludedParts();

                this.DirtyCache();
            }
        }

        public string TargetLimbLabel => this.Props.LabelComparisonForLimb( this.TargetLimb );

        private void UpdateTargetLimbOrRemoveIncludedParts ()
        {
            if ( this.TargetLimb == null )
            {
                foreach ( var comp in this.IncludedPartComps )
                {
                    comp.TargetLimb = null;
                }
            }
            else
            {
                tmpThingList.Clear();
                tmpThingList.AddRange( this.IncludedParts );

                // update compatible parts
                foreach ( (ThingDef thingDef, LimbConfiguration limb) in this.StandardPartsForTargetLimb() )
                {
                    Thing candidate = tmpThingList.Find( t => t.def == thingDef );
                    if ( candidate != null )
                    {
                        tmpThingList.Remove( candidate );

                        CompIncludedChildParts potentialComp = candidate.TryGetComp<CompIncludedChildParts>();

                        if ( potentialComp != null )
                        {
                            potentialComp.TargetLimb = limb;
                        }
                        else
                        {
                            // this will either never happen, or can be checked in standardpartsforlimb
                            if ( limb != null && !limb.ChildLimbs.EnumerableNullOrEmpty() )
                            {
                                Log.Error( string.Format( "[MSE2] Included thing {0} has no CompIncludedChildParts, but was assigned {1}, which has {2} childlimbs.", candidate.def.defName, limb.UniqueName, limb.ChildLimbs.Count() ) );
                            }
                        }
                    }
                }

                // remove the others
                foreach ( var thing in tmpThingList )
                {
                    this.RemoveAndSpawnPart( thing );
                }
            }
        }

        public IEnumerable<(ThingDef, LimbConfiguration)> StandardPartsForTargetLimb ()
        {
            return this.Props.StandardPartsForLimb( this.targetLimb );
        }

        #endregion Target Limb

        #region Standard parts

        public List<ThingDef> StandardParts
        {
            get => this.Props?.standardChildren;
        }

        #endregion Standard parts

        #region Missing Parts

        private (List<(ThingDef, LimbConfiguration)> list, bool valid) cachedMissingParts = (new List<(ThingDef, LimbConfiguration)>(), false);

        public List<(ThingDef, LimbConfiguration)> MissingParts
        {
            get
            {
                if ( !cachedMissingParts.valid )
                {
                    cachedMissingParts.list.Clear();

                    if ( this.TargetLimb != null )
                    {
                        cachedMissingParts.list.AddRange( this.Props.StandardPartsForLimb( this.TargetLimb ) );

                        foreach ( var thing in this.IncludedParts )
                        {
                            var thingComp = thing.TryGetComp<CompIncludedChildParts>();

                            cachedMissingParts.list.Remove( cachedMissingParts.list.Find( c =>
                                 thing.def == c.Item1
                                 && (thingComp == null || thingComp.TargetLimb == c.Item2) ) );
                        }
                    }
                    cachedMissingParts.valid = true;
                }

                return cachedMissingParts.list;
            }
        }

        public bool IsComplete => this.MissingParts.Count == 0 && this.IncludedPartComps.All( c => c.IsComplete );

        public bool AllAlwaysIncludedPartsPresent => this.Props.alwaysInclude?.TrueForAll( p => this.IncludedParts.Find( t => t.def == p ) != null ) ?? true;

        #endregion Missing Parts

        #region Compatible limbs

        private (List<LimbConfiguration> list, bool valid) cachedCompatibleLimbs = (new List<LimbConfiguration>(), false);

        public List<LimbConfiguration> CompatibleLimbs
        {
            get
            {
                if ( !this.cachedCompatibleLimbs.valid )
                {
                    this.cachedCompatibleLimbs.list.Clear();
                    this.cachedCompatibleLimbs.list.AddRange( from lc in this.Props.InstallationDestinations
                                                              where IncludedPartsUtilities.InstallationCompatibility(
                                                                  this.childPartsIncluded,
                                                                  this.Props.IgnoredSubparts == null ?
                                                                  lc.ChildLimbs :
                                                                  lc.ChildLimbs.Where( l => !this.Props.IgnoredSubparts.Contains( l.PartDef ) ) )
                                                              select lc );
                    this.cachedCompatibleLimbs.valid = true;
                }
                return this.cachedCompatibleLimbs.list;
            }
        }

        #endregion Compatible limbs

        #region Creation / Deletion

        public void InitializeFromList ( List<Thing> available )
        {
            this.childPartsIncluded.Clear();
            this.DirtyCache();

            this.AddMissingFromList( available );
        }

        public void AddMissingFromList ( List<Thing> available )
        {
            this.tmpThingList.Clear();
            this.tmpThingList.AddRange( available );

            // first add things that match both def and target
            foreach ( var availableThing in available.ToArray() )
            {
                if ( this.MissingParts.Any( mp => mp.Item1 == availableThing.def && mp.Item2 == availableThing.TryGetComp<CompIncludedChildParts>()?.TargetLimb ) )
                {
                    this.AddPart( availableThing );
                    available.Remove( availableThing );
                }
            }

            this.tmpThingList.Clear();
            this.tmpThingList.AddRange( available );

            // then just match thingdef
            foreach ( var availableThing in available.ToArray() )
            {
                if ( this.MissingParts.Any( mp => mp.Item1 == availableThing.def ) )
                {
                    this.AddPart( availableThing );
                    available.Remove( availableThing );
                }
            }

            this.tmpThingList.Clear();
        }

        public void InitializeForLimb ( LimbConfiguration limb )
        {
            this.TargetLimb = limb;

            this.childPartsIncluded.Clear();
            this.DirtyCache();

            foreach ( (ThingDef childDef, LimbConfiguration bpr) in this.StandardPartsForTargetLimb() )
            {
                Thing child = ThingMaker.MakeThing( childDef );
                child.TryGetComp<CompIncludedChildParts>()?.InitializeForLimb( bpr );
                this.AddPart( child );
            }
        }

        public void InitializeFromSimilar ( CompIncludedChildParts other )
        {
            LimbConfiguration otherTarget = other.TargetLimb;

            // null target
            if ( otherTarget == null )
            {
                this.TargetLimb = null;
                return;
            }

            // same target if possible
            if ( this.Props.EverInstallableOn( otherTarget ) )
            {
                this.TargetLimb = otherTarget;

                // initialize for that target
                var otherMissing = other.MissingParts;
                var otherIncluded = other.IncludedPartsLimbs.ToList();

                this.childPartsIncluded.Clear();
                this.DirtyCache();

                foreach ( (ThingDef childDef, LimbConfiguration bpr) in this.StandardPartsForTargetLimb() )
                {
                    // add if not missing
                    if ( !otherMissing.Remove( i => i.Item2 == bpr ) )
                    {
                        Thing child = ThingMaker.MakeThing( childDef );

                        // if it is a limb segment, try to initialize based on similar or limb
                        if ( bpr != null )
                        {
                            var childComp = child.TryGetComp<CompIncludedChildParts>();

                            // initialize the child comp
                            if ( childComp != null )
                            {
                                // find corresponding
                                var otherChild = otherIncluded.Find( i => i.Item2 == bpr );
                                otherIncluded.Remove( otherChild );
                                var otherComp = otherChild.Item1?.TryGetComp<CompIncludedChildParts>();

                                if ( otherComp != null )
                                {
                                    childComp.InitializeFromSimilar( otherComp );
                                }
                                else
                                {
                                    childComp.InitializeForLimb( bpr );
                                }
                            }
                        }

                        this.AddPart( child );
                    }
                }
                return;
            }

            // target is parent of other
            LimbConfiguration goodTarget = this.Props.InstallationDestinations.Find( l => l.ChildLimbs.Contains( otherTarget ) );
            if ( goodTarget != null )
            {
                this.childPartsIncluded.Clear();
                this.DirtyCache();

                this.TargetLimb = goodTarget;

                foreach ( (ThingDef childDef, LimbConfiguration bpr) in this.StandardPartsForTargetLimb() )
                {
                    Thing child = ThingMaker.MakeThing( childDef );

                    // try to initialize from other on correct child, else just initialize for bpr
                    if ( bpr == otherTarget )
                    {
                        child.TryGetComp<CompIncludedChildParts>()?.InitializeFromSimilar( other );
                    }
                    else
                    {
                        child.TryGetComp<CompIncludedChildParts>()?.InitializeForLimb( bpr );
                    }

                    this.AddPart( child );
                }
            }
        }

        public override void PostDestroy ( DestroyMode mode, Map previousMap )
        {
            base.PostDestroy( mode, previousMap );

            // destroy included child items (idk if it does anything as they aren't spawned)
            foreach ( Thing childPart in this.IncludedParts )
            {
                childPart.Destroy( DestroyMode.Vanish );
            }
        }

        #endregion Creation / Deletion

        #endregion PartHandling

        #region StatsDisplay

        // Label

        protected String cachedTransformLabelString = null;

        //public override string TransformLabel ( string label )
        //{
        //    if ( this.IncludedParts != null )
        //    {
        //        if ( this.cachedTransformLabelString == null )
        //        {
        //            this.cachedTransformLabelString = " (";

        //            if ( this.CompatibleLimbs.Any() )
        //            {
        //                this.cachedTransformLabelString += String.Join( "; ", this.CompatibleLimbs.Select( lc => lc.Label ) );
        //            }
        //            else
        //            {
        //                this.cachedTransformLabelString += "incomplete";
        //            }

        //            this.cachedTransformLabelString += ")";
        //        }

        //        return label + this.cachedTransformLabelString;
        //    }
        //    return null;
        //}

        // Inspect string

        protected string cachedInspectString = null;

        public override string CompInspectStringExtra ()
        {
            if ( this.IncludedParts != null )
            {
                if ( this.cachedInspectString == null )
                {
                    StringBuilder stringBuilder = new StringBuilder();

                    stringBuilder.Append( "CompIncludedChildParts_InspectStringIncludes".Translate( this.IncludedParts.Count ) );

                    if ( this.TargetLimb != null )
                    {
                        stringBuilder.AppendInNewLine( "CompIncludedChildParts_InspectStringTarget".Translate( this.TargetLimbLabel ) );
                        if ( this.AllMissingParts.Any() )
                        {
                            stringBuilder.Append( "CompIncludedChildParts_InspectStringMissing".Translate( this.AllMissingParts.Count() ) ); // maybe optimize
                        }
                    }

                    this.cachedInspectString = stringBuilder.ToString();
                }

                return this.cachedInspectString;
            }
            return null;
        }

        // Stat entries
        private StatDrawEntry IncluededPartsStat => new StatDrawEntry(
            StatCategoryDefOf.Basics,
            "CompIncludedChildParts_StatIncludedParts_Label".Translate(),
            this.IncludedParts.Count.ToString(),
            "CompIncludedChildParts_StatIncludedParts_Description".Translate(),
            2501,
            null,
            this.IncludedParts.Select( p => new Dialog_InfoCard.Hyperlink( p ) ),
            false );

        private StatDrawEntry CompatibilityStat => new StatDrawEntry(
            StatCategoryDefOf.Basics,
            "CompIncludedChildParts_StatCompatibility_Label".Translate(),
            "CompIncludedChildParts_StatCompatibility_Value".Translate( this.CompatibleLimbs.Count, this.Props.InstallationDestinations.Count ),
            this.Props.GetCompatibilityReportDescription( this.CompatibleLimbs.Contains ),
            2500 );

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats ()
        {
            yield return IncluededPartsStat;
            yield return CompatibilityStat;
        }

        #endregion StatsDisplay

        #region RecursiveData

        /// <summary>
        /// Recursively searches for IncludedParts in all of the sub-parts
        /// </summary>
        public IEnumerable<(Thing, CompIncludedChildParts)> AllIncludedParts
        {
            get => Enumerable.Concat(

                // the sub-parts included in this part
                this.IncludedParts.Select( p => (p, this) ),

                // the sub-parts of the children with CompIncludedChildParts
                from i in this.IncludedParts
                let comp = i.TryGetComp<CompIncludedChildParts>()
                where comp != null
                from couple in comp.AllIncludedParts
                select couple );
        }

        /// <summary>
        /// Recursively searches for StandardParts in all of the sub-parts
        /// </summary>
        public IEnumerable<(ThingDef, CompIncludedChildParts)> AllStandardParts
        {
            get => Enumerable.Concat(

                // the standard sub-parts included in this part
                this.StandardParts.Select( p => (p, this) ),

                // the standard sub-parts of the children with CompIncludedChildParts
                from i in this.IncludedParts
                let comp = i.TryGetComp<CompIncludedChildParts>()
                where comp != null
                from couple in comp.AllStandardParts
                select couple );
        }

        public IEnumerable<(ThingDef, LimbConfiguration, CompIncludedChildParts)> AllMissingParts
        {
            get => Enumerable.Concat(

                // the standard sub-parts included in this part
                this.MissingParts.Select( p => (p.Item1, p.Item2, this) ),

                // the standard sub-parts of the children with CompIncludedChildParts
                from i in this.IncludedParts
                let comp = i.TryGetComp<CompIncludedChildParts>()
                where comp != null
                from triplet in comp.AllMissingParts
                select triplet );
        }

        #endregion RecursiveData
    }
}