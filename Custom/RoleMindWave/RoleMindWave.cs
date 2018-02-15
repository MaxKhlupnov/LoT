using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Custom
{
    public class RoleMindWave : Role
    {
        public const string RoleName = ":mindwave:";
        public const string OpGetConnection = RoleName + "->" + "getconnection";
        public const string OpGetAttention = RoleName + "->" + "getattention";
        public const string OpGetMeditation = RoleName + "->" + "getmeditation";
        public const string OpGetWaves = RoleName + "->" + "getwaves";
        public const string OpGetBlinks = RoleName + "->" + "getblinks";
        public const string OpClearBlinks = RoleName + "->" + "clearblinks";

        private static RoleMindWave _instance;

        protected RoleMindWave()
        {
            SetName(RoleName);
            _instance = this;

            //Get Connection
            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpGetConnection, args, retVals));
            }

            //Get Attention
            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpGetAttention, args, retVals));
            }

            //Get Meditation
            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpGetMeditation, args, retVals));
            }

            //Get Waves
            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0), new ParamType(0), new ParamType(0), new ParamType(0), new ParamType(0), new ParamType(0), new ParamType(0), new ParamType(0) };
                AddOperation(new Operation(OpGetWaves, args, retVals));
            }

            //Get Blinks
            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpGetBlinks, args, retVals));
            }

            //Clear Blinks
            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpClearBlinks, args, retVals));
            }

        }

        public static RoleMindWave Instance
        {
            get
            {
                if (_instance == null)
                    new RoleMindWave();
                return _instance;
            }
        }

    }
}
