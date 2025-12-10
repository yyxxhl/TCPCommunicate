using TCPCommunicate.Comm;
using TCPCommunicate.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPCommunicate.Client
{
    internal class TcpClient : BaseTcpClient
    {
        // Signals a connection.
        private AutoResetEvent autoConnectEvent = new AutoResetEvent(false);

        /// <summary>
        /// recv socket回调事件
        /// </summary>
        public Action<CmdPacket> ReceivedPacket;

        /// <summary>
        /// Server在线状态
        /// </summary>
        public Action<bool> ServerState;

        /// <summary>
        /// 广播的客户端在线状态
        /// </summary>
        public Action<List<ClientState>> FunClientState;

        private bool flagServerCon = false;

        /// <summary>
        /// 心跳发送Timer 10秒间隔
        /// </summary>
        private System.Timers.Timer HeardbeatTimer = new System.Timers.Timer();

        /// <summary>
        /// 自动重连次数记录
        /// </summary>
        private int autoConnNum;

        /// <summary>
        /// 是否自动连接中
        /// </summary>
        private bool isAutoConning = false;

        /// <summary>
        /// 接收到重复登录后的消息 不再自动连接
        /// </summary>
        private bool isStop = false;

        ///// <summary>
        ///// 连接超时时间(单位毫秒)
        ///// </summary>
        //private int timeOutTicks = 3000;
        private DateTime dateTimeLast = DateTime.Now;

        /// <summary>
        /// 当前客户端状态
        /// </summary>
        private int clientType;

        /// <summary>
        /// 当前客户端ID
        /// </summary>
        private Guid guid = Guid.NewGuid();

        private string description = "客户端";

        private ConcurrentDictionary<Guid, ClientState> dicClient = new ConcurrentDictionary<Guid, ClientState>();

        public TcpClient()
        {
            HeardbeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(HeardbeatTimer_Elapsed);
            HeardbeatTimer.Interval = HandCmd.TimerTick;
            AutoConnTask();
        }

        /// <summary>
        /// Socket心跳数据包
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HeardbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isStop)
            {
                HeardbeatTimer.Stop();
                return;
            }
            DateTime last = DateTime.Now.AddMilliseconds(-(HandCmd.TimerTick * 3));
            if (last > dateTimeLast)
            {
                dateTimeLast = DateTime.Now;
                Dispose("长时间未收到消息.......");
                return;
            }
            var bRet = this.BeginSend(HandCmd.BytesHeart());
        }

        private void AutoConnTask()
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    while (!isStop)
                    {
                        Thread.Sleep(HandCmd.TimerTick * 3);
                        if (isAutoConning && !isStop)
                        {
                            if (ServerState != null && flagServerCon)
                            {
                                flagServerCon = false;
                                ServerState.Invoke(flagServerCon);
                            }
                            autoConnNum++;
                            TraceHelper.Instance.Info("正在自动重连中..." + autoConnNum);
                            StartConneting();
                            autoConnectEvent.WaitOne(HandCmd.TimerTick * 3, false);//信号等待中
                        }
                    }
                });
            }
            catch (Exception e)
            {
                TraceHelper.Instance.Error(e.Message, e);
            }
        }

        /// <summary>
        /// 开始连接
        /// </summary>
        /// <param name="serverIp"></param>
        /// <param name="port"></param>
        public void StartConneting(string serverIp, int port, int clientType, string strDes = "客户端", string strLocalIP = "", int nPort = 0)
        {
            connectIp = serverIp;
            connectPort = port;
            this.clientType = clientType;
            base.LocalIP = strLocalIP;
            base.LocalPort = nPort;
            this.description = strDes;
            this.Connect();
        }

        public void StartConneting()
        {
            guid = Guid.NewGuid();
            this.Connect();
        }

        protected override void OnConnectCallBack(bool conn)
        {
            if (conn)
            {
                BeginSend(OnHandshakeCmd());
                isAutoConning = false;
                autoConnNum = 0;
                dateTimeLast = DateTime.Now;
                HeardbeatTimer.Start();
            }
            if (isAutoConning)
            {
                autoConnectEvent.Set();
            }
        }

        /// <summary>
        /// 开始发送CmdPacket命令
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public bool SendData(CmdPacket packet)
        {
            return BeginSend(DataPacket.WriteData(packet));
        }

        public bool SendData(CmdPacket packet, string strIp, int nPort = 0)
        {
            foreach (var item in dicClient)
            {
                if (item.Value.ClientIP == strIp && (nPort == item.Value.ClientPort || nPort == 0))
                {
                    packet.MsgDstByGuid = item.Key;
                    BeginSend(DataPacket.WriteData(packet));
                }
            }
            return true;
        }

        /// <summary>
        /// 开始发送数据
        /// </summary>
        /// <param name="data"></param>
        private bool BeginSend(byte[] data)
        {
            return base.Send(data);
        }

        protected override void OnTcpClientNotifyReceivedFrame(byte[] buffer, int byteTransferred)
        {
            byte[] doByte;
            msgBuffer.Push(buffer, byteTransferred);
            while (msgBuffer.Pop(out doByte) == 0)
            {
                int netTotalLen = BitConverter.ToInt32(doByte, 0);
                int totalLen = IPAddress.NetworkToHostOrder(netTotalLen);
                if (totalLen == 1)//心跳包，丢弃
                {
                    dateTimeLast = DateTime.Now;
                    string transData = Encoding.Default.GetString(doByte, 4, 1);
                    if (transData == "a")
                    {
                        TraceHelper.Instance.Info("接收到Server Send 心跳包...(一个字节)");
                    }
                    return;
                }
                else
                {
                    CmdPacket cmdRecvPacket = DataPacket.ReadData(doByte);
                    if (cmdRecvPacket != null && ((cmdRecvPacket.MsgDstByType & clientType) != 0 || cmdRecvPacket.MsgDstByGuid == guid))
                    {
                        dateTimeLast = DateTime.Now;
                        if (cmdRecvPacket.MsgType == ClientState.TypeByte)
                        {
                            TraceHelper.Instance.Info("接收到Server Send 状态包，" + cmdRecvPacket.StringBuff);
                            var state = JsonConvert.DeserializeObject<List<ClientState>>(cmdRecvPacket.StringBuff);
                            if (ServerState != null)
                            {
                                if (state.FirstOrDefault(a => a.ClientGuid == guid && a.ClientType == clientType) == null)
                                {
                                    if (flagServerCon)
                                    {
                                        flagServerCon = false;
                                        ServerState.Invoke(flagServerCon);
                                    }
                                }
                                else
                                {
                                    if (!flagServerCon)
                                    {
                                        flagServerCon = true;
                                        ServerState.Invoke(flagServerCon);
                                    }
                                }
                            }
                            if (FunClientState != null)
                            {
                                FunClientState.Invoke(state);
                            }
                            dicClient = new ConcurrentDictionary<Guid, ClientState>();
                            foreach (var item in state)
                            {
                                if (!dicClient.ContainsKey(item.ClientGuid))
                                {
                                    dicClient.TryAdd(item.ClientGuid, item);
                                }
                            }
                        }
                        else if (ReceivedPacket != null)
                        {
                            try
                            {
                                ReceivedPacket.Invoke(cmdRecvPacket);
                            }
                            catch (Exception ex)
                            {
                                TraceHelper.Instance.Warning("解析包数据出现错误(ReceivedPacket): ", ex);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 收到服务端CRoute返回握手的数据
        /// </summary>
        /// <param name="ReadByte">字节</param>
        /// <param name="Len">长度</param>
        protected override void OnHandshakeCmdArrived(byte[] ReadByte, int Len)
        {
            int byteType;
            Guid guid;
            string strDes;
            dateTimeLast = DateTime.Now;
            if (HandCmd.OnHandCmdCheck(ReadByte, out byteType, out guid, out strDes))
            {
                m_bValidate = true;
                TraceHelper.Instance.Info("接收到Server Send 握手包...");
            }
        }

        /// <summary>
        ///发送握手请求 Cmd
        /// </summary>
        private byte[] OnHandshakeCmd()
        {
            return HandCmd.OnHandCmdSend(clientType, guid, description);
        }

        public override void OnDisConnection()
        {
            HeardbeatTimer.Stop();
            isAutoConning = true;
        }

        public void RepeateLogin()
        {
            isStop = true;
            HeardbeatTimer.Stop();
            TraceHelper.Instance.Warning("接收到重复登录消息,此Client被关闭!");
        }
    }
}