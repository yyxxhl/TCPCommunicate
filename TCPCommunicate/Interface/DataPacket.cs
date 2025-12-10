using TCPCommunicate.Comm;
using System;
using System.Net;

namespace TCPCommunicate.Interface
{
    /// <summary>
    /// byte[] 与Cmd互转
    /// </summary>
    internal class DataPacket
    {
        /// <summary>
        /// 收到服务端Byte进行拆分后 组合成CmdPacket
        /// </summary>
        /// <param name="RecvByte"></param>
        /// <returns></returns>
        public static CmdPacket ReadData(byte[] RecvByte)
        {
            try
            {
                if (RecvByte.Length < 8)
                {
                    return null;
                }

                int netTotalLen = BitConverter.ToInt32(RecvByte, 0);//取总长度，网络字节序
                int totalLen = IPAddress.NetworkToHostOrder(netTotalLen);//总长度

                if (totalLen > 2)
                {
                    short byMsgType = BitConverter.ToInt16(RecvByte, 4);
                    byte byGuidOrType = RecvByte[6];
                    byte[] bytesGuid = new byte[16];
                    int clientType = 0;
                    int offset = 7;
                    if (byGuidOrType == 0x10)
                    {
                        Buffer.BlockCopy(RecvByte, offset, bytesGuid, 0, byGuidOrType);
                    }
                    else
                    {
                        clientType = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(RecvByte, offset));//总长度
                    }
                    offset += byGuidOrType;

                    if (totalLen > offset)
                    {
                        //表示有DT数据
                        byte[] dataByte = new byte[totalLen - offset + 4];
                        Buffer.BlockCopy(RecvByte, offset, dataByte, 0, totalLen - offset + 4);
                        var bytes = CppHelper.UnZipData(dataByte);
                        CmdPacket cmdPacket;
                        if (byGuidOrType == 0x10)
                        {
                            cmdPacket = new CmdPacket(byMsgType, bytes, new Guid(bytesGuid));
                        }
                        else
                        {
                            cmdPacket = new CmdPacket(byMsgType, bytes, clientType);
                        }
                        return cmdPacket;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                TraceHelper.Instance.Error("Error ReadData................", ex);
                return null;
            }
        }

        /// <summary>
        /// 把Header和Data组装成要发送给服务端的Byte
        /// </summary>
        /// <param name="cmdPacket">数据头</param>
        public static byte[] WriteData(CmdPacket cmdPacket)
        {
            var bytes = CppHelper.ZipData(cmdPacket.ByteBuff);
            return WriteData(cmdPacket.MsgType, cmdPacket.MsgDstByGuid, cmdPacket.MsgDstByType, cmdPacket.ByteGuidOrType, bytes, bytes.Length);
        }

        /// <summary>
        /// 把Header和Data组装成要发送给服务端的Byte
        /// </summary>
        /// <param name="headerbyte">数据头</param>
        /// <param name="databyte">数据Data</param>
        /// <param name="dataLen">数据Data长度</param>
        public static byte[] WriteData(short dataType, Guid guid, int clientType, byte sendtype, byte[] databyte, int dataLen)
        {
            byte[] SendBytes = null;
            if (databyte == null)
            {
                return SendBytes;
            }

            int TotalLen = 2 + 1 + sendtype + dataLen;//1字节数据类型 + 1字节客户端类型字节数 + 客户端类型 + 数据长度
            int netTotalLen = IPAddress.HostToNetworkOrder(TotalLen);//网络字节序的总长度
            byte[] TotalBytes = BitConverter.GetBytes(netTotalLen);//总长度字节
            byte[] TypeBytes = BitConverter.GetBytes(dataType);
            SendBytes = new byte[TotalLen + 4];//要发送到服务的数据,+4代表表头即数据长度
            int offset = 0;//Copy到SendByte从什么位置偏移
            Buffer.BlockCopy(TotalBytes, 0, SendBytes, offset, 4);//总长度占用4个字节
            offset += 4;

            //SendBytes[offset] = dataType;//消息类型
            Buffer.BlockCopy(TypeBytes, 0, SendBytes, offset, 2);////消息类型 2字节
            offset += 2;

            SendBytes[offset] = sendtype;//Guid 还是  TYPE
            offset += 1;

            if (sendtype == 0x10)
            {
                Buffer.BlockCopy(guid.ToByteArray(), 0, SendBytes, offset, sendtype);//Guid,16字节
            }
            else
            {
                byte[] clietTmp = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(clientType));
                Buffer.BlockCopy(clietTmp, 0, SendBytes, offset, sendtype);//客户端类型
            }
            offset += sendtype;

            if (dataLen != 0)
            {
                Buffer.BlockCopy(databyte, 0, SendBytes, offset, dataLen);//数据Data部分
            }
            return SendBytes;
        }
    }
}