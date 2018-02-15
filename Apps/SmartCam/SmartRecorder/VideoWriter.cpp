// This is the main DLL file.

#include "stdafx.h"

#include "VideoWriter.h"

using namespace System;
using namespace System::Runtime::InteropServices;

template <class T> void SafeRelease(T **ppT)
{
	if (*ppT)
	{
		(*ppT)->Release();
		*ppT = NULL;
	}
}

// Format constants
const GUID   VIDEO_ENCODING_FORMAT_MP4 = MFVideoFormat_H264; 
const GUID   VIDEO_INPUT_FORMAT = MFVideoFormat_RGB24;

namespace SmartRecorder
{

	HRESULT VideoWriter::AddFrame(
		byte * frameBytes,
		int buffSize, 
		int frameWidth,
		int frameHeight,
		LONGLONG rtStart        // Time stamp.
		)
	{
		IMFSample *pSample = NULL;
		IMFMediaBuffer *pBuffer = NULL;
		BYTE *pData = NULL;

		HRESULT hr = S_OK;

		//proceed only if frame dimensions match video dimensions
		if (frameWidth != _videoWidth || frameHeight != _videoHeight) 
		{
			return -1;
		}

		// Create a new memory buffer.
		hr = MFCreateMemoryBuffer(buffSize, &pBuffer);

		//copy the pixels to a frame buffer
		if (SUCCEEDED(hr))
		{
			hr = pBuffer->Lock(&pData, NULL, NULL);
		}
		if (SUCCEEDED(hr))
		{
			memcpy_s(pData, buffSize, frameBytes, buffSize);
			////memcpy(pData, frameBytes, 3*VIDEO_WIDTH*VIDEO_HEIGHT);
			//for (int i = 0; i < 3*VIDEO_WIDTH*VIDEO_HEIGHT; i++) {
			//	pData[i] = 0; //
			//	byte j = frameBytes[i];
			//}
		}
		if (SUCCEEDED(hr))
		{
			hr = pBuffer->Unlock();
		}

		// Set the data length of the buffer.
		if (SUCCEEDED(hr))
		{
			hr = pBuffer->SetCurrentLength(buffSize);
		}

		// Create a media sample and add the buffer to the sample.
		if (SUCCEEDED(hr))
		{
			hr = MFCreateSample(&pSample);
		}
		if (SUCCEEDED(hr))
		{
			hr = pSample->AddBuffer(pBuffer);
		}

		// Set the time stamp and the duration.
		if (SUCCEEDED(hr))
		{
			hr = pSample->SetSampleTime(rtStart);
		}
		//if (SUCCEEDED(hr))
		//{
		//	hr = pSample->SetSampleDuration(rtDuration);
		//}

		// Send the sample to the Sink Writer.
		if (SUCCEEDED(hr))
		{
			hr = _pSinkWriter->WriteSample(_streamIndex, pSample);
		}

		SafeRelease(&pSample);
		SafeRelease(&pBuffer);

		return hr;
	}

	HRESULT VideoWriter::Init(
					String^ outfileName,
					int videoWidth,
					int videoHeight,
					int videoFPSNum,
					int videoFPSDen,
					int videoBitRate
					)
	{
		IMFSinkWriter   *pSinkWriter = NULL;
		IMFMediaType    *pMediaTypeOut = NULL;   
		IMFMediaType    *pMediaTypeIn = NULL;   
		DWORD           streamIndex;     

		HRESULT hr = S_OK;

		hr = MFStartup(MF_VERSION);

		if (SUCCEEDED(hr)) 
		{
			pin_ptr<const wchar_t> outfileNamePtr = PtrToStringChars(outfileName);

			MFCreateSinkWriterFromURL(outfileNamePtr, NULL, NULL, &pSinkWriter);
		}

		// Set the output media type.
		if (SUCCEEDED(hr))
		{
			hr = MFCreateMediaType(&pMediaTypeOut);   
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeOut->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);     
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeOut->SetGUID(MF_MT_SUBTYPE, VIDEO_ENCODING_FORMAT_MP4);   
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeOut->SetUINT32(MF_MT_AVG_BITRATE, videoBitRate);   
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeOut->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);   
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeSize(pMediaTypeOut, MF_MT_FRAME_SIZE, videoWidth, videoHeight);   
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeRatio(pMediaTypeOut, MF_MT_FRAME_RATE, videoFPSNum, videoFPSDen);   
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeRatio(pMediaTypeOut, MF_MT_PIXEL_ASPECT_RATIO, 1, 1);   
		}
		if (SUCCEEDED(hr))
		{
			hr = pSinkWriter->AddStream(pMediaTypeOut, &streamIndex);   
		}

		// Set the input media type.
		if (SUCCEEDED(hr))
		{
			hr = MFCreateMediaType(&pMediaTypeIn);   
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeIn->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);   
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeIn->SetGUID(MF_MT_SUBTYPE, VIDEO_INPUT_FORMAT);     
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeIn->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);   
		}
		if (SUCCEEDED(hr))
		{
			hr = pMediaTypeIn->SetUINT32(MF_MT_VIDEO_ROTATION, MFVideoRotationFormat_180);
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeSize(pMediaTypeIn, MF_MT_FRAME_SIZE, videoWidth, videoHeight);   
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeRatio(pMediaTypeIn, MF_MT_FRAME_RATE, videoFPSNum, videoFPSDen);   
		}
		if (SUCCEEDED(hr))
		{
			hr = MFSetAttributeRatio(pMediaTypeIn, MF_MT_PIXEL_ASPECT_RATIO, 1, 1);   
		}
		if (SUCCEEDED(hr))
		{
			hr = pSinkWriter->SetInputMediaType(streamIndex, pMediaTypeIn, NULL);   
		}

		// Tell the sink writer to start accepting data.
		if (SUCCEEDED(hr))
		{
			hr = pSinkWriter->BeginWriting();
		}

		// Return the pointer to the caller.
		if (SUCCEEDED(hr))
		{
			_pSinkWriter = pSinkWriter;
			(_pSinkWriter)->AddRef();

			_pMediaTypeIn = pMediaTypeIn;
			(_pMediaTypeIn)->AddRef();

			_streamIndex = streamIndex;
			_videoWidth = videoWidth;
			_videoHeight = videoHeight;
		}

		SafeRelease(&pSinkWriter);
		SafeRelease(&pMediaTypeOut);
		SafeRelease(&pMediaTypeIn);
		return hr;
	}

	HRESULT VideoWriter::Done() 
	{

		HRESULT hr = S_OK;

		if (NULL != _pSinkWriter)
		{
			hr = _pSinkWriter->Finalize();
		}

		IMFSinkWriter * pSinkWriter = _pSinkWriter;
		SafeRelease(&pSinkWriter);

		IMFMediaType * pMediaTypeIn = _pMediaTypeIn;
		SafeRelease(&pMediaTypeIn);

		MFShutdown();

		return hr;
	}

}