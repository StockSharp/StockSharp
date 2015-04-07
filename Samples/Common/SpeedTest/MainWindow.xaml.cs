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
	using StockSharp.Messages;
	using StockSharp.Plaza;
	using StockSharp.Quik;
	using StockSharp.SmartCom;	
	using StockSharp.Localization;	

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
				_connector.Connected += _connector.StartExport;

				var session = new BasketSessionHolder(_connector.TransactionIdGenerator);

				_connector.MarketDataAdapter = new BasketMessageAdapter(session);
				_connector.TransactionAdapter = new BasketMessageAdapter(session);

				_connector.ApplyMessageProcessor(MessageDirections.In, true, false);
				_connector.ApplyMessageProcessor(MessageDirections.In, false, true);
				_connector.ApplyMessageProcessor(MessageDirections.Out, true, true);

				if (QuikCheckBox.IsChecked == true)
				{
					session.InnerSessions.Add(new QuikSessionHolder(_connector.TransactionIdGenerator)
					{
						IsTransactionEnabled = true,
						IsMarketDataEnabled = true,
					}, 1);
				}

				if (SmartComCheckBox.IsChecked == true)
				{
					session.InnerSessions.Add(new SmartComSessionHolder(_connector.TransactionIdGenerator)
					{
						Login = Login.Text,
						Password = Password.Password.To<SecureString>(),
						Address = Address.SelectedAddress,
						IsTransactionEnabled = true,
						IsMarketDataEnabled = true,
					}, 0);
				}

				if (PlazaCheckBox.IsChecked == true)
				{
					session.InnerSessions.Add(new PlazaSessionHolder(_connector.TransactionIdGenerator)
					{
						IsCGate = true,
						IsTransactionEnabled = true,
						IsMarketDataEnabled = true,
					}, 0);
				}

				if (session.InnerSessions.Count == 0)
				{
					MessageBox.Show(LocalizedStrings.Str2971);
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
				_connector.StopExport();
				_connector.Disconnect();
				_connector = null;

			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.ToString());
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
			catch (Exception exception)
			{
				MessageBox.Show(exception.ToString());
			}
		}

		private void NewOrderTestClick(object sender, RoutedEventArgs e)
		{
			if (_connector == null)
				MessageBox.Show(LocalizedStrings.Str2972);
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