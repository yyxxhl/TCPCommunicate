using System.Collections.ObjectModel;

namespace TransmitServer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        private ObservableCollection<ClientModel> _clientListItems = new ObservableCollection<ClientModel>();
        public ObservableCollection<ClientModel> ClientListItems
        {
            get
            {
                return _clientListItems;
            }
            set
            {
                _clientListItems = value;
                RaisePropertyChanged("ClientListItems");
            }
        }

        private ObservableCollection<MessageModel> _detailListItems = new ObservableCollection<MessageModel>();
        public ObservableCollection<MessageModel> DetailListItems
        {
            get
            {
                return _detailListItems;
            }
            set
            {
                _detailListItems = value;
                RaisePropertyChanged("DetailListItems");
            }
        }

        private ClientModel _clientSelectedItem;
        public ClientModel ClientSelectedItem
        {
            get
            {
                return _clientSelectedItem;
            }
            set
            {
                _clientSelectedItem = value;
                RaisePropertyChanged("ClientSelectedItem");
            }
        }

        private string _IPText;
        public string IPText
        {
            get
            {
                return _IPText;
            }
            set
            {
                _IPText = value;
                RaisePropertyChanged("IPText");
            }
        }

        private string _curTime;
        public string CurTime
        {
            get
            {
                return _curTime;
            }
            set
            {
                _curTime = value;
                RaisePropertyChanged("CurTime");
            }
        }

        private string _curCpuAndMemory;
        public string CurCpuAndMemory
        {
            get
            {
                return _curCpuAndMemory;
            }
            set
            {
                _curCpuAndMemory = value;
                RaisePropertyChanged("CurCpuAndMemory");
            }
        }
    }
}
