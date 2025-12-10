using System;
using System.Collections.Generic;
using System.Net;

namespace TCPCommunicate.Comm
{
    /// <summary>
    /// 处理粘包的类
    /// </summary>
    internal class MsgBuffer
    {
        private byte[] buffer;
        private int len = 0;
        private List<byte> Listbuffer;
        private int maxBuffLen;
        private static object lockerBuff = new object();

        public MsgBuffer()
        {
            maxBuffLen = HandCmd.BufferSize * 2;
            buffer = new byte[maxBuffLen];
            Listbuffer = new List<byte>();
        }

        /// <summary>
        /// 放入数组中
        /// </summary>
        /// <param name="b">字节数组</param>
        /// <param name="bLen">数组长度</param>
        public void Push(byte[] b, int bLen)
        {
            lock (lockerBuff)
            {
                if (len > maxBuffLen)
                {
                    Listbuffer.InsertRange(len, b);
                }
                else if (len + bLen > maxBuffLen)
                {
                    Listbuffer.AddRange(buffer);
                    Listbuffer.InsertRange(len, b);
                }
                else
                {
                    Buffer.BlockCopy(b, 0, buffer, len, bLen);
                }
                len += bLen;
            }
        }

        /// <summary>
        /// 取出一个完整的包
        /// </summary>
        /// <param name="b">包的数组</param>
        /// <returns>0表示成功 1标识包未完整 -1标识非法数据</returns>
        public int Pop(out byte[] b)
        {
            lock (lockerBuff)
            {
                b = null;
                if (len < 4)
                {
                    return -2;
                }
                if (len > maxBuffLen)
                {
                    Listbuffer.CopyTo(0, buffer, 0, buffer.Length);
                    int netLen = BitConverter.ToInt32(buffer, 0);
                    int datalLen = IPAddress.NetworkToHostOrder(netLen);
                    if (datalLen + 4 > len)
                    {
                        return -1;
                    }
                    b = new byte[datalLen + 4];
                    Listbuffer.CopyTo(0, b, 0, datalLen + 4);

                    Listbuffer.RemoveRange(0, datalLen + 4);
                    len = len - datalLen - 4;
                    if (len <= maxBuffLen)
                    {
                        Listbuffer.CopyTo(0, buffer, 0, len);
                        Listbuffer.Clear();
                    }
                    return 0;
                }
                else
                {
                    int netLen = BitConverter.ToInt32(buffer, 0);
                    int datalLen = IPAddress.NetworkToHostOrder(netLen);
                    if (datalLen + 4 > len)
                    {
                        return -1;
                    }
                    b = new byte[datalLen + 4];
                    Buffer.BlockCopy(buffer, 0, b, 0, datalLen + 4);
                    len = len - datalLen - 4;
                    Buffer.BlockCopy(buffer, datalLen + 4, buffer, 0, len);
                    return 0;
                }
            }
        }

        public void Clear()
        {
            lock (lockerBuff)
            {
                len = 0;
                Listbuffer.Clear();
            }
        }
    }
}