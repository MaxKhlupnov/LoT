// -
// <copyright file="ServiceConnection.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -

namespace HomeOS.Hub.Platform.Gatekeeper
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using HomeOS.Hub.Common;
    using HomeOS.Shared;
    using HomeOS.Hub.Platform.Views;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Authentication;

    /// <summary>
    /// Represents a connection to the cloud service.
    /// </summary>
    public class ServiceConnection
    {
        /// <summary>
        /// Maintains stream buffer usage state during asynchronous read/writes
        /// </summary>
        private class StreamBufferState
        {
            public void SetBuffer(Byte[] buf, int offset, int length)
            {
                this.Buffer = buf;
                this.Offset = offset;
                this.Length = length;
            }

            public void SetBuffer(int offset, int length)
            {
                Offset = offset;
                Length = length;
            }

            public byte[] Buffer { get; private set; }
            public int Offset { get; private set; }
            public int Length { get; private set; }
        }

        /// <summary>
        /// The version number for our protocol.
        /// </summary>
        private const byte ProtocolVersion = 1;

        /// <summary>
        /// The socket for this connection.
        /// </summary>
        private Socket socket;

        /// <summary>
        /// Network Stream for this client connection.
        /// </summary>
        private NetworkStream netstream;

        /// <summary>
        /// Keeps the stream buffer state information.
        /// </summary>
        private StreamBufferState streamBufState;

        /// <summary>
        /// Switch to turn secure streams on and off.
        /// </summary>
        private bool useSecureStream;

        /// <summary>
        /// Stream created by SSL of the network stream.
        /// </summary>
        private SslStream sslStream;

        /// <summary>
        /// Server and domain names of the ssl server.
        /// </summary>
        private string sslServerHost;

        /// <summary>
        /// The I/O buffer for this connection.
        /// </summary>
        private byte[] buffer;

        /// <summary>
        /// The current byte offset into the buffer.
        /// </summary>
        private int bufferOffset;


        /// <summary>
        /// The version number of the protocol our peer is using.
        /// </summary>
        private byte peerProtocolVersion;

        /// <summary>
        /// The identifier value to use.
        /// </summary>
        private string identifier;

        /// <summary>
        /// The simple authentication value (password) to use.
        /// </summary>
        private uint simpleAuthentication;

        /// <summary>
        /// A unique token for identifying our client with the gatekeeper,
        /// or zero if this is the registration connection.
        /// </summary>
        private uint clientToken;

        /// <summary>
        /// The handler routine to call upon forwarding a connection.
        /// </summary>
        private ForwardingHandler handler;

        /// <summary>
        /// A value indicating whether we have entered forwarding mode.
        /// </summary>
        private bool forwarding;

        public VLogger logger;


        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            // Do not allow this client to communicate with unauthenticated servers. 
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the ServiceConnection class.
        /// </summary>
        /// <param name="connected">
        /// The socket for the new connection.
        /// </param>
        /// <param name="token">
        /// The token for this client of the gatekeeper service,
        /// or zero if this is the registration connection.
        /// </param>
        /// <param name="handler">
        /// The handler routine to call upon forwarding a connection.
        /// </param>
        public ServiceConnection(
            string serverHost,
            Socket connected,
            uint token,
            ForwardingHandler handler, 
            VLogger logger)
        {
            this.useSecureStream = true;

            // -
            // Set up the connection state.
            // -
            this.socket = connected;
            this.netstream = new NetworkStream(this.socket, true /*ownSocket*/);
            this.sslServerHost = serverHost;
            if (this.useSecureStream)
            {
                this.sslStream = new SslStream(
                                    this.netstream,
                                    false /* leaveInnerStreamOpen */,
                                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                                    null
                                    );
                // The server name must match the name on the server certificate. 
                try
                {
                    sslStream.AuthenticateAsClient(this.sslServerHost);
                }
                catch (Exception e)
                {
                    logger.Log("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        logger.Log("Inner exception: {0}", e.InnerException.Message);
                    }
                    logger.Log("Authentication failed - closing the connection.");
                    this.ShutdownAndClose();
                    return;
                }
            }
            this.clientToken = token;
            this.handler = handler;
            this.forwarding = false;
            this.identifier = HomeOS.Shared.Gatekeeper.Settings.HomeId;
            this.simpleAuthentication = HomeOS.Shared.Gatekeeper.Settings.HomePassword;

            this.logger = logger;
//#if false
             //-
             //We use keep-alives on the home <-> cloud service
             //connection in an attempt to prevent NAT/firewall
             //state from timing out and dropping our connection.
             //-
            StaticUtilities.SetKeepAlive(this.socket, 120000, 1000);
//#endif

            // -
            // Prepare our buffer space and asynchronous state holder.
            // This is currently just a simplistic single buffer system.
            // Note that this code assumes that the buffer is larger than
            // the largest possible single message (currently 257 bytes).
            // -
            this.buffer = new byte[1500];
            this.bufferOffset = 0;

            this.streamBufState = new StreamBufferState();
            this.streamBufState.SetBuffer(this.buffer, 0, this.buffer.Length);

            // -
            // Start the dialog with our peer.
            // -
            this.AppendMessage(
                MessageType.Version,
                ServiceConnection.ProtocolVersion);
            this.SendMessage();
        }

        public Stream GetStream()
        {
            if (this.useSecureStream)
                return this.sslStream;
            else
                return this.netstream;
        }

        /// <summary>
        /// A handler for forwarded connections.
        /// </summary>
        /// <param name="connection">
        /// The Connection being forwarded.
        /// </param>
        /// <returns>
        /// True if forwarding was established, false otherwise.
        /// </returns>
        public delegate bool ForwardingHandler(ServiceConnection connection);

        /// <summary>
        /// The types of messages in this protocol.
        /// All messages (except Pad1) consist of three parts:
        ///  Type - One of these MessageType codes.
        ///  Length - The length of the message (in bytes).
        ///  Value - The data (0 to 255 bytes).
        /// </summary>
        private enum MessageType : byte
        {
            /// <summary>
            /// Special one-byte pad message.
            /// </summary>
            Pad1 = 0,

            /// <summary>
            /// Variable length (2-257 bytes) pad message.
            /// </summary>
            PadN = 1,

            /// <summary>
            /// Request to echo data back to sender.
            /// </summary>
            EchoRequest = 2,

            /// <summary>
            /// Reply to an echo request message.
            /// </summary>
            EchoReply = 3,

            /// <summary>
            /// Protocol version number.
            /// </summary>
            Version = 4,

            /// <summary>
            /// Request for identification.
            /// </summary>
            PleaseIdentify = 5,

            /// <summary>
            /// Identification information.
            /// </summary>
            Identification = 6,

            /// <summary>
            /// Request for (simple) authentication.
            /// </summary>
            PleaseAuthenticate = 7,

            /// <summary>
            /// Simple authentication (i.e. password).
            /// </summary>
            SimpleAuthentication = 8,

            /// <summary>
            /// Challenge portion of a challenge/reponse authentication.
            /// </summary>
            AuthenticationChallenge = 9,

            /// <summary>
            /// Reponse portion of a challenge/reponse authentication.
            /// </summary>
            AuthenticationChallengeReponse = 10,

            /// <summary>
            /// Authentication acknowledged.
            /// </summary>
            Authenticated = 11,

            /// <summary>
            /// Register a service with the gatekeeper service.
            /// </summary>
            RegisterService = 12,

            /// <summary>
            /// Request list of available services be sent.
            /// </summary>
            SendServiceList = 13,

            /// <summary>
            /// List of available services.
            /// </summary>
            ServiceList = 14,

            /// <summary>
            /// Request connection forwarding to the specified home service.
            /// </summary>
            ForwardToService = 15,

            /// <summary>
            /// Notification that a client is waiting for connection to a
            /// home service.
            /// </summary>
            ClientAwaits = 16,

            /// <summary>
            /// Request connection forwarding to the specified client.
            /// </summary>
            ForwardToClient = 17,
        }

        /// <summary>
        /// Types of authentication.
        /// </summary>
        private enum AuthenticationType : byte
        {
            /// <summary>
            /// No authentication required.
            /// </summary>
            None = 0,

            /// <summary>
            /// Use simple authentication (i.e. password).
            /// </summary>
            Simple = 1,
        }

        /// <summary>
        /// Gets the socket for this connection.
        /// </summary>
        public Socket Socket
        {
            get { return this.socket; }
        }

        /// <summary>
        /// Closes this connection.
        /// </summary>
        public void Close()
        {
            this.ShutdownAndClose();
        }

        /// <summary>
        /// Append a message to our send buffer.
        /// </summary>
        /// <param name="type">The message type.</param>
        private void AppendMessage(MessageType type)
        {
            if (this.bufferOffset + 2 > this.buffer.Length)
            {
                return;
            }

            this.buffer[this.bufferOffset++] = (byte)type;
            this.buffer[this.bufferOffset++] = 0;
        }

        /// <summary>
        /// Append a message to our send buffer.
        /// </summary>
        /// <param name="type">The message type.</param>
        /// <param name="data">The message data.</param>
        private void AppendMessage(MessageType type, byte data)
        {
            if (this.bufferOffset + 3 > this.buffer.Length)
            {
                return;
            }

            this.buffer[this.bufferOffset++] = (byte)type;
            this.buffer[this.bufferOffset++] = 1;
            this.buffer[this.bufferOffset++] = data;
        }

        /// <summary>
        /// Append a message to our send buffer.
        /// </summary>
        /// <param name="type">The message type.</param>
        /// <param name="data">The message data.</param>
        private void AppendMessage(MessageType type, int data)
        {
            byte[] dataBytes = BitConverter.GetBytes(data);
            this.AppendMessage(type, dataBytes);
        }

        /// <summary>
        /// Append a message to our send buffer.
        /// </summary>
        /// <param name="type">The message type.</param>
        /// <param name="data">The message data.</param>
        private void AppendMessage(MessageType type, byte[] data)
        {
            if (this.bufferOffset + 2 + data.Length > this.buffer.Length)
            {
                return;
            }

            this.buffer[this.bufferOffset++] = (byte)type;
            this.buffer[this.bufferOffset++] = (byte)data.Length;
            Array.Copy(data, 0, this.buffer, this.bufferOffset, data.Length);
            this.bufferOffset += data.Length;
        }

        /// <summary>
        /// Append a message to our send buffer.
        /// </summary>
        /// <param name="type">The message type.</param>
        /// <param name="data">The message data.</param>
        private void AppendMessage(MessageType type, ArraySegment<byte> data)
        {
            if (this.bufferOffset + 2 + data.Count > this.buffer.Length)
            {
                return;
            }

            this.buffer[this.bufferOffset++] = (byte)type;
            this.buffer[this.bufferOffset++] = (byte)data.Count;
            Array.Copy(
                data.Array,
                data.Offset,
                this.buffer,
                this.bufferOffset,
                data.Count);
            this.bufferOffset += data.Count;
        }

        /// <summary>
        /// Send the message currently in our send buffer, starting at
        /// the given byte offset and continuing for the given byte count.
        /// </summary>
        /// <param name="offset">The offset to start at in bytes.</param>
        /// <param name="count">The number of bytes to send.</param>
        private void SendMessage(int offset, int count)
        {
            IAsyncResult result = null;
            this.streamBufState.SetBuffer(offset, count);
            try
            {
                result = GetStream().BeginWrite(this.streamBufState.Buffer, this.streamBufState.Offset, this.streamBufState.Length, this.WriteAsyncCallback, null);
            }
            catch (Exception)
            {
                this.ShutdownAndClose();
                return;
            }
        }

        /// <summary>
        /// Send the message currently in our send buffer.
        /// </summary>
        private void SendMessage()
        {
            this.SendMessage(0, this.bufferOffset);
        }

        /// <summary>
        /// Start a receive operation.
        /// </summary>
        /// <param name="offset">
        /// The offset into the receive buffer at which to start receiving.
        /// </param>
        /// <param name="max">
        /// The maximum number of bytes to receive in this operation.
        /// </param>
        private void StartReceive(int offset, int max)
        {

            this.streamBufState.SetBuffer(offset, max);
            IAsyncResult result = null;
            try
            {
                result = GetStream().BeginRead(this.streamBufState.Buffer, this.streamBufState.Offset, this.streamBufState.Length, this.ReadAsyncCallback, null);
            }
            catch (Exception)
            {
                // -
                // If something failed, close the connection.
                // -
                this.ShutdownAndClose();
                return;
            }
        }

        /// <summary>
        /// Handler for all message types.
        /// </summary>
        /// <remarks>
        /// Note we must not operate on the passed-in 'data' parameter after
        /// this routine returns, as it just references the receive buffer.
        /// </remarks>
        /// <param name="type">The message type.</param>
        /// <param name="data">The message data.</param>
        /// <returns>
        /// True if we entered send mode as a result of receiving this message,
        /// false if we're staying in receive mode and should re-start receive.
        /// </returns>
        private bool HandleMessage(MessageType type, ArraySegment<byte> data)
        {
#if DEBUG
            logger.Log(
                "HomeService Received '{0}' message with {1} bytes of data.",
                type.ToString(),
                data.Count.ToString());
#endif

            switch (type)
            {
                case MessageType.PadN:
                    break;

                case MessageType.EchoRequest:
                    this.bufferOffset = 0;
                    this.AppendMessage(MessageType.EchoReply, data);
                    this.SendMessage();
                    return true;

                case MessageType.EchoReply:
                    break;

                case MessageType.Version:
                    if (data.Count != sizeof(byte))
                    {
                        this.ShutdownAndClose();
                        return true;
                    }

                    this.peerProtocolVersion = data.Array[data.Offset];
#if DEBUG
                    logger.Log(
                        " HomeService Server is using protocol version #{0}",
                        this.peerProtocolVersion.ToString());
#endif
                    break;

                case MessageType.PleaseIdentify:
#if DEBUG
                    logger.Log("  HomeService Sending Identification message");
#endif
                    this.bufferOffset = 0;
                    this.AppendMessage(
                        MessageType.Identification,
                        System.Text.Encoding.ASCII.GetBytes(this.identifier));
                    this.SendMessage();
                    return true;

                case MessageType.PleaseAuthenticate:
                    if (data.Count != sizeof(byte))
                    {
                        this.ShutdownAndClose();
                        return true;
                    }

                    AuthenticationType authType =
                        (AuthenticationType)data.Array[data.Offset];
#if DEBUG
                    logger.Log(
                        "  HomeService Authentication request is for type {0}",
                        authType.ToString());
                    logger.Log("  HomeService Sending SimpleAuthentication message");
#endif
                    this.bufferOffset = 0;
                    this.AppendMessage(
                        MessageType.SimpleAuthentication,
                        BitConverter.GetBytes(this.simpleAuthentication));
                    this.SendMessage();
                    return true;

                case MessageType.Authenticated:
                    this.bufferOffset = 0;
                    if (this.clientToken == 0)
                    {
#if DEBUG
                        logger.Log("  Sending RegisterService message");
#endif
                        this.AppendMessage(MessageType.RegisterService);
                    }
                    else
                    {
#if DEBUG
                        logger.Log("  Sending ForwardToClient message");
#endif
                        this.AppendMessage(
                            MessageType.ForwardToClient,
                            BitConverter.GetBytes(this.clientToken));
                        this.forwarding = true;
                    }

                    this.SendMessage();
                    return true;

                case MessageType.ClientAwaits:
                    if (data.Count != sizeof(uint))
                    {
                        this.ShutdownAndClose();
                        return true;
                    }

                    uint clientToken = BitConverter.ToUInt32(
                        data.Array,
                        data.Offset);
#if DEBUG
                    logger.Log("  Client with instance token {0} awaits matchup.", clientToken.ToString());
                    logger.Log("  Launching client connection to connect back in and match up with gatekeeper client instance.");
#endif

                    // -
                    // Note about garbage collection (GC).
                    // The ServiceConnection constructor will have initiated
                    // one or more async receive calls by the time it returns.
                    // ReceiveAsync holds a reference to the
                    // SocketAsyncEventArgs object in use, and that object
                    // holds a reference to the ServiceConnection.  Everything
                    // is driven off receive handlers, so some reference to the
                    // new ServiceConnection should remain around until the
                    // forwarding handler is invoked (or connection closed).
                    // -
                    Socket socket = StaticUtilities.CreateConnectedSocket(
                        this.socket.RemoteEndPoint);
                    if (null != socket)
                    {
                        ServiceConnection client = new ServiceConnection(
                            this.sslServerHost,
                            socket,
                            clientToken,
                            this.handler, this.logger);
                    }
                    break;

                default:
#if DEBUG
                    logger.Log("  ** Unexpected message!  Closing! **");
#endif
                    this.ShutdownAndClose();
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Handler for the callback for the asynchronous Stream Write operation.
        /// </summary>
        /// <param name="result"></param>
        private void WriteAsyncCallback(IAsyncResult result)
        {
            try
            {
                GetStream().EndWrite(result);
            }
            catch (Exception)
            {
                // -
                // If something failed, close the connection.
                // -
                this.ShutdownAndClose();
                return;
            }

            if (this.forwarding)
            {
                // -
                // Switch out of this protocol and into forwarding mode.
                // -
                if (!this.handler(this))
                {
                    this.ShutdownAndClose();
                }

                return;
            }

            // -
            // Switch to receive mode.
            // -
            this.bufferOffset = 0;
            this.StartReceive(0, this.buffer.Length);
        }

        /// <summary>
        /// Handler for the callback for the asynchronous Stream Read operation. 
        /// </summary>
        /// <param name="sender">The sender of this event.</param>
        /// <param name="ea">Arguments pertaining to this event.</param>
        private void ReadAsyncCallback(IAsyncResult result)
        {
            int BytesTransferred = 0;
            try
            {
                BytesTransferred = GetStream().EndRead(result);
            }
            catch (Exception)
            {
                // -
                // If something failed, close the connection.
                // -
                this.ShutdownAndClose();
                return;
            }

            // -
            // Receive completed.
            // -
            if (BytesTransferred == 0)
            {
                // -
                // Our peer closed the connection.  Reciprocate.
                // -
                this.ShutdownAndClose();
                return;
            }

            // -
            // We have three cases to deal with at this point:
            //  1. We have a complete message from our peer.
            //  2. We have a partial message,
            //     (a) and our receive buffer is full.
            //     (b) and still have room in our receive buffer.
            // -
            int have = this.streamBufState.Offset + BytesTransferred - this.bufferOffset;
            int parsePoint = this.bufferOffset;
            while (have != 0)
            {
                MessageType type;
                byte length;

                // -
                // Check for special-case of a Pad1 message.
                // -
                type = (MessageType)this.buffer[parsePoint++];
                have--;
                if (type == MessageType.Pad1)
                {
                    this.bufferOffset = parsePoint;
                    continue;
                }

                if (have > 0)
                {
                    // -
                    // We could potentially have a complete message.
                    // -
                    length = this.buffer[parsePoint++];
                    have--;
                    if (have >= length)
                    {
                        // -
                        // We have a complete message (as self-described).
                        // -
                        ArraySegment<byte> data;
                        if (length == 0)
                        {
                            data = new ArraySegment<byte>(this.buffer, 0, 0);
                        }
                        else
                        {
                            data = new ArraySegment<byte>(
                                this.buffer,
                                parsePoint,
                                length);
                            parsePoint += length;
                            have -= length;
                        }

                        if (this.HandleMessage(type, data))
                        {
                            // -
                            // We've switched out of receive mode.
                            // Note: If 'have' is non-zero at this point,
                            // our peer has violated the protocol.
                            // -
                            if (have != 0)
                            {
                                this.ShutdownAndClose();
                            }

                            return;
                        }

                        // -
                        // Still in receive mode, but handled a message.
                        // -
                        this.bufferOffset = parsePoint;
                        continue;
                    }
                }

                // -
                // We have a partial message.
                // -
                if (this.streamBufState.Length == BytesTransferred)
                {
                    // -
                    // Our receive buffer is full.  Shift the start of
                    // the current partial message down to the zero index.
                    // -
                    int partialLength = this.buffer.Length
                        - this.bufferOffset;
                    Array.Copy(
                        this.buffer,
                        this.bufferOffset,
                        this.buffer,
                        0,
                        partialLength);

                    this.bufferOffset = 0;
                    this.StartReceive(
                        partialLength,
                        this.buffer.Length - partialLength);
                    return;
                }

                // -
                // Start another receive to fill in the buffer from where
                // the last one left off.
                // -
                this.StartReceive(
                    this.streamBufState.Offset + BytesTransferred,
                    this.streamBufState.Length - BytesTransferred);
                return;
            }

            // -
            // We had an integral number of messages (no partial messages).
            // We're expecting another message, so restart receive.
            // -
            this.bufferOffset = 0;
            this.StartReceive(0, this.buffer.Length);
        }

        /// <summary>
        /// Completes outstanding operations on, and then closes, our socket.
        /// </summary>
        private void ShutdownAndClose()
        {
            try
            {
                this.socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // -
                // Shutdown will throw if the socket is already closed.
                // This is benign, so we just swallow the exception.
                // -
            }

            this.socket.Close();
        }
    }
}
