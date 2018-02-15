using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.PreHeat
{
    public class RetVal
    {
        private float timeToComputeThisVal;
        private int thisVal;

        public RetVal(float timetoCompute, int value)
        {
            this.timeToComputeThisVal = timetoCompute;
            this.thisVal = value;
        }
        public int getVal()
        {
            return this.thisVal;
        }
    }
}
