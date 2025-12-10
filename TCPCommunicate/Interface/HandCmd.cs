using System;
using System.Net;

namespace TCPCommunicate.Comm
{
    internal class HandCmd
    {
        public static int BufferSize = 1024 * 8;

        /// <summary>
        /// 握手命令包 0-0xff
        /// </summary>
        private static int handshakeCmd = 100;

        public const short HandType = 0x7FFD;
        public static int TimerTick = 5000;

        /// <summary>
        ///发送握手请求 Cmd
        /// </summary>
        public static byte[] OnHandCmdSend(int _clientType, Guid guid, string strDes = "")
        {
            var bytesDes = System.Text.Encoding.UTF8.GetBytes(strDes);

            int index = 0;
            byte[] c2nSend = new byte[4 + 4 + 4 + 4 + 16 + bytesDes.Length]; //4字节长度 + 4字节包类型 +  4字节握手包 + 4字节客户端类型 + 16字节Guid + 客户端描述
            var totalBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(c2nSend.Length - 4));
            var clientType = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(_clientType));
            var handBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(handshakeCmd));
            Buffer.BlockCopy(totalBytes, 0, c2nSend, index, totalBytes.Length);//4字节长度
            index += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(HandType), 0, c2nSend, index, 2);//2字节长度 2字节对齐
            //c2nSend[index] = HandType;//4字节包类型 //1字节数据包类型 3字节对齐
            index += 4;
            Buffer.BlockCopy(handBytes, 0, c2nSend, index, handBytes.Length);//4字节握手包
            index += 4;
            Buffer.BlockCopy(clientType, 0, c2nSend, index, clientType.Length);//4字节客户端类型
            index += 4;
            Buffer.BlockCopy(guid.ToByteArray(), 0, c2nSend, index, 16);
            index += 16;
            if (strDes != "")
            {
                Buffer.BlockCopy(bytesDes, 0, c2nSend, index, bytesDes.Length);
            }

            return c2nSend;
        }

        public static bool OnHandCmdCheck(byte[] _bytesRecv, out int _clientType, out Guid guid, out string strDes)
        {
            strDes = "客户端";
            if (_bytesRecv.Length > 8)
            {
                int nLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_bytesRecv, 0));
                short byteType = BitConverter.ToInt16(_bytesRecv, 4);
                if (nLen >= 28 && byteType == HandType)
                {
                    var netHand = BitConverter.ToInt32(_bytesRecv, 8);
                    var handCmd = IPAddress.NetworkToHostOrder(netHand);
                    if (handCmd != handshakeCmd)
                    {
                        _clientType = 0x0000;
                        guid = Guid.NewGuid();
                        return false;
                    }
                    int netClientType = BitConverter.ToInt32(_bytesRecv, 12);
                    _clientType = IPAddress.NetworkToHostOrder(netClientType);

                    byte[] guidBuff = new byte[16];
                    Buffer.BlockCopy(_bytesRecv, 16, guidBuff, 0, guidBuff.Length);//16字节Guid
                    guid = new Guid(guidBuff);

                    int nDesLen = nLen - 28;
                    if (nDesLen > 0)
                    {
                        byte[] bytesDes = new byte[nDesLen];
                        Buffer.BlockCopy(_bytesRecv, 32, bytesDes, 0, bytesDes.Length);//描述
                        strDes = System.Text.Encoding.UTF8.GetString(bytesDes);
                    }

                    return handCmd == handshakeCmd;
                }
                else
                {
                    guid = Guid.NewGuid();
                    _clientType = 0x0000;
                    return false;
                }
            }
            else
            {
                guid = Guid.NewGuid();
                _clientType = 0x0000;
                return false;
            }
        }

        public static byte[] BytesHeart()
        {
            byte[] heardbeat = System.Text.Encoding.Default.GetBytes("a");
            int heardbeatLen = heardbeat.Length;
            int netLen = IPAddress.HostToNetworkOrder(heardbeatLen);//转为网络字节序
            byte[] netLenBytes = BitConverter.GetBytes(netLen);
            byte[] Sendheardbeat = new byte[heardbeatLen + 4];
            int offset = 0;
            Buffer.BlockCopy(netLenBytes, 0, Sendheardbeat, 0, 4);
            offset += 4;
            Buffer.BlockCopy(heardbeat, 0, Sendheardbeat, offset, heardbeatLen);
            return Sendheardbeat;
        }
    }
}