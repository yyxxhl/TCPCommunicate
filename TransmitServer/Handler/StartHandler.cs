using System;
using System.Collections.Generic;
using TCPCommunicate;
using TCPCommunicate.Comm;
using TCPCommunicate.UserDefine;

namespace TransmitServer.Handler
{
    public class StartHandler
    {
        private static StartHandler _instance;
        private bool isStart;
        public Action<List<ClientState>> ActionClientState;
        public Action<string, int, Exception> ActionLog;
        public Action<ClientInfo> ClientOnLineEvent { get; set; }
        public Action<Guid> ClientOffLineEvent { get; set; }
        public Action<Guid, bool, int> DataTransmitEvent { get; set; }
        private StartHandler()
        {
            isStart = false;
        }

        public static StartHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StartHandler();
                }
                return _instance;
            }
        }

        public void Start(int port)
        {
            if (isStart)
            {
                return;
            }
            isStart = true;
            TCPServerHandler.Instance().Start(port);
            TCPServerHandler.Instance().ActionLog += TransTcpLog;
            TCPServerHandler.Instance().ActionClientState += TranTcpState;
            TCPServerHandler.Instance().ClientOnLineEvent += DoClientOnLine;
            TCPServerHandler.Instance().ClientOffLineEvent += DoClientOffLine;
            TCPServerHandler.Instance().DataTransmitEvent += DoDataTransmit;
        }

        private void DoDataTransmit(Guid id, bool isSend, int length)
        {
            DataTransmitEvent?.Invoke(id, isSend, length);
        }

        private void DoClientOffLine(Guid id)
        {
            ClientOffLineEvent?.Invoke(id);
        }

        private void DoClientOnLine(ClientInfo info)
        {
            ClientOnLineEvent?.Invoke(info);
        }

        private void TransTcpLog(string str, int type, Exception ex)
        {
            ActionLog?.Invoke(str, type, ex);
        }

        private void TranTcpState(List<ClientState> list)
        {
            ActionClientState?.Invoke(list);
        }
    }
}
