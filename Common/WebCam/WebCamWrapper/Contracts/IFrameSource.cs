using System;

namespace HomeOS.Hub.Common.WebCam.WebCamWrapper.Contracts
{
    public interface IFrameSource : ITouchlessAddIn
    {
        event Action<IFrameSource, Frame, double> NewFrame;

        void StartFrameCapture();
        void StopFrameCapture();
    }
}
