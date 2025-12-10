using TCPCommunicate.Comm;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TCPCommunicate.Server
{
    /// <summary>
    /// https://msdn.microsoft.com/zh-cn/library/system.net.sockets.socketasynceventargs(v=vs.110).aspx
    /// https://msdn.microsoft.com/zh-cn/library/system.net.sockets.socketasynceventargs.socketasynceventargs(v=vs.110).aspx
    /// https://msdn.microsoft.com/zh-cn/library/bb517542.aspx
    /// </summary>
    internal class SocketServer
    {
        private string serverIP = "127.0.0.1";

        private int serverPort = 9090;

        private Socket socket;

        private SocketConnect _connectedStatus;

        private ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// 连接状态
        /// </summary>
        public SocketConnect ConnectedStatus
        {
            get
            {
                return _connectedStatus;
            }

            private set
            {
                _connectedStatus = value;
            }
        }

        public SocketServer()
        {
        }

        public void StartServer(string _ip, int _port)
        {
            serverIP = _ip;
            serverPort = _port;
            if (socket == null)
            {
                ThreadStart infots = new ThreadStart(Start);
                Thread infothread = new Thread(infots);
                infothread.IsBackground = true;
                infothread.Start();
            }
        }

        /// <summary>
        /// 开启服务监听
        /// </summary>
        private void Start()
        {
            try
            {
                TraceHelper.Instance.Info("SockServer服务启动..." + serverIP + "   " + serverPort);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.ReceiveBufferSize = 1024 * 2;
                socket.SendBufferSize = 1024 * 2;
                socket.Bind(new IPEndPoint(IPAddress.Any, serverPort));
                //socket.Bind(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Listen(30);
                Connected(SocketConnect.Successful, "服务启动成功");
                TraceHelper.Instance.Info("SockServer服务启动成功..." + serverIP + "   " + serverPort);
                while (true)
                {
                    socket.BeginAccept(new AsyncCallback(ClientConnectComplete), socket);
                    allDone.WaitOne();
                    //Close();
                }
            }
            catch (SocketException socketEx)
            {
                if (socketEx.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Connected(SocketConnect.Fail, "该端口(" + serverPort + ")已经被占用");
                    PortUsingKill.CheckPortUsedToKill(serverPort);
                }
                else
                {
                    Connected(SocketConnect.Fail, socketEx.Message);
                }
                TraceHelper.Instance.Error("服务启动异常(SocketException)......", socketEx);
            }
            catch (Exception ex)
            {
                TraceHelper.Instance.Error("服务启动异常......", ex);
                Connected(SocketConnect.Fail, "服务启动异常");
            }
        }

        private void ClientConnectComplete(IAsyncResult async)
        {
            try
            {
                ClientWrapper client = new ClientWrapper();
                client.SocketObj = socket.EndAccept(async);
                if (client.SocketObj.Connected == true)
                {
                    #region【记录登陆信息】
                    client.IP = (client.SocketObj.RemoteEndPoint as IPEndPoint).Address.ToString();
                    client.Port = (client.SocketObj.RemoteEndPoint as IPEndPoint).Port;
                    client.IPPort = client.IP + ":" + client.Port;
                    TCPServer.Instance.Add(client, false);
                    TraceHelper.Instance.Info(string.Format("总共连接数{0} 本次连接信息:{1}:{2}", TCPServer.Instance.ClientCount(), client.IP, client.IPPort));
                    #endregion
                }
                SocketAsyncEventArgs argsRecv = new SocketAsyncEventArgs();
                argsRecv.SetBuffer(client.Buffer, 0, client.Buffer.Length);
                argsRecv.UserToken = client;
                argsRecv.Completed += new EventHandler<SocketAsyncEventArgs>(client.OnReceiveComplete);
                client.SocketArgs = argsRecv;
                client.SocketObj.ReceiveAsync(argsRecv);
                //client.SocketObj.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, new AsyncCallback(client.ClientReceiveComplete), client);
                socket.BeginAccept(ClientConnectComplete, null);
            }
            catch (SocketException ex)
            {
                TraceHelper.Instance.Error("Socket连接异常" + ex);
            }
            catch (Exception ex)
            {
                TraceHelper.Instance.Error("Socket其他信息异常" + ex);
            }
        }

        /// <summary>
        /// 连接的回调函数
        /// </summary>
        /// <param name="connStatus"></param>
        /// <param name="reason"></param>
        private void Connected(SocketConnect connStatus, string reason)
        {
            this.ConnectedStatus = connStatus;
            if (TCPServer.Instance.SvrSocketConnectedStatus != null)
            {
                TCPServer.Instance.SvrSocketConnectedStatus.Invoke((int)connStatus, reason);
            }
        }
    }

    public enum SocketConnect
    {
        /// <summary>
        /// 未知错误
        /// </summary>
        None = 0,

        /// <summary>
        /// 成功
        /// </summary>
        Successful = 1,

        /// <summary>
        /// 失败
        /// </summary>
        Fail = 2,
    }
}