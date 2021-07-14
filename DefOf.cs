using RimWorld;

using Verse;

namespace MSE2
{
    [RimWorld.DefOf]
    internal static class DefOf
    {
        static DefOf ()
        {
            DefOfHelper.EnsureInitializedInCtor( typeof( DefOf ) );
        }

        public static HediffDef MSE_ModuleSlot;
    }
}