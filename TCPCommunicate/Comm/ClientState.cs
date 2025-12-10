using System;

namespace TCPCommunicate.Comm
{
    public class ClientState
    {
        public static short TypeByte = 0x7FFF;
        public int ClientType { get; set; }
        public Guid ClientGuid { get; set; }
        public string ClientIP { get; set; }
        public int ClientPort { get; set; }
        public string ClientDes { get; set; }
    }
}