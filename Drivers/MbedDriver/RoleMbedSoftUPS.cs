using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Drivers.MbedDriver
{
    #region "SoftUPS Role"
    public class RoleMbedSoftUps : Role
    {
        public const string RoleName = ":mbedsoftups:";//":mbedSoftUPS:";
        public const string OpOnSwitch = RoleName + "->" + "on";
        public const string OpOffSwitch = RoleName + "->" + "off";
        public const string OpGetDeviceNum = RoleName + "->" + "getdevicenum";

        private static RoleMbedSoftUps _instance;

        protected RoleMbedSoftUps()
        {
            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> args = new List<VParamType>() { new ParamType(0) };
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpOnSwitch, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>() { new ParamType(0) };
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpOffSwitch, args, retVals));
            }


            {
                List<VParamType> args = new List<VParamType>() { new ParamType(0) };
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpGetDeviceNum, args, retVals));
            }

        }

        public static RoleMbedSoftUps Instance
        {
            get
            {
                if (_instance == null)
                {
                    new RoleMbedSoftUps();
                }

                return _instance;
            }
        }
    }
    #endregion
}
