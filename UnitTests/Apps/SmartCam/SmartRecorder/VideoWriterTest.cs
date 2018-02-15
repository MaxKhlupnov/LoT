using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using HomeOS.Hub.Apps.SmartCam;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using HomeOS.Hub.UnitTests.MFHelper;
using SmartRecorder;

namespace HomeOS.Hub.UnitTests.Apps.SmartCam.SmartRecorder
{
    [TestClass]
    public class VideoWriterTest
    {
        const string DataFilesPath = "..\\..\\Apps\\SmartCam\\SmartRecorder\\Data\\640x480";
        const string TestOutputFilesPath = "..\\..\\Apps\\SmartCam\\SmartRecorder\\Output\\VideoWriterTest\\CreateTestWMVFile_640x480_24fps_15s";
        const double VIDEO_FPS = 24.0;
        const double VIDEO_FPS_VAR = 0.5; // tolerate variation of upto 0.5 fps
        const string MP4_FILENAME = "TestMP4File_640x480_24fps_15s.mp4";
        const int VIDEO_WIDTH = 640;
        const int VIDEO_HEIGHT = 480;
        const ulong VIDEO_DURATION_IN_100_NS = 15 * 10000000; // 15 secs
        const ulong VIDEO_DURATION_VAR_IN_100_NS = 900 * 100000; // we can tolerate 900 ms
        const int VIDEO_ENCODE_BITRATE = 240000;
        const int VIDEO_ENCODE_BITRATE_VAR = 24000;  // 10% toleraance

        [TestMethod]
        public void VideoWriterTest_CreateTestMP4File_640x480_24fps_15s()
        {

            VideoWriter videoWriter = new VideoWriter();

            string TestOutputDirectoryQualified = Directory.GetCurrentDirectory() + "\\" + TestOutputFilesPath;
            if (!Directory.Exists(TestOutputDirectoryQualified))
            {
                Directory.CreateDirectory(TestOutputDirectoryQualified);
            }

            videoWriter.Init(TestOutputDirectoryQualified + "\\" + MP4_FILENAME, VIDEO_WIDTH, VIDEO_HEIGHT, (int)VIDEO_FPS, 1, (int)VIDEO_ENCODE_BITRATE);

            var filepaths = Directory.EnumerateFiles(Directory.GetCurrentDirectory() + "\\" + DataFilesPath, "*.jpg", SearchOption.TopDirectoryOnly);
            DateTime dateTimeNow = DateTime.Now;
            long startTimeTicks = dateTimeNow.Ticks;

            do
            {
                foreach (string filepath in filepaths)
                {
                    byte[] imagebitArray = GetImageByteArray(filepath);

                    MemoryStream stream = new MemoryStream(imagebitArray);

                    Bitmap image = (Bitmap)Image.FromStream(stream);

                    //lets check if the image is what we expect
                    if (image.PixelFormat != PixelFormat.Format24bppRgb)
                    {
                        string message = String.Format("Image format from is not correct. PixelFormat: {1}", image.PixelFormat);
                        throw new Exception(message);
                    }

                    // Lock the bitmap's bits.  
                    Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
                    BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

                    // Get the address of the first line.
                    IntPtr ptr = bmpData.Scan0;

                    unsafe
                    {
                        videoWriter.AddFrame((byte *)ptr, 3*VIDEO_WIDTH*VIDEO_HEIGHT, VIDEO_WIDTH, VIDEO_HEIGHT, DateTime.Now.Ticks - startTimeTicks);
                    }
                    Thread.Sleep(new TimeSpan(0, 0, 0, 0, (int)(1000.0 / VIDEO_FPS)));

                    image.UnlockBits(bmpData);
                }

            } while (TimeSpan.Compare(new TimeSpan(DateTime.Now.Ticks - startTimeTicks), new TimeSpan(0, 0, (int)((double)VIDEO_DURATION_IN_100_NS/10000000.0))) < 0);
            
            videoWriter.Done();

            ValidateMP4OutputFile(TestOutputDirectoryQualified + "\\" + MP4_FILENAME);

        }

