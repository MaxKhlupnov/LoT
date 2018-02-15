 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn;
using System.Net;
using System.Threading;
using System.Drawing;
using HomeOS.Hub.Common;
using HomeOS.Hub.Common.WebCam.WebCamWrapper.Camera;
using HomeOS.Hub.Platform.Views;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace HomeOS.Hub.Drivers.Kinect
{

    [System.AddIn.AddIn("HomeOS.Hub.Drivers.Kinect", Version = "1.0.0.0")]
    public class DriverKinect : ModuleBase
    {
        Port kinectPort;
        private KinectSensor sensor;
        KinectAudioSource audioSource;
        SafeThread worker = null;

        //only turn these on if someone asks for them because they suck up memory
        bool skeletonStreamOn = false;
        bool depthStreamOn = false;

        /// <summary>
        /// Speech recognition engine, preferences Kinect for audio data
        /// </summary>
        private SpeechRecognitionEngine speechEngine = null;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        //private WriteableBitmap depthBitmap; //private WriteableBitmap colorBitmap, 

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        //private byte[] depthByte; //colorPixels, 
        // private DepthImagePixel[] depthPixels;

        byte[] _lastColorBytes = new byte[0];
        byte[] _lastDepthBytes = new byte[0];
        string _lastSkeletonArray = null;
        short[] _lastDepthArray = new short[0];

        string lastAudioClipName;

        public override void Start()
        {


            string kinectStr = moduleInfo.Args()[0];

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    //encode the id string as per the encoding in the scout so the match will work 
                    // '\' and '&' cause problem when the values get written to the config file (XML writer not happy)
                    string kinectid = "Kinect Sensor:" + potentialSensor.UniqueKinectId.Replace("\\", "-").Replace("&", ".");
                    if (kinectStr == kinectid)
                    { 
                    this.sensor = potentialSensor;
                    break;
                    }
                }
            }

            //Create a new port on the platform.
            
            VPortInfo pInfo = GetPortInfoFromPlatform("kinect" + kinectStr);
            List<VRole> roles = new List<VRole>() { RoleCamera.Instance, RoleDepthCam.Instance, RoleMicrophone.Instance, RoleSkeletonTracker.Instance, RoleSpeechReco.Instance };

            kinectPort = InitPort(pInfo);
            BindRoles(kinectPort, roles, OnOperationInvoke);

            RegisterPortWithPlatform(kinectPort);

     
            logger.Log("Kinect Sensor Registered.");
            StartKinect();
        }

        private void RecAudio(int recLength)
        {

            if (!Directory.Exists("..\\KinectAudios\\"))
            {
                DirectoryInfo di = Directory.CreateDirectory("..\\KinectAudios\\");
                logger.Log("New directory created to store audio files at {0}.", di.FullName);
            }

            lastAudioClipName = "..\\KinectAudios\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_wav.wav";
            worker = new SafeThread(delegate()
            {
                AudioRecording(recLength);
            }, "DriverKinect-RecAudio", logger);
            worker.Start();
            logger.Log("Audio Recording Started.");
        }




        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            List<VParamType> ret = new List<VParamType>();

            // Allocate space to put the pixels we'll receive
            byte[] colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

            // This is the bitmap we'll display on-screen
            WriteableBitmap colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);


            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(colorPixels);

                    // Write the pixel data into our bitmap
                    colorBitmap.WritePixels(
                       new Int32Rect(0, 0,
                           colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                       colorPixels,
                       colorBitmap.PixelWidth * sizeof(int),
                       0);

                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(colorBitmap));
                    using (MemoryStream stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        _lastColorBytes = stream.ToArray();
                    }

                    //For subscribed apps
                    ret.Add(new ParamType(ParamType.SimpleType.jpegimage, "byteImg", _lastColorBytes));
                    kinectPort.Notify(RoleCamera.RoleName, RoleCamera.OpGetVideo, ret);
                }
            }
        }





        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            List<VParamType> ret = new List<VParamType>();
            List<VParamType> ret_array = new List<VParamType>();

            // Allocate space to put the pixels we'll receive
            DepthImagePixel[] depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
            // Allocate space to put the color pixels we'll create
            byte[] depthByte = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
            // This is the bitmap we'll display on-screen
            WriteableBitmap depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);


            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(depthPixels);

                    // Get the min and max reliable depth for the current frame
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;
                    short[] depthArray = new short[depthPixels.Length];


                    // Convert the depth to RGB
                    int colorPixelIndex = 0;
                    for (int i = 0; i < depthPixels.Length; ++i)
                    {
                        // Get the depth for this pixel
                        short depth = depthPixels[i].Depth;
                        depthArray.SetValue(depth, i);

                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).

                        // Note: Using conditionals in this loop could degrade performance.
                        // Consider using a lookup table instead when writing production code.
                        // See the KinectDepthViewer class used by the KinectExplorer sample
                        // for a lookup table example.
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                        // Write out blue byte
                        depthByte[colorPixelIndex++] = intensity;

                        // Write out green byte
                        depthByte[colorPixelIndex++] = intensity;

                        // Write out red byte                        
                        depthByte[colorPixelIndex++] = intensity;

                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }

                    // Write the pixel data into our bitmap
                    depthBitmap.WritePixels(
                        new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                        depthByte,
                        depthBitmap.PixelWidth * sizeof(int),
                        0);

                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(depthBitmap));
                    using (MemoryStream stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        _lastDepthBytes = stream.ToArray();
                    }

                    //For invocation apps
                    _lastDepthArray = depthArray;

                    //For subscription apps
                    ret.Add(new ParamType(ParamType.SimpleType.jpegimage, "byteImg", _lastDepthBytes));
                    kinectPort.Notify(RoleDepthCam.RoleName, RoleDepthCam.OpRcvDepthStreamName, ret);

                    ret_array.Add(new ParamType(ParamType.SimpleType.list, "depthArray", depthArray));
                    kinectPort.Notify(RoleDepthCam.RoleName, RoleDepthCam.OpRcvDepthArrayName, ret_array);

                }
            }
        }


        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];
            List<VParamType> ret = new List<VParamType>();
            string retSkeletons = null;

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);

                    foreach (Skeleton skeleton in skeletons)
                    {
                        retSkeletons += "skeleton ID: " + skeleton.TrackingId.ToString() + "\n";
                        foreach (Joint joint in skeleton.Joints)
                        {
                            retSkeletons += "Joint type: " + joint.JointType.ToString() + ", Joint Position: (" + joint.Position.X.ToString() + ", " + joint.Position.Y.ToString() + ", " + joint.Position.Z.ToString() + "), Joint Tracking State: " + joint.TrackingState.ToString() + ".\n";
                        }
                    }

                    _lastSkeletonArray = retSkeletons;

                    ret.Add(new ParamType(ParamType.SimpleType.text, "skeletonArray", retSkeletons));
                    kinectPort.Notify(RoleSkeletonTracker.RoleName, RoleSkeletonTracker.OpRcvSkeletonStreamName, ret);
                }
            }


        }

        private void StartKinect()
        {
            if (sensor != null)
            {

                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;
   
                //NOTE- skeleton and depth streams are only turned on if app calls for them
              
                try
                {
                    this.sensor.Start();
                    logger.Log("Kinect started.");
                }
                catch (IOException)
                {
                    this.sensor = null;
                    logger.Log("Cannot start the kinect.");
                }

                //Start SpeechRecognition, for now always starts
                //AJB : Remove if only want to application registers a phrase - see  in OnOperationInvoke see case RoleSpeechReco.OpSetSpeechPhraseName:
                if (speechEngine == null)
                {
                    SetupSpeechRecognition();
                }
            }
        }

        private void startDepthStream() {            
            // Turn on the depth stream to receive depth frames
             this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
           // Add an event handler to be called whenever there is new color frame data
            this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
            depthStreamOn = true;
        }

        private void startSkeletonStream()
        {
            // Turn on the skeleton stream to receive skeleton frames
            this.sensor.SkeletonStream.Enable();
            // Add an event handler to be called whenever there is new color frame data
            this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
            skeletonStreamOn = true;

        }

        private void StopKinect()
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    sensor.Stop();
                }
            }
            logger.Log("Kinect stopped.");
        }




        private void AudioRecording(int recLength)
        {
            audioSource = sensor.AudioSource;
            audioSource.EchoCancellationMode = EchoCancellationMode.CancellationOnly;
            audioSource.AutomaticGainControlEnabled = false;
            audioSource.BeamAngleMode = BeamAngleMode.Adaptive;

            var recordingLength = recLength * 2 * 16000;
            var buffer = new byte[1024];

            using (var fileStream = new FileStream(lastAudioClipName, FileMode.Create))
            {
                WriteWavHeader(fileStream, recordingLength);

                //Start capturing audio                               
                using (var audioStream = audioSource.Start())
                {
                    //Simply copy the data from the stream down to the file
                    int count, totalCount = 0;
                    while ((count = audioStream.Read(buffer, 0, buffer.Length)) > 0 && totalCount < recordingLength)
                    {
                        fileStream.Write(buffer, 0, count);
                        totalCount += count;
                    }
                    logger.Log("Writing to audio file {0} completed.", lastAudioClipName);
                }
                audioSource.Stop();
            }
        }


        static void WriteWavHeader(Stream stream, int dataLength)
        {

            using (MemoryStream memStream = new MemoryStream(64))
            {
                int cbFormat = 18; //sizeof(WAVEFORMATEX)
                WAVEFORMATEX format = new WAVEFORMATEX()
                {
                    wFormatTag = 1,
                    nChannels = 1,
                    nSamplesPerSec = 16000,
                    nAvgBytesPerSec = 32000,
                    nBlockAlign = 2,
                    wBitsPerSample = 16,
                    cbSize = 0
                };
                using (var bw = new BinaryWriter(memStream))
                {
                    //RIFF header
                    WriteString(memStream, "RIFF");
                    bw.Write(dataLength + cbFormat + 4); //File size - 8
                    WriteString(memStream, "WAVE");
                    WriteString(memStream, "fmt ");
                    bw.Write(cbFormat);
                    //WAVEFORMATEX
                    bw.Write(format.wFormatTag);
                    bw.Write(format.nChannels);
                    bw.Write(format.nSamplesPerSec);
                    bw.Write(format.nAvgBytesPerSec);
                    bw.Write(format.nBlockAlign);
                    bw.Write(format.wBitsPerSample);
                    bw.Write(format.cbSize);
                    //data header
                    WriteString(memStream, "data");
                    bw.Write(dataLength);
                    memStream.WriteTo(stream);
                }
            }
        }
        static void WriteString(Stream stream, string s)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            Debug.Assert(bytes.Length == s.Length);
            stream.Write(bytes, 0, bytes.Length);
        }
        struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }


        public override void Stop()
        {
            if (worker != null)
                worker.Abort();
            logger.Log("Kinect Sensor Stopped.");

            StopKinect();
        }

        /// <summary>
        /// The demultiplexing routing for incoming
        /// </summary>
        /// <param name="message"></param>
        private List<VParamType> OnOperationInvoke(string roleName, String opName, IList<VParamType> parameters)
        {
            List<VParamType> ret = new List<VParamType>();
            int recLength = 1;

            switch (opName.ToLower())
            {
                case RoleMicrophone.OpRecAudioName:
                    lock (this)
                    {
                        recLength = (int)parameters[0].Value();
                        logger.Log("Driver Kinect received expected audio length: {0}.", recLength.ToString());
                        RecAudio(recLength);
                        logger.Log("Audio recording requested.");
                        if (lastAudioClipName != null)
                        {
                            ret.Add(new ParamType(ParamType.SimpleType.text, "audioFullPath", lastAudioClipName));
                        }
                    }
                    break;
                case RoleCamera.OpGetImageName:
                    if (_lastColorBytes != null)
                    {
                        ret.Add(new ParamType(ParamType.SimpleType.jpegimage, "byteImg", _lastColorBytes));
                    }
                    break;
                case RoleDepthCam.OpGetLastDepthImgName:

                    if (!depthStreamOn)  //if someone asks for it turn it on
                        startDepthStream();

                    if (_lastDepthBytes != null)
                    {
                        ret.Add(new ParamType(ParamType.SimpleType.jpegimage, "byteImg", _lastDepthBytes));
                    }
                    break;
                case RoleDepthCam.OpGetLastDepthArrayName:
                    if (!depthStreamOn)  //if someone asks for it turn it on
                        startDepthStream();

                    if (_lastDepthArray != null)
                    {
                        ret.Add(new ParamType(ParamType.SimpleType.list, "depthArray", _lastDepthArray));
                    }
                    break;
                case RoleSkeletonTracker.OpGetLastskeletonName:

                    if (!skeletonStreamOn)
                        startSkeletonStream();

                    if (_lastSkeletonArray != null)
                    {
                        ret.Add(new ParamType(ParamType.SimpleType.text, "skeletonArray", _lastSkeletonArray));
                    }
                    break;

                case RoleSpeechReco.OpSetSpeechPhraseName:
                    if (speechEngine == null)
                    {
                        SetupSpeechRecognition();
                    }

                    //string phrase = (string)parameters[0].Value();
                    //string valueStr = (string)parameters[1].Value();
                    //if not in the list of ports to notify when something is recognized
                    //add this phrase to the grammar

                    break;

             
                default:
                    logger.Log("Unhandled kinect operation {0}", opName);
                    break;
            }

            return ret;
        }



        public override string GetDescription(string hint)
        {
            return "Kinect";
        }

        public override string ToString()
        {
            return "DriverKinect-" + String.Join(",", moduleInfo.Args());
        }

        public override void PortRegistered(VPort port) { }
        public override void PortDeregistered(VPort port) { }

        #region SpeechRecognition

       
        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        private bool SetupSpeechRecognition()
        {
            RecognizerInfo ri = GetKinectRecognizer();

            if (null == ri)
                return false;  //didn't work

            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                //  Create grammar programmatically rather than from a grammar file.              
                var directions = new Choices();
                directions.Add(new SemanticResultValue("all on", "ALLON"));
                directions.Add(new SemanticResultValue("all off", "ALLOFF"));
                directions.Add(new SemanticResultValue("good night", "ALLOFF"));
                directions.Add(new SemanticResultValue("good morning", "ALLON"));
                directions.Add(new SemanticResultValue("play movie", "PLAYMOVIE"));
                directions.Add(new SemanticResultValue("go disco", "DISCO"));
                //directions.Add(new SemanticResultValue("turn left", "LEFT"));
                //directions.Add(new SemanticResultValue("turn right", "RIGHT"));

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);

                var g = new Grammar(gb);
                speechEngine.LoadGrammar(g);

                speechEngine.SpeechRecognized += SpeechRecognized;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                speechEngine.SetInputToAudioStream(
                    sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);

            }

            return true;
        }

        private void UpdateSpeechGrammar()
        {


        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.4;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                //Notify subscribed ports for any recognized phrase - possibly later keep dictionary of port->phrases and  they wanted
                string semValue = e.Result.Semantics.Value.ToString();
                Notify(kinectPort, RoleSpeechReco.Instance, RoleSpeechReco.OpPhraseRecognizedSubName, new ParamType(ParamType.SimpleType.text, semValue));

            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            logger.Log("Kinect Driver:speech rejected");
        }




        #endregion

    }

}