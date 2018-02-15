
namespace HomeOS.Hub.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HomeOS.Hub.Platform.Views;

    public class Role : VRole
    {
        private string name;

        protected Dictionary<string, VOperation> operations = new Dictionary<string, VOperation>();

        //empty constructor to allow for inheritance
        public Role() { }

        public Role(string name)
        {
            SetName(name);
        }

        protected void SetName(string value)
        {
                if (!value.StartsWith(":") || !value.EndsWith(":"))
                    throw new Exception("Invalid role name " + value + ". Role names should beging and end with ':'");

                name = value;
        }

        public void AddOperation(Operation operation)
        {
            lock (operations)
            {
                if (operations.ContainsKey(operation.Name()))
                    throw new Exception("operation name " + operation.Name() + " already exists!");

                if (operation.Parameters() == null)
                    throw new Exception("Argument list cannot be empty");

                if (operation.ReturnValues() == null)
                    throw new Exception("Argument list cannot be empty");

                operations.Add(operation.Name().ToLower(), operation);
            }
        }

        /// <summary>
        /// Returns the operation object that corresponds to opName
        /// </summary>
        /// <param name="opName"></param>
        /// <returns></returns>
        public VOperation GetOperation(string opName)
        {
            lock (operations)
            {
                if (operations.ContainsKey(opName)) 
                    return operations[opName.ToLower()];
            }

            return null;
        }

        /// <summary>
        /// Returns the list of operations supported by the role
        /// </summary>
        /// <returns></returns>
        public override IList<VOperation> GetOperations()
        {
            lock (operations)
            {
                return operations.Values.ToList();
            }
        }

        /// <summary>
        /// Does the given port contain the given role
        /// </summary>
        /// <param name="port"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public static bool ContainsRole(VPort port, string roleName)
        {
            foreach (VRole role in port.GetInfo().GetRoles())
            {
                if (ContainsRole(role.Name(), roleName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if roleB is contained within roleA. That is, if roleA (:camera::ptcamera::) implies roleB (:camera:).
        /// </summary>
        /// <param name="roleA"></param>
        /// <param name="roleB"></param>
        /// <returns></returns>
        public static bool ContainsRole(VRole roleA, VRole roleB)
        {
            return ContainsRole(roleA.Name(), roleB.Name());
        }

        /// <summary>
        /// Checks if roleB is contained within roleA. That is, if roleA (:camera::ptcamera::) implies roleB (:camera:).
        /// </summary>
        /// <param name="roleA"></param>
        /// <param name="roleB"></param>
        /// <returns></returns>
        public static bool ContainsRole(string roleA, string roleB)
        {
            return roleA.Contains(roleB);
        }

        public override string Name()
        {
            return this.name;
        }

        public override string ToString()
        {
            return name;
        }
    }

    public class RoleDummy : Role
    {
        public const string RoleName = ":dummy:";
        public const string OpEchoName = RoleName + "->" + "echo";
        public const string OpEchoSubName = RoleName + "->" + "echosub";

        private static RoleDummy _instance;

        protected RoleDummy()
        {
            SetName(RoleName);
             _instance = this;

            {
                List<VParamType> args = new List<VParamType>() {new ParamType(0)};
                List<VParamType> retVals = new List<VParamType>() {new ParamType(0)};
                AddOperation(new Operation(OpEchoName, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpEchoSubName, args, retVals, true));
            }

        }

        public static RoleDummy Instance
        {
            get
            {
                if (_instance == null)
                     new RoleDummy();
                return _instance;
            }
        }
    }

 



    public class RoleDoorjamb : Role
    {
        public const string RoleName = ":doorjamb:";
        public const string OpEchoName = RoleName + "->" + "echo";
        public const string OpEchoSubName = RoleName + "->" + "echosub";

        private static RoleDoorjamb _instance;

        protected RoleDoorjamb()
        {
            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> args = new List<VParamType>() { new ParamType(0) };
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpEchoName, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(ParamType.SimpleType.text, null) };
                AddOperation(new Operation(OpEchoSubName, args, retVals, true));
            }
        }

        public static RoleDoorjamb Instance
        {
            get
            {
                if (_instance == null)
                    new RoleDoorjamb();
                return _instance;
            }
        }
    }

    #region old roles with obsolete definition styles
    //public class RoleIRLearner : Role
    //{
    //    public const string RoleName = "IRLearner";
    //    public const string OpLearnName = "Learn";
    //    // exporting string Learn(void)
    //    public RoleIRLearner()
    //        : base(RoleName)
    //    {
    //        List<VParamType> retTypes = new List<VParamType>();
    //        retTypes.Add(new ParamType(ParamType.SimpleType.text, ""));
    //        AddOperation(new Operation(OpLearnName, null, retTypes));
    //    }
    //}

    //public class RoleIRSender : Role
    //{
    //    public const string RoleName = "IRSender";
    //    public const string OpSendName = "Send";
    //    // exporting void Send(string)
    //    public RoleIRSender()
    //        : base(RoleName)
    //    {
    //        List<VParamType> argTypes = new List<VParamType>();
    //        argTypes.Add(new ParamType(ParamType.SimpleType.text, ""));

    //        List<VParamType> retTypes = new List<VParamType>();
    //        retTypes.Add(new ParamType(ParamType.SimpleType.binary, null));

    //        AddOperation(new Operation(OpSendName, argTypes, retTypes));
    //    }
    //}

    //public class RoleImgRec : Role
    //{
    //    public const string RoleName = "image-recognize";
    //    public const string OpRecognizeName = "Recognize";

    //    public RoleImgRec()
    //        : base(RoleName)
    //    {
    //        IList<VParamType> args = new List<VParamType>();
    //        args.Add(new ParamType(ParamType.SimpleType.image, System.Net.Mime.MediaTypeNames.Image.Jpeg, null, "image"));

    //        IList<VParamType> retVals = new List<VParamType>();
    //        retVals.Add(new ParamType(ParamType.SimpleType.integer, "", null, "id"));

    //        AddOperation(new Operation(OpRecognizeName, args, retVals));
    //    }
    //}

    //public class RoleDms : Role
    //{
    //    public const string RoleName = "dms";
    //    public const string OpListMediaName = "listmedia";

    //    public RoleDms()
    //        : base(RoleName)
    //    {
    //        AddOperation(new Operation(RoleDms.OpListMediaName, null, null));
    //    }
    //}

    //public class RoleDmr : Role
    //{
    //    public const string RoleName = "dmr";
    //    public const string OpPlayName = "play";
    //    public const string OpPlayAtName = "playat";
    //    public const string OpStopName = "stop";
    //    public const string OpStatusRequestName = "statusrequest";

    //    public RoleDmr()
    //        : base(RoleName)
    //    {
    //        {
    //            List<VParamType> playParameters = new List<VParamType>();
    //            playParameters.Add(new ParamType(ParamType.SimpleType.text, "", null, "uri"));
    //            AddOperation(new Operation(OpPlayName, playParameters, null));  //play(uri)
    //        }
    //        {
    //            List<VParamType> playAtParameters = new List<VParamType>();
    //            playAtParameters.Add(new ParamType(ParamType.SimpleType.text, "", null, "url"));
    //            playAtParameters.Add(new ParamType(ParamType.SimpleType.text, "", null, "time"));
    //            AddOperation(new Operation(OpPlayAtName, playAtParameters, new List<VParamType>())); //play(uri,time)
    //        }

    //        {
    //            AddOperation(new Operation(OpStopName, null, null)); //stop()
    //        }

    //        {
    //            List<VParamType> statusRets = new List<VParamType>();
    //            statusRets.Add(new ParamType(ParamType.SimpleType.text, "", null, "uri"));
    //            statusRets.Add(new ParamType(ParamType.SimpleType.text, "", null, "time"));
    //            AddOperation(new Operation(OpStatusRequestName, new List<VParamType>(), statusRets)); //(uri,time) <- statusrequest
    //        }
    //    }
    //}
    #endregion

    #region roles related to sensors
    public class RoleSensor : Role
    {
        public const string RoleName = ":sensor:";
        public const string OpGetName = RoleName + "->" + "get";

        private static RoleSensor _instance;

        public static RoleSensor Instance
        {
            get
            {
                if (_instance == null)
                    new RoleSensor();
                return _instance;
            }
        }

        protected RoleSensor()
        {
            SetName(RoleName);
            _instance = this;

            List<VParamType> args = new List<VParamType>();
            List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
            AddOperation(new Operation(OpGetName, args, retVals, true));
        }
    }

    public class RoleProximitySensor : Role
    {
        public const string RoleName = ":proximitysensor:";
        public const string OpGetName = RoleName + "->" + "get";

        private static RoleProximitySensor _instance;

        public static RoleProximitySensor Instance
        {
            get
            {
                if (_instance == null)
                    new RoleProximitySensor();
                return _instance;
            }
        }

        protected RoleProximitySensor()
        {
            SetName(RoleName);
            _instance = this;

            List<VParamType> args = new List<VParamType>();
            List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
            AddOperation(new Operation(OpGetName, args, retVals, true));
        }
    }
      


    public class RolePowerSensor : RoleSensorMultiLevel
    {
        public new const string RoleName = ":sensormultilevel::powersensor:";

        private static Role _instance;

        public new static Role Instance
        {
            get
            {
                if (_instance == null)
                    new RolePowerSensor();
                return _instance;
            }
        }

        protected RolePowerSensor()
        {
            SetName(RoleName);
            _instance = this;
        }
    }

    public class RoleTemperatureSensor : RoleSensorMultiLevel
    {
        public new const string RoleName = ":sensormultilevel::temperaturesensor:";

        private static Role _instance;

        public new static Role Instance
        {
            get
            {
                if (_instance == null)
                    new RoleTemperatureSensor();
                return _instance;
            }
        }

        protected RoleTemperatureSensor()
        {
            SetName(RoleName);
            _instance = this;
        }
    }

    public class RoleHumiditySensor : RoleSensorMultiLevel
    {
        public new const string RoleName = ":sensormultilevel::humiditysensor:";

        private static Role _instance;

        public new static Role Instance
        {
            get
            {
                if (_instance == null)
                    new RoleHumiditySensor();
                return _instance;
            }
        }

        protected RoleHumiditySensor()
        {
            SetName(RoleName);
            _instance = this;
        }
    }

    public class RoleLuminositySensor : RoleSensorMultiLevel
    {
        public new const string RoleName = ":sensormultilevel::luminositysensor:";

        private static Role _instance;

        public new static Role Instance
        {
            get
            {
                if (_instance == null)
                    new RoleLuminositySensor();
                return _instance;
            }
        }

        protected RoleLuminositySensor()
        {
            SetName(RoleName);
            _instance = this;
        }
    }
 

    public class RoleBatteryLevel : RoleSensorMultiLevel
    {
        public new const string RoleName = ":sensormultilevel::batterylevel:";

        private static Role _instance;

        public new static Role Instance
        {
            get
            {
                if (_instance == null)
                    new RoleBatteryLevel();
                return _instance;
            }
        }

        protected RoleBatteryLevel()
        {
            SetName(RoleName);
            _instance = this;
        }
    }

    public class RoleSensorMultiLevel : Role
    {
        public const string RoleName = ":sensormultilevel:";
        public const string OpGetName = RoleName + "->" + "get";

        private static Role _instance;

        public static Role Instance
        {
            get
            {
                if (_instance == null)
                    new RoleSensorMultiLevel();
                return _instance;
            }
        }

        protected RoleSensorMultiLevel()
        {
            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0.0) };
                AddOperation(new Operation(OpGetName, args, retVals, true));
            }
        }
    }
    #endregion

    public class RoleActuator : Role
    {
        public const string RoleName = ":actuator:";
        public const string OpPutName = RoleName + "->" + "put";

        private static RoleActuator _instance;

        public static RoleActuator Instance
        {
            get
            {
                if (_instance == null)
                    new RoleActuator();
                return _instance;
            }
        }

        protected RoleActuator()
        {
            SetName(RoleName);
            _instance = this;

            List<VParamType> args = new List<VParamType>();
            List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
            AddOperation(new Operation(OpPutName, args, retVals, true));
        }
    }

    public class RoleSwitchBinary : Role
    {
        public const string RoleName = ":switchbinary:";
        public const string OpSetName = RoleName + "->" + "set";
        public const string OpGetName = RoleName + "->" + "get";

        private static RoleSwitchBinary _instance;

        public static RoleSwitchBinary Instance
        {
            get
            {
                if (_instance == null)
                    new RoleSwitchBinary();
                return _instance;
            }
        }

        protected RoleSwitchBinary()
        {
            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> args = new List<VParamType>() { new ParamType(false) };
                List<VParamType> retVals = new List<VParamType>();
                AddOperation(new Operation(OpSetName, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(false) };
                AddOperation(new Operation(OpGetName, args, retVals, true));
            }
        }
    }

    public class RoleSwitchMultiLevel : Role
    {
        public const string RoleName = ":switchmultilevel:";
        public const string OpSetName = RoleName + "->" + "set";
        public const string OpGetName = RoleName + "->" + "get";

        private static Role _instance;

        public static Role Instance
        {
            get
            {
                if (_instance == null)
                    new RoleSwitchMultiLevel();
                return _instance;
            }
        }

        protected RoleSwitchMultiLevel()
        {
            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> args = new List<VParamType>() { new ParamType(0.0) };
                List<VParamType> retVals = new List<VParamType>();
                AddOperation(new Operation(OpSetName, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() {new ParamType(0.0)};
                AddOperation(new Operation(OpGetName, args, retVals, true));
            }
        }
    }

    public class RoleLightColor : Role
    {
        public const string RoleName = ":lightcolor:";
        public const string OpSetName = RoleName + "->" + "set";
        public const string OpGetName = RoleName + "->" + "get";

        private static Role _instance;

        public static Role Instance
        {
            get
            {
                if (_instance == null)
                    new RoleLightColor();
                return _instance;
            }
        }

        protected RoleLightColor()
        {
            SetName(RoleName);
            _instance = this;

            {
                //the three parameters are red, green, and blue
                List<VParamType> args = new List<VParamType>() { new ParamType(0), new ParamType(0), new ParamType(0) };
                List<VParamType> retVals = new List<VParamType>();
                AddOperation(new Operation(OpSetName, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>();
                //the three parameters are red, green, and blue
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0), new ParamType(0), new ParamType(0) };
                AddOperation(new Operation(OpGetName, args, retVals, true));
            }
        }
    }

    public class RoleMicrophone : Role
    {
        public const string RoleName = ":microphone:";
        public const string OpRecAudioName = RoleName + "->" + "recaudio";
        public const string OpRecBytesName = RoleName + "->" + "recbytes";

        private static RoleMicrophone _instance;

        public static RoleMicrophone Instance
        {

            get
            {
                if (_instance == null)
                    new RoleMicrophone();
                return _instance;
            }
        }

        public RoleMicrophone()
            : base(RoleName)
        {
            SetName(RoleName);
            _instance = this;
            {
                List<HomeOS.Hub.Platform.Views.VParamType> args = new List<HomeOS.Hub.Platform.Views.VParamType>();
                args.Add(new ParamType(ParamType.SimpleType.integer, "recLength", null));

                List<HomeOS.Hub.Platform.Views.VParamType> retVals = new List<HomeOS.Hub.Platform.Views.VParamType>();
                retVals.Add(new ParamType(ParamType.SimpleType.text, "audioFullPath", null));

                AddOperation(new Operation(RoleMicrophone.OpRecAudioName, args, retVals));
            }

            {
                List<HomeOS.Hub.Platform.Views.VParamType> args = new List<HomeOS.Hub.Platform.Views.VParamType>();
                args.Add(new ParamType(0.0));

                List<HomeOS.Hub.Platform.Views.VParamType> retVals = new List<HomeOS.Hub.Platform.Views.VParamType>();
                retVals.Add(new ParamType(ParamType.SimpleType.text, "recbytes", null));

                AddOperation(new Operation(RoleMicrophone.OpRecBytesName, args, retVals, true));
            }

        }
    }

    public class RoleSkeletonTracker : Role
    {
        public const string RoleName = ":skeletontracker:";
        public const string OpGetLastskeletonName = RoleName + "->" + "lastskeleton";
        public const string OpRcvSkeletonStreamName = RoleName + "->" + "rcvskeletonstream";

        private static RoleSkeletonTracker _instance;

        public static RoleSkeletonTracker Instance
        {
            get
            {
                if (_instance == null)
                    new RoleSkeletonTracker();
                return _instance;
            }
        }

        public RoleSkeletonTracker()
            : base(RoleName)
        {
            SetName(RoleName);
            _instance = this;
            {
                List<HomeOS.Hub.Platform.Views.VParamType> retVals = new List<HomeOS.Hub.Platform.Views.VParamType>();
                retVals.Add(new ParamType(ParamType.SimpleType.text, "skeletonArray", null));
                AddOperation(new Operation(RoleSkeletonTracker.OpGetLastskeletonName, null, retVals));
            }

            {
                List<HomeOS.Hub.Platform.Views.VParamType> retVals = new List<HomeOS.Hub.Platform.Views.VParamType>();
                retVals.Add(new ParamType(ParamType.SimpleType.text, "skeletonArray", null));
                AddOperation(new Operation(RoleSkeletonTracker.OpRcvSkeletonStreamName, null, retVals, true));
            }
        }
    }

    #region roles related to cameras
    public class RoleCamera : Role
    {
        public const string RoleName = ":camera:";
        public const string OpGetImageName = RoleName + "->" + "getimage";
        public const string OpGetVideo = RoleName + "->" + "getvideo";

        private static RoleCamera _instance;

        public static RoleCamera Instance
        {
            get
            {
                if (_instance == null)
                    new RoleCamera();
                return _instance;
            }
        }

        protected RoleCamera()
        {

            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(ParamType.SimpleType.jpegimage, null) };
                AddOperation(new Operation(RoleCamera.OpGetImageName, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(ParamType.SimpleType.jpegimage, null) };
                AddOperation(new Operation(RoleCamera.OpGetVideo, args, retVals, true));
            }
        }
    }

    public class RoleOccupancy : Role
    {
        public const string RoleName = ":occupancy:";
        public const string OpGetOccupancy = RoleName + "->" + "getoccupancy";

        private static RoleOccupancy _instance;

        public static RoleOccupancy Instance
        {
            get
            {
                if (_instance == null)
                    new RoleOccupancy();
                return _instance;
            }
        }

        protected RoleOccupancy()
        {

            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(ParamType.SimpleType.integer, null) };
                AddOperation(new Operation(RoleOccupancy.OpGetOccupancy, args, retVals, true));
            }
        }
    }

    public class RolePTCamera : RoleCamera
    {
        public new const string RoleName = ":camera::ptcamera:";
        public const string OpUpName = RoleName + "->" + "up";
        public const string OpDownName = RoleName + "->" + "down";
        public const string OpLeftName = RoleName + "->" + "left";
        public const string OpRightName = RoleName + "->" + "right";

        private static RolePTCamera _instance;

        public new static RolePTCamera Instance
        {
            get
            {
                if (_instance == null)
                    new RolePTCamera();
                return _instance;
            }
        }

        protected RolePTCamera()
        {

            SetName(RoleName);
            _instance = this;

            List<VParamType> emptyParamList = new List<VParamType>();

            AddOperation(new Operation(OpUpName, emptyParamList, emptyParamList));
            AddOperation(new Operation(OpDownName, emptyParamList, emptyParamList));
            AddOperation(new Operation(OpLeftName, emptyParamList, emptyParamList));
            AddOperation(new Operation(OpRightName, emptyParamList, emptyParamList));
        }
    }

    public class RolePTZCamera : RolePTCamera
    {
        public new const string RoleName = ":camera::ptcamera::ptzcamera:";
        public const string OpZoomInName = RoleName + "->" + "zoomin";
        public const string OpZommOutName = RoleName + "->" + "zoomout";

        private static RolePTZCamera _instance;

        public new static RolePTZCamera Instance
        {
            get
            {
                if (_instance == null)
                    new RolePTZCamera();
                return _instance;
            }
        }

        protected RolePTZCamera()
        {

            SetName(RoleName);
            _instance = this;

            List<VParamType> emptyParamList = new List<VParamType>();

            AddOperation(new Operation(OpZoomInName, emptyParamList, emptyParamList));
            AddOperation(new Operation(OpZommOutName, emptyParamList, emptyParamList));
        }
    }

    public class RoleDepthCam : Role
    {
        public const string RoleName = ":depthcam:";
        public const string OpGetLastDepthImgName = RoleName + "->" + "lastdepthimg";
        public const string OpRcvDepthStreamName = RoleName + "->" + "rcvdepthstream";
        public const string OpGetLastDepthArrayName = RoleName + "->" + "lastdeptharray";
        public const string OpRcvDepthArrayName = RoleName + "->" + "rcvdeptharray";

        private static RoleDepthCam _instance;

        public static RoleDepthCam Instance
        {
            get
            {
                if (_instance == null)
                    new RoleDepthCam();
                return _instance;
            }
        }

        public RoleDepthCam()
            : base(RoleName)
        {
            SetName(RoleName);
            _instance = this;

            {
                List<HomeOS.Hub.Platform.Views.VParamType> retVals = new List<HomeOS.Hub.Platform.Views.VParamType>();
                retVals.Add(new ParamType(ParamType.SimpleType.jpegimage, "byteImg", null));
                AddOperation(new Operation(RoleDepthCam.OpGetLastDepthImgName, null, retVals));
            }

            {
                List<HomeOS.Hub.Platform.Views.VParamType> retVals = new List<HomeOS.Hub.Platform.Views.VParamType>();
                retVals.Add(new ParamType(ParamType.SimpleType.jpegimage, "byteImg", null));
                AddOperation(new Operation(RoleDepthCam.OpRcvDepthStreamName, null, retVals, true));
            }

            {
                List<HomeOS.Hub.Platform.Views.VParamType> retVals = new List<HomeOS.Hub.Platform.Views.VParamType>();
                retVals.Add(new ParamType(ParamType.SimpleType.list, "depthArray", null));
                AddOperation(new Operation(RoleDepthCam.OpGetLastDepthArrayName, null, retVals));
            }

            {
                List<HomeOS.Hub.Platform.Views.VParamType> retVals = new List<HomeOS.Hub.Platform.Views.VParamType>();
                retVals.Add(new ParamType(ParamType.SimpleType.list, "depthArray", null));
                AddOperation(new Operation(RoleDepthCam.OpRcvDepthArrayName, null, retVals, true));
            }
        }
    }

    #endregion

    public class RoleSpeechReco : Role
    {       
        public const string RoleName = ":speechreco:";
        public const string OpSetSpeechPhraseName = RoleName + "->" + "speechphrase";
        public const string OpPhraseRecognizedSubName = RoleName + "->" + "speechrecosub";

        private static RoleSpeechReco _instance;

        protected RoleSpeechReco()
        {
            SetName(RoleName);
             _instance = this;

            {   //first argument is phrase to recognize, second is the value to return, this is for specifying grammar and result  e.g. ("all off", "ALLOFF"), and ("good night", "ALLOFF")
                List<VParamType> args = new List<VParamType>() { new ParamType(ParamType.SimpleType.text, null), new ParamType(ParamType.SimpleType.text, null)};
                List<VParamType> retVals = new List<VParamType>() {new ParamType(0)};
                AddOperation(new Operation(OpSetSpeechPhraseName, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(ParamType.SimpleType.text, "phrase") };
                AddOperation(new Operation(OpPhraseRecognizedSubName, args, retVals, true));
            }

        }

        public static RoleSpeechReco Instance
        {
            get
            {
                if (_instance == null)
                     new RoleSpeechReco();
                return _instance;
            }
        }


    }

    //Not sure if we want to have the couch as mulitple different roles (lights, pressure)
    public class RoleCouch: Role
    {
        public const string RoleName = ":couch:";
        public const string OpSetEmotion = RoleName + "->" + "setemotion";
        public const string OpGetEmotion = RoleName + "->" + "getemotion";

        
        private static RoleCouch _instance;

        public static RoleCouch Instance
        {
            get
            {
                if (_instance == null)
                    new RoleCouch();
                return _instance;
            }
        }

        protected RoleCouch()
            : base(RoleName)
        {
            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> eArgs = new List<VParamType>() { new ParamType("text") };
                List<VParamType> eRetVals = new List<VParamType>() { new ParamType("text") };
                AddOperation(new Operation(OpSetEmotion, eArgs, eRetVals));
            }

            {
               // List<VParamType> args = new List<VParamType>() { new ParamType("text") };
                List<VParamType> retVals = new List<VParamType>() { new ParamType("text") };
                AddOperation(new Operation(OpGetEmotion, null, retVals, true));
            }
        }
    }

    public class RoleMSRChair : Role
    {
        public const string RoleName = ":chair:";
        public const string OpSendString = RoleName + "->" + "sendstring";
        public const string OpReceiveString = RoleName + "->" + "receivestring";

        private static RoleMSRChair _instance;

        protected RoleMSRChair()
        {
            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> args = new List<VParamType>() { new ParamType("text") };
                List<VParamType> retVals = new List<VParamType>() { new ParamType("text") };
                AddOperation(new Operation(OpSendString, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>() { new ParamType("text") };
                List<VParamType> retVals = new List<VParamType>() { new ParamType("text") };
                AddOperation(new Operation(OpReceiveString, args, retVals, true));
            }

        }

        public static RoleMSRChair Instance
        {
            get
            {
                if (_instance == null)
                    new RoleMSRChair();
                return _instance;
            }
        }
    }

    //Not sure if we want to have the couch as mulitple different roles (lights, pressure)
    public class RoleWeather : Role
    {
        public const string RoleName = ":weather:";

        public const string OpGetWeather = RoleName + "->" + "getweather";
        public const string OpGetTemperature = RoleName + "->" + "gettemperature";
        public const string OpGetPrecipitation = RoleName + "->" + "getprecipitation";

        private static RoleWeather _instance;

        public static RoleWeather Instance
        {
            get
            {
                if (_instance == null)
                    new RoleWeather();
                return _instance;
            }
        }

        protected RoleWeather()
            : base(RoleName)
        {
            SetName(RoleName);
            _instance = this;

            {
                List<VParamType> eArgs = new List<VParamType>() { };
                List<VParamType> eRetVals = new List<VParamType>() { new ParamType("text") };
                AddOperation(new Operation(OpGetWeather, eArgs, eRetVals));
            }

            {
                List<VParamType> eArgs = new List<VParamType>() { };
                List<VParamType> eRetVals = new List<VParamType>() { new ParamType(0.0), new ParamType(0.0), new ParamType(0.0) };
                AddOperation(new Operation(OpGetTemperature, eArgs, eRetVals));
            }

            {
                List<VParamType> eArgs = new List<VParamType>() { };
                List<VParamType> eRetVals = new List<VParamType>() { new ParamType("text") };
                AddOperation(new Operation(OpGetPrecipitation, eArgs, eRetVals));
            }

        }
    }


    //public class RoleHueBridge : Role
    //{
    //    public const string RoleName = ":huebridge:";
    //    public const string OpToggleAll = RoleName + "->" + "toggleall";
    //    public const string OpTurnOffAll = RoleName + "->" + "turnoffall";
    //    public const string OpTurnOnAll = RoleName + "->" + "turnonall";
    //    public const string OpResetAll = RoleName + "->" + "resetall";
    //    public const string OpUnlockAll = RoleName + "->" + "unlockall";
    //    public const string OpSetColorAll = RoleName + "->" + "setcolorall";
    //    public const string OpSetBrightnessAll = RoleName + "->" + "setbrightnessall";

    //    public const string OpToggleBulb = RoleName + "->" + "togglebulb";
    //    public const string OpTurnOffBulb = RoleName + "->" + "turnoffbulb";
    //    public const string OpTurnOnBulb = RoleName + "->" + "turnonbulb";
    //    public const string OpResetBulb = RoleName + "->" + "resetbulb";
    //    public const string OpUnlockBulb = RoleName + "->" + "unlockbulb";
    //    public const string OpSetColorBulb = RoleName + "->" + "setcolorbulb";
    //    public const string OpGetColorBulb = RoleName + "->" + "getcolorbulb";
    //    public const string OpSetBrightnessBulb = RoleName + "->" + "setbrightnessbulb";
    //    public const string OpBumpBulb = RoleName + "->" + "bumpbulb";

    //    private static Role _instance;

    //    public static Role Instance
    //    {
    //        get
    //        {
    //            if (_instance == null)
    //                _instance = new RoleHueBridge();
    //            return _instance;
    //        }
    //    }

    //    public RoleHueBridge()
    //        : base(RoleName)
    //    {
    //        SetName(RoleName);
    //        _instance = this;

    //        {
    //            List<VParamType> args = new List<VParamType>();

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpToggleAll, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpTurnOffAll, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpTurnOnAll, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpResetAll, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpUnlockAll, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpSetColorAll, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpSetBrightnessAll, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpToggleBulb, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpTurnOffBulb, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpTurnOnBulb, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpResetBulb, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpUnlockBulb, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpSetColorBulb, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));
    //            retVals.Add(new ParamType(0));
    //            retVals.Add(new ParamType(0));
    //            retVals.Add(new ParamType(0));

    //            AddOperation(new Operation(OpGetColorBulb, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpSetBrightnessBulb, args, retVals));
    //        }

    //        {
    //            List<VParamType> args = new List<VParamType>();
    //            args.Add(new ParamType(0));

    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(false));

    //            AddOperation(new Operation(OpBumpBulb, args, retVals));
    //        }

    //    }
    //}
}