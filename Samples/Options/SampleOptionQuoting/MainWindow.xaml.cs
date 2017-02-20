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
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;
	using System.Windows.Threading;

	using DevExpress.Xpf.Editors;

	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Derivatives;
	using StockSharp.Algo.Strategies.Derivatives;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;

	public partial class MainWindow
	{
		private class DummyProvider : CollectionSecurityProvider, IMarketDataProvider
		{
			public DummyProvider(IEnumerable<Security> securities)
				: base(securities)
			{
			}

			event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> IMarketDataProvider.ValuesChanged
			{
				add { }
				remove { }
			}

			MarketDepth IMarketDataProvider.GetMarketDepth(Security security)
			{
				return null;
			}

			object IMarketDataProvider.GetSecurityValue(Security security, Level1Fields field)
			{
				switch (field)
				{
					case Level1Fields.OpenInterest:
						return security.OpenInterest;

					case Level1Fields.ImpliedVolatility:
						return security.ImpliedVolatility;

					case Level1Fields.HistoricalVolatility:
						return security.HistoricalVolatility;

					case Level1Fields.Volume:
						return security.Volume;

					case Level1Fields.LastTradePrice:
						return security.LastTrade?.Price;

					case Level1Fields.LastTradeVolume:
						return security.LastTrade?.Volume;

					case Level1Fields.BestBidPrice:
						return security.BestBid?.Price;

					case Level1Fields.BestBidVolume:
						return security.BestBid?.Volume;

					case Level1Fields.BestAskPrice:
						return security.BestAsk?.Price;

					case Level1Fields.BestAskVolume:
						return security.BestAsk?.Volume;
				}

				return null;
			}

			IEnumerable<Level1Fields> IMarketDataProvider.GetLevel1Fields(Security security)
			{
				return new[]
				{
					Level1Fields.OpenInterest,
					Level1Fields.ImpliedVolatility,
					Level1Fields.HistoricalVolatility,
					Level1Fields.Volume,
					Level1Fields.LastTradePrice,
					Level1Fields.LastTradeVolume,
					Level1Fields.BestBidPrice,
					Level1Fields.BestAskPrice,
					Level1Fields.BestBidVolume,
					Level1Fields.BestAskVolume
				};
			}
		}

		private const string _settingsFile = "connection.xml";

		public readonly Connector Connector = new Connector();

		private readonly ThreadSafeObservableCollection<Security> _assets;
		private readonly ThreadSafeObservableCollection<Security> _options;
		private readonly OptionDeskModel _model;
		private readonly ICollection<LineData<double>> _putBidSmile;
		private readonly ICollection<LineData<double>> _putAskSmile;
		private readonly ICollection<LineData<double>> _putLastSmile;
		private readonly ICollection<LineData<double>> _callBidSmile;
		private readonly ICollection<LineData<double>> _callAskSmile;
		private readonly ICollection<LineData<double>> _callLastSmile;

		private bool _isDirty;
		private bool _isConnected;

		private Security SelectedOption => (Security)Options.SelectedItem;

		private Security SelectedAsset => (Security)Assets.SelectedItem;

		public static MainWindow Instance { get; private set; }

		public MainWindow()
		{
			InitializeComponent();

			var assetsSource = new ObservableCollectionEx<Security>();
			var optionsSource = new ObservableCollectionEx<Security>();

			Options.ItemsSource = optionsSource;
			Assets.ItemsSource = assetsSource;

			_assets = new ThreadSafeObservableCollection<Security>(assetsSource);
			_options = new ThreadSafeObservableCollection<Security>(optionsSource);

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

			Level1FieldsCtrl.ItemsSource = new[]
			{
				Level1Fields.ImpliedVolatility,
				Level1Fields.Delta,
				Level1Fields.Gamma,
				Level1Fields.Vega,
				Level1Fields.Theta,
				Level1Fields.Rho,
			}.ToDictionary(t => t, t => t.GetDisplayName());
			Level1FieldsCtrl.SelectedFields = _model.EvaluateFildes.ToArray();

			Instance = this;

			DrawTestData();
			InitConnector();
		}

		private void DrawTestData()
		{
			//
			// draw test data on the pos chart

			var asset = new Security
			{
				Id = "RIM4@FORTS"
			};

			var dummyProvider = new DummyProvider(new[] {asset});

			PosChart.AssetPosition = new Position
			{
				Security = asset,
				CurrentValue = -1,
			};

			PosChart.MarketDataProvider = dummyProvider;
			PosChart.SecurityProvider = dummyProvider;

			var expDate = new DateTime(2014, 6, 14);

			PosChart.Positions.Add(new Position
			{
				Security = new Security
				{
					Code = "RI C 110000",
					Strike = 110000,
					ImpliedVolatility = 45,
					OptionType = OptionTypes.Call,
					ExpiryDate = expDate,
					Board = ExchangeBoard.Forts,
					UnderlyingSecurityId = asset.Id
				},
				CurrentValue = 10,
			});
			PosChart.Positions.Add(new Position
			{
				Security = new Security
				{
					Code = "RI P 95000",
					Strike = 95000,
					ImpliedVolatility = 30,
					OptionType = OptionTypes.Put,
					ExpiryDate = expDate,
					Board = ExchangeBoard.Forts,
					UnderlyingSecurityId = asset.Id
				},
				CurrentValue = -3,
			});

			PosChart.Refresh(100000, 10, new DateTime(2014, 5, 5), expDate);

			//
			// draw test data on the desk

			var expiryDate = new DateTime(2014, 09, 15);

			_model.MarketDataProvider = dummyProvider;
			_model.UnderlyingAsset = asset;

			_model.Add(CreateStrike(05000, 10, 60, OptionTypes.Call, expiryDate, asset, 100));
			_model.Add(CreateStrike(10000, 10, 53, OptionTypes.Call, expiryDate, asset, 343));
			_model.Add(CreateStrike(15000, 10, 47, OptionTypes.Call, expiryDate, asset, 3454));
			_model.Add(CreateStrike(20000, 78, 42, OptionTypes.Call, expiryDate, asset, null));
			_model.Add(CreateStrike(25000, 32, 35, OptionTypes.Call, expiryDate, asset, 100));
			_model.Add(CreateStrike(30000, 3245, 32, OptionTypes.Call, expiryDate, asset, 55));
			_model.Add(CreateStrike(35000, 3454, 37, OptionTypes.Call, expiryDate, asset, 456));
			_model.Add(CreateStrike(40000, 34, 45, OptionTypes.Call, expiryDate, asset, 4));
			_model.Add(CreateStrike(45000, 3566, 51, OptionTypes.Call, expiryDate, asset, 67));
			_model.Add(CreateStrike(50000, 454, 57, OptionTypes.Call, expiryDate, asset, null));
			_model.Add(CreateStrike(55000, 10, 59, OptionTypes.Call, expiryDate, asset, 334));

			_model.Add(CreateStrike(05000, 10, 50, OptionTypes.Put, expiryDate, asset, 100));
			_model.Add(CreateStrike(10000, 10, 47, OptionTypes.Put, expiryDate, asset, 343));
			_model.Add(CreateStrike(15000, 6788, 42, OptionTypes.Put, expiryDate, asset, 3454));
			_model.Add(CreateStrike(20000, 10, 37, OptionTypes.Put, expiryDate, asset, null));
			_model.Add(CreateStrike(25000, 567, 32, OptionTypes.Put, expiryDate, asset, 100));
			_model.Add(CreateStrike(30000, 4577, 30, OptionTypes.Put, expiryDate, asset, 55));
			_model.Add(CreateStrike(35000, 67835, 32, OptionTypes.Put, expiryDate, asset, 456));
			_model.Add(CreateStrike(40000, 13245, 35, OptionTypes.Put, expiryDate, asset, 4));
			_model.Add(CreateStrike(45000, 10, 37, OptionTypes.Put, expiryDate, asset, 67));
			_model.Add(CreateStrike(50000, 454, 39, OptionTypes.Put, expiryDate, asset, null));
			_model.Add(CreateStrike(55000, 10, 41, OptionTypes.Put, expiryDate, asset, 334));

			//
			// draw test data on the smile chart

			RefreshSmile(new DateTime(2014, 08, 15));
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

			s.BestBid = new Quote(s, s.StepPrice ?? 1m * RandomGen.GetInt(100), s.VolumeStep ?? 1m * RandomGen.GetInt(100), Sides.Buy);
			s.BestAsk = new Quote(s, s.BestBid.Price.Max(s.StepPrice ?? 1m * RandomGen.GetInt(100)), s.VolumeStep ?? 1m * RandomGen.GetInt(100), Sides.Sell);

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
			Connector.NewSecurity += security =>
			{
				if (security.Type == SecurityTypes.Future)
					_assets.Add(security);
			};

			Connector.SecurityChanged += security =>
			{
				if (_model.UnderlyingAsset == security || _model.UnderlyingAsset.Id == security.UnderlyingSecurityId)
					_isDirty = true;
			};

			// subscribing on tick prices and updating asset price
			Connector.NewTrade += trade =>
			{
				if (_model.UnderlyingAsset == trade.Security || _model.UnderlyingAsset.Id == trade.Security.UnderlyingSecurityId)
					_isDirty = true;
			};

			Connector.NewPosition += position => this.GuiAsync(() =>
			{
				var asset = SelectedAsset;

				if (asset == null)
					return;

				var assetPos = position.Security == asset;
				var newPos = position.Security.UnderlyingSecurityId == asset.Id;

				if (!assetPos && !newPos)
					return;

				if (assetPos)
					PosChart.AssetPosition = position;

				if (newPos)
					PosChart.Positions.Add(position);

				RefreshChart();
			});

			Connector.PositionChanged += position => this.GuiAsync(() =>
			{
				if ((PosChart.AssetPosition != null && PosChart.AssetPosition == position) || PosChart.Positions.Cache.Contains(position))
					RefreshChart();
			});

			try
			{
				if (File.Exists(_settingsFile))
					Connector.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));
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
				new XmlSerializer<SettingsStorage>().Serialize(Connector.Save(), _settingsFile);
		}

		private void Level1FieldsCtrl_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
		{
			_model.EvaluateFildes.Clear();
			_model.EvaluateFildes.AddRange(Level1FieldsCtrl.SelectedFields);
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

				PosChart.Positions.Clear();
				PosChart.AssetPosition = null;
				PosChart.Refresh(1, 1, default(DateTimeOffset), default(DateTimeOffset));

				Portfolio.Portfolios = new PortfolioDataSource(Connector);

				PosChart.MarketDataProvider = Connector;
				PosChart.SecurityProvider = Connector;

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
				PosChart.Refresh(trade.Price, asset.PriceStep ?? 1m, TimeHelper.NowWithOffset, asset.ExpiryDate ?? DateTimeOffset.Now.Date + TimeSpan.FromDays(1));
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

				TryAddSmileItem(_callBidSmile, strike.Value, row.Call?.ImpliedVolatilityBestBid);
				TryAddSmileItem(_callAskSmile, strike.Value, row.Call?.ImpliedVolatilityBestAsk);
				TryAddSmileItem(_callLastSmile, strike.Value, row.Call?.ImpliedVolatilityLastTrade);

				TryAddSmileItem(_putBidSmile, strike.Value, row.Put?.ImpliedVolatilityBestBid);
				TryAddSmileItem(_putAskSmile, strike.Value, row.Put?.ImpliedVolatilityBestAsk);
				TryAddSmileItem(_putLastSmile, strike.Value, row.Put?.ImpliedVolatilityLastTrade);
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
			_putBidSmile.Clear();
			_putAskSmile.Clear();
			_putLastSmile.Clear();
			_callBidSmile.Clear();
			_callAskSmile.Clear();
			_callLastSmile.Clear();
		}

		private void Assets_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var asset = SelectedAsset;

			_model.UnderlyingAsset = asset;

			_model.Clear();
			_options.Clear();

			var options = asset.GetDerivatives(Connector);

			foreach (var security in options)
			{
				_model.Add(security);
				_options.Add(security);
			}

			ProcessPositions();
		}

		private void Portfolio_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ProcessPositions();
		}

		private void ProcessPositions()
		{
			var portfolio = Portfolio.SelectedPortfolio;

			if (portfolio == null)
				return;

			PosChart.Positions.AddRange(_model.Options.Select(s => Connector.GetPosition(portfolio, s)));

			if (SelectedAsset != null)
				PosChart.AssetPosition = Connector.GetPosition(portfolio, SelectedAsset);

			RefreshChart();
		}

		private void Options_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var option = SelectedOption;

			if (option != null)
			{
				ImpliedVolatility.Text = option.ImpliedVolatility.To<string>();
				ImpliedVolatilityMin.Value = ImpliedVolatilityMax.Value = option.ImpliedVolatility;
			}

			Start.IsEnabled = option != null;
		}

		private void StartClick(object sender, RoutedEventArgs e)
		{
			var option = SelectedOption;

			// create DOM window
			var wnd = new QuotesWindow { Title = option.Name };
			wnd.Init(option);

			// create delta hedge strategy
			var hedge = new DeltaHedgeStrategy
			{
				Security = option.GetUnderlyingAsset(Connector),
				Portfolio = Portfolio.SelectedPortfolio,
				Connector = Connector,
			};

			// create option quoting for 20 contracts
			var quoting = new VolatilityQuotingStrategy(Sides.Buy, 20,
					new Range<decimal>(ImpliedVolatilityMin.Value ?? 0, ImpliedVolatilityMax.Value ?? 100))
			{
				// working size is 1 contract
				Volume = 1,
				Security = option,
				Portfolio = Portfolio.SelectedPortfolio,
				Connector = Connector,
			};

			// link quoting and hending
			hedge.ChildStrategies.Add(quoting);

			// start henging
			hedge.Start();

			wnd.Closed += (s1, e1) =>
			{
				// force close all strategies while the DOM was closed
				hedge.Stop();
			};

			// show DOM
			wnd.Show();
		}
	}
}