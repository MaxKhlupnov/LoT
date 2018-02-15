using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeOS.Hub.Apps.SmartCam;
using HomeOS.Hub.Platform;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.UnitTests.Apps.SmartCam
{
    [TestClass]
    public class SmartCamTest
    {
        [TestMethod]
        public void SmartCamTest_TestRecordedClipsAccessMethods()
        {
            // this now happens in the constructor of the platform
            //Globals.Initialize();

            using (HomeOS.Hub.Platform.Platform platform = new HomeOS.Hub.Platform.Platform(new[] {"-r", "unittesting"}))
            {
                platform.Start();
                int moduleCount = platform.GetRunningModules().Count;
                Assert.IsTrue(moduleCount == 0);

                // ensure that the test data recorded video files have the write times for our test
                // this is necessary since the time stamp on the data files changes whenever you sync from source control

                Helpers.FixFileTime("..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\foscam-157.54.148.65 - foscamdriver2\\2013-2-12\\15-4.mp4", new DateTime(2013, 2, 12, 15, 4, 0, DateTimeKind.Local));
                Helpers.FixFileTime("..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\foscam-157.54.148.65 - foscamdriver2\\2013-2-12\\15-5.mp4", new DateTime(2013, 2, 12, 15, 5, 0, DateTimeKind.Local));
                Helpers.FixFileTime("..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\foscam-157.54.148.65 - foscamdriver2\\2013-2-12\\15-5_2.mp4", new DateTime(2013, 2, 12, 15, 5, 2, DateTimeKind.Local));
                Helpers.FixFileTime("..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\webcam - webcamdriver\\2013-2-12\\15-20.mp4", new DateTime(2013, 2, 12, 15, 20, 0, DateTimeKind.Local));
                Helpers.FixFileTime("..\\..\\Apps\\SmartCam\\Data\\SmartCamApp\\videos\\webcam - webcamdriver\\2013-2-12\\15-21.mp4", new DateTime(2013, 2, 12, 15, 21, 0, DateTimeKind.Local));

                HomeOS.Hub.Apps.SmartCam.SmartCam smartCam = new HomeOS.Hub.Apps.SmartCam.SmartCam();
                Logger logger = new Logger();
                smartCam.Initialize(platform, logger, new ModuleInfo("SmartCamApp", "AppSmartCam", "HomeOS.Hub.Apps.SmartCam", "..\\..\\Apps\\SmartCam\\Data\\SmartCamApp", false), 0);
                smartCam.Start();
                string[] cameraArray = smartCam.GetRecordedCamerasList();

                Assert.IsTrue(cameraArray.Length == 2);
                Assert.IsTrue(cameraArray[0] == "foscam-157.54.148.65 - foscamdriver2");
                Assert.IsTrue(cameraArray[1] == "webcam - webcamdriver");
                Assert.IsTrue(smartCam.GetRecordedClipsCount(cameraArray[0]) == 3);
                Assert.IsTrue(smartCam.GetRecordedClipsCount(cameraArray[1]) == 2);

                string[] clipUrlsArray1 = smartCam.GetRecordedClips(cameraArray[0], 3);
                Assert.IsTrue(clipUrlsArray1.Length == 3);
                Assert.IsTrue(clipUrlsArray1[0] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-5_2.mp4", Helpers.GetLocalHostIpAddress()));
                Assert.IsTrue(clipUrlsArray1[1] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-5.mp4", Helpers.GetLocalHostIpAddress()));
                Assert.IsTrue(clipUrlsArray1[2] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-4.mp4", Helpers.GetLocalHostIpAddress()));

                string[] clipUrlsArray2 = smartCam.GetRecordedClips(cameraArray[0], 2);
                Assert.IsTrue(clipUrlsArray2.Length == 2);
                Assert.IsTrue(clipUrlsArray2[0] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-5_2.mp4", Helpers.GetLocalHostIpAddress()));
                Assert.IsTrue(clipUrlsArray2[1] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/foscam-157.54.148.65 - foscamdriver2/2013-2-12/15-5.mp4", Helpers.GetLocalHostIpAddress()));

                string[] clipUrlsArray3 = smartCam.GetRecordedClips(cameraArray[1], 2);
                Assert.IsTrue(clipUrlsArray3.Length == 2);
                Assert.IsTrue(clipUrlsArray3[0] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/webcam - webcamdriver/2013-2-12/15-21.mp4", Helpers.GetLocalHostIpAddress()));
                Assert.IsTrue(clipUrlsArray3[1] == String.Format("http://{0}:51430/DefaultHomeId/SmartCamApp/videos/webcam - webcamdriver/2013-2-12/15-20.mp4", Helpers.GetLocalHostIpAddress()));

                logger.Close();
                smartCam.Stop();
            }
        }
    }
}
