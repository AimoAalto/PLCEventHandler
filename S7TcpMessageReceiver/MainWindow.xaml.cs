using S7TcpEventMsgHandler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace S7TcpMessageReceiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Variables

        private S7TcpEventMsgHandler.S7TcpEventMsgHandler handler;

        public string TcpCommandText { get { if (handler == null) return "Start"; if (handler.Listener == null) return "Start"; if (handler.Listener.Connected) return "Close"; else return "Start"; } }
        public int ListenerPort { get; set; }
        public int RemotePort { get; set; }
        public bool TimestampInMsg { get; set; }
        public ObservableCollection<string> Log { get; set; }
        public string ListenerState { get { if (handler == null) return "#FFF4F4F5"; if (handler.Listener == null) return "#FFF4F4F5"; if (handler.Listener.Connected) return "LightGreen"; else return "#FFF4F4F5"; } }
        public ObservableCollection<string> LastData { get; set; }
        public string TreeData { get; set; }

        #endregion

        #region INotifiedProperty Block

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public MainWindow()
        {
            LastData = new ObservableCollection<string>();

            ListenerPort = 2000;
            RemotePort = 50200;

            InitializeComponent();
            DataContext = this;

            //handler = new S7TcpEventMsgHandler.S7TcpEventMsgHandler() { AddLogMessage = AddLogMessage, Port = ListenerPort };
        }

        private void Click_TcpCommand(object sender, RoutedEventArgs e)
        {
            if (handler == null) handler = new S7TcpEventMsgHandler.S7TcpEventMsgHandler() { AddLogMessage = AddLogMessage, ConnCheck = ConnCheck, Port = ListenerPort };
            handler.OnOff(ListenerPort);
            OnPropertyChanged("TcpCommandText");
        }

        private readonly object msg_lock = new object();
        private void AddLogMessage(string msg)
        {
            Dispatcher.Invoke(delegate
            {
                lock (msg_lock)
                {
                    while (LastData.Count > 250) LastData.RemoveAt(0);
                    LastData.Add(msg);
                }
            });
        }

        private void ConnCheck()
        {
            Dispatcher.Invoke(delegate
            {
                OnPropertyChanged("ListenerState");
            });
        }

        private void UpdateTree_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder treedata = new StringBuilder();
            if (handler != null)
                foreach (string tkey in handler.Datatree.Keys)
                {
                    if (handler.Datatree[tkey].Children.Count > 0)
                    {
                        treedata.AppendLine($"{tkey}");
                        handler.Datatree[tkey].Print(treedata, "\t");
                    }
                    else
                        treedata.AppendLine($"{tkey} = {handler.Datatree[tkey].Value}:");
                    TreeData = treedata.ToString();
                    OnPropertyChanged("TreeData");
                }
        }

        private void Click_ListToText(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in LastData) sb.AppendLine(s);
            TreeData = sb.ToString();
            OnPropertyChanged("TreeData");
        }

        private void Click_ClearList(object sender, RoutedEventArgs e)
        {
            LastData.Clear();
        }

        /*private void Click_TcpTestSend(object sender, RoutedEventArgs e)
        {
            if (communication == null)
                communication = new Communication();

            communication.TcpSend(RemotePort, "kissa");
        }*/
    }
}
