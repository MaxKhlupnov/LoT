using System;

namespace HomeOS.Hub.Common
{

    public sealed class Capability : HomeOS.Hub.Platform.Views.VCapability
    {
        static Random randGenerator = new Random();

        string issuerId;
        DateTime expiryTime;
        int randomVal;

        public Capability(string issuerId, DateTime expiryTime)
        {
            this.issuerId = issuerId;
            this.expiryTime = expiryTime;

            randomVal = randGenerator.Next();
        }

        public override string IssuerId()
        {
            return issuerId;
        }

        public override DateTime ExpiryTime()
        {
            return expiryTime;
        }

        public override int RandomVal()
        {
            return randomVal;
        }

        public override string ToString()
        {
            return String.Format("cap:{0}:{1}:{2}", issuerId, expiryTime, randomVal);
        }

        internal static bool Expired(HomeOS.Hub.Platform.Views.VCapability capability)
        {
            return capability.ExpiryTime() < DateTime.Now;
        }
    }
}