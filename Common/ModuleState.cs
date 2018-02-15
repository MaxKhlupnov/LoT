
namespace HomeOS.Hub.Common
{

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
   

    public sealed class ModuleState : HomeOS.Hub.Platform.Views.VModuleState
    {
        public enum SimpleState { 
            [Description("EnterStart")] EnterStart = -8 ,
            [Description("ExitStart")] ExitStart,
            [Description("EnterPortRegistered")] EnterPortRegistered,
            [Description("ExitPortRegistered")] ExitPortRegistered,
            [Description("EnterPortDeregistered")] EnterPortDeregistered,
            [Description("ExitPortDeregistered")] ExitPortDeregistered,
            [Description("EnterStop")] EnterStop,
            [Description("ExitStop")] ExitStop
        }

        SimpleState state;
        DateTime timestamp;

        
        public ModuleState(SimpleState s, DateTime t)
        {
            this.state = s;
            this.timestamp = t;
        }



        public override int GetSimpleState()
        {
            return (int)this.state; 
        }

        public override DateTime GetTimestamp()
        {
            return this.timestamp;
        }


        public override void Update(HomeOS.Hub.Platform.Views.VModuleState s)
        {
            this.state = (SimpleState)s.GetSimpleState();
            this.timestamp = s.GetTimestamp();
        }
       



    }
}
