using TCPCommunicate.Comm;
using TCPCommunicate.Interface;
using System;
using System.Net.Sockets;

namespace TCPCommunicate.Server
{
    internal class ClientWrapper
    {
        ///// <summary>
        ///// 当前的处理对象
        ///// </summary>
        //private BaseProcessors currentProc;

        private SocketAsyncEventArgs _socketArgs;

        private Guid _id;

        public ClientWrapper()
        {
            ConnectTime = DateTime.Now;
            ConnectFirstTime = DateTime.Now;
            _id = Guid.NewGuid();
        }

        private bool _isValiadte;

        /// <summary>
        /// 连接对象
        /// </summary>
        private Socket _socketObj;

        /// <summary>
        /// IP地址
        /// </summary>
        public string IP;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port;

        /// <summary>
        /// 链接的客户端类型 四字节，每一字节的每一位代表一个客户端类型;
        /// </summary>
        public int ClientType;

        /// <summary>
        /// 客户端描述
        /// </summary>
        public string ClientDes;

        /// <summary>
        /// 当前对象连接的时间
        /// </summary>
        public DateTime ConnectTime { private set; get; }

        /// <summary>
        /// 第一次连接的时间
        /// </summary>
        public DateTime ConnectFirstTime { private set; get; }

        /// <summary>
        /// 是否连接
        /// </summary>
        private bool IsConnected = true;

        /// <summary>
        /// 数据缓存
        /// </summary>
        public byte[] Buffer = new byte[HandCmd.BufferSize];

        /// <summary>
        /// IP和Port
        /// </summary>
        public string IPPort { set; get; }

        /// <summary>
        /// 数据缓存buffer
        /// </summary>
        public MsgBuffer DataBuffer = new MsgBuffer();

        /// <summary>
        /// 是否有效(握手成功)
        /// </summary>
        public bool IsValiadte
        {
            get
            {
                return _isValiadte;
            }

            set
            {
                _isValiadte = value;
            }
        }

        /// <summary>
        /// 客户端的连接对象
        /// </summary>
        public Socket SocketObj
        {
            get
            {
                return _socketObj;
            }

            set
            {
                _socketObj = value;
            }
        }

        public SocketAsyncEventArgs SocketArgs
        {
            get
            {
                return _socketArgs;
            }

            set
            {
                _socketArgs = value;
            }
        }

        /// <summary>
        /// 唯一Id
        /// </summary>
        public Guid Id
        {
            get
            {
                return _id;
            }

            private set
            {
                _id = value;
            }
        }

        /// <summary>
        /// 接收处理包数据
        /// </summary>
        public void RecievedData(ClientWrapper client)
        {
            byte[] doByte;
            int net = client.DataBuffer.Pop(out doByte);
            ConnectTime = DateTime.Now;
            while (net == 0)
            {
                int clientType;
                Guid guid;
                string strDes;
                if (!client.IsValiadte && HandCmd.OnHandCmdCheck(doByte, out clientType, out guid, out strDes))//握手
                {
                    client.IsValiadte = true;
                    client.ClientType = clientType;
                    client.Id = guid;
                    client.ClientDes = strDes;
                    //收到注册包
                    TraceHelper.Instance.Info("接收到Client握手包..." + client.IPPort);
                    client.Send(doByte);
                    TCPServer.Instance.Add(client, true);
                    //返回注册包
                    return;
                }
                if (client.IsValiadte == false)
                {
                    return;
                }
                else if (doByte.Length == 5)
                {
                    TraceHelper.Instance.Info("接收到Client心跳包..." + client.IPPort);
                }
                else
                {
                    CmdPacket cmdPacket = DataPacket.ReadData(doByte);
                    TCPServer.Instance.DataArrived(this, cmdPacket, doByte.Length);
                }
                net = client.DataBuffer.Pop(out doByte);
            }
        }

