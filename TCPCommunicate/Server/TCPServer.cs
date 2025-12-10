using TCPCommunicate.Comm;
using TCPCommunicate.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TCPCommunicate.Server
{
    internal class TCPServer
    {
        /// <summary>
        /// 单位毫秒
        /// </summary>
        private static TCPServer _instance = new TCPServer();

        private List<ClientWrapper> OnLineUser = new List<ClientWrapper>();//所有登陆客户端
        private static readonly object clientlock = new object();
        private SocketServer _sockSvr = new SocketServer();

        /// <summary>
        /// recv socket回调事件
        /// </summary>
        public Action<int, Guid, CmdPacket> ReceivedPacket;

        /// <summary>
        /// server 监听事件结果回调
        /// </summary>
        public Action<int, string> SvrSocketConnectedStatus { get; set; }

        private ConcurrentDictionary<Guid, ClientState> _dicClientState = new ConcurrentDictionary<Guid, ClientState>();
        private ConcurrentQueue<CmdPacket> _queueSend = new ConcurrentQueue<CmdPacket>();
        public Action<List<ClientState>> ActionClientState;
        public Action<ClientWrapper> ClientOnLineEvent { get; set; }
        public Action<Guid> ClientOffLineEvent { get; set; }
        public Action<Guid, bool, int> DataTransmitEvent { get; set; }

        public static TCPServer Instance
        {
            get
            {
                return _instance;
            }
        }

        public SocketServer SockSvr
        {
            get
            {
                return _sockSvr;
            }
            set
            {
                _sockSvr = value;
            }
        }

        private TCPServer()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(HandCmd.TimerTick);//清理异常断开的资源
                    timerCallback();
                }
            }).ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    TraceHelper.Instance.Error(t.Exception?.Message, t.Exception);
                }
            });
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    //发送状态
                    var clientListTmp = _dicClientState.Select(a => a.Value).ToList();
                    SendMsg(new CmdPacket(ClientState.TypeByte, clientListTmp, int.MaxValue));
                    if (ActionClientState != null)
                    {
                        ActionClientState.Invoke(clientListTmp);
                    }
                    Thread.Sleep(HandCmd.TimerTick);
                }
            }).ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    TraceHelper.Instance.Error(t.Exception?.Message, t.Exception);
                }
            });
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (_queueSend.Count == 0)
                    {
                        Thread.Sleep(100);
                    }
                    else
                    {
                        CmdPacket cmdPacket;
                        _queueSend.TryDequeue(out cmdPacket);
                        SendCmdToClient(cmdPacket);
                    }
                }
            }).ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    TraceHelper.Instance.Error(t.Exception?.Message, t.Exception);
                }
            });

            if (ConfigurationManager.AppSettings["IsReStart"] == null || ConfigurationManager.AppSettings["IsReStart"].ToString() == "true")
            {
                SendSelfStatus();
            }
        }

        private void SendSelfStatus()
        {
            Guid guid = Guid.NewGuid();
            _dicClientState.TryAdd(guid, new ClientState() { ClientGuid = guid, ClientType = 0x40000000, ClientIP = "127.0.0.1", ClientPort = 8905, ClientDes = "Server" });
        }

        private void timerCallback()
        {
            try
            {
                DateTime last = DateTime.Now.AddMilliseconds(-(HandCmd.TimerTick * 3));
                List<ClientWrapper> outClients;
                lock (clientlock)
                {
                    outClients = OnLineUser.Where(m => m.ConnectTime <= last).ToList();
                }
                for (int i = outClients.Count - 1; i >= 0; i--)
                {
                    var tmp = outClients[i];
                    this.Close(tmp, "长时间未收到心跳包，关闭客户端" + tmp.IPPort);
                }
            }
            catch (Exception ex)
            {
                TraceHelper.Instance.Error("TimerOut_Tick: " + ex);
            }
        }

        /// <summary>
        /// 重复登录
        /// </summary>
        /// <param name="client"></param>
        private bool CheckRepeatLogin(ClientWrapper client)
        {
            bool bret = false;
            //var repeatClient = OnLineUser.FirstOrDefault(m => m.IsValiadte && m.IP == client.IP && m.Id != client.Id);
            //if (repeatClient != null)
            //{
            //    bret = true;
            //    CmdPacket req = new CmdPacket(Xns.XNS_Connect, Cmd.Cmd_Connect_RepeatLogin);
            //    SendCmdToSingleClient(client.Id, req);
            //    //Thread.Sleep(1000);
            //    Close(client, "重复登录踢掉已经登录的客户端" + client.IPPort);
            //}
            return bret;
        }

        /// <summary>
        /// 关闭客户端连接
        /// </summary>
        /// <param name="client"></param>
        /// <param name="closemsg"></param>
        public void Close(ClientWrapper client, string closemsg)
        {
            Remove(client);
            client.Close(closemsg);
            TraceHelper.Instance.Warning("关闭客户端连接消息来自:" + closemsg);
        }

        /// <summary>
        /// 存储客户端连接的列表
        /// </summary>
        /// <param name="client"></param>
        /// <param name="isValiadte"></param>
        public void Add(ClientWrapper client, bool isValiadte)
        {
            lock (clientlock)
            {
                if (isValiadte)
                {
                    if (!CheckRepeatLogin(client))
                    {
                        ClientListChanging(client, 1);
                        OnClientOnLine(client);
                    }
                }
                else
                {
                    //AddUser(client);
                    OnLineUser.Add(client);
                }
            }
        }

        private void AddUser(ClientWrapper client)
        {
            var oldClient = OnLineUser.Where(u => u.ClientType == client.ClientType && u.IP == client.IP).FirstOrDefault();
            if (oldClient != null)
            {
                Remove(oldClient);
                OnLineUser.Add(client);
            }
        }

        public void OnClientOnLine(ClientWrapper client)
        {
            ClientOnLineEvent?.Invoke(client);
        }

        public void Remove(ClientWrapper client)
        {
            ClientOffLineEvent?.Invoke(client.Id);
            lock (clientlock)
            {
                ClientListChanging(client, 0);
                OnLineUser.Remove(client);
            }
        }

        /// <summary>
        /// 通知UI客户端发生变化
        /// </summary>
        /// <param name="client"></param>
        /// <param name="isAdd">0移除 1添加</param>
        private void ClientListChanging(ClientWrapper client, int isAdd)
        {
            if (isAdd == 1 && !_dicClientState.ContainsKey(client.Id))
            {
                _dicClientState.TryAdd(client.Id, new ClientState() { ClientGuid = client.Id, ClientType = client.ClientType, ClientIP = client.IP, ClientPort = client.Port, ClientDes = client.ClientDes });
            }
            else if (isAdd == 0 && _dicClientState.ContainsKey(client.Id))
            {
                ClientState state;
                if (!_dicClientState.TryRemove(client.Id, out state))
                {
                    TraceHelper.Instance.Error("移除_dicClientState失败，client.ip=" + client.IP + " IPPort:" + client.IPPort + "  desc=" + client.ClientDes);
                }
            }
        }

        public void DataArrived(ClientWrapper client, CmdPacket cmdPacket, int byteLength)
        {
            try
            {
                if (cmdPacket == null)
                {
                    return;
                }
                TraceHelper.Instance.Info("接收到Client命令:" + cmdPacket.MsgType + "------" + cmdPacket.StringBuff + " IPPort:" + client.IPPort);
                if (cmdPacket.ByteGuidOrType == 0x10)
                {
                    //发给一个guid
                    SendMsg(cmdPacket);
                }
                else if (cmdPacket.MsgDstByType == 0x40000000)
                {
                    //就是发给tcpserver的
                    ReceivedPacket?.Invoke(client.ClientType, client.Id, cmdPacket);
                }
                else
                {
                    SendMsg(cmdPacket);
                    if ((cmdPacket.MsgDstByType & 0x40000000) != 0)
                    {
                        //发送给一类，并且该类信息
                        ReceivedPacket?.Invoke(client.ClientType, client.Id, cmdPacket);
                    }
                }
                DataTransmitEvent?.Invoke(client.Id, false, byteLength);
            }
            catch (Exception ex)
            {
                TraceHelper.Instance.Error("ReceivedPacket处理命令包异常: " + client.IPPort + "    " + ex);
            }
        }

        public int ClientCount()
        {
            lock (clientlock)
            {
                return OnLineUser.Count;
            }
        }

        ///// <summary>
        ///// 发给所有客户端
        ///// </summary>
        ///// <param name="cmdPacket"></param>
        //public void SendCmdToAllClient(CmdPacket cmdPacket)
        //{
        //    lock (clientlock)
        //    {
        //        var outClients = OnLineUser.ToList();
        //        foreach (var item in outClients)
        //        {
        //            try
        //            {
        //                item.Send(cmdPacket.GetData());
        //            }
        //            catch (Exception ex)
        //            {
        //                TraceHelper.Instance.Error("SendCmdToAllClient Exception: " + item.IPPort, ex);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// 单独发给一个客户端
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cmdPacket"></param>
        private void SendCmdToSingleClient(Guid id, CmdPacket cmdPacket)
        {
            string ipPort = " ipPort is empty";
            lock (clientlock)
            {
                try
                {
                    var client = OnLineUser.FirstOrDefault(m => m.Id == id);
                    if (client != null)
                    {
                        ipPort = client.IPPort;
                        client.Send(DataPacket.WriteData(cmdPacket));
                        TraceHelper.Instance.Info("发送到Client命令:" + cmdPacket.MsgType + "------" + cmdPacket.StringBuff + " IPPort:" + client.IPPort);
                    }
                    else
                    {
                        TraceHelper.Instance.Warning("未找到该Id:" + id);
                    }
                }
                catch (Exception ex)
                {
                    TraceHelper.Instance.Error("SendCmdToSingleClient ipPort:" + ipPort, ex);
                }
            }
        }

        public void SendMsg(CmdPacket cmdPacket)
        {
            _queueSend.Enqueue(cmdPacket);
        }

        public bool SendMsg(CmdPacket packet, string strIp, int nPort = 0)
        {
            lock (clientlock)
            {
                foreach (var item in OnLineUser)
                {
                    if (item.IP == strIp && (nPort == 0 || nPort == Convert.ToInt32(item.IPPort)))
                    {
                        packet.MsgDstByGuid = item.Id;
                        SendMsg(packet);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 单独发给一类客户端
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cmdPacket"></param>
        private void SendCmdToClient(CmdPacket cmdPacket)
        {
            string ipPort = " ipPort is empty";
            byte[] data = null;
            try
            {
                if (cmdPacket.ByteGuidOrType == 0x10)
                {
                    lock (clientlock)
                    {
                        var client = OnLineUser.FirstOrDefault(a => a.Id == cmdPacket.MsgDstByGuid);
                        if (client != null)
                        {
                            ipPort = client.IPPort;
                            data = DataPacket.WriteData(cmdPacket);
                            client.Send(data);
                            DataTransmitEvent?.Invoke(client.Id, true, data.Length);
                            TraceHelper.Instance.Info("发送到Client命令:" + cmdPacket.MsgDstByGuid + "------" + cmdPacket.StringBuff + " IPPort:" + client.IPPort);
                        }
                    }
                }
                else
                {
                    lock (clientlock)
                    {
                        foreach (var client in OnLineUser)
                        {
                            if (client != null && (client.ClientType & cmdPacket.MsgDstByType) != 0)
                            {
                                ipPort = client.IPPort;
                                data = DataPacket.WriteData(cmdPacket);
                                client.Send(data);
                                DataTransmitEvent?.Invoke(client.Id, true, data.Length);
                                TraceHelper.Instance.Info("发送到Client命令:" + cmdPacket.MsgType + "------" + cmdPacket.StringBuff + " IPPort:" + client.IPPort);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceHelper.Instance.Error("SendCmdToTypeClient ipPort:" + ipPort, ex);
            }
        }
    }
}