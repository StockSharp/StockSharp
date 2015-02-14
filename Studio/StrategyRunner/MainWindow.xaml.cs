namespace StockSharp.Studio.StrategyRunner
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Security;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Diagram;

	public partial class MainWindow
	{
		public static readonly RoutedCommand ConnectCommand = new RoutedCommand();
		public static readonly RoutedCommand ConnectionSettingsCommand = new RoutedCommand();
		//public static readonly RoutedCommand AddStrategyCommand = new RoutedCommand();
		//public static readonly RoutedCommand RemoveStrategyCommand = new RoutedCommand();
		public static readonly RoutedCommand OpenStrategyCommand = new RoutedCommand();
		public static readonly RoutedCommand StartStrategyCommand = new RoutedCommand();
		public static readonly RoutedCommand StopStrategyCommand = new RoutedCommand();

		private const string _settingsFile = "StrategyRunner_settings.xml";
		//private const string _strategyFile = "strategy.xml";

		private readonly StrategyConnector _connector;

		private readonly ICollection<EquityData> _totalPnL;
		private readonly ICollection<EquityData> _unrealizedPnL;
		private readonly ICollection<EquityData> _commission;
		private readonly Brush _stoppedBg;
		private readonly Brush _startedBg;
		private readonly LogManager _logManager;

		private DateTimeOffset _lastPnlTime;
		private SettingsStorage _settings;
		private DiagramStrategyEx _strategy;

		public string ProductTitle
		{
			get { return TypeHelper.ApplicationNameWithVersion; }
		}

		public MainWindow()
		{
			ConfigManager.RegisterService<IStorage>(new InMemoryStorage());

			InitializeComponent();

			if (AutomaticUpdater.ClosingForInstall)
			{
				Application.Current.Shutdown();
				return;
			}

			AutomaticUpdater.Translate();
			
			_totalPnL = EquityCurveChart.CreateCurve("P&L", Colors.Green, Colors.Red, EquityCurveChartStyles.Area);
			_unrealizedPnL = EquityCurveChart.CreateCurve(LocalizedStrings.Str3261, Colors.Black);
			_commission = EquityCurveChart.CreateCurve(LocalizedStrings.Str159, Colors.Red, EquityCurveChartStyles.DashedLine);
			_stoppedBg = ConnectBtn.Background;
			_startedBg = Brushes.Pink;

			_logManager = new LogManager();
			_logManager.Listeners.Add(new FileLogListener { SeparateByDates = SeparateByDateModes.SubDirectories });

			_connector = new StrategyConnector();
			_connector.Connected += ConnectionChanged;
			_connector.Disconnected += ConnectionChanged;

			LoadSettings();

			_logManager.Listeners.Add(new GuiLogListener(Monitor));
			_logManager.Sources.Add(_connector);

			ConfigManager.RegisterService(_connector);
			ConfigManager.RegisterService<IConnector>(_connector);
			ConfigManager.RegisterService(new FilterableSecurityProvider(_connector.SecurityList));

			InitializeCompositions();
		}

		private void InitializeCompositions()
		{
			var compositionSerializer = new CompositionRegistry();

			ConfigManager.RegisterService(compositionSerializer);

			compositionSerializer.DiagramElements.AddRange(AppConfig.Instance.DiagramElements.Select(t => t.CreateInstance<DiagramElement>()));
			Directory.GetFiles("Compositions", "*.xml").Select(File.ReadAllText).ForEach(s => compositionSerializer.Load(s.LoadSettingsStorage(), true));
		}

		private void ConnectionChanged()
		{
			this.GuiAsync(() =>
			{
				switch (_connector.ConnectionState)
				{
					case ConnectionStates.Connected:
						ConnectBtn.Background = _startedBg;
						_connector.StartExport();
						break;
					case ConnectionStates.Disconnected:
						ConnectBtn.Background = _stoppedBg;
						break;
				}
			});
		}

		private void LoadSettings()
		{
			_settings = File.Exists(_settingsFile) 
				? CultureInfo.InvariantCulture.DoInCulture(() => new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile)) 
				: new SettingsStorage();

			//var logManager = _settings.GetValue<SettingsStorage>("LogManager");

			//if (logManager != null)
			//{
			//	_logManager.Load(logManager);
			//}
			//else
			//	_logManager.Listeners.Add(new FileLogListener { SeparateByDates = SeparateByDateModes.SubDirectories });

			var connectionSettings = _settings.GetValue<SettingsStorage>("Connection");

			if (connectionSettings != null)
				_connector.BasketSessionHolder.Load(connectionSettings);
		}

		private void SaveSettings()
		{
			_settings.SetValue("Connection", _connector.BasketSessionHolder.Save());
			//_settings.SetValue("LogManager", _logManager.Save());
			_settings.SetValue("Layout", DockSite.SaveLayout());

			CultureInfo.InvariantCulture.DoInCulture(() => new XmlSerializer<SettingsStorage>().Serialize(_settings, _settingsFile));
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			var layout = _settings.GetValue<string>("Layout");

			if (layout != null)
			{
				try
				{
					DockSite.LoadLayout(layout);
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}

			var strategyFile = _settings.GetValue<string>("StrategyFile");

			if (!strategyFile.IsEmpty() && File.Exists(strategyFile))
				LoadStrategy(strategyFile);
			//else
			//{
			//	new MessageBoxBuilder()
			//		.Error()
			//		.Text("Файл стратегии не найден.")
			//		.Owner(this)
			//		.Button(MessageBoxButton.OK)
			//		.Show();
			//}
		}

		private void MainWindow_OnClosing(object sender, CancelEventArgs e)
		{
			SaveSettings();
		}

		private void ExecutedConnect(object sender, ExecutedRoutedEventArgs e)
		{
			switch (_connector.ConnectionState)
			{
				case ConnectionStates.Connected:
					_connector.StopExport();
					_connector.Disconnect();
					break;

				case ConnectionStates.Disconnected:
					_connector.Connect();
					break;
			}
		}

		private void CanExecuteConnect(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _connector != null && (_connector.ConnectionState == ConnectionStates.Disconnected || _connector.ConnectionState == ConnectionStates.Connected);
		}

		private void ExecutedConnectionSettings(object sender, ExecutedRoutedEventArgs e)
		{
			var wnd = new SessionHoldersWindow();
			wnd.CheckConnectionState += () => _connector.ConnectionState;
			wnd.ConnectorsInfo.AddRange(AppConfig.Instance.Connections);
			wnd.SessionHolder = _connector.BasketSessionHolder;

			if (wnd.ShowModal(this))
			{
				SaveSettings();
			}
		}

		private void CanExecuteConnectionSettings(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecutedStartStrategyCommand(object sender, ExecutedRoutedEventArgs e)
		{
			string error = null;

			if (_strategy.Security == null)
				error = LocalizedStrings.Str3613Params.Put(_strategy.Name);
			else if (_strategy.Portfolio == null)
				error = LocalizedStrings.Str3614Params.Put(_strategy.Name);
			else if (_strategy.Composition == null)
				error = LocalizedStrings.Str3615Params.Put(_strategy.Name);
			else if (_strategy.Composition.HasErrors)
				error = LocalizedStrings.Str3616Params.Put(_strategy.Name);

			if (!error.IsEmpty())
			{
				new MessageBoxBuilder()
					.Error()
					.Text(error)
					.Owner(this)
					.Show();
			}
			else
				_strategy.Start();
		}

		private void CanExecuteStartStrategyCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _strategy != null && _strategy.ProcessState == ProcessStates.Stopped;
		}

		private void ExecutedStopStrategyCommand(object sender, ExecutedRoutedEventArgs e)
		{
			_strategy.Stop();
		}

		private void CanExecuteStopStrategyCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _strategy != null && _strategy.ProcessState == ProcessStates.Started;
		}

		private void ExecutedOpenStrategy(object sender, ExecutedRoutedEventArgs e)
		{
			var dlg = new VistaOpenFileDialog
			{
				Filter = "{0} (.xml)|*.xml".Put(LocalizedStrings.Str1355),
				CheckFileExists = true,
				RestoreDirectory = true
			};

			if (dlg.ShowDialog(this) != true)
				return;

			UnloadStrategy();
			
			if (!LoadStrategy(dlg.FileName))
				return;

			_settings.SetValue("StrategyFile", dlg.FileName);

		}

		private void CanExecuteOpenStrategy(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		#region Strategy

		private bool LoadStrategy(string strategyFile)
		{
			var composition = LoadComposition(strategyFile);

			if (composition == null)
			{
				new MessageBoxBuilder()
					.Error()
					.Text(LocalizedStrings.StrategyLoadingCancelled)
					.Owner(this)
					.Button(MessageBoxButton.OK)
					.Show();

				return false;
			}

			_strategy = new DiagramStrategyEx
			{
				Connector = _connector,
				Composition = composition,
			};

			_strategy.OrderRegistering += OnStrategyOrderRegistering;
			_strategy.OrderReRegistering += OnStrategyOrderReRegistering;
			_strategy.OrderRegisterFailed += OnStrategyOrderRegisterFailed;

			_strategy.StopOrderRegistering += OnStrategyOrderRegistering;
			_strategy.StopOrderReRegistering += OnStrategyOrderReRegistering;
			_strategy.StopOrderRegisterFailed += OnStrategyOrderRegisterFailed;

			_strategy.NewMyTrades += OnStrategyNewMyTrade;

			_strategy.PositionManager.NewPosition += OnStrategyNewPosition;
			_strategy.PositionManager.Positions.ForEach(OnStrategyNewPosition);

			_strategy.PnLChanged += OnStrategyPnLChanged;
			_strategy.Reseted += OnStrategyReseted;

			_strategy.SetCandleManager(new CandleManager(_connector));
			_strategy.SetChart(ChartPanel);

			PropertyGrid.SelectedObject = _strategy;
			StatisticParameterGrid.StatisticManager = _strategy.StatisticManager;

			return true;
		}

		private ExportDiagramElement LoadComposition(string strategyFile)
		{
			var password = _settings.GetValue<SecureString>("Password");
			var hasPassword = _settings.ContainsKey("Password");
			var savePassword = false;

			var strategy = File.ReadAllText(strategyFile);

			while (true)
			{
				if (!hasPassword)
				{
					var wnd = new SecretWindow { Secret = password };

					if (!wnd.ShowModal(this))
						return null;

					savePassword = wnd.SaveSecret;
					password = wnd.Secret;
				}

				try
				{
					var data = strategy;

					if (!password.IsEmpty())
						data = data.Decrypt(password.To<string>());

					var composition = ConfigManager.GetService<CompositionRegistry>().DeserializeExported(data.LoadSettingsStorage());

					if (!savePassword)
						return composition;

					_settings.SetValue("Password", password);
					SaveSettings();

					return composition;
				}
				catch (Exception ex)
				{
					ex.LogError();

					new MessageBoxBuilder()
						.Error()
						.Text(LocalizedStrings.StrategyLoadingError)
						.Owner(this)
						.Button(MessageBoxButton.OK)
						.Show();

					hasPassword = false;
				}
			}
		}

		private void UnloadStrategy()
		{
			PropertyGrid.SelectedObject = null;
			StatisticParameterGrid.StatisticManager = null;

			OrderGrid.Orders.Clear();
			MyTradeGrid.Trades.Clear();
			PositionGrid.Positions.Clear();
			PositionGrid.Portfolios.Clear();

			_totalPnL.Clear();
			_unrealizedPnL.Clear();
			_commission.Clear();

			ChartPanel.ClearAreas();

			if (_strategy == null)
				return;

			_strategy.OrderRegistering -= OnStrategyOrderRegistering;
			_strategy.OrderReRegistering -= OnStrategyOrderReRegistering;
			_strategy.OrderRegisterFailed -= OnStrategyOrderRegisterFailed;

			_strategy.StopOrderRegistering -= OnStrategyOrderRegistering;
			_strategy.StopOrderReRegistering -= OnStrategyOrderReRegistering;
			_strategy.StopOrderRegisterFailed -= OnStrategyOrderRegisterFailed;

			_strategy.NewMyTrades -= OnStrategyNewMyTrade;

			_strategy.PositionManager.NewPosition -= OnStrategyNewPosition;

			_strategy.PnLChanged -= OnStrategyPnLChanged;
			_strategy.Reseted -= OnStrategyReseted;
		}

		private void OnStrategyOrderRegisterFailed(OrderFail fail)
		{
			OrderGrid.AddRegistrationFail(fail);
		}

		private void OnStrategyOrderReRegistering(Order oldOrder, Order newOrder)
		{
			OrderGrid.Orders.Add(newOrder);
		}

		private void OnStrategyOrderRegistering(Order order)
		{
			OrderGrid.Orders.Add(order);
		}

		private void OnStrategyNewMyTrade(IEnumerable<MyTrade> trades)
		{
			MyTradeGrid.Trades.AddRange(trades);
		}

		private void OnStrategyNewPosition(Position position)
		{
			PositionGrid.Positions.Add(position);
		}

		private void OnStrategyPnLChanged()
		{
			var time = _strategy.CurrentTime;

			if (time < _lastPnlTime)
				return; // TODO нужен перевод стратегий на месседжи
			//throw new InvalidOperationException("Новое значение даты для PnL {0} меньше ранее добавленного {1}.".Put(time, _lastPnlTime));

			_lastPnlTime = time;

			_totalPnL.Add(new EquityData { Time = time, Value = _strategy.PnL - (_strategy.Commission ?? 0) });
			_unrealizedPnL.Add(new EquityData { Time = time, Value = _strategy.PnLManager.UnrealizedPnL });
			_commission.Add(new EquityData { Time = time, Value = _strategy.Commission ?? 0 });
		}

		private void OnStrategyReseted()
		{
			_lastPnlTime = DateTimeOffset.MinValue;

			_totalPnL.Clear();
			_unrealizedPnL.Clear();
			_commission.Clear();

			OrderGrid.Orders.Clear();
			MyTradeGrid.Trades.Clear();
			PositionGrid.Positions.Clear();

			ChartPanel.Reset(ChartPanel.Elements);
		}

		#endregion
	}
}