        public void OnReceiveComplete(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of receive.
            int _receiveCount = 0;
            ClientWrapper client = e.UserToken as ClientWrapper;
            try
            {
                _receiveCount = e.BytesTransferred;
                if (e.SocketError == SocketError.Success && _receiveCount > 0)
                {
                    client.DataBuffer.Push(e.Buffer, _receiveCount);
                    client.RecievedData(client);
                    if (client.IsConnected)
                    {
                        Array.Clear(e.Buffer, 0, e.Buffer.Length);
                        client.SocketObj.ReceiveAsync(e);
                    }
                }
                else if (e.SocketError == SocketError.ConnectionReset)
                {
                    TCPServer.Instance.Close(this, "客户端主动断开连接, IPPort: " + client.IPPort);
                }
                else
                {
                    TCPServer.Instance.Close(this, "客户端主动断开连接, IPPort: " + client.IPPort + ",SocketError:" + e.SocketError.ToString() + ",LastOperation:" + e.LastOperation.ToString());
                }
            }
            catch (SocketException ex)
            {
                TraceHelper.Instance.Error("ClientWrapper.ClientWrapper.OnReceiveComplete is SocketException " + IPPort, ex);
                TCPServer.Instance.Close(this, "接收数据异常断开连接OnReceiveComplete: ReceiveCount=" + _receiveCount + "  IPPort: " + client.IPPort);
            }
            catch (OverflowException ex)
            {
                TraceHelper.Instance.Error("ClientWrapper.ClientWrapper.OnReceiveComplete is OverflowException " + IPPort + ";;;StackTrace=" + ex.StackTrace, ex);
            }
            catch (Exception ex)
            {
                TraceHelper.Instance.Error("ClientWrapper.ClientWrapper.OnReceiveComplete is Exception " + IPPort, ex);
            }
        }

        /// <summary>
        /// Socket发送异步回调
        /// </summary>
        /// <param name="async"></param>
        private void SendComplete(IAsyncResult async)
        {
            ClientWrapper client = async.AsyncState as ClientWrapper;
            try
            {
                if (client.SocketObj.Connected)
                {
                    client.SocketObj.EndSend(async);
                    this.IsConnected = client.SocketObj.Connected;
                }
                else
                {
                    this.IsConnected = client.SocketObj.Connected;
                    TraceHelper.Instance.Info("SendComplete Connected 已经断开连接 " + this.IPPort);
                }
            }
            catch (Exception ex)
            {
                string msg = "ClientWrapper.SendComplete is SocketException";
                TCPServer.Instance.Close(this, "发送数据异常断开连接OnReceiveComplete: IsConnected=" + this.IsConnected + "  IPPort: " + client.IPPort + "  IsValiadte:" + IsValiadte);
                TraceHelper.Instance.Error(msg, ex);
            }
        }

        /// <summary>
        /// Socket发送字节数据
        /// </summary>
        /// <param name="bytesToSend"></param>
        public bool Send(byte[] data)
        {
            //if (IsConnected)
            //{
            //    SocketObj.BeginSend(bytesToSend, 0, bytesToSend.Length, SocketFlags.None, new AsyncCallback(SendComplete), this);
            //}
            if (data == null)
            {
                return IsConnected;
            }
            try
            {
                if (IsConnected && SocketObj.Connected)
                {
                    SocketObj.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendComplete), this);
                }
            }
            catch (SocketException exSocket)
            {
                IsConnected = false;
                //记录日志
                //var v = exSocket.SocketErrorCode;//10053
                //此连接由 .NET Framework 或基础套接字提供程序中止。
                if (exSocket.SocketErrorCode != SocketError.Success)
                {
                    TCPServer.Instance.Close(this, "Send数据时断开..." + this.IPPort + " ErrorCode: " + exSocket.SocketErrorCode + " ErrorMsg:" + exSocket);
                }
            }
            catch (Exception ex)
            {
                TCPServer.Instance.Close(this, "Send数据出现异常..." + this.IPPort + " ErrorCode " + ex);
            }
            return IsConnected;
        }

        /// <summary>
        /// 关闭客户端连接
        /// </summary>
        /// <param name="closemsg"></param>
        public void Close(string closemsg)
        {
            try
            {
                if (IsConnected)
                {
                    SocketArgs.Completed -= OnReceiveComplete;
                    DataBuffer.Clear();
                    IsConnected = false;
                    IsValiadte = false;
                    if (SocketObj.Connected)
                    {
                        SocketObj.Shutdown(SocketShutdown.Both);
                        SocketObj.Disconnect(false);
                    }
                    SocketObj.Close();
                    SocketObj.Dispose();
                }
            }
            catch (Exception ex)
            {
                TraceHelper.Instance.Error("关闭客户端连接发生异常" + closemsg + "  ", ex);
            }
        }
    }
}