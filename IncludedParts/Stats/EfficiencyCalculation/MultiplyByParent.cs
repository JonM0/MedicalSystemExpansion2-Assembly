using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MSE2
{
    internal class MultiplyByParent : DefModExtension
    {
        public MultiplyByParent ()
        {
            anyExist = true;
        }

        internal static bool anyExist = false;
    }
}
