using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TCPCommunicate.Server
{
    internal class PortUsingKill
    {
        internal static void CheckPortUsedToKill(int port)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;          //不显示程序窗口
            List<UsingPort> list_pid = GetPidByPort(p, port);
            if (list_pid.Count == 0)
            {
                Trace.WriteLine("暂无占用的端口");
                return;
            }
            List<UsingPort> list_process = GetProcessNameByPid(p, list_pid);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("占用" + port + "端口的进程有:");
            foreach (var item in list_process)
            {
                sb.Append(item.ToString() + "\r\n");
            }
            Trace.WriteLine(sb.ToString());
            PidKill(p, list_pid.Select(m => m.PID).ToList());
            Trace.WriteLine("进程终止完成");
        }

        private static List<UsingPort> GetPidByPort(Process p, int port)
        {
            int result;
            bool b = true;
            p.Start();
            p.StandardInput.WriteLine(string.Format("netstat -ano|find \"{0}\"", port));
            p.StandardInput.WriteLine("exit");
            StreamReader reader = p.StandardOutput;
            string strLine = reader.ReadLine();
            List<UsingPort> list_pid = new List<UsingPort>();
            while (!reader.EndOfStream)
            {
                strLine = strLine.Trim();
                if (strLine.Length > 0 && ((strLine.Contains("TCP") || strLine.Contains("UDP"))))
                {
                    Regex r = new Regex(@"\s+");
                    string[] strArr = r.Split(strLine);
                    if (strArr.Length >= 4)
                    {
                        var ipt = strArr[1].Split(':');
                        if (ipt.Count() >= 2 && ipt[1] == port.ToString())
                        {
                            b = int.TryParse(strArr[3], out result);
                            if (b && !list_pid.Exists(m => m.PID == result))
                            {
                                list_pid.Add(new UsingPort() { UsingType = strArr[0], UsingName = strArr[1], PID = result, Port = port });
                            }
                        }
                    }
                }
                strLine = reader.ReadLine();
            }
            p.WaitForExit();
            reader.Close();
            p.Close();
            return list_pid;
        }

        private static List<UsingPort> GetProcessNameByPid(Process p, List<UsingPort> list_pid)
        {
            p.Start();
            foreach (var item in list_pid)
            {
                p.StandardInput.WriteLine(string.Format("tasklist |find \"{0}\"", item.PID));
                p.StandardInput.WriteLine("exit");
                StreamReader reader = p.StandardOutput;//截取输出流
                string strLine = reader.ReadLine();//每次读取一行

                while (!reader.EndOfStream)
                {
                    strLine = strLine.Trim();
                    if (strLine.Length > 0 && ((strLine.Contains(".exe"))))
                    {
                        Regex r = new Regex(@"\s+");
                        string[] strArr = r.Split(strLine);
                        if (strArr.Length > 0)
                        {
                            item.ProccessName = strArr[0];
                        }
                    }
                    strLine = reader.ReadLine();
                }
                p.WaitForExit();
                reader.Close();
            }
            p.Close();
            return list_pid;
        }

        private static void PidKill(Process p, List<int> list_pid)
        {
            p.Start();
            foreach (var item in list_pid)
            {
                p.StandardInput.WriteLine("taskkill /pid " + item + " /f");
                p.StandardInput.WriteLine("exit");
            }
            p.Close();
        }
    }

    public class UsingPort
    {
        public string UsingType;

        public string UsingName;

        public string ProccessName;

        public int Port;

        public int PID;

        public override string ToString()
        {
            return string.Format("  占用类型:{0}  占用名称:{1}  进程名称:{2}  进程ID:{3}  Port:{4}", UsingType, UsingName, ProccessName, PID, Port);
        }
    }
}