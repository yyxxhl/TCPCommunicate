using System;
using System.Net;
using System.Net.Sockets;
using TCPCommunicate.Comm;

namespace TCPCommunicate.Client
{
    /// <summary>
    /// TCP的基类
    /// </summary>
    internal abstract class BaseTcpClient
    {
        // Flag for connected socket.
        private Boolean _connected = false;

        /// <summary>
        /// 标示是否第一次握手
        /// </summary>
        protected bool m_bValidate = false;

        /// <summary>
        /// 连接服务器IP
        /// </summary>
        protected string connectIp;

        /// <summary>
        /// 服务端口号
        /// </summary>
        protected int connectPort;

        /// <summary>
        /// 当前本地的ip
        /// </summary>
        protected string LocalIP;

        /// <summary>
        /// 当前本地的ip
        /// </summary>
        protected int LocalPort;

        /// <summary>
        /// 当前本地的ip和端口，从socket取的
        /// </summary>
        protected string SocketIPAndPort = "";

        public string GetSocketIP()
        {
            if (SocketIPAndPort.Contains(":"))
            {
                return SocketIPAndPort.Split(':')[0];
            }
            return "";
        }

        public int GetSocketPort()
        {
            if (SocketIPAndPort.Contains(":"))
            {
                return Convert.ToInt32(SocketIPAndPort.Split(':')[1]);
            }
            return 0;
        }

        //// Signals a connection.
        //private AutoResetEvent connectEvent =
        //                      new AutoResetEvent(false);
        protected Socket sock;

        private IPEndPoint serverFullAddr;

        protected MsgBuffer msgBuffer = new MsgBuffer();

        public bool Connected
        {
            get
            {
                return _connected;
            }

            set
            {
                _connected = value;
            }
        }

        public BaseTcpClient()
        {
        }

        /// <summary>
        /// 开始创建连接的Socket
        /// </summary>
        /// <returns></returns>
        protected void Connect()
        {
            try
            {
                //创建TCP Socket
                serverFullAddr = new IPEndPoint(IPAddress.Parse(connectIp), connectPort);
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (LocalIP != "")
                {
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    sock.Bind(new IPEndPoint(IPAddress.Parse(LocalIP), 0));
                    SocketIPAndPort = sock.LocalEndPoint.ToString();
                    System.Threading.Thread.Sleep(1000);
                }
                m_bValidate = false;
                Connected = false;
                SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
                connectArgs.SocketError = SocketError.SocketError;
                connectArgs.UserToken = sock;
                connectArgs.RemoteEndPoint = serverFullAddr;
                //建立与远程主机的连接
                //connectArgs.Completed -= OnConnect;
                connectArgs.Completed += OnConnect;
                sock.ConnectAsync(connectArgs);
            }
            catch (System.Exception ex)
            {
                Connected = false;
                TraceHelper.Instance.Error("Connect is Error...", ex);
            }
            TraceHelper.Instance.Info("Starting Connect To Server......");
        }

