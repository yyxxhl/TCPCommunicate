using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using TCPCommunicate.Comm;
using TCPCommunicate.UserDefine;
using TransmitServer.Handler;
using TransmitServer.Utils;
using TransmitServer.ViewModels;

namespace TransmitServer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeData();
        }

        private MainWindowViewModel _mainWindowViewModel;
        private int logLevel = 0;
        private Timer timer = new Timer(3 * 1000);
        private DateTime _startTime;
        private string curProcessName;
        private PerformanceCounter ramCounter;
        public static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("loginfo");
        private void InitializeData()
        {
            _mainWindowViewModel = new MainWindowViewModel();
            this.DataContext = _mainWindowViewModel;
            int port = ConfigsUtil.Instance.GetIntConfig("port");
            logLevel = ConfigsUtil.Instance.GetIntConfig("LogLevel");
            if (port <= 0 || port > 65535)
            {
                MessageBox.Show("端口配置错误", "错误", MessageBoxButton.OK);
                return;
            }
            ///获取本地的IP地址  
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    _mainWindowViewModel.IPText = _IPAddress.ToString() + ":" + port;
                }
            }
            _startTime = DateTime.Now;
            curProcessName = Process.GetCurrentProcess().ProcessName;
            ramCounter = new PerformanceCounter("Process", "Working Set - private", curProcessName);
            timer.Start();
            timer.Enabled = true;
            timer.Elapsed += OnElapsed;
            StartHandler.Instance.Start(port);
            StartHandler.Instance.ActionClientState = DoRecClientState;
            StartHandler.Instance.ActionLog = DoRecLog;
            StartHandler.Instance.ClientOnLineEvent = DoClientOnLine;
            StartHandler.Instance.ClientOffLineEvent = DoClientOffLine;
            StartHandler.Instance.DataTransmitEvent = DoDataTransmit;
            log4net.Config.XmlConfigurator.Configure();
        }

        private void DoDataTransmit(Guid id, bool isSend, int length)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                var client = _mainWindowViewModel.ClientListItems.FirstOrDefault(c => c.Id == id);
                if (client != null)
                {
                    if (!isSend)
                    {
                        client.SendCount += 1;
                        client.SendLength += length;
                    }
                    else
                    {
                        client.RecCount += 1;
                        client.RecLength += length;
                    }
                }
            }
            ));
        }

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                _mainWindowViewModel.CurTime = DateTime.Now.ToString("HH:mm:ss");
                _mainWindowViewModel.CurCpuAndMemory = GetCpuMemory();
            }));
        }

        private string GetCpuMemory()
        {
            return "内存 :" + Math.Round(ramCounter.NextValue() / 1024 / 1024, 2) + "M  ";
        }

        private void DoClientOffLine(Guid id)
        {
            var client = _mainWindowViewModel.ClientListItems.FirstOrDefault(c => c.Id == id);
            if (client != null)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    _mainWindowViewModel.ClientListItems.Remove(client);
                }
                ));
                AddLogs(new MessageModel() { Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Msg = client.IP + ":" + client.Port + "已断开连接" });
                loginfo.Info(client.IP + ":" + client.Port + "已断开连接");
            }
        }

        private void DoClientOnLine(ClientInfo info)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                bool isContains = true;
                var model = _mainWindowViewModel.ClientListItems.FirstOrDefault(c => c.Id == info.Id);
                if (model == null)
                {
                    isContains = false;
                    model = new ClientModel();
                }
                model.Id = info.Id;
                model.Desc = info.Desc;
                model.FirstConnectTime = info.FirstConnectTime.ToString("yyyy-MM-dd HH:mm:ss");
                model.IP = info.IP;
                model.Port = info.Port;
                model.Type = Enum.GetName(typeof(ClientType), info.Type);
                if (!isContains)
                {
                    _mainWindowViewModel.ClientListItems.Add(model);
                    AddLogs(new MessageModel() { Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Msg = info.IP + ":" + info.Port + "已连接" });
                    loginfo.Info(info.IP + ":" + info.Port + "已连接");
                }
            }));
        }

        private void DoRecLog(string msg, int type, Exception ex)
        {
            switch (type)
            {
                case 0:
                    loginfo.Info(msg, ex);
                    break;
                case 1:
                    loginfo.Warn(msg, ex);
                    break;
                case 2:
                    loginfo.Error(msg, ex);
                    break;
                default:
                    break;
            }

            if (type >= logLevel)
            {
                MessageModel model = new MessageModel();
                model.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                model.Msg = msg;
                if (ex != null)
                {
                    model.Msg = msg + "," + ex.Message;
                }
                AddLogs(model);
            }
        }

        private void DoRecClientState(List<ClientState> list)
        {

        }

        private void AddLogs(MessageModel msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (_mainWindowViewModel.DetailListItems.Count >= 200)
                {
                    _mainWindowViewModel.DetailListItems.Remove(_mainWindowViewModel.DetailListItems.Last());
                }
                _mainWindowViewModel.DetailListItems.Insert(0, msg);
            }));
        }

        private void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            //if (_mainWindowViewModel.ClientSelectedItem != null)
            //{
            //    _mainWindowViewModel.DetailListItems.Clear();
            //    _mainWindowViewModel.DetailListItems.Add(_mainWindowViewModel.ClientSelectedItem);
            //}
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            _mainWindowViewModel.DetailListItems.Clear();
        }
    }
}
