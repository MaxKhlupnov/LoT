using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace HomeOS.Hub.UnitTests.MFHelper
{

    internal static class CLSID
    {
        static CLSID()
        {
        }

        internal const string MFReadWriteClassFactory = "48e2ed0f-98c2-4a37-bed5-166312ddd83f";
        internal const string MFSinkWriter = "a3bbfb17-8273-4e52-9e0e-9739dc887990";
        internal const string MFSourceReader = "1777133c-0881-411b-a577-ad545f0714c4";
        internal const string MFSourceResolver = "90eab60f-e43a-4188-bcc4-e47fdf04868c";
    }

    internal static class IID
    {
        static IID()
        {
        }

        internal const string IMFAttributes = "2cd2d921-c447-44a7-a13c-4adabfc247e3";
        internal const string IMFClock = "2EB1E945-18B8-4139-9B1A-D5D584818530";
        internal const string IMFCollection = "5BC8A76B-869A-46A3-9B03-FA218A66AEBE";
        internal const string IMFMediaBuffer = "045FA593-8799-42B8-BC8D-8968C6453507";
        internal const string IMFMediaEvent = "DF598932-F10C-4E39-BBA2-C308F101DAA3";
        internal const string IMFMediaSession = "90377834-21D0-4DEE-8214-BA2E3E6C1127";
        internal const string IMFMediaSource = "279A808D-AEC7-40C8-9C6B-A6B492C78A66";
        internal const string IMFMediaType = "44AE0FA8-EA31-4109-8D2E-4CAE4997C555";
        internal const string IMFPMediaItem = "90EB3E6B-ECBF-45cc-B1DA-C6FE3EA70D57";
        internal const string IMFPMediaPlayer = "A714590A-58AF-430a-85BF-44F5EC838D85";
        internal const string IMFReadWriteClassFactory = "E7FE2E12-661C-40DA-92F9-4F002AB67627";
        internal const string IMFSample = "C40A00F2-B93A-4D80-AE8C-5A1C634F58E4";
        internal const string IMFSinkWriter = "3137f1cd-fe5e-4805-a5d8-fb477448cb3d";
        internal const string IMFSourceReader = "70ae66f2-c809-4e4f-8915-bdcb406b7993";
        internal const string IMFSourceResolver = "FBE5A32D-A497-4B61-BB85-97B1A848A6E3";
        internal const string IMFTopology = "83CF873A-F6DA-4BC8-823F-BACFD55DC433";
        internal const string IMFTopologyNode = "83CF873A-F6DA-4bc8-823F-BACFD55DC430";
        internal const string IMFTranscodeProfile = "4ADFDBA3-7AB0-4953-A62B-461E7FF3DA1E";
        internal const string IMFPMediaPlayerCallback = "766C8FFB-5FDB-4fea-A28D-B912996F51BD";
        internal const string IMFAsyncCallback = "a27003cf-2354-4f2a-8d6a-ab7cff15437e";
        internal const string IMFAsyncResult = "ac6b7889-0740-4d51-8619-905994a55cc6";
        internal const string IMFPresentationClock = "868CE85C-8EA9-4f55-AB82-B009A910A805";
        internal const string IMFPresentationDescriptor = "03CB2711-24D7-4DB6-A17F-F3A7A479A536";
        internal const string IMFStreamDescriptor = "56C03D9C-9DBB-45F5-AB4B-D80F47C05938";
        internal const string IMFMediaTypeHandler = "E93DCF6C-4B07-4E1E-8123-AA16ED6EADF5";
        internal const string IPropertyStore = "886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99";
    }

    internal static class Consts
    {
        static Consts()
        {
        }

        internal const string MFMediaType_Audio = "73647561-0000-0010-8000-00aa00389b71";
        internal const string MFMediaType_Video = "73646976-0000-0010-8000-00aa00389b71";
        internal const string MFMediaType_Protected = "7b4b6fe6-9d04-4494-be14-7e0bd076c8e4";
        internal const string MFMediaType_SAMI = "e69669a0-3dcd-40cb-9e2e-3708387c0616";
        internal const string MFMediaType_Script = "72178c22-e45b-11d5-bc2a-00b0d0f3f4ab";
        internal const string MFMediaType_Image = "72178c23-e45b-11d5-bc2a-00b0d0f3f4ab";
        internal const string MFMediaType_HTML = "72178c24-e45b-11d5-bc2a-00b0d0f3f4ab";
        internal const string MFMediaType_Binary = "72178c25-e45b-11d5-bc2a-00b0d0f3f4ab";
        internal const string MFMediaType_FileTransfer = "72178c26-e45b-11d5-bc2a-00b0d0f3f4ab";

        internal const string MFAudioFormat_AAC = "00001610-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_ADTS = "00001600-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_Dolby_AC3_SPDIF = "00000092-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_DRM = "00000009-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_DTS = "00000008-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_Float = "00000003-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_MP3 = "00000055-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_MPEG = "00000050-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_MSP1 = "0000000a-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_PCM = "00000001-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_WMASPDIF = "00000164-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_WMAudio_Lossless = "00000163-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_WMAudioV8 = "00000161-0000-0010-8000-00aa00389b71";
        internal const string MFAudioFormat_WMAudioV9 = "00000162-0000-0010-8000-00aa00389b71";

        internal const string MFTranscodeContainerType_ASF = "430f6f6e-b6bf-4fc1-a0bd-9ee46eee2afb";
        internal const string MFTranscodeContainerType_MPEG4 = "dc6cd05d-b9d0-40ef-bd35-fa622c1ab28a";
        internal const string MFTranscodeContainerType_MP3 = "e438b912-83f1-4de6-9e3a-9ffbc6dd24d1";
        internal const string MFTranscodeContainerType_3GP = "34c50167-4472-4f34-9ea0-c49fbacf037d";

        internal const string MFVideoFormat_ARGB32 = "00000021-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_RGB24 = "00000020-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_RGB32 = "00000022-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_RGB555 = "00000024-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_RGB565 = "00000023-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_AYUV = "56555941-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_NV11 = "3131564e-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_NV12 = "3231564e-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_UYVY = "59565955-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_YUY2 = "32595559-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_YV12 = "32315659-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_H264 = "34363248-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_WMV1 = "31564d57-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_WMV2 = "32564d57-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_WMV3 = "33564d57-0000-0010-8000-00aa00389b71";
        internal const string MFVideoFormat_WVC1 = "31435657-0000-0010-8000-00aa00389b71";

        internal const string MF_TOPONODE_MEDIASTART = "835c58ea-e075-4bc7-bcba-4de000df9ae6";
        internal const string MF_TOPONODE_MEDIASTOP = "835c58eb-e075-4bc7-bcba-4de000df9ae6";
        internal const string MF_TOPONODE_MARKIN_HERE = "494bbd00-b031-4e38-97c4-d5422dd618dc";
        internal const string MF_TOPONODE_MARKOUT_HERE = "494bbd01-b031-4e38-97c4-d5422dd618dc";

        internal const string MF_TOPOLOGY_PROJECTSTART = "7ed3f802-86bb-4b3f-b7e4-7cb43afd4b80";
        internal const string MF_TOPOLOGY_PROJECTSTOP = "7ed3f803-86bb-4b3f-b7e4-7cb43afd4b80";
        internal const string MF_TOPOLOGY_NO_MARKIN_MARKOUT = "7ed3f804-86bb-4b3f-b7e4-7cb43afd4b80";
        internal const string MF_TOPOLOGY_DXVA_MODE = "1e8d34f6-f5ab-4e23-bb88-874aa3a1a74d";
        internal const string MF_TOPOLOGY_STATIC_PLAYBACK_OPTIMIZATIONS = "b86cac42-41a6-4b79-897a-1ab0e52b4a1b";
        internal const string MF_TOPOLOGY_PLAYBACK_MAX_DIMS = "5715cf19-5768-44aa-ad6e-8721f1b0f9bb";
        internal const string MF_TOPOLOGY_HARDWARE_MODE = "d2d362fd-4e4f-4191-a579-c618b6676af";
        internal const string MF_TOPOLOGY_PLAYBACK_FRAMERATE = "c164737a-c2b1-4553-83bb-5a526072448f";
        internal const string MF_TOPOLOGY_DYNAMIC_CHANGE_NOT_ALLOWED = "d529950b-d484-4527-a9cd-b1909532b5b0";
        internal const string MF_TOPOLOGY_ENUMERATE_SOURCE_TYPES = "6248c36d-5d0b-4f40-a0bb-b0b305f77698";
        internal const string MF_TOPOLOGY_START_TIME_ON_PRESENTATION_SWITCH = "c8cc113f-7951-4548-aad6-9ed6202e62b3";

        internal const string MF_PD_PMPHOST_CONTEXT = "6c990d31-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_APP_CONTEXT = "6c990d32-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_DURATION = "6c990d33-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_TOTAL_FILE_SIZE = "6c990d34-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_AUDIO_ENCODING_BITRATE = "6c990d35-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_VIDEO_ENCODING_BITRATE = "6c990d36-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_MIME_TYPE = "6c990d37-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_LAST_MODIFIED_TIME = "6c990d38-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_PLAYBACK_ELEMENT_ID = "6c990d39-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_PREFERRED_LANGUAGE = "6c990d3a-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_PLAYBACK_BOUNDARY_TIME = "6c990d3b-bb8e-477a-8598-0d5d96fcd88a";
        internal const string MF_PD_AUDIO_ISVARIABLEBITRATE = "33026ee0-e387-4582-ae0a-34a2ad3baa18";

        internal const string MF_TRANSCODE_ENCODINGPROFILE = "6947787c-f508-4ea9-b1e9-a1fe3a49fbc9";
        internal const string MF_TRANSCODE_QUALITYVSSPEED = "98332df8-03cd-476b-89fa-3f9e442dec9f";
        internal const string MF_TRANSCODE_CONTAINERTYPE = "150ff23f-4abc-478b-ac4f-e1916fba1cca";
        internal const string MF_TRANSCODE_TOPOLOGYMODE = "3E3DF610-394A-40B2-9DEA-3BAB650BEBF2";

        internal const string MF_MT_MAJOR_TYPE = "48eba18e-f8c9-4687-bf11-0a74c9f96a8f";
        internal const string MF_MT_SUBTYPE = "f7e34c9a-42e8-4714-b74b-cb29d72c35e5";
        internal const string MF_MT_ALL_SAMPLES_INDEPENDENT = "c9173739-5e56-461c-b713-46fb995cb95f";
        internal const string MF_MT_FIXED_SIZE_SAMPLES = "b8ebefaf-b718-4e04-b0a9-116775e3321b";
        internal const string MF_MT_COMPRESSED = "3afd0cee-18f2-4ba5-a110-8bea502e1f92";
        internal const string MF_MT_SAMPLE_SIZE = "dad3ab78-1990-408b-bce2-eba673dacc10";
        internal const string MF_MT_WRAPPED_TYPE = "4d3f7b23-d02f-4e6c-9bee-e4bf2c6c695d";
        internal const string MF_MT_USER_DATA = "b6bc765f-4c3b-40a4-bd51-2535b66fe09d";

        internal const string MF_MT_AUDIO_NUM_CHANNELS = "37e48bf5-645e-4c5b-89de-ada9e29b696a";
        internal const string MF_MT_AUDIO_SAMPLES_PER_SECOND = "5faeeae7-0290-4c31-9e8a-c534f68d9dba";
        internal const string MF_MT_AUDIO_FLOAT_SAMPLES_PER_SECOND = "fb3b724a-cfb5-4319-aefe-6e42b2406132";
        internal const string MF_MT_AUDIO_AVG_BYTES_PER_SECOND = "1aab75c8-cfef-451c-ab95-ac034b8e1731";
        internal const string MF_MT_AUDIO_BLOCK_ALIGNMENT = "322de230-9eeb-43bd-ab7a-ff412251541d";
        internal const string MF_MT_AUDIO_BITS_PER_SAMPLE = "f2deb57f-40fa-4764-aa33-ed4f2d1ff669";
        internal const string MF_MT_AUDIO_VALID_BITS_PER_SAMPLE = "d9bf8d6a-9530-4b7c-9ddf-ff6fd58bbd06";
        internal const string MF_MT_AUDIO_SAMPLES_PER_BLOCK = "aab15aac-e13a-4995-9222-501ea15c6877";
        internal const string MF_MT_AUDIO_CHANNEL_MASK = "55fb5765-644a-4caf-8479-938983bb1588";
        internal const string MF_MT_AUDIO_FOLDDOWN_MATRIX = "9d62927c-36be-4cf2-b5c4-a3926e3e8711";
        internal const string MF_MT_AUDIO_WMADRC_PEAKREF = "9d62927d-36be-4cf2-b5c4-a3926e3e8711";
        internal const string MF_MT_AUDIO_WMADRC_PEAKTARGET = "9d62927e-36be-4cf2-b5c4-a3926e3e8711";
        internal const string MF_MT_AUDIO_WMADRC_AVGREF = "9d62927f-36be-4cf2-b5c4-a3926e3e8711";
        internal const string MF_MT_AUDIO_WMADRC_AVGTARGET = "9d629280-36be-4cf2-b5c4-a3926e3e8711";
        internal const string MF_MT_AUDIO_PREFER_WAVEFORMATEX = "a901aaba-e037-458a-bdf6-545be2074042";
        internal const string MF_MT_AAC_PAYLOAD_TYPE = "bfbabe79-7434-4d1c-94f0-72a3b9e17188";
        internal const string MF_MT_AAC_AUDIO_PROFILE_LEVEL_INDICATION = "7632f0e6-9538-4d61-acda-ea29c8c14456";

        internal const string MF_MT_FRAME_SIZE = "1652c33d-d6b2-4012-b834-72030849a37d";
        internal const string MF_MT_FRAME_RATE = "c459a2e8-3d2c-4e44-b132-fee5156c7bb0";
        internal const string MF_MT_PIXEL_ASPECT_RATIO = "c6376a1e-8d0a-4027-be45-6d9a0ad39bb6";
        internal const string MF_MT_DRM_FLAGS = "8772f323-355a-4cc7-bb78-6d61a048ae82";
        internal const string MF_MT_PAD_CONTROL_FLAGS = "4d0e73e5-80ea-4354-a9d0-1176ceb028ea";
        internal const string MF_MT_SOURCE_CONTENT_HINT = "68aca3cc-22d0-44e6-85f8-28167197fa38";
        internal const string MF_MT_INTERLACE_MODE = "e2724bb8-e676-4806-b4b2-a8d6efb44ccd";
        internal const string MF_MT_TRANSFER_FUNCTION = "5fb0fce9-be5c-4935-a811-ec838f8eed93";
        internal const string MF_MT_CUSTOM_VIDEO_PRIMARIES = "47537213-8cfb-4722-aa34-fbc9e24d77b8";
        internal const string MF_MT_YUV_MATRIX = "3e23d450-2c75-4d25-a00e-b91670d12327";
        internal const string MF_MT_GEOMETRIC_APERTURE = "66758743-7e5f-400d-980a-aa8596c85696";
        internal const string MF_MT_MINIMUM_DISPLAY_APERTURE = "d7388766-18fe-48c6-a177-ee894867c8c4";
        internal const string MF_MT_PAN_SCAN_APERTURE = "79614dde-9187-48fb-b8c7-4d52689de649";
        internal const string MF_MT_PAN_SCAN_ENABLED = "4b7f6bc3-8b13-40b2-a993-abf630b8204e";
        internal const string MF_MT_AVG_BITRATE = "20332624-fb0d-4d9e-bd0d-cbf6786c102e";
        internal const string MF_MT_AVG_BIT_ERROR_RATE = "799cabd6-3508-4db4-a3c7-569cd533deb1";
        internal const string MF_MT_MAX_KEYFRAME_SPACING = "c16eb52b-73a1-476f-8d62-839d6a020652";
        internal const string MF_MT_MPEG4_SAMPLE_DESCRIPTION = "261e9d83-9529-4b8f-a111-8b9c950a81a9";
        internal const string MF_MT_MPEG4_CURRENT_SAMPLE_ENTRY = "9aa7e155-b64a-4c1d-a500-455d600b6560";
        internal const string MF_MT_VIDEO_CHROMA_SITING = "65df2370-c773-4c33-aa64-843e068efb0c";
        internal const string MF_MT_VIDEO_PRIMARIES = "dbfbe4d7-0740-4ee0-8192-850ab0e21935";
        internal const string MF_MT_VIDEO_LIGHTING = "53a0529c-890b-4216-8bf9-599367ad6d20";
        internal const string MF_MT_VIDEO_NOMINAL_RANGE = "c21b8ee5-b956-4071-8daf-325edf5cab11";

        internal const string MF_SOURCE_READER_ASYNC_CALLBACK = "1e3dbeac-bb43-4c35-b507-cd644464c965";
        internal const string MF_SOURCE_READER_D3D_MANAGER = "ec822da2-e1e9-4b29-a0d8-563c719f5269";
        internal const string MF_SOURCE_READER_DISABLE_DXVA = "aa456cfd-3943-4a1e-a77d-1838c0ea2e35";
        internal const string MF_SOURCE_READER_MEDIASOURCE_CONFIG = "9085abeb-0354-48f9-abb5-200df838c68e";
        internal const string MF_SOURCE_READER_MEDIASOURCE_CHARACTERISTICS = "6d23f5c8-c5d7-4a9b-9971-5d11f8bca880";
        internal const string MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING = "fb394f3d-ccf1-42ee-bbb3-f9b845d5681d";
        internal const string MF_SOURCE_READER_DISCONNECT_MEDIASOURCE_ON_SHUTDOWN = "56b67165-219e-456d-a22e-2d3004c7fe56";

        internal const string MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS = "a634a91c-822b-41b9-a494-4de4643612b0";

        internal const string MF_SESSION_TOPOLOADER = "1e83d482-1f1c-4571-8405-88f4b2181f71";
        internal const string MF_SESSION_GLOBAL_TIME = "1e83d482-1f1c-4571-8405-88f4b2181f72";
        internal const string MF_SESSION_QUALITY_MANAGER = "1e83d482-1f1c-4571-8405-88f4b2181f73";

        internal const uint MF_SOURCE_READER_MEDIASOURCE = 0xFFFFFFFF;
        internal const uint MF_SOURCE_READER_ANY_STREAM = 0xFFFFFFFE;
        internal const uint MF_SOURCE_READER_FIRST_AUDIO_STREAM = 0xFFFFFFFD;
        internal const uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;
        internal const uint MF_SOURCE_READER_ALL_STREAMS = 0xFFFFFFFE;

        internal const uint MF_RESOLUTION_MEDIASOURCE = 0x00000001;
        internal const uint MF_RESOLUTION_BYTESTREAM = 0x00000002;
        internal const uint MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE = 0x00000010;
        internal const uint MF_RESOLUTION_KEEP_BYTE_STREAM_ALIVE_ON_FAIL = 0x00000020;
        internal const uint MF_RESOLUTION_READ = 0x00010000;
        internal const uint MF_RESOLUTION_WRITE = 0x00020000;

        internal const uint MEError = 1;
        internal const uint MEExtendedType = 2;
        internal const uint MENonFatalError = 3;
        internal const uint MESessionUnknown = 100;
        internal const uint MESessionTopologySet = 101;
        internal const uint MESessionTopologiesCleared = 102;
        internal const uint MESessionStarted = 103;
        internal const uint MESessionPaused = 104;
        internal const uint MESessionStopped = 105;
        internal const uint MESessionClosed = 106;
        internal const uint MESessionEnded = 107;
    }

    internal static class Enums
    {
        static Enums()
        {
        }

        [Flags]
        internal enum MF_SOURCE_READER_FLAG : uint
        {
            None = 0x00000000,
            ERROR = 0x00000001,
            ENDOFSTREAM = 0x00000002,
            NEWSTREAM = 0x00000004,
            NATIVEMEDIATYPECHANGED = 0x00000010,
            CURRENTMEDIATYPECHANGED = 0x00000020,
            STREAMTICK = 0x00000100
        }

        [Flags]
        internal enum MFSESSION_SETTOPOLOGY_FLAGS : uint
        {
            None = 0x0,
            MFSESSION_SETTOPOLOGY_IMMEDIATE = 0x1,
            MFSESSION_SETTOPOLOGY_NORESOLUTION = 0x2,
            MFSESSION_SETTOPOLOGY_CLEAR_CURRENT = 0x4
        }

        [Flags]
        internal enum MFT_ENUM_FLAG : uint
        {
            None = 0,
            MFT_ENUM_FLAG_SYNCMFT = 0x00000001,
            MFT_ENUM_FLAG_ASYNCMFT = 0x00000002,
            MFT_ENUM_FLAG_HARDWARE = 0x00000004,
            MFT_ENUM_FLAG_FIELDOFUSE = 0x00000008,
            MFT_ENUM_FLAG_LOCALMFT = 0x00000010,
            MFT_ENUM_FLAG_TRANSCODE_ONLY = 0x00000020,
            MFT_ENUM_FLAG_SORTANDFILTER = 0x00000040,
            MFT_ENUM_FLAG_ALL = 0x0000003F
        }

        [Flags]
        internal enum MFSESSION_GETFULLTOPOLOGY_FLAGS : uint
        {
            None = 0,
            MF_SESSION_GETFULLTOPOLOGY_CURRENT = 1
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal class MediaSessionStartPosition
    {
        public MediaSessionStartPosition(long startPosition)
        {
            this.internalUse = 20; // VT_18
            this.startPosition = startPosition;
        }

        [FieldOffset(0)]
        public short internalUse;
        [FieldOffset(8)]
        public long startPosition;
    }

    [ComImport, Guid(IID.IMFAttributes), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFAttributes
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, uint MatchType, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out ulong punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] string pwszValue, uint cchBufSize, [Out] out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, uint cbBufSize, [Out] out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out] IntPtr ppBuf, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref Guid riid, out IntPtr ppv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteAllItems();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, uint unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ulong unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, uint cbBufSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnlockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCount(out uint pcItems);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);
    }
 
    [ComImport, Guid(IID.IMFClock), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFClock
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetClockCharacteristics(out uint pdwCharacteristics);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCorrelatedTime([In] uint dwReserved, out long pllClockTime, out long phnsSystemTime);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetContinuityKey(out uint pdwContinuityKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetState([In] uint dwReserved, out uint peClockState);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetProperties([MarshalAs(UnmanagedType.LPStruct)] out object pClockProperties);
    }

    [ComImport, Guid(IID.IMFCollection), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFCollection
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetElementCount(out uint pcElements);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetElement([In] uint dwElementIndex, [MarshalAs(UnmanagedType.IUnknown)] out object ppUnkElement);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddElement([In, MarshalAs(UnmanagedType.IUnknown)] object pUnkElement);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveElement([In] uint dwElementIndex, [MarshalAs(UnmanagedType.IUnknown)] out object ppUnkElement);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void InsertElementAt([In] uint dwIndex, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveAllElements();
    }

    [ComImport, Guid(IID.IMFMediaBuffer), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaBuffer
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Lock([Out] IntPtr ppbBuffer, out uint pcbMaxLength, out uint pcbCurrentLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Unlock();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCurrentLength(out uint pcbCurrentLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetCurrentLength([In] uint cbCurrentLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetMaxLength(out uint pcbMaxLength);
    }

    [ComImport, Guid(IID.IMFMediaEvent), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaEvent 
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, uint MatchType, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out ulong punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] string pwszValue, uint cchBufSize, [Out] out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, uint cbBufSize, [Out] out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out] IntPtr ppBuf, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref Guid riid, out IntPtr ppv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteAllItems();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, uint unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ulong unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, uint cbBufSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnlockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCount(out uint pcItems);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetType(out uint pmet);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetExtendedType(out Guid pguidExtendedType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStatus([MarshalAs(UnmanagedType.Error)] out int phrStatus);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetValue(out object pvValue);
    }

    [ComImport, Guid(IID.IMFMediaSession), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaSession
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetEvent([In] uint dwFlags, [MarshalAs(UnmanagedType.Interface)] out IMFMediaEvent ppEvent);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void BeginGetEvent([In, MarshalAs(UnmanagedType.Interface)] IMFAsyncCallback pCallback, [In, MarshalAs(UnmanagedType.IUnknown)] object punkState);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void EndGetEvent([In, MarshalAs(UnmanagedType.Interface)] IMFAsyncResult pResult, [Out, MarshalAs(UnmanagedType.Interface)] out IMFMediaEvent ppEvent);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void QueueEvent([In] uint met, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidExtendedType, [In, MarshalAs(UnmanagedType.Error)] int hrStatus, [In] ref object pvValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetTopology([In] uint dwSetTopologyFlags, [In, MarshalAs(UnmanagedType.Interface)] IMFTopology pTopology);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ClearTopologies();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Start([In, MarshalAs(UnmanagedType.LPStruct)] Guid pguidTimeFormat, [In, MarshalAs(UnmanagedType.LPStruct)] MediaSessionStartPosition pvarStartPosition);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Pause();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Stop();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Close();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Shutdown();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetClock([MarshalAs(UnmanagedType.Interface)] out IMFClock ppClock);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetSessionCapabilities(out uint pdwCaps);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFullTopology([In] uint dwGetFullTopologyFlags, [In] ulong TopoId, [MarshalAs(UnmanagedType.Interface)] out IMFTopology ppFullTopology);
    }

    [ComImport, Guid(IID.IMFMediaSource), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaSource 
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetEvent([In] uint dwFlags, [MarshalAs(UnmanagedType.Interface)] out IMFMediaEvent ppEvent);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void BeginGetEvent([In, MarshalAs(UnmanagedType.Interface)] IMFAsyncCallback pCallback, [In, MarshalAs(UnmanagedType.IUnknown)] object punkState);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void EndGetEvent([In, MarshalAs(UnmanagedType.Interface)] IMFAsyncResult pResult, [Out, MarshalAs(UnmanagedType.Interface)] out IMFMediaEvent ppEvent);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void QueueEvent([In] uint met, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidExtendedType, [In, MarshalAs(UnmanagedType.Error)] int hrStatus, [In] ref object pvValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCharacteristics(out uint pdwCharacteristics);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CreatePresentationDescriptor([MarshalAs(UnmanagedType.Interface)] out IMFPresentationDescriptor ppPresentationDescriptor);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Start([In, MarshalAs(UnmanagedType.Interface)] IMFPresentationDescriptor pPresentationDescriptor, [In, MarshalAs(UnmanagedType.LPStruct)] Guid pguidTimeFormat, [In] object pvarStartPosition);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Stop();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Pause();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Shutdown();
    }

    [ComImport, Guid(IID.IMFMediaType), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaType
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, uint MatchType, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out ulong punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] string pwszValue, uint cchBufSize, [Out] out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, uint cbBufSize, [Out] out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out] IntPtr ppBuf, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref Guid riid, out IntPtr ppv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteAllItems();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, uint unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ulong unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, uint cbBufSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnlockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCount(out uint pcItems);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetMajorType(out Guid pguidMajorType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void IsCompressedFormat([MarshalAs(UnmanagedType.Bool)] out bool pfCompressed);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void IsEqual([In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pIMediaType, out uint pdwFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetRepresentation([In] Guid guidRepresentation, out IntPtr ppvRepresentation);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void FreeRepresentation([In] Guid guidRepresentation, [In] IntPtr pvRepresentation);
    }

    [ComImport, Guid(IID.IMFReadWriteClassFactory), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFReadWriteClassFactory
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CreateInstanceFromURL([In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid, [In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL, [In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppvObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CreateInstanceFromObject([In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid, [In, MarshalAs(UnmanagedType.IUnknown)] object punkObject, [In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppvObject);
    }

    [ComImport, Guid(IID.IMFSample), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFSample
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, uint MatchType, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out ulong punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] string pwszValue, uint cchBufSize, [Out] out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, uint cbBufSize, [Out] out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out] IntPtr ppBuf, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref Guid riid, out IntPtr ppv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteAllItems();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, uint unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ulong unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, uint cbBufSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnlockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCount(out uint pcItems);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetSampleFlags(out uint pdwSampleFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSampleFlags([In] uint dwSampleFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetSampleTime(out ulong phnsSampleTime);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSampleTime([In] ulong hnsSampleTime);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetSampleDuration(out ulong phnsSampleDuration);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSampleDuration([In] ulong hnsSampleDuration);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBufferCount(out uint pdwBufferCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBufferByIndex([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IMFMediaBuffer ppBuffer);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ConvertToContiguousBuffer([MarshalAs(UnmanagedType.Interface)] out IMFMediaBuffer ppBuffer);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddBuffer([In, MarshalAs(UnmanagedType.Interface)] IMFMediaBuffer pBuffer);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveBufferByIndex([In] uint dwIndex);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveAllBuffers();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetTotalLength(out uint pcbTotalLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CopyToBuffer([In, MarshalAs(UnmanagedType.Interface)] IMFMediaBuffer pBuffer);
    }

    [ComImport, Guid(IID.IMFSinkWriter),
        System.Security.SuppressUnmanagedCodeSecurity,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFSinkWriter
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddStream([In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pTargetMediaType, out uint pdwStreamIndex);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetInputMediaType([In] uint dwStreamIndex, [In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pInputMediaType, [In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pEncodingParameters);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void BeginWriting();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void WriteSample([In] uint dwStreamIndex, [In, MarshalAs(UnmanagedType.Interface)] IMFSample pSample);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SendStreamTick([In] uint dwStreamIndex, [In] ulong llTimestamp);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PlaceMarker([In] uint dwStreamIndex, [In] IntPtr pvContext);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void NotifyEndOfSegment([In] uint dwStreamIndex);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Flush([In] uint dwStreamIndex);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DoFinalize();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetServiceForStream([In] uint dwStreamIndex, [In] ref Guid guidService, [In] ref Guid riid, out IntPtr ppvObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStatistics([In] uint dwStreamIndex, out object pStats);
    }

    [ComImport, Guid(IID.IMFSourceReader), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFSourceReader
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStreamSelection([In] uint dwStreamIndex, [Out, MarshalAs(UnmanagedType.Bool)] out bool pfSelected);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetStreamSelection([In] uint dwStreamIndex, [In, MarshalAs(UnmanagedType.Bool)] bool fSelected);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetNativeMediaType([In] uint dwStreamIndex, [In] uint dwMediaTypeIndex, [MarshalAs(UnmanagedType.Interface)] out IMFMediaType ppMediaType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCurrentMediaType([In] uint dwStreamIndex, [MarshalAs(UnmanagedType.Interface)] out IMFMediaType ppMediaType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetCurrentMediaType([In] uint dwStreamIndex, [In, Out] IntPtr pdwReserved, [In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pMediaType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetCurrentPosition([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidTimeFormat, [In] ref object varPosition);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ReadSample([In] uint dwStreamIndex, [In] uint dwControlFlags, out uint pdwActualStreamIndex, out uint pdwStreamFlags, out ulong pllTimestamp, [MarshalAs(UnmanagedType.Interface)] out IMFSample ppSample);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Flush([In] uint dwStreamIndex);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetServiceForStream([In] uint dwStreamIndex, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidService, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppvObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetPresentationAttribute([In] uint dwStreamIndex, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidAttribute, out object pvarAttribute);
    }

    [ComImport, Guid(IID.IMFSourceResolver), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFSourceResolver
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CreateObjectFromURL([In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL, [In] uint dwFlags, [In, MarshalAs(UnmanagedType.Interface)] IPropertyStore pProps, out uint pObjectType, [MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CreateObjectFromByteStream([In, MarshalAs(UnmanagedType.Interface)] object pByteStream, [In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL, [In] uint dwFlags, [In, MarshalAs(UnmanagedType.Interface)] IPropertyStore pProps, out uint pObjectType, [MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void BeginCreateObjectFromURL([In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL, [In] uint dwFlags, [In, MarshalAs(UnmanagedType.Interface)] IPropertyStore pProps, [Out, MarshalAs(UnmanagedType.Interface)] out object ppIUnknownCancelCookie, [In, MarshalAs(UnmanagedType.Interface)] IMFAsyncCallback pCallback, [In, MarshalAs(UnmanagedType.IUnknown)] object punkState);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void EndCreateObjectFromURL([In, MarshalAs(UnmanagedType.IUnknown)] object pResult, out uint pObjectType, [MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void BeginCreateObjectFromByteStream([In, MarshalAs(UnmanagedType.Interface)] object pByteStream, [In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL, [In] uint dwFlags, [In, MarshalAs(UnmanagedType.Interface)] IPropertyStore pProps, [Out, MarshalAs(UnmanagedType.IUnknown)] out IMFAsyncCallback ppIUnknownCancelCookie, [In, MarshalAs(UnmanagedType.Interface)] object pCallback, [In, MarshalAs(UnmanagedType.IUnknown)] object punkState);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void EndCreateObjectFromByteStream([In, MarshalAs(UnmanagedType.IUnknown)] object pResult, out uint pObjectType, [MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CancelObjectCreation([In, MarshalAs(UnmanagedType.IUnknown)] object pIUnknownCancelCookie);
    }

    [ComImport, Guid(IID.IMFTopology), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFTopology 
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, uint MatchType, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out ulong punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] string pwszValue, uint cchBufSize, [Out] out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, uint cbBufSize, [Out] out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out] IntPtr ppBuf, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref Guid riid, out IntPtr ppv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteAllItems();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, uint unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ulong unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, uint cbBufSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnlockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCount(out uint pcItems);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetTopologyID(out ulong pID);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddNode([In, MarshalAs(UnmanagedType.Interface)] IMFTopologyNode pNode);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveNode([In, MarshalAs(UnmanagedType.Interface)] IMFTopologyNode pNode);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetNodeCount(out ushort pwNodes);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetNode([In] ushort wIndex, [MarshalAs(UnmanagedType.Interface)] out IMFTopologyNode ppNode);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Clear();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CloneFrom([In, MarshalAs(UnmanagedType.Interface)] IMFTopology pTopology);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetNodeByID([In] ulong qwTopoNodeID, [MarshalAs(UnmanagedType.Interface)] out IMFTopologyNode ppNode);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetSourceNodeCollection([MarshalAs(UnmanagedType.Interface)] out IMFCollection ppCollection);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOutputNodeCollection([MarshalAs(UnmanagedType.Interface)] out IMFCollection ppCollection);
    }

    [ComImport, Guid(IID.IMFTopologyNode), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFTopologyNode
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, uint MatchType, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out ulong punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] string pwszValue, uint cchBufSize, [Out] out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, uint cbBufSize, [Out] out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out] IntPtr ppBuf, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref Guid riid, out IntPtr ppv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteAllItems();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, uint unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ulong unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, uint cbBufSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnlockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCount(out uint pcItems);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetObject([In, MarshalAs(UnmanagedType.IUnknown)] object pObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetObject([MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetNodeType(out uint pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetTopoNodeID(out ulong pID);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetTopoNodeID([In] ulong ullTopoID);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetInputCount(out uint pcInputs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOutputCount(out uint pcOutputs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ConnectOutput([In] uint dwOutputIndex, [In, MarshalAs(UnmanagedType.Interface)] IMFTopologyNode pDownstreamNode, [In] uint dwInputIndexOnDownstreamNode);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DisconnectOutput([In] uint dwOutputIndex);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetInput([In] uint dwInputIndex, [MarshalAs(UnmanagedType.Interface)] out IMFTopologyNode ppUpstreamNode, out uint pdwOutputIndexOnUpstreamNode);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOutput([In] uint dwOutputIndex, [MarshalAs(UnmanagedType.Interface)] out IMFTopologyNode ppDownstreamNode, out uint pdwInputIndexOnDownstreamNode);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOutputPrefType([In] uint dwOutputIndex, [In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoteGetOutputPrefType([In] uint dwOutputIndex, out uint pcbData, [Out] IntPtr ppbData);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetInputPrefType([In] uint dwInputIndex, [In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoteGetInputPrefType([In] uint dwInputIndex, out uint pcbData, [Out] IntPtr ppbData);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CloneFrom([In, MarshalAs(UnmanagedType.Interface)] IMFTopologyNode pNode);
    }

    [ComImport, Guid(IID.IMFTranscodeProfile), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFTranscodeProfile
    {

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetAudioAttributes([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttrs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAudioAttributes([MarshalAs(UnmanagedType.Interface)] out IMFAttributes ppAttrs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetVideoAttributes([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttrs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetVideoAttributes([MarshalAs(UnmanagedType.Interface)] out IMFAttributes ppAttrs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetContainerAttributes([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttrs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetContainerAttributes([MarshalAs(UnmanagedType.Interface)] out IMFAttributes ppAttrs);
    }

    [ComImport, Guid(IID.IMFAsyncResult), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFAsyncResult 
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetState([MarshalAs(UnmanagedType.IUnknown)] out object ppunkState);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        // Suppress the HRESULT signature transformation and return the value to the caller instead
        [PreserveSig()]
        int GetStatus();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetStatus([In, MarshalAs(UnmanagedType.Error)] int hrStatus);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetObject([MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
    }

    [ComImport, Guid(IID.IMFAsyncCallback), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFAsyncCallback 
    {
        // Suppress the HRESULT signature transformation and return the value to the caller instead
        [PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetParameters(out uint pdwFlags, out uint pdwQueue);
        // Suppress the HRESULT signature transformation and return the value to the caller instead
        [PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Invoke([In, MarshalAs(UnmanagedType.Interface)] IMFAsyncResult pAsyncResult);
    }

    [ComImport, Guid(IID.IMFPresentationClock), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFPresentationClock
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetClockCharacteristics(out uint pdwCharacteristics);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCorrelatedTime([In] uint dwReserved, out long pllClockTime, out long phnsSystemTime);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetContinuityKey(out uint pdwContinuityKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetState([In] uint dwReserved, out uint peClockState);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetProperties(out IntPtr pClockProperties);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetTimeSource([In, MarshalAs(UnmanagedType.Interface)] object pTimeSource);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetTimeSource([MarshalAs(UnmanagedType.Interface)] out object ppTimeSource);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetTime(out long phnsClockTime);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddClockStateSink([In, MarshalAs(UnmanagedType.Interface)] object pStateSink);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveClockStateSink([In, MarshalAs(UnmanagedType.Interface)] object pStateSink);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Start([In] long llClockStartOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Stop();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Pause();
    }

    [ComImport, Guid(IID.IMFPresentationDescriptor), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFPresentationDescriptor
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, uint MatchType, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out ulong punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] string pwszValue, uint cchBufSize, [Out] out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, uint cbBufSize, [Out] out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out] IntPtr ppBuf, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref Guid riid, out IntPtr ppv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteAllItems();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, uint unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ulong unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, uint cbBufSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnlockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCount(out uint pcItems);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStreamDescriptorCount(out uint pdwDescriptorCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStreamDescriptorByIndex([In] uint dwIndex, [Out, MarshalAs(UnmanagedType.Bool)] out bool pfSelected, [MarshalAs(UnmanagedType.Interface)] out IMFStreamDescriptor ppDescriptor);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SelectStream([In] uint dwDescriptorIndex);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeselectStream([In] uint dwDescriptorIndex);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Clone([MarshalAs(UnmanagedType.Interface)] out IMFPresentationDescriptor ppPresentationDescriptor);
    }

    [ComImport, Guid(IID.IMFStreamDescriptor), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFStreamDescriptor
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, uint MatchType, out int pbResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out ulong punValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] string pwszValue, uint cchBufSize, [Out] out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, uint cbBufSize, [Out] out uint pcbBlobSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out] IntPtr ppBuf, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref Guid riid, out IntPtr ppv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ref object Value);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void DeleteAllItems();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, uint unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ulong unValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, uint cbBufSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnlockStore();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCount(out uint pcItems);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] ref object pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStreamIdentifier(out uint pdwStreamIdentifier);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetMediaTypeHandler([MarshalAs(UnmanagedType.Interface)] out IMFMediaTypeHandler ppMediaTypeHandler);
    }

    [ComImport, Guid(IID.IMFMediaTypeHandler), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaTypeHandler
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void IsMediaTypeSupported([In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pMediaType, [MarshalAs(UnmanagedType.Interface)] out IMFMediaType ppMediaType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetMediaTypeCount(out uint pdwTypeCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetMediaTypeByIndex([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IMFMediaType ppType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetCurrentMediaType([In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pMediaType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCurrentMediaType([Out] out IMFMediaType pMediaType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetMajorType(out Guid pguidMajorType);
    }

    [ComImport, Guid(IID.IPropertyStore), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCount(out uint cProps);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAt([In] uint iProp, [MarshalAs(UnmanagedType.LPStruct)] out object pkey);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetValue([In, MarshalAs(UnmanagedType.LPStruct)] object key, out object pv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetValue([In, MarshalAs(UnmanagedType.LPStruct)] object key, [In] object propvar);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Commit();
    }

    [ComImport, Guid(CLSID.MFReadWriteClassFactory)]
    internal class MFReadWriteClassFactory
    {
    }

    internal static class API
    {
        private static readonly uint mediaFoundationVersion = 0x0270;

        [DllImport("mfplat.dll", EntryPoint = "MFStartup")]
        private static extern int ExternMFStartup(
            [In] uint Version,
            [In] uint dwFlags = default(uint));

        [DllImport("mfplat.dll", EntryPoint = "MFShutdown")]
        private static extern int ExternMFShutdown();

        [DllImport("mfplat.dll", EntryPoint = "MFCreateMediaType")]
        private static extern int ExternMFCreateMediaType(
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFMediaType ppMFType);

        [DllImport("mfplat.dll", EntryPoint = "MFCreateSourceResolver")]
        private static extern int ExternMFCreateSourceResolver(
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFSourceResolver ppISourceResolver);

        [DllImport("mfplat.dll", EntryPoint = "MFCreateAttributes")]
        private static extern int ExternMFCreateAttributes(
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFAttributes ppMFAttributes,
            [In] uint cInitialSize);

        [DllImport("Mf.dll", EntryPoint = "MFCreateMediaSession")]
        private static extern int ExternMFCreateMediaSession(
            [In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pConfiguration,
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFMediaSession ppMS);

        [DllImport("Mf.dll", EntryPoint = "MFCreateTranscodeProfile")]
        private static extern int ExternMFCreateTranscodeProfile(
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFTranscodeProfile ppTranscodeProfile);

        [DllImport("Mf.dll", EntryPoint = "MFTranscodeGetAudioOutputAvailableTypes")]
        private static extern int ExternMFTranscodeGetAudioOutputAvailableTypes(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidSubType,
            [In] uint dwMFTFlags,
            [In] uint pCodecConfig,
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFCollection ppAvailableTypes);

        [DllImport("Mf.dll", EntryPoint = "MFCreateTranscodeTopology")]
        private static extern int ExternMFCreateTranscodeTopology(
            [In, MarshalAs(UnmanagedType.Interface)] IMFMediaSource pSrc,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwszOutputFilePath,
            [In, MarshalAs(UnmanagedType.Interface)] IMFTranscodeProfile pProfile,
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFTopology ppTranscodeTopo);

        [DllImport("MFReadWrite.dll", EntryPoint = "MFCreateSourceReaderFromMediaSource")]
        private static extern int ExternMFCreateSourceReaderFromMediaSource(
            [In, MarshalAs(UnmanagedType.Interface)] IMFMediaSource pSrc,
            [In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttrs,
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFSourceReader psrcReader);

        /// <summary>
        ///     Starts Media Foundation
        /// </summary>
        /// <remarks>
        ///     Will fail if the OS version is prior Windows 7
        /// </remarks>
        public static void MFStartup()
        {
            int result = ExternMFStartup(mediaFoundationVersion, 0);
            if (result < 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + "(MFStartup)", result);
            }
        }
        
        public static void MFShutdown()
        {
            int result = ExternMFShutdown();
            if (result < 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + " (MFShutdown)", result);
            }
        }

        public static void MFCreateMediaType(out IMFMediaType mediaType)
        {
            int result = ExternMFCreateMediaType(out mediaType);
            if (result < 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + " (MFCreateMediaType)", result);
            }
        }

        public static void MFCreateSourceResolver(out IMFSourceResolver sourceResolver)
        {
            int result = ExternMFCreateSourceResolver(out sourceResolver);
            if (result < 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + " (MFCreateSourceResolver)", result);
            }
        }

        public static void MFCreateAttributes(out IMFAttributes attributes, uint initialSize)
        {
            int result = ExternMFCreateAttributes(out attributes, initialSize);
            if (result < 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + " (MFCreateAttributes)", result);
            }
        }

        public static void MFCreateMediaSession(IMFAttributes configuration, out IMFMediaSession mediaSession)
        {
            int result = ExternMFCreateMediaSession(configuration, out mediaSession);
            if (result < 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + " (MFCreateMediaSession)", result);
            }
        }

        public static void MFCreateTranscodeProfile(out IMFTranscodeProfile transcodeProfile)
        {
            int result = ExternMFCreateTranscodeProfile(out transcodeProfile);
            if (result < 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + " (MFCreateTranscodeProfile)", result);
            }
        }

        public static void MFTranscodeGetAudioOutputAvailableTypes(
            Guid subType,
            uint flags,
            uint codecConfig,
            out IMFCollection availableTypes)
        {
            int result = ExternMFTranscodeGetAudioOutputAvailableTypes(subType, flags, codecConfig, out availableTypes);
            if (result != 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + "(MFTranscodeGetAudioOutputAvailableTypes)", result);
            }
        }

        public static void MFCreateTranscodeTopology(
            IMFMediaSource mediaSource,
            string outputFilePath,
            IMFTranscodeProfile transcodeProfile,
            out IMFTopology transcodeTopology)
        {
            int result = ExternMFCreateTranscodeTopology(mediaSource, outputFilePath, transcodeProfile, out transcodeTopology);
            if (result < 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + " (MFCreateTranscodeTopology failed)", result);
            }
        }

        public static void MFCreateSourceReaderFromMediaSource(
            IMFMediaSource mediaSource,
            IMFAttributes attributes,
            out IMFSourceReader sourceReader)
        {
            int result = ExternMFCreateSourceReaderFromMediaSource(mediaSource, attributes, out sourceReader);
            if (result < 0)
            {
                throw new COMException("Exception from HRESULT: 0x" + result.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo) + " (MFCreateTranscodeTopology failed)", result);
            }
        }

    }
}