        private byte[] GetImageByteArray(string filepath)
        {
            FileStream fileStream = null;
            BinaryReader binaryReader = null;
            byte[] byteArray = null;

            try
            {
                fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);

                // attach filestream to binary reader
                binaryReader = new BinaryReader(fileStream);

                // get total byte length of the file
                long totalBytes = new FileInfo(filepath).Length;

                // read entire file into buffer
                byteArray = binaryReader.ReadBytes((Int32)totalBytes);
            }
            catch (Exception exception)
            {
                // Error

                Console.WriteLine("Exception failure: {0}", exception.ToString());
                byteArray = null;
                Assert.IsFalse(true);
            }
            finally
            {
                // close file and binary readers and free used memory
                fileStream.Close();
                fileStream.Dispose();
                fileStream = null;
                binaryReader.Close();
                binaryReader.Dispose();
                binaryReader = null;
            }

            return byteArray;
        }

        void ValidateMP4OutputFile(string mp4filepath)
        {
            ulong duration = 0;
            uint videoWidth = 0;
            uint videoHeight = 0;
            double videoFPS = 0.0;
            uint videoBitrate = 0;
            try
            {
                IMFMediaSource mediaSource = null;
                IMFSourceReader sourceReader = null;
                ulong videoSize = 0;
                ulong frameRate = 0;
                MFHelper.IMFMediaType mediaType = null;
                IMFPresentationDescriptor presentationDescriptor = null;
                uint objectType = default(uint);
                object objectSource = null;

                API.MFStartup();

                // Create the media source using source resolver and the input URL

                IMFSourceResolver sourceResolver = null;
                API.MFCreateSourceResolver(out sourceResolver);

                // sourceResolver.CreateObjectFromURL("..\\..\\Apps\\SmartCam\\SmartRecorder\\Output\\VideoWriterTest\\CreateTestWMVFile_640x480_24fps_15s\\TestMP4File_640x480_24fps_15s.mp4", Consts.MF_RESOLUTION_MEDIASOURCE, null, out objectType, out objectSource);
                sourceResolver.CreateObjectFromURL(mp4filepath, Consts.MF_RESOLUTION_MEDIASOURCE, null, out objectType, out objectSource);

                mediaSource = (IMFMediaSource)objectSource;

                API.MFCreateSourceReaderFromMediaSource(mediaSource, null, out sourceReader);

                mediaSource.CreatePresentationDescriptor(out presentationDescriptor);

                // Get the duration
                presentationDescriptor.GetUINT64(new Guid(Consts.MF_PD_DURATION), out duration);


                // Get the video width and height
                sourceReader.GetCurrentMediaType(0, out mediaType);

                mediaType.GetUINT64(Guid.Parse(Consts.MF_MT_FRAME_SIZE), out videoSize);

                videoWidth = (uint)(videoSize >> 32);
                videoHeight = (uint)(videoSize & 0x00000000FFFFFFFF);

                // Get the Frame Rate
                mediaType.GetUINT64(Guid.Parse(Consts.MF_MT_FRAME_RATE), out frameRate);

                if ((frameRate & 0x00000000FFFFFFFF) != 0)
                {
                    videoFPS = (double)(frameRate >> 32) / (double)(frameRate & 0x00000000FFFFFFFF);
                }

                // Get the encoding bitrate
                mediaType.GetUINT32(new Guid(Consts.MF_MT_AVG_BITRATE), out videoBitrate);

                API.MFShutdown();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception failure: {0}", exception.ToString());
                Assert.IsFalse(true);
            }

            Assert.IsFalse(Math.Abs((double)duration - (double)VIDEO_DURATION_IN_100_NS) > (double)VIDEO_DURATION_VAR_IN_100_NS);
            Assert.IsFalse(videoWidth != VIDEO_WIDTH);
            Assert.IsFalse(videoHeight != VIDEO_HEIGHT);
            Assert.IsFalse(Math.Abs(videoFPS - VIDEO_FPS) > VIDEO_FPS_VAR);
            Assert.IsFalse(Math.Abs((int)videoBitrate -VIDEO_ENCODE_BITRATE) > VIDEO_ENCODE_BITRATE_VAR);
        }
    }

}
