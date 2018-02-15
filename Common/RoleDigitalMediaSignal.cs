using System;
using System.Collections.Generic;
using System.Linq;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Common
{
    public class RoleSignalDigital : Role
    {
        public const string RoleName = ":crestronsignal:";
        public const string OpSetDigitalName = RoleName + "->" + "setdigital";
        public const string OnDigitalEvent = RoleName + "->" + "ondigital";
        public const string OnConnectEvent = RoleName + "->" + "onconnect";
        public const string OnDisconnectEvent = RoleName + "->" + "ondisconnect";
        public const string OnErrorEvent = RoleName + "->" + "onerror";

        //TODO
        public const string OpSetAnalogName = RoleName + "->" + "setanalog";  
        public const string OpSetSerialName = RoleName + "->" + "setserial";
  
        

        private static Role _instance;

        public static Role Instance
        {
            get
            {
                if (_instance == null)
                    new RoleSignalDigital();
                return _instance;
            }
        }

        protected RoleSignalDigital()
        {
            SetName(RoleName);
            _instance = this;

            

            {
                List<VParamType> args = new List<VParamType>() { new ParamType(0), new ParamType(0), new ParamType(string.Empty) };
                List<VParamType> retVals = new List<VParamType>(){new ParamType(true)};                
                AddOperation(new Operation(OpSetDigitalName, args, retVals,true));
            }

            {
                List<VParamType> args = new List<VParamType>() { };
                List<VParamType> retVals = new List<VParamType>() {};
                AddOperation(new Operation(OnConnectEvent, args, retVals,true));
            }

            {
                List<VParamType> args = new List<VParamType>() { };
                List<VParamType> retVals = new List<VParamType>() { new ParamType(string.Empty) };
                AddOperation(new Operation(OnDisconnectEvent, args, retVals, true));
            }

            {
                List<VParamType> args = new List<VParamType>() { };
                List<VParamType> retVals = new List<VParamType>() { new ParamType(string.Empty) };
                AddOperation(new Operation(OnErrorEvent, args, retVals,true));
            }

            {
                List<VParamType> args = new List<VParamType>() { };
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0), new ParamType(0), new ParamType(true) };
                AddOperation(new Operation(OnDigitalEvent, args, retVals, true));
            }

        }
    }
  }

