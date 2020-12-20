using System.Runtime.CompilerServices;

using Verse;

namespace MSE2
{
    public class Hediff_ModuleSlot : Hediff_ModuleAbstract
    {
        public void InstallModule ( Hediff_ModuleAdded module )
        {
            this.ModuleHolder.AddModule( module, this );
        }

        public override string ToString ()
        {
            return "ModuleSlot_" + this.Part;
        }
    }
}