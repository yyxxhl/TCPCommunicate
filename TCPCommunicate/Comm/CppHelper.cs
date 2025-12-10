using System.IO;
using System.IO.Compression;

namespace TCPCommunicate.Comm
{
    internal class CppHelper
    {
        /// <summary>
        /// 压缩数据的方法
        /// </summary>
        /// <param name="src">传入压缩前的数据，返回压缩后的数据</param>
        /// <returns></returns>
        public static byte[] ZipData(byte[] src)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                using (GZipStream zipStream = new GZipStream(outStream, CompressionMode.Compress, true))
                {
                    zipStream.Write(src, 0, src.Length);
                    zipStream.Close(); //很重要，必须关闭，否则无法正确解压
                    return outStream.ToArray();
                }
            }
        }

        /// <summary>
        /// 解析压缩数据的方法
        /// </summary>
        /// <param name="src">传入解析前的数据，返回解析后的数据</param>
        /// <returns></returns>
        public static byte[] UnZipData(byte[] src)
        {
            using (MemoryStream inputStream = new MemoryStream(src))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    using (GZipStream zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        zipStream.CopyTo(outStream);
                        zipStream.Close();
                        return outStream.ToArray();
                    }
                }
            }
        }
    }
}