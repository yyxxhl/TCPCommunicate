using Newtonsoft.Json;
using System;
using System.Text;

namespace TCPCommunicate.Interface
{
    /// <summary>
    /// 消息数据转化
    /// </summary>
    public class CmdPacket
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public short MsgType { get; set; }

        /// <summary>
        /// 发送目的地 唯一ID
        /// </summary>
        public Guid MsgDstByGuid { get; set; }

        /// <summary>
        /// 发送目的地，某一类
        /// </summary>
        public int MsgDstByType { get; set; }

        /// <summary>
        /// 消息Json数据
        /// </summary>
        public string StringBuff { get; set; }

        /// <summary>
        /// 消息byte数组
        /// </summary>
        public byte[] ByteBuff { get; set; }

        /// <summary>
        /// 按照Guid还是类型发送 // 0x04  按照类型  0x10 按照
        /// </summary>
        public byte ByteGuidOrType { get; set; }

        /// <summary>
        /// 创建发送数据包
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="data">数据数组</param>
        /// <param name="guid">发送目的地的唯一标识</param>
        public CmdPacket(short type, byte[] data, Guid guid)
        {
            MsgType = type;
            StringBuff = Encoding.UTF8.GetString(data) == "null" ? null : Encoding.UTF8.GetString(data);
            ByteBuff = data;
            MsgDstByGuid = guid;
            ByteGuidOrType = 0x10;
        }

        /// <summary>
        /// 创建发送数据包
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="data">数据数组</param>
        /// <param name="dstType">发送目的地的类型</param>
        public CmdPacket(short type, byte[] data, int dstType)
        {
            MsgType = type;
            StringBuff = Encoding.UTF8.GetString(data) == "null" ? null : Encoding.UTF8.GetString(data);
            ByteBuff = data;
            MsgDstByType = dstType;
            ByteGuidOrType = 0x04;
        }

        /// <summary>
        /// 创建发送数据包
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="obj">发送消息的消息类</param>
        /// <param name="guid">发送目的地的唯一标识</param>
        public CmdPacket(short type, Object obj, Guid guid)
        {
            if (obj == null)
            {
                obj = "null";
            }
            MsgType = type;
            StringBuff = JsonConvert.SerializeObject(obj);
            ByteBuff = System.Text.Encoding.UTF8.GetBytes(StringBuff);
            MsgDstByGuid = guid;
            ByteGuidOrType = 0x10;
        }

        /// <summary>
        /// 创建发送数据包
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="obj">发送消息的消息类</param>
        /// <param name="dstType">发送目的地的类型</param>
        public CmdPacket(short type, Object obj, int dstType)
        {
            MsgType = type;
            if (obj == null)
            {
                obj = "null";
            }
            StringBuff = JsonConvert.SerializeObject(obj);
            ByteBuff = System.Text.Encoding.UTF8.GetBytes(StringBuff);
            MsgDstByType = dstType;
            ByteGuidOrType = 0x04;
        }
    }
}