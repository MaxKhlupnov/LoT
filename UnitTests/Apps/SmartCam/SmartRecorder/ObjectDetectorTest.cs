using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartRecorder;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HomeOS.Hub.UnitTests.Apps.SmartCam.SmartRecorder
{
    [TestClass]
    public class ObjectDetectorTest
    {
        const string DataFilesPath_640x480 = "..\\..\\Apps\\SmartCam\\SmartRecorder\\Data\\640x480";
        const string ObjectDetectConfigFilePath = "..\\..\\Apps\\SmartCam\\SmartRecorder\\Data\\objectdetectparam.txt";
        const string TestOutputFilesPath = "..\\..\\Apps\\SmartCam\\SmartRecorder\\Output\\ObjectDetectorTest\\DetectObjects_640x480";

        Rectangle[] ObjectDetectResultArray = 
                    { 
                        new Rectangle(189, 10, 450, 469), /* 0 */
                        new Rectangle(60, 56, 579, 414),  /* 1 */ 
                        new Rectangle(80, 56, 559, 413),  /* 2 */
                        new Rectangle(107, 56, 532, 414), /* 3 */
                        new Rectangle(80, 10, 559, 451),  /* 4 */
                        new Rectangle(50, 10, 589, 448),  /* 5 */
                        new Rectangle(0, 56, 639, 405),   /* 6 */
                        new Rectangle(201, 56, 438, 403)  /* 7 */
                    };

        [TestMethod]
        public void ObjectDetectorTest_DetectObjects_640x480()
        {
            ObjectDetector objectDetector = new ObjectDetector();

            string TestOutputDirectoryQualified = Directory.GetCurrentDirectory() + "\\" + TestOutputFilesPath;
            if (!Directory.Exists(TestOutputDirectoryQualified))
            {
                Directory.CreateDirectory(TestOutputDirectoryQualified);
            }

            var filepaths = Directory.EnumerateFiles(Directory.GetCurrentDirectory() + "\\" + DataFilesPath_640x480, "*.jpg", SearchOption.TopDirectoryOnly);
            DateTime dateTimeNow = DateTime.Now;
            long startTimeTicks = dateTimeNow.Ticks;

            DetectObjectsUsingImageCount(objectDetector, filepaths);
        }

        private void DetectObjectsUsingImageCount(ObjectDetector objectDetector, IEnumerable<string> filepaths)
        {
            List<Rectangle> rectObjectList = new List<Rectangle>();
            List<string> filepathsList = new List<string>(filepaths);
            int count = filepathsList.Count;
            filepathsList.Sort();
            for (int i = 0; i < filepathsList.Count; ++i)
            {
                string filepath = filepathsList[i];
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
                    if (!objectDetector.IsInitialized())
                    {
                        IntPtr ptrPath = IntPtr.Zero;
                        try
                        {
                            ptrPath = Marshal.StringToHGlobalAnsi(ObjectDetectConfigFilePath);

                            objectDetector.InitializeFromFrame((byte*)ptr, GetBitmapSizeInBytes(bmpData), bmpData.Width, bmpData.Height, (sbyte*)ptrPath);
                        }
                        finally
                        {
                            if (ptr != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(ptrPath);
                            }
                        }
                    }
                    else
                    {
                        Rectangle rectObject = objectDetector.GetObjectRect((byte*)ptr, GetBitmapSizeInBytes(bmpData));
                        byte[] binaryImageArray = new byte[image.Width * image.Height];
                        unsafe
                        {
                            fixed (byte* binaryImage = binaryImageArray)
                            {
                                objectDetector.GetBinaryImage(binaryImage);
                            }
                        }
                        string filenameAppend = String.Format("{0}", i + 1);
                        TextWriter binaryFileWriter = new StreamWriter(TestOutputFilesPath + "\\" + "binaryimage" + filenameAppend + ".txt");
                        for (int j = 0; j < image.Height; ++j)
                        {
                            for (int k = 0; k < image.Width; ++k)
                            {
                                if (binaryImageArray[k + j * image.Width] == 255)
                                {
                                    binaryFileWriter.Write("1");
                                }
                                else if (binaryImageArray[k + j * image.Width] == 0)
                                {
                                    binaryFileWriter.Write("0");
                                }
                                else
                                {
                                    Debug.Assert(false, "Should never happen");
                                }
                            }
                            binaryFileWriter.WriteLine("");
                        }
                        binaryFileWriter.Flush();
                        binaryFileWriter.Close();
                        rectObjectList.Add(rectObject);
                    }
                }

                image.UnlockBits(bmpData);

            }

            ValidateObjectRectsList(rectObjectList);
        }

        private void ValidateObjectRectsList(List<Rectangle> rectObjectList)
        {
            TextWriter ObjectRectsWriter = new StreamWriter(TestOutputFilesPath + "\\" + "objectrects" + rectObjectList.Count +  ".txt");
            for (int i = 0; i < rectObjectList.Count; ++i)
            {
                ObjectRectsWriter.WriteLine("Rect Object[{0}] X={1},Y={2},Width={3},Height={4}", i,
                        rectObjectList[i].X, rectObjectList[i].Y, rectObjectList[i].Width, rectObjectList[i].Height);
                Assert.IsTrue(ObjectDetectResultArray[i] == rectObjectList[i]);
            }
            ObjectRectsWriter.Flush();
            ObjectRectsWriter.Close();
        }

        private int GetBitmapSizeInBytes(BitmapData bmpData)
        {
            int bytePerPixel = -1;
            switch (bmpData.PixelFormat)
            {
                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                    {
                        bytePerPixel = 2;
                    }
                    break;
                case PixelFormat.Format24bppRgb:
                    {
                        bytePerPixel = 3;
                    }
                    break;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    {
                        bytePerPixel = 4;
                    }
                    break;
                case PixelFormat.Format48bppRgb:
                    {
                        bytePerPixel = 6;
                    }
                    break;
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    {
                        bytePerPixel = 8;
                    }
                    break;
                case PixelFormat.Format8bppIndexed:
                    {
                        bytePerPixel = 1;
                    }
                    break;
                default:
                    {
                        bytePerPixel = -1;
                    }
                    break;
            }
            if (-1 != bytePerPixel)
            {
                return bytePerPixel * bmpData.Width * bmpData.Height;
            }
            else
            {
                return -1;
            }
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

    }
}
