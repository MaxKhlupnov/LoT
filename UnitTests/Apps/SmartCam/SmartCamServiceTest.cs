using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Web;
using HomeOS.Hub.Apps.SmartCam;
using HomeOS.Hub.Platform;
using HomeOS.Hub.Common;
using System.Collections.Generic;

namespace HomeOS.Hub.UnitTests.Apps.SmartCam
{
    //public interface ISimplexSmartCamContract
    //{
    //byte[] GetImage(string cameraFriendlyName);

    //string GetWebImage(string cameraFriendlyName);

    //void ControlCamera(string control, string cameraFriendlyName);

    //string[] GetCameraList(string username, string password);

    //void RecordVideo(string cameraFriendlyName, bool enable);

    //void EnableMotionTrigger(string cameraFriendlyName, bool enable);

    //bool IsMotionedTriggerEnabled(string cameraFriendlyName);

    //string[] GetRecordedCamerasList(string username, string password);

    //int GetRecordedClipsCount(string cameraFriendlyName);

    //string [] GetRecordedClips(string cameraFriendlyName, int countMax);

    //int GetClipTriggerImagesCount(string clipUrl);

    //string[] GetClipTriggerImages(string clipUrl, int countMax);

    //void SendEmail(string toAddress, string subject, string body, string[] attachmentUrls);
    //}

    [TestClass]
    public class SmartCamServiceTest
    {
        private Uri baseUri;
        private Logger logger;
        private HomeOS.Hub.Platform.Platform platform;
        HomeOS.Hub.Apps.SmartCam.SmartCam smartCam;

        [TestMethod]
        public void ISmartCamServiceTest_TestRecordedClipsAccessMethods()
        {
            ISmartCamContract channelSmartCam;

            try
            {
                WebChannelFactory<ISmartCamContract> cf = new WebChannelFactory<ISmartCamContract>(this.baseUri);
                channelSmartCam = cf.CreateChannel();
            }
            catch (CommunicationException ex)
            {
                throw new AssertFailedException("An exception occurred: " + ex.Message);
            }

            List<string> cameraArray = channelSmartCam.GetRecordedCamerasList();

            Assert.IsTrue(cameraArray.Count == 3);  //2 cameras + one status

            Assert.IsTrue(cameraArray[0] == String.Empty);
            Assert.IsTrue(cameraArray[1] == "foscam-157.54.148.65 - foscamdriver2");
            Assert.IsTrue(cameraArray[2] == "webcam - webcamdriver");
            Assert.IsTrue(int.Parse(channelSmartCam.GetRecordedClipsCount(cameraArray[1])[1]) == 3);
            Assert.IsTrue(int.Parse(channelSmartCam.GetRecordedClipsCount(cameraArray[2])[1]) == 2);

            List<string> clipUrlsArray1 = channelSmartCam.GetRecordedClips(cameraArray[1], 3);
            Assert.IsTrue(clipUrlsArray1.Count == 4);
            Assert.IsTrue(clipUrlsArray1[1] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-5_2.mp4", Helpers.GetLocalHostIpAddress()));
            Assert.IsTrue(clipUrlsArray1[2] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-5.mp4", Helpers.GetLocalHostIpAddress()));
            Assert.IsTrue(clipUrlsArray1[3] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-4.mp4", Helpers.GetLocalHostIpAddress()));

            List<string> clipUrlsArray2 = channelSmartCam.GetRecordedClips(cameraArray[1], 2);
            Assert.IsTrue(clipUrlsArray2.Count == 3);
            Assert.IsTrue(clipUrlsArray2[1] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-5_2.mp4", Helpers.GetLocalHostIpAddress()));
            Assert.IsTrue(clipUrlsArray2[2] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-5.mp4", Helpers.GetLocalHostIpAddress()));

            List<string> clipUrlsArray3 = channelSmartCam.GetRecordedClips(cameraArray[2], 2);
            Assert.IsTrue(clipUrlsArray3.Count == 3);
            Assert.IsTrue(clipUrlsArray3[1] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/webcam - webcamdriver/2013-2-12/15-21.mp4", Helpers.GetLocalHostIpAddress()));
            Assert.IsTrue(clipUrlsArray3[2] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/webcam - webcamdriver/2013-2-12/15-20.mp4", Helpers.GetLocalHostIpAddress()));

        }

        [TestInitialize]
        public void StartCamService()
        {
            // force garbage collection
            GC.Collect();
            
            //this now occurs in the constuctor of platform
            //Globals.Initialize();

            this.platform = new HomeOS.Hub.Platform.Platform(new[] { "-r", "unittesting" });
            this.platform.Start();
            int moduleCount = platform.GetRunningModules().Count;
            Assert.IsTrue(moduleCount == 0);

            // ensure that the test data recorded video files have the write times for our test
            // this is necessary since the time stamp on the data files changes whenever you sync from source control

            Helpers.FixFileTime(Environment.CurrentDirectory + "\\..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\foscam-157.54.148.65 - foscamdriver2\\2013-2-12\\15-4.mp4", new DateTime(2013, 2, 12, 15, 4, 0, DateTimeKind.Local));
            Helpers.FixFileTime(Environment.CurrentDirectory + "\\..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\foscam-157.54.148.65 - foscamdriver2\\2013-2-12\\15-5.mp4", new DateTime(2013, 2, 12, 15, 5, 0, DateTimeKind.Local));
            Helpers.FixFileTime(Environment.CurrentDirectory + "\\..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\foscam-157.54.148.65 - foscamdriver2\\2013-2-12\\15-5_2.mp4", new DateTime(2013, 2, 12, 15, 5, 2, DateTimeKind.Local));
            Helpers.FixFileTime(Environment.CurrentDirectory + "\\..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\webcam - webcamdriver\\2013-2-12\\15-20.mp4", new DateTime(2013, 2, 12, 15, 20, 0, DateTimeKind.Local));
            Helpers.FixFileTime(Environment.CurrentDirectory + "\\..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\webcam - webcamdriver\\2013-2-12\\15-21.mp4", new DateTime(2013, 2, 12, 15, 21, 0, DateTimeKind.Local));

            this.smartCam = new HomeOS.Hub.Apps.SmartCam.SmartCam();
            this.logger = new Logger();
            this.smartCam.Initialize(platform, logger, new ModuleInfo("SmartCamApp", "AppSmartCam", "HomeOS.Hub.Apps.SmartCam", "..\\..\\Apps\\SmartCam\\Data\\SmartCamApp", false), 0);
            this.smartCam.Start();


            string homeId = smartCam.GetConfSetting("HomeId");
            string homeIdPart = string.Empty;
            if (!string.IsNullOrEmpty(homeId))
            {
                homeIdPart = "/" + homeId;
            }

            this.baseUri = new Uri(smartCam.GetInfo().BaseURL() + "/webapp");
        }

        [TestCleanup]
        public void StopCameraService()
        {
            this.smartCam.Stop();
            this.logger.Close();
            this.platform.Dispose();
        }
    }
}