        // Calback for connect operation
        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                // Set the flag for socket connected.
                this.Connected = (e.SocketError == SocketError.Success);
                TraceHelper.Instance.Info("OnConnect CallBack Result..." + e.SocketError);
                // Signals the end of connection.
                //connectEvent.Set();
                if (Connected)
                {
                    SocketAsyncEventArgs argsRecv = new SocketAsyncEventArgs();
                    byte[] buffer = new byte[HandCmd.BufferSize];
                    argsRecv.SetBuffer(buffer, 0, buffer.Length);
                    argsRecv.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);
                    e.ConnectSocket.ReceiveAsync(argsRecv);
                    Connected = true;
                    var sock = sender as Socket;
                    SocketIPAndPort = sock.LocalEndPoint.ToString();
                }
            }
            catch (Exception exception)
            {
                TraceHelper.Instance.Error("OnConnect is Exception...   ", exception);
                this.Connected = false;
                //this.Dispose("OnConnect is Exception...");
            }
            finally
            {
                OnConnectCallBack(this.Connected);
                if (this.Connected == false)
                {
                    this.Dispose("OnConnect After Dispose...");
                }
            }
        }

        protected virtual void OnConnectCallBack(bool isConn)
        {
        }

        // Exchange a message with the host.
        protected bool Send(byte[] data)
        {
            if (data == null)
            {
                return Connected;
            }
            try
            {
                if (Connected && sock.Connected)
                {
                    //int datalen = data.Length / HandCmd.BufferSize;
                    //int outsize = data.Length % HandCmd.BufferSize;
                    //for (int i = 0; i < datalen; i++)
                    //{
                    //    sock.BeginSend(data, HandCmd.BufferSize * i, HandCmd.BufferSize, SocketFlags.None, new AsyncCallback(SendComplated), sock);
                    //}
                    //sock.BeginSend(data, HandCmd.BufferSize * datalen, outsize, SocketFlags.None, new AsyncCallback(SendComplated), sock);
                    sock.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendComplated), sock);
                }
            }
            catch (SocketException exSocket)
            {
                Connected = false;
                //记录日志
                //var v = exSocket.SocketErrorCode;//10053
                //此连接由 .NET Framework 或基础套接字提供程序中止。
                if (exSocket.SocketErrorCode != SocketError.Success)
                {
                    this.Dispose("Send数据时断开........" + exSocket.ToString());
                    //OnDisConnection();
                }
            }
            return Connected;
        }

        private void SendComplated(IAsyncResult async)
        {
            try
            {
                Socket _skt = async.AsyncState as Socket;
                if (_skt.Connected && Connected)
                {
                    _skt.EndSend(async);
                }
            }
            catch (SocketException ex)
            {
                TraceHelper.Instance.Error("SendComplated is SocketException...", ex);
            }
        }

        #region IDisposable 成员

        protected void Dispose(string message)
        {
            message = message + "本地IP和端口 " + SocketIPAndPort;
            try
            {
                if (this.Connected)
                {
                    this.Connected = false;
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Disconnect(true);
                    msgBuffer.Clear();
                }
                TraceHelper.Instance.Info("connected=" + Connected + "   " + message);
            }
            catch (Exception ex)
            {
                TraceHelper.Instance.Error("BaseTcpClient:Dispose message=" + message, ex);
            }
            finally
            {
                sock.Dispose();
            }
            OnDisConnection();
        }

        #endregion IDisposable 成员

        /// <summary>
        /// 正常数据通信
        /// </summary>
        /// <param name="buffer">数据缓冲</param>
        /// <param name="byteTransferred">数据长度</param>
        protected abstract void OnTcpClientNotifyReceivedFrame(byte[] buffer, int byteTransferred);

        /// <summary>
        /// 握手数据通信
        /// </summary>
        /// <param name="ReadByte">数据</param>
        /// <param name="Len">数据长度</param>
        protected abstract void OnHandshakeCmdArrived(byte[] ReadByte, int Len);

        /// <summary>
        /// 底层Socket断开释放资源后调用
        /// </summary>
        public virtual void OnDisConnection()
        {
            sock.Close();
        }

        // Calback for receive operation
        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of receive.
            try
            {
                if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
                {
                    if (m_bValidate)
                    {
                        OnTcpClientNotifyReceivedFrame(e.Buffer, e.BytesTransferred);
                    }
                    else
                    {
                        //第一次握手cRouter 特殊格式解析命令的方式
                        this.OnHandshakeCmdArrived(e.Buffer, e.BytesTransferred);
                        m_bValidate = true;
                    }
                    if (sock.Connected)
                    {
                        sock.ReceiveAsync(e);
                    }
                }
                else
                {
                    //服务端断开 进行重连接
                    Dispose("OnReceive断开.......BytesTransferred=" + e.BytesTransferred);
                }
            }
            catch (Exception ex)
            {
                //服务端断开 进行重连接
                Dispose("OnReceive Exception 服务端断开........" + ex.Message);
            }
        }
    }
}