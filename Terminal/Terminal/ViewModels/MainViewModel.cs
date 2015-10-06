namespace Terminal
{
    using System;
    using System.Windows.Controls;

    using ActiproSoftware.Windows;

    using Ecng.Xaml;

    using StockSharp.Messages;
    using StockSharp.Localization;

    internal class MainViewModel : ViewModelBase
    {
        private readonly Root _root;

        public MainViewModel()
        {
            _root = Root.GetInstance();

            ToolItemViewModel viewModel = new SecuritiesViewModel();
            viewModel.DefaultDock = Dock.Top;
            viewModel.Title = LocalizedStrings.Securities;
            ToolItems.Add(viewModel);

            ConnectCommand = new DelegateCommand(Connect, CanConnect);

            _root.Connector.Connected += () => NotifyPropertyChanged("ConnectionState");
            _root.Connector.Disconnected += () => NotifyPropertyChanged("ConnectionState");
        }

        public DeferrableObservableCollection<ToolItemViewModel> ToolItems
        {
            get { return _root.ToolItems; }
        }

        public ConnectionStates ConnectionState
        {
            get { return _root.Connector.ConnectionState; }
        }

        public DelegateCommand ConnectCommand { private set; get; }

        private void Connect(object obj)
        {
            switch (_root.Connector.ConnectionState)
            {
                case ConnectionStates.Disconnected:
                    _root.Connector.Connect();
                    break;
                case ConnectionStates.Connected:
                    _root.Connector.Disconnect();
                    break;
                case ConnectionStates.Disconnecting:
                    break;
                case ConnectionStates.Connecting:
                    break;
                case ConnectionStates.Failed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool CanConnect(object obj)
        {
            return true;
        }
    }
}