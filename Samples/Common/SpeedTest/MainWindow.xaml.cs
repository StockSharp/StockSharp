namespace SpeedTest
{
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Security;
	using System.Windows;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Fix;
	using StockSharp.Plaza;
	using StockSharp.SmartCom;	
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Quik;
	using StockSharp.Quik.Lua;

	public partial class MainWindow
	{
		private Connector _connector;
		private FilterableSecurityProvider _securityProvider;

		public MainWindow()
		{
			Instance = this;
			Strategies = new ObservableCollection<SpeedTestStrategy>();
			InitializeComponent();
		}

		public static ObservableCollection<SpeedTestStrategy> Strategies { get; set; }
		public static MainWindow Instance { get; set; }

		private void QuikConnectionMouseDoubleClick(object sender, RoutedEventArgs e)
		{
			if (_connector == null)
			{
				_connector = new Connector();

				if (QuikCheckBox.IsChecked == true)
				{
					var quikTs = new LuaFixTransactionMessageAdapter(_connector.TransactionIdGenerator)
					{
						Login = "quik",
						Password = "quik".To<SecureString>(),
						Address = QuikTrader.DefaultLuaAddress,
						TargetCompId = "StockSharpTS",
						SenderCompId = "quik",
						ExchangeBoard = ExchangeBoard.Forts,
						Version = FixVersions.Fix44_Lua,
						RequestAllPortfolios = true,
						MarketData = FixMarketData.None,
						TimeZone = TimeHelper.Moscow
					};
					var quikMd = new FixMessageAdapter(_connector.TransactionIdGenerator)
					{
						Login = "quik",
						Password = "quik".To<SecureString>(),
						Address = QuikTrader.DefaultLuaAddress,
						TargetCompId = "StockSharpMD",
						SenderCompId = "quik",
						ExchangeBoard = ExchangeBoard.Forts,
						Version = FixVersions.Fix44_Lua,
						RequestAllSecurities = true,
						MarketData = FixMarketData.MarketData,
						TimeZone = TimeHelper.Moscow
					};
					quikMd.RemoveTransactionalSupport();

					_connector.Adapter.InnerAdapters[quikMd.ToChannel(_connector, "Quik MD")] = 1;
					_connector.Adapter.InnerAdapters[quikTs.ToChannel(_connector, "Quik TS")] = 1;
				}

				if (SmartComCheckBox.IsChecked == true)
				{
					var smartCom = new SmartComMessageAdapter(_connector.TransactionIdGenerator)
					{
						Login = Login.Text,
						Password = Password.Password.To<SecureString>(),
						Address = Address.SelectedAddress,
					};
					_connector.Adapter.InnerAdapters[smartCom.ToChannel(_connector, "SmartCOM")] = 0;
				}

				if (PlazaCheckBox.IsChecked == true)
				{
					var pool = new PlazaConnectionPool();

					_connector.Adapter.InnerAdapters[new PlazaTransactionMessageAdapter(_connector.TransactionIdGenerator, pool)
					{
						ConnectionPoolSettings =
						{
							IsCGate = true,
						}
					}.ToChannel(_connector, "Plaza TS")] = 0;
					_connector.Adapter.InnerAdapters[new PlazaStreamMessageAdapter(_connector.TransactionIdGenerator, pool)
					{
						ConnectionPoolSettings =
						{
							IsCGate = true,
						}
					}.ToChannel(_connector, "Plaza MD")] = 0;
				}

				if (_connector.Adapter.InnerAdapters.Count == 0)
				{
					MessageBox.Show(this, LocalizedStrings.Str2971);
					return;
				}

				_securityProvider = new FilterableSecurityProvider(_connector);

				_connector.ConnectionError += error => this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959));
				_connector.Connect();
			}
			else
			{
				Disconnect();
			}
		}

		private void Disconnect()
		{
			try
			{
				foreach (var security in Strategies.Select(s => s.Security))
				{
					_connector.UnRegisterMarketDepth(security);
				}

				QuikCheckBox.IsEnabled = SmartComCheckBox.IsEnabled = true;
				Strategies.Clear();

				Connection.Background = new SolidColorBrush(Colors.LightCoral);
				ConnectButton.Header = LocalizedStrings.Connect;

				_connector.Disconnect();
				_connector = null;

			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString());
			}
		}

		private SpeedTestStrategy SelectedStrategy
		{
			get { return (SpeedTestStrategy)TestStrategies.SelectedItem; }
		}

		private void StartClick(object sender, RoutedEventArgs e)
		{
			try
			{
				if (SelectedStrategy.ProcessState == ProcessStates.Started)
				{
					SelectedStrategy.Stop();
				}
				else
				{
					SelectedStrategy.OrderTimeChanged += () => this.GuiAsync(() => TestStrategies.Items.Refresh());
					SelectedStrategy.Start();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString());
			}
		}

		private void NewOrderTestClick(object sender, RoutedEventArgs e)
		{
			if (_connector == null)
				MessageBox.Show(this, LocalizedStrings.Str2972);
			else
			{
				var window = new NewOrderTest(_connector, _securityProvider);
				window.Show();
			}
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (_connector != null)
			{
				Disconnect();
			}
			base.OnClosing(e);
		}

		private void SmartComCheckBoxClick(object sender, RoutedEventArgs e)
		{
			if (SmartComCheckBox.IsChecked == true)
			{
				Login.IsEnabled = Password.IsEnabled = Address.IsEnabled = true;
			}
			else
			{
				Login.IsEnabled = Password.IsEnabled = Address.IsEnabled = false;
			}
		}

		private void TestStrategiesSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			StartButton.IsEnabled = SelectedStrategy != null;
		}
	}
}