using System;

namespace TransmitServer.ViewModels
{
    public class ClientModel : ViewModelBase
    {
        private string _type;
        public string Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
                RaisePropertyChanged("Type");
            }
        }

        private Guid _id;
        public Guid Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                RaisePropertyChanged("Id");
            }
        }

        private string _iP;
        public string IP
        {
            get
            {
                return _iP;
            }
            set
            {
                _iP = value;
                RaisePropertyChanged("IP");
            }
        }

        private int _port;
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
                RaisePropertyChanged("Port");
            }
        }

        private string _desc;
        public string Desc
        {
            get
            {
                return _desc;
            }
            set
            {
                _desc = value;
                RaisePropertyChanged("Desc");
            }
        }

        private string _firstConnectTime;
        public string FirstConnectTime
        {
            get
            {
                return _firstConnectTime;
            }
            set
            {
                _firstConnectTime = value;
                RaisePropertyChanged("FirstConnectTime");
            }
        }

        private long _sendCount;
        public long SendCount
        {
            get
            {
                return _sendCount;
            }
            set
            {
                _sendCount = value;
                RaisePropertyChanged("SendCount");
            }
        }

        private long _sendLength;
        public long SendLength
        {
            get
            {
                return _sendLength;
            }
            set
            {
                _sendLength = value;
                RaisePropertyChanged("SendLength");
            }
        }

        private long _recCount;
        public long RecCount
        {
            get
            {
                return _recCount;
            }
            set
            {
                _recCount = value;
                RaisePropertyChanged("RecCount");
            }
        }

        private long _recLength;
        public long RecLength
        {
            get
            {
                return _recLength;
            }
            set
            {
                _recLength = value;
                RaisePropertyChanged("RecLength");
            }
        }
    }
}
