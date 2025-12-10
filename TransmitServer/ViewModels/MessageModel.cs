namespace TransmitServer.ViewModels
{
    public class MessageModel : ViewModelBase
    {
        private string _time;
        public string Time
        {
            get { return _time; }
            set
            {
                _time = value;
                RaisePropertyChanged("Time");
            }
        }

        private string _msg;
        public string Msg
        {
            get
            {
                return _msg;
            }
            set
            {
                _msg = value;
                RaisePropertyChanged("Msg");
            }
        }
    }
}
