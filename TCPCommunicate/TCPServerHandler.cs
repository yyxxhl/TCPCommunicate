using TCPCommunicate.Comm;
using TCPCommunicate.Interface;
using TCPCommunicate.Server;
using TCPCommunicate.UserDefine;
using System;
using System.Collections.Generic;

namespace TCPCommunicate
{
    public class TCPServerHandler
    {
        private static TCPServerHandler instance;

        private TCPServerHandler()
        {
        }

        private void TransRecv(int client, Guid clientID, CmdPacket packet)
        {
            ActionRecv?.Invoke((ClientType)client, clientID, (MsgType)packet.MsgType, packet.StringBuff, packet.ByteBuff);
        }

        private void TransClientState(List<Comm.ClientState> listState)
        {
            ActionClientState?.Invoke(listState);
        }

        private void TransLog(string msg, int type, Exception ex)
        {
            ActionLog?.Invoke(msg, type, ex);
        }

        public static TCPServerHandler Instance()
        {
            if (instance == null)
            {
                instance = new TCPServerHandler();
            }
            return instance;
        }

        #region 对外接口

        /// <summary>
        /// 订阅接收消息，ClientType为消息来自的客户端类型，Guid为消息来自的客户端ID，MsgType消息类型  string为json对象  byte[]为接收到的二进制流//以后拓展用
        /// </summary>
        public Action<ClientType, Guid, MsgType, string, byte[]> ActionRecv { get; set; }

        /// <summary>
        /// 订阅所有连接的tcpclient状态
        /// </summary>
        public Action<List<Comm.ClientState>> ActionClientState { get; set; }

        /// <summary>
        /// 订阅Log日志 string 内容  int 消息类型 0 info ,1 warning 2 error   Exception 告警
        /// </summary>
        public Action<string, int, Exception> ActionLog { get; set; }

        /// <summary>
        /// 客户端上线通知
        /// </summary>
        public Action<ClientInfo> ClientOnLineEvent { get; set; }

        /// <summary>
        /// 客户端掉线
        /// </summary>
        public Action<Guid> ClientOffLineEvent { get; set; }

        /// <summary>
        /// 传输数据计数
        /// </summary>
        public Action<Guid, bool, int> DataTransmitEvent { get; set; }

        /// <summary>
        /// 开启监听TcpServer端口
        /// </summary>
        /// <param name="port">server端口号</param>
        public void Start(int port)
        {
            TCPServer.Instance.SockSvr.StartServer("", port);
            TCPServer.Instance.ReceivedPacket += TransRecv;
            TCPServer.Instance.ActionClientState += TransClientState;
            TCPServer.Instance.ClientOnLineEvent += DoClientOnLine;
            TCPServer.Instance.ClientOffLineEvent += DoClientOffLine;
            TCPServer.Instance.DataTransmitEvent += DoDataTransmit;
            TraceHelper.Instance.ActionLog += TransLog;
        }

        private void DoDataTransmit(Guid id, bool isSend, int length)
        {
            DataTransmitEvent?.Invoke(id, isSend, length);
        }

        private void DoClientOffLine(Guid id)
        {
            ClientOffLineEvent?.Invoke(id);
        }

        private void DoClientOnLine(ClientWrapper client)
        {
            ClientInfo info = new ClientInfo();
            info.IP = client.IP;
            info.Port = client.Port;
            info.FirstConnectTime = client.ConnectFirstTime;
            info.IsConnected = true;
            info.Type = client.ClientType;
            info.Desc = client.ClientDes;
            info.Id = client.Id;
            ClientOnLineEvent?.Invoke(info);
        }

        /// <summary>
        /// 发送消息到指定类型的客户端或server
        /// </summary>
        /// <param name="msgType">消息类型</param>
        /// <param name="obj">可序列化的消息对象</param>
        /// <param name="clientType">消息发送目的客户端类型，发送多个可采用或的形式即 clientType1 | clientType2,发送全部直接用ClientType.ALl</param>
        public void SendPacketByType(MsgType msgType, object obj, ClientType clientType)
        {
            TCPServer.Instance.SendMsg(new CmdPacket((short)msgType, obj, (int)clientType));
        }

        /// <summary>
        /// 发送消息至指定唯一客户端
        /// </summary>
        /// <param name="msgType">消息类型</param>
        /// <param name="obj">可序列化的消息对象</param>
        /// <param name="guid">消息发送的唯一目的地</param>
        public void SendPacketByGuid(MsgType msgType, object obj, Guid guid)
        {
            TCPServer.Instance.SendMsg(new CmdPacket((short)msgType, obj, guid));
        }

        /// <summary>
        /// 发送消息至指定的IP和端口
        /// </summary>
        /// <param name="msgType">消息类型</param>
        /// <param name="obj">可序列化的消息对象</param>
        /// <param name="strIP">消息发送目的IP</param>
        /// <param name="nPort">消息发送目的端口</param>
        public void SendPacketByIP(MsgType msgType, object obj, string strIP, int nPort = 0)
        {
            TCPServer.Instance.SendMsg(new CmdPacket((short)msgType, obj, Guid.NewGuid()), strIP, nPort = 0);
        }

        #endregion 对外接口
    }
}