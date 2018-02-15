// VideoWriter.h

#pragma once

using namespace System;

namespace SmartRecorder {

	public ref class VideoWriter
	{
	private:
        IMFSinkWriter * _pSinkWriter;
		DWORD _streamIndex;
		IMFMediaType * _pMediaTypeIn;

		int _videoWidth;
		int _videoHeight;

	public: 
	HRESULT Init(
				String^ outfileName,
				int videoWidth,
				int videoHeight,
				int videoFPSNum,
				int videoFPSDen,
				int videoBitRate
				);
		HRESULT AddFrame(Byte * frameBytes, int buffSize, int frameWidth, int frameHeight, LONGLONG rtSample);
		HRESULT Done();
	};
}
