using SciTrader.ViewModels;
using SciTrader.Views;
using DevExpress.Xpf.Core;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;
using System.Windows;
using System.IO;

using Ecng.Serialization;
using Ecng.Configuration;

using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.DarkHorse;
using System.Security;
using System.Collections.Generic;
using System.Threading;
using System;

namespace SciTrader {
    public partial class MainWindow : ThemedWindow {

        private readonly Connector _connector = new();
        private const string _connectorFile = "ConnectorFile.json";

        private readonly List<Subscription> _subscriptions = new();
        private SecurityId? _selectedSecurityId;

        private DarkHorseMessageAdapter darkhorseMessageAdapter;

        public class DarkHorseIdGenerator : Ecng.Common.IdGenerator
        {
            private long _currentId;

            public DarkHorseIdGenerator()
            {
                _currentId = 1;
            }

            public override long GetNextId()
            {
                return Interlocked.Increment(ref _currentId);
            }
        }

        public MainWindow() {
            InitializeComponent();
            InitDarkHorseMessageAdapter();

            // registering all connectors
            ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(_connector.Adapter.InnerAdapters));

            if (File.Exists(_connectorFile))
            {
                _connector.Load(_connectorFile.Deserialize<SettingsStorage>());
            }

            var portfolios = _connector.Portfolios;
            // Assuming 'connector' is already initialized and contains portfolios
            foreach (var portfolio in _connector.Portfolios)
            {
                Console.WriteLine($"Portfolio Name: {portfolio.Name}");
            }
        }

        private static SecureString ToSecureString(string str)
        {
            var secureString = new SecureString();
            foreach (char c in str)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();
            return secureString;
        }

        private void InitDarkHorseMessageAdapter()
        {
            darkhorseMessageAdapter = new DarkHorseMessageAdapter(new DarkHorseIdGenerator());
            var apiKey = ToSecureString("angelpie"); // Replace with your actual API key
            var apiSecret = ToSecureString("orion"); // Replace with your actual API secret

            darkhorseMessageAdapter.Key = apiKey;
            darkhorseMessageAdapter.Secret = apiSecret;

            // Add the Coinbase adapter to the connector
            _connector.Adapter.InnerAdapters.Add(darkhorseMessageAdapter);
        }

        void OnInformationPanelLoaded(object sender, System.Windows.RoutedEventArgs e) {
            ((InformationPanel)sender).DataContext = ((MainViewModel)DataContext).InformationPanelModel;
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            if (_connector.Configure(this))
            {
                _connector.Save().Serialize(_connectorFile);
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            //SecurityPicker.SecurityProvider = _connector;
            //SecurityPicker.MarketDataProvider = _connector;
            _connector.Connected += Connector_Connected;
            _connector.Connect();
        }

        private void Connector_Connected()
        {
            // try lookup all securities
            _connector.LookupSecurities(StockSharp.Messages.Extensions.LookupAllCriteriaMessage);
        }

        private void SecurityPicker_SecuritySelected(Security security)
        {
            if (security == null) return;
            _connector.SubscribeLevel1(security);
        }
    }
}
