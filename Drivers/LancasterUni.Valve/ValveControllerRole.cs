
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
        public const string OpEchoName = "echo";
        public const string OpEchoSubName = "echosub";

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
        public const string OpEchoName = "echo";
        public const string OpEchoSubName = "echosub";

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

    #region old incomplete roles
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

    public class RoleSensor : Role
    {
        public const string RoleName = ":sensor:";
        public const string OpGetName = "get";

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

    public class RoleValve : Role
    {
        public const string RoleName = ":valve:";
        public const string OpGetValveNumber = "getnumber";
        public const string OpSend = "send";
        public const string OpDone = "done";
        public const string OpSetValve = "setvalve";
        public const string OpSetAllValves = "setallvalves";
        public const string OpReset = "reset";

        private static RoleValve _instance;

        public static RoleValve Instance
        {
            get
            {
                if (_instance == null)
                    new RoleValve();
                return _instance;
            }
        }

        protected RoleValve()
        {
            SetName(RoleName);
            _instance = this;

            List<VParamType> args = new List<VParamType>();
            List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };

            AddOperation(new Operation(OpGetValveNumber, args, retVals, true));
            AddOperation(new Operation(OpSend, args, retVals, true));
            AddOperation(new Operation(OpDone, args, retVals, true));
            AddOperation(new Operation(OpSetValve, args, retVals, true));
            AddOperation(new Operation(OpSetAllValves, args, retVals, true));
            AddOperation(new Operation(OpReset, args, retVals, true));
        }
    }

    public class RoleSwitchMultiLevel : Role
    {
        public const string RoleName = ":switchmultilevel:";
        public const string OpSetName = "set";
        public const string OpGetName = "get";

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
                List<VParamType> args = new List<VParamType>() { new ParamType(0) };
                List<VParamType> retVals = new List<VParamType>();
                AddOperation(new Operation(OpSetName, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() { new ParamType(0) };
                AddOperation(new Operation(OpGetName, args, retVals, true));
            }
        }
    }

    public class RoleSwitchBinary : Role
    {
        public const string RoleName = ":switchbinary:";
        public const string OpSetName = "set";
        public const string OpGetName = "get";

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
                List<VParamType> args = new List<VParamType>() { new ParamType(0) };
                List<VParamType> retVals = new List<VParamType>();
                AddOperation(new Operation(OpSetName, args, retVals));
            }

            {
                List<VParamType> args = new List<VParamType>();
                List<VParamType> retVals = new List<VParamType>() {new ParamType(0)};
                AddOperation(new Operation(OpGetName, args, retVals, true));
            }
        }
    }

    public class RoleDepthCam : Role
    {
        public const string RoleName = ":depthcam:";
        public const string OpGetLastDepthImgName = "lastdepthimg";
        public const string OpRcvDepthStreamName = "rcvdepthstream";
        public const string OpGetLastDepthArrayName = "lastdeptharray";
        public const string OpRcvDepthArrayName = "rcvdeptharray";

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

    public class RoleMicrophone : Role
    {
        public const string RoleName = ":microphone:";
        public const string OpRecAudioName = "recaudio";

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
        }
    }

    public class RoleSkeletonTracker : Role
    {
        public const string RoleName = ":skeletontracker:";
        public const string OpGetLastskeletonName = "lastskeleton";
        public const string OpRcvSkeletonStreamName = "rcvskeletonstream";

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

    //public class RoleCamera : Role
    //{
    //    public const string RoleName = "camera";
    //    public const string OpUpName = "up";
    //    public const string OpDownName = "down";
    //    public const string OpLeftName = "left";
    //    public const string OpRightName = "right";
    //    public const string OpZoomInName = "zoomin";
    //    public const string OpZommOutName = "zoomout";
    //    public const string OpGetImageName = "getimage";
    //    public const string OpGetVideo = "getvideo";

    //    private static Role _instance;

    //    public static Role Instance
    //    {
    //        get
    //        {
    //            if (_instance == null)
    //                _instance = new RoleCamera();
    //            return _instance;
    //        }
    //    }

    //    protected RoleCamera()
    //        : base(RoleName)
    //    {

    //        _instance = this;

    //        AddOperation(new Operation(RoleCamera.OpUpName, null, null));
    //        AddOperation(new Operation(RoleCamera.OpDownName, null, null));
    //        AddOperation(new Operation(RoleCamera.OpLeftName, null, null));
    //        AddOperation(new Operation(RoleCamera.OpRightName, null, null));
    //        AddOperation(new Operation(RoleCamera.OpZoomInName, null, null));
    //        AddOperation(new Operation(RoleCamera.OpZommOutName, null, null));

    //        {
    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(ParamType.SimpleType.jpegimage, null));

    //            AddOperation(new Operation(RoleCamera.OpGetImageName, null, retVals));
    //        }

    //        {
    //            List<VParamType> retVals = new List<VParamType>();
    //            retVals.Add(new ParamType(ParamType.SimpleType.jpegimage, null));

    //            AddOperation(new Operation(RoleCamera.OpGetVideo, null, retVals, true));
    //        }

    //    }
    //}

    #region experiments with role inheritance -- to be picked up later
    public class RoleCamera : Role
    {
        public const string RoleName = ":camera:";
        public const string OpGetImageName = "getimage";
        public const string OpGetVideo = "getvideo";

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

    public class RolePTCamera : RoleCamera
    {
        public new const string RoleName = ":camera::ptcamera:";
        public const string OpUpName = "up";
        public const string OpDownName = "down";
        public const string OpLeftName = "left";
        public const string OpRightName = "right";

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
        public new const string RoleName = ":camera::ptcamera:ptzcamera:";
        public const string OpZoomInName = "zoomin";
        public const string OpZommOutName = "zoomout";

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
    #endregion

    //Not sure if we want to have the couch as mulitple different roles (lights, pressure)
    public class RoleCouch: Role
    {
        public const string RoleName = ":couch:";
        public const string OpGoHappyName = "gohappy";
        //public const string OpUpName = "up";
        //public const string OpDownName = "down";
        //public const string OpLeftName = "left";
        //public const string OpRightName = "right";
        //public const string OpZoomInName = "zoomin";
        //public const string OpZommOutName = "zoomout";
        //public const string OpGetImageName = "getimage";
        //public const string OpGetVideo = "getvideo";

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
                List<VParamType> args = new List<VParamType>() {new ParamType(0)};
                List<VParamType> retVals = new List<VParamType>() {new ParamType(0)};
                AddOperation(new Operation(OpGoHappyName, args, retVals));
            }
        }
    }


#region revive later
    //public class RoleHueBridge : Role
    //{
    //    public const string RoleName = "huebridge";
    //    public const string OpToggleAll = "toggleall";
    //    public const string OpTurnOffAll = "turnoffall";
    //    public const string OpTurnOnAll = "turnonall";
    //    public const string OpResetAll = "resetall";
    //    public const string OpUnlockAll = "unlockall";
    //    public const string OpSetColorAll = "setcolorall";
    //    public const string OpSetBrightnessAll = "setbrightnessall"; 

    //    public const string OpToggleBulb = "togglebulb";
    //    public const string OpTurnOffBulb = "turnoffbulb";
    //    public const string OpTurnOnBulb = "turnonbulb";
    //    public const string OpResetBulb = "resetbulb";
    //    public const string OpUnlockBulb = "unlockbulb";
    //    public const string OpSetColorBulb = "setcolorbulb";
    //    public const string OpGetColorBulb = "getcolorbulb";
    //    public const string OpSetBrightnessBulb = "setbrightnessbulb";
    //    public const string OpBumpBulb = "bumpbulb";

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
#endregion

}