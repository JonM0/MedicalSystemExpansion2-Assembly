using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

namespace MSE2
{
    internal class LimbLabeller
    {
        private IReadOnlyList<LimbConfiguration> limbPool;

        private HashSet<BodyDef> bodyRestrictions;

        private HashSet<BodyDef> cachedAllBodies;

        private ISet<BodyDef> AllBodies => cachedAllBodies
            ?? (cachedAllBodies = new HashSet<BodyDef>( limbPool.SelectMany( l => l.Bodies ).Where( bodyRestrictions.Contains ) ));

        private HashSet<BodyDef> cachedCommonBodies;

        public LimbLabeller ( List<LimbConfiguration> limbPool, HashSet<BodyDef> bodyRestrictions )
        {
            this.limbPool = limbPool;
            this.bodyRestrictions = bodyRestrictions;
        }

        private ISet<BodyDef> CommonBodies => cachedCommonBodies ??
            (cachedCommonBodies = new HashSet<BodyDef>( AllBodies.Where( b => limbPool.All( l => l.Bodies.Contains( b ) ) ) ));

        private bool AllTargetSameBodies => AllBodies.All( CommonBodies.Contains );

        public string GetLabelForLimb ( LimbConfiguration limb )
        {
            if(limb == null)
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();

            foreach ( var body in limb.Bodies.Where( AllBodies.Contains ) )
            {
                // if is the only limb from this body
                if ( !limbPool.Except( limb ).Any( l => l.Bodies.Contains( body ) ) )
                {
                    stringBuilder.AppendWithSeparator( body.label, "; " );
                }
                else
                {
                    var recordUniqueNames = from bpr in limb.AllRecords
                                            where bpr.body == body
                                            select bpr.Label.Replace( bpr.LabelShort, "" ).Trim();

                    string records = string.Join( ", ", recordUniqueNames );

                    stringBuilder.AppendWithSeparator( records + " " + body.label + " " + limb.PartDef.LabelShort, "; " );
                }
            }

            return stringBuilder.ToString();
        }
    }
}