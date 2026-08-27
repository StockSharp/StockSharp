namespace StockSharp.Samples.Strategies.LiveOptionsQuoting.Avalonia;
#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Threading;

using Ecng.Collections;
using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Derivatives;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml;
using StockSharp.Xaml.Avalonia;
using StockSharp.Xaml.Charting.Avalonia.Specialized;
using StockSharp.Xaml.Grids.Avalonia;

using MonitorControl = StockSharp.Xaml.Windows.Avalonia.Monitor;

public partial class MainWindow : Window
{
	private sealed class QuoteSession(
		SecurityId securityId,
		int generation,
		QuotesWindow window,
		Strategy strategy)
	{
		public SecurityId SecurityId { get; } = securityId;
		public int Generation { get; } = generation;
		public QuotesWindow Window { get; } = window;
		public Strategy Strategy { get; } = strategy;
		public EventSubscription Events { get; set; }
	}

	private readonly SampleConnectorContext _context = new();
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly SemaphoreSlim _strategyGate = new(1, 1);
	private readonly EventSubscription _connectorEvents;
	private readonly EventSubscription _timerEvents;
	private readonly ObservableCollection<Security> _assets = [];
	private readonly ObservableCollection<Security> _options = [];
	private readonly List<Subscription> _marketSubscriptions = [];
	private readonly Dictionary<SecurityId, QuoteSession> _quoteSessions = [];
	private readonly object _quoteSync = new();
	private readonly OptionDeskModel _model = new();
	private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(5) };
	private readonly LogManager _logManager;
	private readonly ComboBox _assetCombo;
	private readonly ComboBox _optionCombo;
	private readonly PortfolioEditor _portfolioEditor;
	private readonly TextBox _impliedVolatility;
	private readonly NumericUpDown _impliedVolatilityMin;
	private readonly NumericUpDown _impliedVolatilityMax;
	private readonly OptionPositionChart _positionChart;
	private readonly OptionDesk _desk;
	private readonly OptionVolatilitySmileChart _smileChart;
	private readonly OptionVolatilitySmileSeries _putBidSmile;
	private readonly OptionVolatilitySmileSeries _putAskSmile;
	private readonly OptionVolatilitySmileSeries _putLastSmile;
	private readonly OptionVolatilitySmileSeries _callBidSmile;
	private readonly OptionVolatilitySmileSeries _callAskSmile;
	private readonly OptionVolatilitySmileSeries _callLastSmile;
	private readonly OrderGrid _orderGrid;
	private readonly MyTradeGrid _myTradeGrid;
	private readonly PortfolioGrid _portfolioGrid;
	private readonly MonitorControl _monitor;
	private readonly Button _settingsButton;
	private readonly Button _connectButton;
	private readonly Button _startButton;
	private readonly TextBlock _status;
	private Task _configurationTask = Task.CompletedTask;
	private int _quoteGeneration;
	private int _isDirty;
	private bool _eventsAttached;
	private bool _connectStarted;
	private bool _isConnected;
	private bool _isClosing;
	private bool _closeApproved;

	private Connector Connector => _context.Connector;

	private Security SelectedAsset => _assetCombo.SelectedItem as Security;

	private Security SelectedOption => _optionCombo.SelectedItem as Security;

	public MainWindow()
	{
		InitializeComponent();

		_assetCombo = this.FindControl<ComboBox>(nameof(AssetCombo));
		_optionCombo = this.FindControl<ComboBox>(nameof(OptionCombo));
		_portfolioEditor = this.FindControl<PortfolioEditor>(nameof(PortfolioEditor));
		_impliedVolatility = this.FindControl<TextBox>(nameof(ImpliedVolatilityText));
		_impliedVolatilityMin = this.FindControl<NumericUpDown>(nameof(ImpliedVolatilityMin));
		_impliedVolatilityMax = this.FindControl<NumericUpDown>(nameof(ImpliedVolatilityMax));
		_positionChart = this.FindControl<OptionPositionChart>(nameof(PositionChart));
		_desk = this.FindControl<OptionDesk>(nameof(OptionDesk));
		_smileChart = this.FindControl<OptionVolatilitySmileChart>(nameof(SmileChart));
		_orderGrid = this.FindControl<OrderGrid>(nameof(OrderGrid));
		_myTradeGrid = this.FindControl<MyTradeGrid>(nameof(MyTradeGrid));
		_portfolioGrid = this.FindControl<PortfolioGrid>(nameof(PortfolioGrid));
		_monitor = this.FindControl<MonitorControl>(nameof(LogMonitor));
		_settingsButton = this.FindControl<Button>(nameof(SettingsButton));
		_connectButton = this.FindControl<Button>(nameof(ConnectButton));
		_startButton = this.FindControl<Button>(nameof(StartButton));
		_status = this.FindControl<TextBlock>(nameof(StatusText));

		_assetCombo.ItemsSource = _assets;
		_optionCombo.ItemsSource = _options;
		_portfolioEditor.PortfolioProvider = Connector;
		_desk.Model = _model;

		_putBidSmile = _smileChart.CreateSmile("Put (B)", 0);
		_putAskSmile = _smileChart.CreateSmile("Put (A)", 1);
		_putLastSmile = _smileChart.CreateSmile("Put (L)", 2);
		_callBidSmile = _smileChart.CreateSmile("Call (B)", 3);
		_callAskSmile = _smileChart.CreateSmile("Call (A)", 4);
		_callLastSmile = _smileChart.CreateSmile("Call (L)", 5);

		_logManager = new LogManager();
		_logManager.Listeners.Add(new GuiLogListener(_monitor));
		_logManager.Sources.Add(Connector);

		_connectorEvents = new(
			() =>
			{
				Connector.Connected += OnConnected;
				Connector.Disconnected += OnDisconnected;
				Connector.ConnectionError += OnConnectionError;
				Connector.Error += OnConnectorError;
				Connector.SubscriptionFailed += OnSubscriptionFailed;
				Connector.SecurityReceived += OnSecurityReceived;
				Connector.TickTradeReceived += OnTickTradeReceived;
				Connector.PortfolioReceived += OnPortfolioReceived;
				Connector.PositionReceived += OnPositionReceived;
				Connector.OrderBookReceived += OnOrderBookReceived;
			},
			() =>
			{
				Connector.Connected -= OnConnected;
				Connector.Disconnected -= OnDisconnected;
				Connector.ConnectionError -= OnConnectionError;
				Connector.Error -= OnConnectorError;
				Connector.SubscriptionFailed -= OnSubscriptionFailed;
				Connector.SecurityReceived -= OnSecurityReceived;
				Connector.TickTradeReceived -= OnTickTradeReceived;
				Connector.PortfolioReceived -= OnPortfolioReceived;
				Connector.PositionReceived -= OnPositionReceived;
				Connector.OrderBookReceived -= OnOrderBookReceived;
			});

		_timerEvents = new(
			() => _refreshTimer.Tick += OnRefreshTick,
			() => _refreshTimer.Tick -= OnRefreshTick);
		_timerEvents.Attach();
		_refreshTimer.Start();

		ApplyGreekSelection();
		DrawTestData();

		Opened += OnOpened;
		Closing += OnClosing;
	}

	private void OnSettingsClick(object sender, RoutedEventArgs e)
	{
		if (_isClosing || !_configurationTask.IsCompleted)
			return;

		_configurationTask = ConfigureAsync();
	}

	private async Task ConfigureAsync()
	{
		_settingsButton.IsEnabled = false;
		try
		{
			await _context.ConfigureAsync(this, _lifetimeCancellation.Token);
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			if (!_isClosing)
				_status.Text = $"Settings failed: {error.Message}";
		}
		finally
		{
			if (!_isClosing && !_isConnected && !_connectStarted)
				_settingsButton.IsEnabled = true;
		}
	}

	private void OnOpened(object sender, EventArgs e)
	{
		if (_context.AutoConnect)
			StartConnect();
	}

	private void OnConnectClick(object sender, RoutedEventArgs e)
	{
		if (_isConnected)
		{
			_connectButton.IsEnabled = false;
			_status.Text = "Disconnecting";
			Connector.Disconnect();
		}
		else
			StartConnect();
	}

	private void StartConnect()
	{
		if (_connectStarted || _isClosing)
			return;

		if (!_eventsAttached)
		{
			_connectorEvents.Attach();
			_eventsAttached = true;
		}

		_connectStarted = true;
		_settingsButton.IsEnabled = false;
		_connectButton.IsEnabled = false;
		_status.Text = "Connecting";
		_model.Clear();
		_model.MarketDataProvider = Connector;
		_positionChart.Model = null;
		ClearSmiles();
		Connector.Connect();
	}

	private void OnConnected()
		=> _uiEvents.Dispatch(() =>
		{
			if (_isClosing)
				return;

			_isConnected = true;
			_connectStarted = false;
			_settingsButton.IsEnabled = false;
			_connectButton.IsEnabled = true;
			_connectButton.Content = "Disconnect";
			_status.Text = "Connected";
		});

	private void OnDisconnected()
		=> _uiEvents.Dispatch(() => ResetConnectionUi("Disconnected"));

	private void OnConnectionError(Exception error)
		=> _uiEvents.Dispatch(() => ResetConnectionUi($"Connection failed: {error.Message}"));

	private void OnConnectorError(Exception error)
		=> _uiEvents.Dispatch(() =>
		{
			if (!_isClosing)
				_status.Text = $"Data error: {error.Message}";
		});

	private void OnSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
		=> _uiEvents.Dispatch(() =>
		{
			if (!_isClosing)
				_status.Text = $"Subscription failed ({subscription.DataType}): {error.Message}";
		});

	private void ResetConnectionUi(string status)
	{
		if (_isClosing)
			return;

		_isConnected = false;
		_connectStarted = false;
		_settingsButton.IsEnabled = true;
		_connectButton.IsEnabled = true;
		_connectButton.Content = "Connect";
		_status.Text = status;
	}

	private void OnSecurityReceived(Subscription subscription, Security security)
		=> _uiEvents.Dispatch(() =>
		{
			if (_isClosing)
				return;

			if (security.Type == SecurityTypes.Future)
				_assets.TryAdd(security);

			var asset = _model.UnderlyingAsset;
			if (asset == security || (asset is not null && asset.Id == security.UnderlyingSecurityId))
				Interlocked.Exchange(ref _isDirty, 1);
		});

	private void OnTickTradeReceived(Subscription subscription, ITickTradeMessage trade)
	{
		if (_model.UnderlyingAssetId == trade.SecurityId)
			Interlocked.Exchange(ref _isDirty, 1);
	}

	private void OnPortfolioReceived(Subscription subscription, Portfolio portfolio)
		=> _uiEvents.Dispatch(() =>
		{
			if (!_isClosing)
				_portfolioGrid.Positions.TryAdd(portfolio);
		});

	private void OnPositionReceived(Subscription subscription, Position position)
		=> _uiEvents.Dispatch(() =>
		{
			if (_isClosing)
				return;

			_portfolioGrid.Positions.TryAdd(position);
			var asset = SelectedAsset;
			if (asset is not null && (position.Security == asset || position.Security.UnderlyingSecurityId == asset.Id))
				RefreshPositionChart();
		});

	private void OnOrderBookReceived(Subscription subscription, IOrderBookMessage depth)
	{
		QuoteSession session;
		lock (_quoteSync)
			_quoteSessions.TryGetValue(depth.SecurityId, out session);

		if (session is null)
			return;

		_uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, session.Generation))
				session.Window.Update(depth.ImpliedVolatility(Connector, Connector, depth.ServerTime));
		});
	}

	private void OnRefreshTick(object sender, EventArgs e)
	{
		if (_isClosing || Interlocked.Exchange(ref _isDirty, 0) == 0)
			return;

		try
		{
			RefreshSmile();
			RefreshPositionChart();
		}
		catch (Exception error)
		{
			error.LogError();
			_status.Text = $"Refresh failed: {error.Message}";
		}
	}

	private void OnGreekSelectionChanged(object sender, RoutedEventArgs e)
	{
		ApplyGreekSelection();
		Interlocked.Exchange(ref _isDirty, 1);
	}

	private void ApplyGreekSelection()
	{
		_model.EvaluateFields.Clear();
		TryAddGreek(nameof(ImpliedVolatilityField), Level1Fields.ImpliedVolatility);
		TryAddGreek(nameof(DeltaField), Level1Fields.Delta);
		TryAddGreek(nameof(GammaField), Level1Fields.Gamma);
		TryAddGreek(nameof(VegaField), Level1Fields.Vega);
		TryAddGreek(nameof(ThetaField), Level1Fields.Theta);
		TryAddGreek(nameof(RhoField), Level1Fields.Rho);
	}

	private void TryAddGreek(string controlName, Level1Fields field)
	{
		if (this.FindControl<CheckBox>(controlName)?.IsChecked == true)
			_model.EvaluateFields.Add(field);
	}

	private void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isClosing)
			return;

		UnsubscribeMarketData();
		_options.Clear();
		_model.Clear();
		ClearSmiles();
		_startButton.IsEnabled = false;

		var asset = SelectedAsset;
		if (asset is null)
		{
			_model.UnderlyingAsset = null;
			_positionChart.Model = null;
			return;
		}

		_model.MarketDataProvider = Connector;
		_model.UnderlyingAsset = asset;
		SubscribeMarketData(asset);

		var basket = new BasketBlackScholes(asset, Connector, Connector);
		foreach (var option in asset.GetDerivatives(Connector))
		{
			_model.Add(option);
			_options.Add(option);
			SubscribeMarketData(option);
			basket.InnerModels.Add(new BlackScholes(option, asset, Connector));
		}

		_positionChart.Model = basket;
		Interlocked.Exchange(ref _isDirty, 1);
	}

	private void SubscribeMarketData(Security security)
	{
		foreach (var dataType in new[] { DataType.Level1, DataType.MarketDepth, DataType.Ticks })
		{
			var subscription = new Subscription(dataType, security);
			_marketSubscriptions.Add(subscription);
			Connector.Subscribe(subscription);
		}
	}

	private void UnsubscribeMarketData()
	{
		foreach (var subscription in _marketSubscriptions.ToArray())
		{
			try
			{
				Connector.UnSubscribe(subscription);
			}
			catch (Exception error)
			{
				error.LogError();
			}
		}

		_marketSubscriptions.Clear();
	}

	private void OnOptionSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var option = SelectedOption;
		if (option is null)
		{
			_impliedVolatility.Text = string.Empty;
			_startButton.IsEnabled = false;
			return;
		}

		_impliedVolatility.Text = option.ImpliedVolatility?.ToString() ?? string.Empty;
		_impliedVolatilityMin.Value = option.ImpliedVolatility ?? 0m;
		_impliedVolatilityMax.Value = option.ImpliedVolatility ?? 100m;
		_startButton.IsEnabled = !_isClosing;
	}

	private async void OnStartClick(object sender, RoutedEventArgs e)
	{
		try
		{
			await StartQuoteSessionAsync();
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			if (!_isClosing)
				_status.Text = $"Quoting failed: {error.Message}";
		}
	}

	private async Task StartQuoteSessionAsync()
	{
		await _strategyGate.WaitAsync(_lifetimeCancellation.Token);
		try
		{
			if (_isClosing)
				return;

			var option = SelectedOption;
			var portfolio = _portfolioEditor.SelectedPortfolio;
			var model = _positionChart.Model;
			if (option is null || portfolio is null || model is null)
			{
				_status.Text = "Select an underlying asset, option, and portfolio.";
				return;
			}

			var securityId = option.ToSecurityId();
			QuoteSession current;
			lock (_quoteSync)
				_quoteSessions.TryGetValue(securityId, out current);
			if (current is not null)
			{
				if (current.Window.IsVisible)
					current.Window.Activate();
				else
					current.Window.Show(this);
				return;
			}

			var underlying = option.GetUnderlyingAsset(Connector) ?? SelectedAsset;
			if (underlying is null)
			{
				_status.Text = "The option has no underlying asset.";
				return;
			}

			var hedge = new global::StockSharp.Samples.Strategies.LiveOptionsQuoting.DeltaHedgeStrategy(model)
			{
				Security = underlying,
				Portfolio = portfolio,
				Connector = Connector,
			};
			QuotesWindow window = null;
			QuoteSession session = null;
			try
			{
				var quoting = new VolatilityQuotingStrategy
				{
					QuotingSide = Sides.Buy,
					QuotingVolume = 20,
					IVRange = new Range<decimal>(_impliedVolatilityMin.Value ?? 0m, _impliedVolatilityMax.Value ?? 100m),
					Volume = 1,
					Security = option,
					Portfolio = portfolio,
					Connector = Connector,
				};
				try
				{
					hedge.ChildStrategies.Add(quoting);
				}
				catch
				{
					quoting.Dispose();
					throw;
				}

				window = new QuotesWindow { Title = option.Name ?? option.Id };
				session = new QuoteSession(securityId, ++_quoteGeneration, window, hedge);
				AttachQuoteSessionEvents(session);
				lock (_quoteSync)
					_quoteSessions.Add(securityId, session);
				_logManager.Sources.Add(hedge);
				_status.Text = $"Starting {option.Id}";

				await hedge.StartAsync(_lifetimeCancellation.Token);
				if (IsCurrent(session, session.Generation))
				{
					window.Show(this);
					_status.Text = $"Quoting {option.Id}";
				}
			}
			catch
			{
				if (session is not null)
					await StopQuoteSessionCoreAsync(session, closeWindow: true);
				else
				{
					window?.Dispose();
					hedge.Dispose();
				}
				throw;
			}
		}
		finally
		{
			_strategyGate.Release();
		}
	}

	private void AttachQuoteSessionEvents(QuoteSession session)
	{
		var strategy = session.Strategy;
		var generation = session.Generation;
		Action<Subscription, Order> orderReceived = (_, order) => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, generation))
			{
				_orderGrid.Orders.TryAdd(order);
				session.Window.ProcessOrder(order);
			}
		});
		Action<Subscription, OrderFail> orderFailed = (_, fail) => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, generation))
				_orderGrid.AddRegistrationFail(fail);
		});
		Action<Subscription, MyTrade> ownTradeReceived = (_, trade) => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, generation))
				_myTradeGrid.Trades.TryAdd(trade);
		});
		EventHandler closed = (_, _) => OnQuoteWindowClosed(session, generation);

		session.Events = new(
			() =>
			{
				strategy.OrderReceived += orderReceived;
				strategy.OrderRegisterFailReceived += orderFailed;
				strategy.OwnTradeReceived += ownTradeReceived;
				session.Window.Closed += closed;
			},
			() =>
			{
				strategy.OrderReceived -= orderReceived;
				strategy.OrderRegisterFailReceived -= orderFailed;
				strategy.OwnTradeReceived -= ownTradeReceived;
				session.Window.Closed -= closed;
			});
		session.Events.Attach();
	}

	private async void OnQuoteWindowClosed(QuoteSession session, int generation)
	{
		if (!IsCurrent(session, generation))
			return;

		try
		{
			await StopQuoteSessionAsync(session, closeWindow: false);
		}
		catch (Exception error)
		{
			error.LogError();
			if (!_isClosing)
				_status.Text = $"Stop failed: {error.Message}";
		}
	}

	private async Task StopQuoteSessionAsync(QuoteSession session, bool closeWindow)
	{
		await _strategyGate.WaitAsync();
		try
		{
			if (IsCurrent(session, session.Generation))
				await StopQuoteSessionCoreAsync(session, closeWindow);
		}
		finally
		{
			_strategyGate.Release();
		}
	}

	private async Task StopQuoteSessionCoreAsync(QuoteSession session, bool closeWindow)
	{
		lock (_quoteSync)
		{
			if (_quoteSessions.TryGetValue(session.SecurityId, out var current) && ReferenceEquals(current, session))
				_quoteSessions.Remove(session.SecurityId);
		}

		session.Events?.Dispose();
		TryRemoveLogSource(session.Strategy);

		try
		{
			await session.Strategy.StopAsync();
		}
		finally
		{
			session.Strategy.Dispose();
			if (closeWindow && session.Window.IsVisible)
				session.Window.Close();
			session.Window.Dispose();
			if (!_isClosing)
				_status.Text = "Connected";
		}
	}

	private bool IsCurrent(QuoteSession session, int generation)
	{
		if (_isClosing || session.Generation != generation)
			return false;

		lock (_quoteSync)
			return _quoteSessions.TryGetValue(session.SecurityId, out var current) && ReferenceEquals(current, session);
	}

	private void RefreshPositionChart()
	{
		var trade = SelectedAsset?.LastTick;
		if (trade is not null)
			_positionChart.Refresh(trade.Price);
	}

	private void RefreshSmile(DateTime? time = null)
	{
		_model.Refresh(time);
		_putBidSmile.Replace(CreateSmilePoints(row => row.Put?.ImpliedVolatilityBestBid));
		_putAskSmile.Replace(CreateSmilePoints(row => row.Put?.ImpliedVolatilityBestAsk));
		_putLastSmile.Replace(CreateSmilePoints(row => row.Put?.ImpliedVolatilityLastTrade));
		_callBidSmile.Replace(CreateSmilePoints(row => row.Call?.ImpliedVolatilityBestBid));
		_callAskSmile.Replace(CreateSmilePoints(row => row.Call?.ImpliedVolatilityBestAsk));
		_callLastSmile.Replace(CreateSmilePoints(row => row.Call?.ImpliedVolatilityLastTrade));
	}

	private IEnumerable<OptionVolatilitySmilePoint> CreateSmilePoints(Func<OptionDeskRow, decimal?> getVolatility)
		=> _model.Rows
			.Where(row => row.Strike is not null)
			.Select(row => (Strike: row.Strike.Value, Value: getVolatility(row)))
			.Where(point => point.Value is not null)
			.Select(point => new OptionVolatilitySmilePoint((double)point.Strike, (double)point.Value.Value));

	private void ClearSmiles()
	{
		_putBidSmile.Replace([]);
		_putAskSmile.Replace([]);
		_putLastSmile.Replace([]);
		_callBidSmile.Replace([]);
		_callAskSmile.Replace([]);
		_callLastSmile.Replace([]);
	}

	private void DrawTestData()
	{
		var asset = new Security
		{
			Id = "RIM4@FORTS",
			PriceStep = 10,
		};
		asset.LastTick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = asset.ToSecurityId(),
			TradePrice = 130000,
		};

		var expiryDate = new DateTime(2014, 9, 15);
		var currentDate = new DateTime(2014, 8, 15);
		var securities = new List<Security> { asset };
		foreach (var optionType in new[] { OptionTypes.Call, OptionTypes.Put })
		{
			foreach (var strike in new[] { 105000m, 110000m, 115000m, 120000m, 125000m, 130000m, 135000m, 140000m, 145000m, 150000m, 155000m })
				securities.Add(CreateStrike(strike, optionType, expiryDate, asset));
		}

		var dummyProvider = new global::StockSharp.Samples.Strategies.LiveOptionsQuoting.DummyProvider(
			securities,
			[
				new Position { Security = asset, CurrentValue = -1 },
				new Position { Security = securities.First(s => s.OptionType == OptionTypes.Call), CurrentValue = 10 },
				new Position { Security = securities.First(s => s.OptionType == OptionTypes.Put), CurrentValue = -3 },
			]);

		_model.MarketDataProvider = dummyProvider;
		_model.UnderlyingAsset = asset;
		var preview = new BasketBlackScholes(asset, dummyProvider, dummyProvider);
		foreach (var option in securities.Where(s => s.Type == SecurityTypes.Option))
		{
			_model.Add(option);
			preview.InnerModels.Add(new BlackScholes(option, asset, dummyProvider));
		}

		_positionChart.Model = preview;
		_positionChart.Refresh(150000, currentDate, expiryDate);
		RefreshSmile(currentDate);
	}

	private static Security CreateStrike(decimal strike, OptionTypes optionType, DateTime expiryDate, Security asset)
	{
		var option = new Security
		{
			Id = $"RI {optionType} {strike}@FORTS",
			Code = $"RI {(optionType == OptionTypes.Call ? 'C' : 'P')} {strike}",
			Strike = strike,
			OpenInterest = RandomGen.GetInt(10, 5000),
			ImpliedVolatility = RandomGen.GetInt(25, 65),
			HistoricalVolatility = RandomGen.GetInt(25, 65),
			OptionType = optionType,
			ExpiryDate = expiryDate,
			Board = ExchangeBoard.Forts,
			UnderlyingSecurityId = asset.Id,
			LastTick = new ExecutionMessage { DataTypeEx = DataType.Ticks, TradePrice = RandomGen.GetInt(10, 5000) },
			Volume = RandomGen.GetInt(10, 10000),
			Type = SecurityTypes.Option,
		};
		option.BestBid = new QuoteChange(RandomGen.GetInt(10, 1000), RandomGen.GetInt(1, 100));
		option.BestAsk = new QuoteChange(option.BestBid.Value.Price + 10, RandomGen.GetInt(1, 100));
		return option;
	}

	private async void OnClosing(object sender, WindowClosingEventArgs e)
	{
		if (_closeApproved)
			return;

		e.Cancel = true;
		if (_isClosing)
			return;

		_isClosing = true;
		_lifetimeCancellation.Cancel();
		_refreshTimer.Stop();
		_timerEvents.Dispose();
		_startButton.IsEnabled = false;

		try
		{
			await _configurationTask;
			await _strategyGate.WaitAsync();
			try
			{
				QuoteSession[] sessions;
				lock (_quoteSync)
					sessions = [.. _quoteSessions.Values];
				foreach (var session in sessions)
				{
					try
					{
						await StopQuoteSessionCoreAsync(session, closeWindow: true);
					}
					catch (Exception error)
					{
						error.LogError();
					}
				}
			}
			finally
			{
				_strategyGate.Release();
			}
		}
		finally
		{
			Closing -= OnClosing;
			Opened -= OnOpened;
			_connectorEvents.Dispose();
			UnsubscribeMarketData();
			_uiEvents.Dispose();
			TryRemoveLogSource(Connector);
			TryDispose(_assetCombo);
			TryDispose(_optionCombo);
			TryDispose(_portfolioEditor);
			TryDispose(_positionChart);
			TryDispose(_desk);
			TryDispose(_smileChart);
			TryDispose(_orderGrid);
			TryDispose(_myTradeGrid);
			TryDispose(_portfolioGrid);
			TryDispose(_logManager);
			TryDispose(_monitor);
			TryDispose(_context);
			TryDispose(_lifetimeCancellation);
			TryDispose(_strategyGate);
			_closeApproved = true;
			Close();
		}
	}

	private void TryRemoveLogSource(ILogSource source)
	{
		try
		{
			_logManager.Sources.Remove(source);
		}
		catch
		{
		}
	}

	private static void TryDispose(object value)
	{
		try
		{
			if (value is IDisposable disposable)
				disposable.Dispose();
		}
		catch
		{
		}
	}
}
