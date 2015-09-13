/* Copyright (C) 2015 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IBApi
{
    public class EClientSocket : EClient,  EClientMsgSink
    {
        private int port;

        public EClientSocket(EWrapper wrapper, EReaderSignal eReaderSignal):
            base(wrapper)
        {
            this.eReaderSignal = eReaderSignal;
        }

        void EClientMsgSink.serverVersion(int version, string time)
        {
            base.serverVersion = version;

            if (!useV100Plus)
            {
                if (!CheckServerVersion(MinServerVer.MIN_VERSION, ""))
                {
                    ReportUpdateTWS("");
                    return;
                }
            }
            else if (serverVersion < Constants.MinVersion || serverVersion > Constants.MaxVersion)
            {
                wrapper.error(clientId, EClientErrors.UNSUPPORTED_VERSION.Code, EClientErrors.UNSUPPORTED_VERSION.Message);
                return;
            }

            if (serverVersion >= 3)
            {
                if (serverVersion < MinServerVer.LINKING)
                {
                    List<byte> buf = new List<byte>();

                    buf.AddRange(UTF8Encoding.UTF8.GetBytes(clientId.ToString()));
                    buf.Add(Constants.EOL);
                    socketTransport.Send(new EMessage(buf.ToArray()));
                }
            }

            ServerTime = time;
            isConnected = true;

            if (!this.AsyncEConnect)
                startApi();
        }

        public void eConnect(string host, int port, int clientId)
        {
            eConnect(host, port, clientId, false);
        }

        protected virtual Stream createClientStream(string host, int port)
        {
            return new TcpClient(host, port).GetStream();
        }

        public void eConnect(string host, int port, int clientId, bool extraAuth)
        {
            if (isConnected)
            {
                wrapper.error(IncomingMessage.NotValid, EClientErrors.AlreadyConnected.Code, EClientErrors.AlreadyConnected.Message);
                return;
            }
            try
            {
                tcpStream = createClientStream(host, port);
                this.port = port;
                socketTransport = new ESocket(tcpStream);

                this.clientId = clientId;
                this.extraAuth = extraAuth;

                sendConnectRequest();

                if (!AsyncEConnect)
                {
                    var eReader = new EReader(this, eReaderSignal);

                    while (serverVersion == 0 && eReader.putMessageToQueue())
                    {
                        eReaderSignal.waitForSignal();
                        eReader.processMsgs();
                    }
                }
            }
            catch (ArgumentNullException ane)
            {
                wrapper.error(ane);
            }
            catch (SocketException se)
            {
                wrapper.error(se);
            }
            catch (EClientException e)
            {
                var cmp = (e as EClientException).Err;

                wrapper.error(-1, cmp.Code, cmp.Message);
            }
            catch (Exception e)
            {
                wrapper.error(e);
            }
        }

        private EReaderSignal eReaderSignal;

        protected override uint prepareBuffer(BinaryWriter paramsList)
        {
            var rval = (uint)paramsList.BaseStream.Position;

            if (this.useV100Plus)
                paramsList.Write((int)0);

            return rval;
        }

        protected override void CloseAndSend(BinaryWriter request, uint lengthPos)
        {
            if (useV100Plus)
            {
                request.Seek((int)lengthPos, SeekOrigin.Begin);
                request.Write(IPAddress.HostToNetworkOrder((int)(request.BaseStream.Length - lengthPos - sizeof(int))));
            }

            request.Seek(0, SeekOrigin.Begin);

            var buf = new MemoryStream();
            
            request.BaseStream.CopyTo(buf);
            socketTransport.Send(new EMessage(buf.ToArray()));
        }

        public void redirect(string host)
        {
            if (!allowRedirect)
            {
                wrapper.error(clientId, EClientErrors.CONNECT_FAIL.Code, EClientErrors.CONNECT_FAIL.Message);
                return;
            }

            var srv = host.Split(':');

            if (srv.Length > 1)
                if (!int.TryParse(srv[1], out port))
                    throw new EClientException(EClientErrors.BAD_MESSAGE);

            eDisconnect();
            eConnect(srv[0], port, clientId, extraAuth);

            return;
        }
    }
}
