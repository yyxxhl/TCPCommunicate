using System;

namespace TCPCommunicate.UserDefine
{
    public class ClientInfo
    {
        public Guid Id { get; set; }
        public string IP { get; set; }

        public int Port { get; set; }

        public DateTime FirstConnectTime { get; set; }

        public bool IsConnected { get; set; }

        public double ReceviedCount { get; set; }

        public double SendCount { get; set; }

        public string Name { get; set; }

        public int Type { get; set; }

        public string Desc { get; set; }

        public string Note { get; set; }
    }
}