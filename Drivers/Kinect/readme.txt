 The Kinect sensor offers the capability of taking color images, detecting depth information, recording audio and tracking skeletons within the captured frames.This information is sent from the Kinect to LoT driver as different types of frames. 
For color, depth and skeleton, the LoT driver offers the capability of retrieving the last frame (invocation) and all future frames (subscription) operations. 
The color images are returned in JPEG byte array. The depth frame can be returned in the format of JPEG image or depth array. Audio recording returns the path where the recorded WAV file will be stored. Skeleton returns a string with all tracked/untracked skeleton ID, name, position(X,Y,Z) and tracking status.
For the LoT driver to run correctly, you need the Kinect for Windows sensor and please ensure that the Kinect SDK (http://www.microsoft.com/en-us/kinectforwindows/develop/developer-downloads.aspx) is installed. 
Note “System.xaml” needs to be referenced in the Kinect Driver C# project. It can be loaded from (C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Xaml.dll) if you cannot find it in the list of references from the Visual Studio dialog.


Some changes were made to the Common and Platform along with this module: 
\Hub\Common\Roles.cs 
\Hub\Common\ParamType.cs
\Hub\Platform\Adapters\AParamType.cs
\Hub\Platform\Contracts\IParamType.cs
\Hub\Platform\Views\VParamType.cs

To get the Driver running: 
	• Add the Hub\Drivers\Kinect project to the solution
	• Add the Hub\Scout\Kinect project to the solution
	• Add a scout entry in Scouts.xml config file:  
	• <Scout Name="HomeOS.Hub.Scouts.Kinect" DllName="HomeOS.Hub.Scouts.Kinect.dll"/>
	• Build the solution

Once built successfully you can test it out with the CameraViewer app, because the Kinect driver exposes a camera role. 