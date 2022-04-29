#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleOptionQuoting.SampleOptionQuotingPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleOptionQuoting
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Media;
	using System.Windows.Threading;

	using DevExpress.Xpf.Editors;

	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Configuration;
	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Derivatives;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies.Derivatives;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;

	public partial class MainWindow
	{
		private static readonly string _settingsFile = $"connection{Paths.DefaultSettingsExt}";

		public readonly Connector Connector = new();

		private readonly ObservableCollection<Security> _assets;
		private readonly ObservableCollection<Security> _options;
		private readonly OptionDeskModel _model;
		private readonly ICollection<LineData<double>> _putBidSmile;
		private readonly ICollection<LineData<double>> _putAskSmile;
		private readonly ICollection<LineData<double>> _putLastSmile;
		private readonly ICollection<LineData<double>> _callBidSmile;
		private readonly ICollection<LineData<double>> _callAskSmile;
		private readonly ICollection<LineData<double>> _callLastSmile;

		private bool _isDirty;
		private bool _isConnected;

		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new();

		private Security SelectedOption => (Security)Options.SelectedItem;
		private Security SelectedAsset => (Security)Assets.SelectedItem;

		public static MainWindow Instance { get; private set; }

		public MainWindow()
		{
			InitializeComponent();

			_assets = new ObservableCollection<Security>();
			_options = new ObservableCollection<Security>();

			Assets.ItemsSource = _assets;
			Options.ItemsSource = _options;

			_model = new OptionDeskModel();
			Desk.Model = _model;

			_putBidSmile = SmileChart.CreateSmile("Put (B)", Colors.DarkRed);
			_putAskSmile = SmileChart.CreateSmile("Put (A)", Colors.Red);
			_putLastSmile = SmileChart.CreateSmile("Put (L)", Colors.OrangeRed);
			_callBidSmile = SmileChart.CreateSmile("Call (B)", Colors.GreenYellow);
			_callAskSmile = SmileChart.CreateSmile("Call (A)", Colors.DarkGreen);
			_callLastSmile = SmileChart.CreateSmile("Call (L)", Colors.DarkOliveGreen);

			var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
			timer.Tick += (sender, args) =>
			{
				if (!_isDirty)
					return;

				_isDirty = false;

				try
				{
					RefreshSmile();
					RefreshChart();
				}
				catch (Exception excp)
				{
					excp.LogError();
				}
			};
			timer.Start();

			Level1FieldsCtrl.SetItemsSource(new[]
			{
				Level1Fields.ImpliedVolatility,
				Level1Fields.Delta,
				Level1Fields.Gamma,
				Level1Fields.Vega,
				Level1Fields.Theta,
				Level1Fields.Rho,
			});
			Level1FieldsCtrl.SetSelected(_model.EvaluateFields.ToArray());

			Instance = this;

			var loaded = false;

			Loaded += (sender, args) =>
			{
				if(loaded) return;

				loaded = true;

				DrawTestData();
				InitConnector();
			};
		}

		private void DrawTestData()
		{
			//
			// prepare test data

			var asset = new Security
			{
				Id = "RIM4@FORTS",
				PriceStep = 10,
			};

			asset.LastTrade = new Trade
			{
				Security = asset,
				Price = 130000,
			};

			var expiryDate = new DateTime(2014, 09, 15);
			var currDate = new DateTime(2014, 08, 15);

			var securities = new List<Security>
			{
				asset,

				CreateStrike(105000, 10, 60, OptionTypes.Call, expiryDate, asset, 100),
				CreateStrike(110000, 10, 53, OptionTypes.Call, expiryDate, asset, 343),
				CreateStrike(115000, 10, 47, OptionTypes.Call, expiryDate, asset, 3454),
				CreateStrike(120000, 78, 42, OptionTypes.Call, expiryDate, asset, null),
				CreateStrike(125000, 32, 35, OptionTypes.Call, expiryDate, asset, 100),
				CreateStrike(130000, 3245, 32, OptionTypes.Call, expiryDate, asset, 55),
				CreateStrike(135000, 3454, 37, OptionTypes.Call, expiryDate, asset, 456),
				CreateStrike(140000, 34, 45, OptionTypes.Call, expiryDate, asset, 4),
				CreateStrike(145000, 3566, 51, OptionTypes.Call, expiryDate, asset, 67),
				CreateStrike(150000, 454, 57, OptionTypes.Call, expiryDate, asset, null),
				CreateStrike(155000, 10, 59, OptionTypes.Call, expiryDate, asset, 334),

				CreateStrike(105000, 10, 50, OptionTypes.Put, expiryDate, asset, 100),
				CreateStrike(110000, 10, 47, OptionTypes.Put, expiryDate, asset, 343),
				CreateStrike(115000, 6788, 42, OptionTypes.Put, expiryDate, asset, 3454),
				CreateStrike(120000, 10, 37, OptionTypes.Put, expiryDate, asset, null),
				CreateStrike(125000, 567, 32, OptionTypes.Put, expiryDate, asset, 100),
				CreateStrike(130000, 4577, 30, OptionTypes.Put, expiryDate, asset, 55),
				CreateStrike(135000, 67835, 32, OptionTypes.Put, expiryDate, asset, 456),
				CreateStrike(140000, 13245, 35, OptionTypes.Put, expiryDate, asset, 4),
				CreateStrike(145000, 10, 37, OptionTypes.Put, expiryDate, asset, 67),
				CreateStrike(150000, 454, 39, OptionTypes.Put, expiryDate, asset, null),
				CreateStrike(155000, 10, 41, OptionTypes.Put, expiryDate, asset, 334)
			};

			var dummyProvider = new DummyProvider(securities, new[]
			{
				new Position
				{
					Security = asset,
					CurrentValue = -1,
				},

				new Position
				{
					Security = securities.First(s => s.OptionType == OptionTypes.Call),
					CurrentValue = 10,
				},

				new Position
				{
					Security = securities.First(s => s.OptionType == OptionTypes.Put),
					CurrentValue = -3,
				}
			});

			_model.MarketDataProvider = dummyProvider;
			_model.ExchangeInfoProvider = new InMemoryExchangeInfoProvider();
			_model.UnderlyingAsset = asset;

			//
			// draw test data on the pos chart

			PosChart.MarketDataProvider = dummyProvider;
			PosChart.ExchangeInfoProvider = _model.ExchangeInfoProvider;
			PosChart.SecurityProvider = dummyProvider;
			PosChart.PositionProvider = dummyProvider;

			PosChart.UnderlyingAsset = asset;
			PosChart.Options.Add(securities.First(s => s.OptionType == OptionTypes.Call));
			PosChart.Options.Add(securities.First(s => s.OptionType == OptionTypes.Put));

			PosChart.Refresh(null, currDate, expiryDate);

			//
			// draw test data on the desk

			foreach (var option in securities.Where(s => s.Type == SecurityTypes.Option))
			{
				_model.Add(option);
			}

			//
			// draw test data on the smile chart

			RefreshSmile(currDate);
		}

		private static Security CreateStrike(decimal strike, decimal oi, decimal iv, OptionTypes type, DateTime expiryDate, Security asset, decimal? lastTrade)
		{
			var s = new Security
			{
				Code = "RI {0} {1}".Put(type == OptionTypes.Call ? 'C' : 'P', strike),
				Strike = strike,
				OpenInterest = oi,
				ImpliedVolatility = iv,
				HistoricalVolatility = iv,
				OptionType = type,
				ExpiryDate = expiryDate,
				Board = ExchangeBoard.Forts,
				UnderlyingSecurityId = asset.Id,
				LastTrade = lastTrade == null ? null : new Trade { Price = lastTrade.Value },
				Volume = RandomGen.GetInt(10000),
				Type = SecurityTypes.Option
			};

			s.BestBid = new QuoteChange(s.StepPrice ?? 1m * RandomGen.GetInt(100), s.VolumeStep ?? 1m * RandomGen.GetInt(100));
			s.BestAsk = new QuoteChange(s.BestBid.Value.Price.Max(s.StepPrice ?? 1m * RandomGen.GetInt(100)), s.VolumeStep ?? 1m * RandomGen.GetInt(100));

			return s;
		}

		private void InitConnector()
		{
			// subscribe on connection successfully event
			Connector.Connected += () =>
			{
				// update gui labels
				this.GuiAsync(() => ChangeConnectStatus(true));
			};

			// subscribe on disconnection event
			Connector.Disconnected += () =>
			{
				// update gui labels
				this.GuiAsync(() => ChangeConnectStatus(false));
			};

			// subscribe on connection error event
			Connector.ConnectionError += error => this.GuiAsync(() =>
			{
				// update gui labels
				ChangeConnectStatus(false);

				MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
			});

			// fill underlying asset's list
			Connector.SecurityReceived += (sub, security) =>
			{
				if (security.Type == SecurityTypes.Future)
					this.GuiAsync(() => _assets.TryAdd(security));

				if (_model.UnderlyingAsset == security || _model.UnderlyingAsset.Id == security.UnderlyingSecurityId)
					_isDirty = true;
			};

			// subscribing on tick prices and updating asset price
			Connector.TickTradeReceived += (sub, trade) =>
			{
				if (_model.UnderlyingAsset == trade.Security || _model.UnderlyingAsset.Id == trade.Security.UnderlyingSecurityId)
					_isDirty = true;
			};

			Connector.PositionReceived += (sub, position) => this.GuiAsync(() =>
			{
				var asset = SelectedAsset;

				if (asset == null)
					return;

				var assetPos = position.Security == asset;
				var newPos = position.Security.UnderlyingSecurityId == asset.Id;

				if (!assetPos && !newPos)
					return;

				if ((PosChart.UnderlyingAsset != null && PosChart.UnderlyingAsset == position.Security) || PosChart.Options.Contains(position.Security))
					RefreshChart();
			});

			Connector.MarketDepthReceived += TryUpdateDepth;

			try
			{
				if (_settingsFile.IsConfigExists())
				{
					var ctx = new ContinueOnExceptionContext();
					ctx.Error += ex => ex.LogError();

					using (ctx.ToScope())
						Connector.LoadIfNotNull(_settingsFile.Deserialize<SettingsStorage>());
				}
			}
			catch
			{
			}
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			// set flag (connection is established or not)
			_isConnected = isConnected;

			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
			ConnectBtn.IsEnabled = true;
		}

		private void SettingsClick(object sender, RoutedEventArgs e)
		{
			if (Connector.Configure(this))
				Connector.Save().Serialize(_settingsFile);
		}

		private void Level1FieldsCtrl_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
		{
			_model.EvaluateFields.Clear();
			_model.EvaluateFields.AddRange(Level1FieldsCtrl.GetSelecteds<Level1Fields>() ?? Enumerable.Empty<Level1Fields>());
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			Connector?.Dispose();

			base.OnClosing(e);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				ConnectBtn.IsEnabled = false;

				_model.Clear();
				_model.MarketDataProvider = Connector;

				ClearSmiles();

				PosChart.UnderlyingAsset = null;
				//PosChart.Positions.Clear();
				//PosChart.AssetPosition = null;
				//PosChart.Refresh(1, 1, default(DateTimeOffset), default(DateTimeOffset));

				Portfolio.Portfolios = new PortfolioDataSource(Connector);

				PosChart.MarketDataProvider = Connector;
				PosChart.SecurityProvider = Connector;
				PosChart.PositionProvider = Connector;

				Connector.Connect();
			}
			else
				Connector.Disconnect();
		}

		private void RefreshChart()
		{
			var asset = SelectedAsset;
			var trade = asset.LastTrade;

			if (trade != null)
				PosChart.Refresh(trade.Price);
		}

		private void RefreshSmile(DateTimeOffset? time = null)
		{
			_model.Refresh(time);

			ClearSmiles();

			foreach (var row in _model.Rows)
			{
				var strike = row.Strike;

				if (strike == null)
					continue;

				if (_callBidSmile != null) TryAddSmileItem(_callBidSmile, strike.Value, row.Call?.ImpliedVolatilityBestBid);
				if (_callAskSmile != null) TryAddSmileItem(_callAskSmile, strike.Value, row.Call?.ImpliedVolatilityBestAsk);
				if (_callLastSmile != null) TryAddSmileItem(_callLastSmile, strike.Value, row.Call?.ImpliedVolatilityLastTrade);

				if (_putBidSmile != null) TryAddSmileItem(_putBidSmile, strike.Value, row.Put?.ImpliedVolatilityBestBid);
				if (_putAskSmile != null) TryAddSmileItem(_putAskSmile, strike.Value, row.Put?.ImpliedVolatilityBestAsk);
				if (_putLastSmile != null) TryAddSmileItem(_putLastSmile, strike.Value, row.Put?.ImpliedVolatilityLastTrade);
			}
		}

		private static void TryAddSmileItem(ICollection<LineData<double>> smile, decimal strike, decimal? iv)
		{
			if (iv == null)
				return;

			smile.Add(new LineData<double>
			{
				X = (double)strike,
				Y = iv.Value
			});
		}

		private void ClearSmiles()
		{
			_putBidSmile?.Clear();
			_putAskSmile?.Clear();
			_putLastSmile?.Clear();
			_callBidSmile?.Clear();
			_callAskSmile?.Clear();
			_callLastSmile?.Clear();
		}

		private readonly List<Subscription> _prevLevel1 = new();

		private void Assets_OnSelectionChanged(object sender, EditValueChangedEventArgs e)
		{
			foreach (var subscription in _prevLevel1)
			{
				Connector.UnSubscribe(subscription);
			}

			_prevLevel1.Clear();

			void Subscribe(Security security)
			{
				_prevLevel1.Add(Connector.SubscribeLevel1(security));
				_prevLevel1.Add(Connector.SubscribeMarketDepth(security));
				_prevLevel1.Add(Connector.SubscribeTrades(security));
			}

			var asset = SelectedAsset;

			_model.UnderlyingAsset = asset;

			Subscribe(asset);

			_model.Clear();
			_options.Clear();

			var options = asset.GetDerivatives(Connector);

			foreach (var security in options)
			{
				_model.Add(security);
				_options.Add(security);

				Subscribe(security);
			}

			ProcessPositions();
			RefreshSmile();
		}

		private void Portfolio_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
		{
			ProcessPositions();
		}

		private void ProcessPositions()
		{
			var portfolio = Portfolio.SelectedPortfolio;

			if (portfolio == null)
				return;

			//PosChart.Positions.AddRange(_model.Options.Select(s => Connector.GetPosition(portfolio, s)));

			//if (SelectedAsset != null)
			//	PosChart.AssetPosition = Connector.GetPosition(portfolio, SelectedAsset);

			RefreshChart();
		}

		private void Options_OnSelectionChanged(object sender, EditValueChangedEventArgs e)
		{
			var option = SelectedOption;

			if (option != null)
			{
				ImpliedVolatility.Text = option.ImpliedVolatility.To<string>();
				ImpliedVolatilityMin.EditValue = ImpliedVolatilityMax.EditValue = option.ImpliedVolatility;
			}

			Start.IsEnabled = option != null;
		}

		private void StartClick(object sender, RoutedEventArgs e)
		{
			var option = SelectedOption;

			// create DOM window
			var wnd = new QuotesWindow { Title = option.Name };
			_quotesWindows.Add(option, wnd);

			//TryUpdateDepth(Connector.GetMarketDepth(option));

			// create delta hedge strategy
			var hedge = new DeltaHedgeStrategy(_model.ExchangeInfoProvider)
			{
				Security = option.GetUnderlyingAsset(Connector),
				Portfolio = Portfolio.SelectedPortfolio,
				Connector = Connector,
			};

			// create option quoting for 20 contracts
			var quoting = new VolatilityQuotingStrategy(Sides.Buy, 20,
					new Range<decimal>((decimal?)ImpliedVolatilityMin.EditValue ?? 0, (decimal?)ImpliedVolatilityMax.EditValue ?? 100), _model.ExchangeInfoProvider)
			{
				// working size is 1 contract
				Volume = 1,
				Security = option,
				Portfolio = Portfolio.SelectedPortfolio,
				Connector = Connector,
			};

			// link quoting and hedging
			hedge.ChildStrategies.Add(quoting);

			// start hedging
			hedge.Start();

			wnd.Closed += (s1, e1) =>
			{
				// force close all strategies while the DOM was closed
				hedge.Stop();
			};

			// show DOM
			wnd.Show();
		}

		private void TryUpdateDepth(Subscription subscription, MarketDepth depth)
		{
			if (!_quotesWindows.TryGetValue(depth.Security, out var wnd))
				return;

			wnd.Update(depth.ImpliedVolatility(Connector, Connector, _model.ExchangeInfoProvider, depth.LastChangeTime));
		}
	}
}