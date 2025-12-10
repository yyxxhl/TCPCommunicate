using TCPCommunicate.Comm;
using TCPCommunicate.Interface;
using TCPCommunicate.UserDefine;
using System;
using System.Collections.Generic;
using TcpClient = TCPCommunicate.Client.TcpClient;

namespace TCPCommunicate
{
    public class TCPClientHandler
    {
        private TcpClient _tcpClient = new TcpClient();

        private void TransRecv(CmdPacket packet)
        {
            ActionRecv?.Invoke((MsgType)packet.MsgType, packet.StringBuff, packet.ByteBuff);
        }

        private void TransServerState(bool flag)
        {
            ActionServerState?.Invoke(flag);
        }

        private void TransClientState(List<ClientState> listState)
        {
            ActionClientState?.Invoke(listState);
        }

        private void TransLog(string msg, int type, Exception ex)
        {
            ActionLog?.Invoke(msg, type, ex);
        }

        #region 对外接口

        /// <summary>
        /// 订阅接收消息，MsgType消息类型  string为json对象  byte[]为接收到的二进制流//以后拓展用
        /// </summary>
        public Action<MsgType, string, byte[]> ActionRecv;

        /// <summary>
        /// 订阅tcpserver是否在线消息
        /// </summary>
        public Action<bool> ActionServerState;

        /// <summary>
        /// 订阅所有连接的tcpclient状态
        /// </summary>
        public Action<List<ClientState>> ActionClientState;

        /// <summary>
        /// 订阅Log日志 string 内容  int 消息类型 0 info ,1 warning 2 error   Exception 告警
        /// </summary>
        public Action<string, int, Exception> ActionLog;

        /// <summary>
        /// 获取本地socket的ip
        /// </summary>
        /// <returns>本地socket的ip</returns>
        public string GetSocketIP()
        {
            return _tcpClient.GetSocketIP();
        }

        /// <summary>
        /// 获取本地socket的端口
        /// </summary>
        /// <returns>本地socket的端口</returns>
        public int GetSocketPort()
        {
            return _tcpClient.GetSocketPort();
        }

        /// <summary>
        /// 创建TCPClientSocket
        /// </summary>
        /// <param name="serverIp">TCPServerIP</param>
        /// <param name="port">TCPServer端口</param>
        /// <param name="clientType">自己的客户端类型</param>
        /// <param name="strLoacl">本地绑定IP，无需绑定传入“”</param>
        /// <param name="nPort">本地绑定端口，无需绑定传入 0 </param>
        public void Start(string serverIp, int port, ClientType clientType, string strDes = "客户端", string strLoacl = "", int nPort = 0)
        {
            _tcpClient.StartConneting(serverIp, port, (int)clientType, strDes, strLoacl, nPort);
            _tcpClient.ReceivedPacket += TransRecv;
            _tcpClient.ServerState += TransServerState;
            _tcpClient.FunClientState += TransClientState;
            TraceHelper.Instance.ActionLog += TransLog;
        }

        /// <summary>
        /// 发送消息到tcpserver
        /// </summary>
        /// <param name="msgType">消息类型</param>
        /// <param name="obj">可序列化的消息对象</param>
        public void SendPacktToServer(MsgType msgType, object obj)
        {
            _tcpClient.SendData(new CmdPacket((short)msgType, obj, (int)ClientType.TCPServer));
        }

        /// <summary>
        /// 发送消息到指定类型的客户端或server
        /// </summary>
        /// <param name="msgType">消息类型</param>
        /// <param name="obj">可序列化的消息对象</param>
        /// <param name="clientType">消息发送目的客户端类型，发送多个可采用或的形式即 clientType1 | clientType2,发送全部直接用ClientType.ALl</param>
        public bool SendPacketByType(MsgType msgType, object obj, ClientType clientType)
        {
            return _tcpClient.SendData(new CmdPacket((short)msgType, obj, (int)clientType));
        }

        /// <summary>
        /// 发送消息至指定唯一客户端
        /// </summary>
        /// <param name="msgType">消息类型</param>
        /// <param name="obj">可序列化的消息对象</param>
        /// <param name="guid">消息发送的唯一目的地</param>
        public bool SendPacketByGUID(MsgType msgType, object obj, Guid guid)
        {
            return _tcpClient.SendData(new CmdPacket((short)msgType, obj, guid));
        }

        /// <summary>
        /// 发送消息至指定的IP和端口
        /// </summary>
        /// <param name="msgType">消息类型</param>
        /// <param name="obj">可序列化的消息对象</param>
        /// <param name="strIP">消息发送目的IP</param>
        /// <param name="nPort">消息发送目的端口</param>
        public bool SendPacketByIP(MsgType msgType, object obj, string strIP, int nPort = 0)
        {
            return _tcpClient.SendData(new CmdPacket((short)msgType, obj, Guid.NewGuid()), strIP, nPort = 0);
        }

        #endregion 对外接口
    }
}