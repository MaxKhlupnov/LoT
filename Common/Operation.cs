using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeOS.Hub.Common
{

    /// <summary>
    /// An operation has 3 modes: it is an operation description, a call or a return
    /// </summary>
    public sealed class Operation : HomeOS.Hub.Platform.Views.VOperation
    {
        private string name;
        private IList<HomeOS.Hub.Platform.Views.VParamType> parameters;
        private IList<HomeOS.Hub.Platform.Views.VParamType> returnVals;
        private bool subscribeable;

        public Operation(string name, IList<HomeOS.Hub.Platform.Views.VParamType> parameters, IList<HomeOS.Hub.Platform.Views.VParamType> retVals, bool canSub = false)
        {
            this.name = name;
            this.parameters = parameters == null ? new List<HomeOS.Hub.Platform.Views.VParamType>() : parameters;
            this.returnVals = retVals == null ? new List<HomeOS.Hub.Platform.Views.VParamType>() : retVals;
            subscribeable = canSub;
        }

        public override IList<HomeOS.Hub.Platform.Views.VParamType> Parameters()
        {
            return parameters;
        }

        public override IList<HomeOS.Hub.Platform.Views.VParamType> ReturnValues()
        {
            return returnVals;
        }

        public override string Name()
        {
            return name;
        }

        public override bool Subscribeable()
        {
            return subscribeable;
        }
    }
}
