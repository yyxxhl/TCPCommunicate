using System.Collections.Generic;

namespace TCPCommunicate.UserDefine
{
    public class AgentState
    {
        public string ClientIP { get; set; }
        public int ClientPort { get; set; }
        public byte ConnectStatus { get; set; }
    }

    public class AgentRest
    {
        public List<string> ResetIPList = new List<string>();
    }
}