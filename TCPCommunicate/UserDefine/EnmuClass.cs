namespace TCPCommunicate.UserDefine
{
    /// <summary>
    /// 客户端类型 四个字节 每个字节的每一位代表一种类型
    /// </summary>
    public enum ClientType
    {
        ClientA = 0x01,//客户端A
        ClientB = 0x02,//客户端B
        ClientC = 0x04,//客户端C
        ClientD = 0x08,//客户端D

        //以下不可更改
        TCPServer = 0x40000000,//TCP客户端发给server

        All = 0x7FFFFFFF,
    }

    /// <summary>
    /// 消息类型 范围 0x0000-0x7FFF
    /// </summary>
    public enum MsgType : short
    {
        // 0x0000-0x00FF 系统消息
        // 0x0100-0x01FF ClientA消息范围
        // ......

        SystemMsg = 0x0000,//系统消息。
        ClientAMsg = 0x0100,//ClientA独有的消息
        ClientAClickMsg = 0x0101,//ClientA点击

        //自定义消息


        //以下不可修改
        HandCmd = 0x7FFD,//握手信息包

        StateCmd = 0x7FFF,//状态包
    }
}