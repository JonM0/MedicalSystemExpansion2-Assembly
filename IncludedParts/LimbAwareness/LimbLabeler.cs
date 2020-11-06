using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using System.Collections;

namespace MSE2
{
    internal class LimbLabeler
    {
        private readonly List<LimbConfiguration> limbPool;

        private readonly List<BodyPartDef> ignoredParts;

        private readonly Predicate<BodyDef> bodyRestrictions;

        private HashSet<BodyDef> cachedAllBodies;

        private ISet<BodyDef> AllBodies => cachedAllBodies
            ?? (cachedAllBodies = new HashSet<BodyDef>( limbPool.SelectMany( l => l.Bodies ).Where( bodyRestrictions.Invoke ) ));

        public LimbLabeler ( List<LimbConfiguration> limbPool, List<BodyPartDef> ignoredParts, Predicate<BodyDef> bodyRestrictions )
        {
            this.limbPool = limbPool;
            this.ignoredParts = ignoredParts;
            this.bodyRestrictions = bodyRestrictions;

            this.cachedLimbComparisons = new string[limbPool.Count];
            for ( int i = 0; i < limbPool.Count; i++ )
            {
                cachedLimbComparisons[i] = this.GetComparisonForLimb_Int( limbPool[i] );
            }
        }

        private bool PartShouldBeIgnored ( BodyPartDef bodyPartDef )
        {
            return ignoredParts != null && ignoredParts.Contains( bodyPartDef );
        }

        public List<string> GetRacesForLimb ( LimbConfiguration limb )
        {
            if ( limb == null )
            {
                return null;
            }

            var outList = new List<string>();

            var pawns = DefDatabase<ThingDef>.AllDefsListForReading;
            for ( int i = 0; i < pawns.Count; i++ )
            {
                var pawnDef = pawns[i];
                var body = pawnDef.race?.body;
                if ( body != null && limb.Bodies.Contains( body ) && AllBodies.Contains( body ) )
                {
                    // if is the only limb from this body
                    if ( !limbPool.Except( limb ).Any( l => l.Bodies.Contains( body ) ) )
                    {
                        outList.AddDistinct( pawnDef.label );
                    }
                    else
                    {
                        var recordUniqueNames = from bpr in limb.AllRecords
                                                where bpr.body == body
                                                select bpr.Label.Replace( bpr.LabelShort, "" ).Trim();

                        string records = string.Join( ", ", recordUniqueNames );

                        outList.AddDistinct( string.Format( "{0} ({1} {2})", pawnDef.label, records, limb.PartDef.LabelShort ) );
                    }
                }
            }

            return outList;
        }

        private List<(BodyPartDef, int)> LimbDifference ( LimbConfiguration limb )
        {
            var difference = new List<(BodyPartDef, int)>();

            for ( int i = 0; i < limb.AllSegments.Count; i++ )
            {
                var item = limb.AllSegments[i];

                if ( !limbPool.TrueForAll( l => l.AllSegments.Contains( item ) ) )
                {
                    difference.Add( item );
                }
            }

            return difference;
        }

        private readonly string[] cachedLimbComparisons;

        private string GetComparisonForLimb_Int ( LimbConfiguration limb )
        {
            if ( limb == null ) return null;

            var diffLimbs = LimbDifference( limb );
            builder.Clear();

            for ( int i = 0; i < diffLimbs.Count; i++ )
            {
                (BodyPartDef part, int count) = diffLimbs[i];

                if ( !PartShouldBeIgnored( part ) && count > 0 )
                {
                    builder.AppendWithComma( count.ToStringCached() );
                    builder.Append( " " );
                    if ( count > 1 )
                    {
                        builder.Append( Find.ActiveLanguageWorker.Pluralize( part.label ) );
                    }
                    else
                    {
                        builder.Append( part.label );
                    }
                }
            }

            //if ( builder.Length == 0 )
            //{
            //    return "LimbComplete".Translate();
            //}
            //else
            //{
                return builder.ToString();
            //}
        }

        public string GetComparisonForLimb ( LimbConfiguration limb )
        {
            int i = limbPool.IndexOf( limb );

            if ( i != -1 )
            {
                return cachedLimbComparisons[i];
            }
            else
            {
                return null;
            }
        }

        public string GetCompatibilityReport ( Predicate<LimbConfiguration> isCompatible )
        {
            builder.Clear();

            for ( int i = 0; i < limbPool.Count; i++ )
            {
                builder.AppendFormat( "{0} {1}: {2}",
                    cachedLimbComparisons[i],
                    "LimbVersion".Translate(),
                    isCompatible( limbPool[i] ) ? "LimbCompatible".TranslateSimple() : "LimbIncompatible".TranslateSimple() ).AppendLine();

                var labels = GetRacesForLimb( limbPool[i] );
                for ( int l = 0; l < labels.Count; l++ )
                {
                    builder.AppendFormat( " - {0}", labels[l] ).AppendLine();
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static readonly StringBuilder builder = new StringBuilder();
    }
}