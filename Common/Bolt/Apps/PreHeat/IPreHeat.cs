using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.PreHeat
{
    public interface IPreHeat
    {
        List<RetVal> PredictOccupancy(long startSlotIndex = 0, long endSlotIndex = long.MaxValue);
    }
}
