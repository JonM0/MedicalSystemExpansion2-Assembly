using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

namespace MSE2
{
    internal interface IHediffCompPropsWithStats
    {
        IEnumerable<StatDrawEntry> SpecialDisplayStats ( StatRequest req );
    }
}