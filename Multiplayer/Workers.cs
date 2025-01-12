using Multiplayer.API;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

#pragma warning disable IDE0051 // Remove unused private members
namespace MSE2.Multiplayer
{
    public static class Workers
    {
        [SyncWorker]
        static void SyncCompIncludedChildParts(SyncWorker sync, ref CompIncludedChildParts comp)
        {
            if (sync.isWriting)
            {
                sync.Write(comp.parent);
            }
            else
            {
                comp = sync.Read<ThingWithComps>().GetComp<CompIncludedChildParts>();
            }
        }


        [SyncWorker]
        static void SyncProsthesisVersion(SyncWorker sync, ref ProsthesisVersion version)
        {
            if (sync.isWriting)
            {
                sync.Write(version.CompProp);
                sync.Write(version.CompProp.SupportedVersions.IndexOf(version));
            }
            else
            {
                var compProp = sync.Read<CompProperties_IncludedChildParts>();
                var index = sync.Read<int>();
                version = compProp.SupportedVersions[index];
            }
        }


        [SyncWorker]
        static void SyncLimbConfiguration(SyncWorker sync, ref LimbConfiguration limb)
        {
            if (sync.isWriting)
            {
                sync.Write(limb.RecordExample);
            }
            else
            {
                limb = LimbConfiguration.LimbConfigForBodyPartRecord(sync.Read<BodyPartRecord>());
            }
        }


        [SyncWorker]
        static void SyncCompPropertiesIncludedChildParts(SyncWorker sync, ref CompProperties_IncludedChildParts props)
        {
            if (sync.isWriting)
            {
                sync.Write(props.parentDef);
            }
            else
            {
                props = sync.Read<ThingDef>().GetCompProperties<CompProperties_IncludedChildParts>();
            }
        }

    }
}
#pragma warning restore IDE0051 // Remove unused private members
