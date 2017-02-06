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
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;
	using System.Windows.Threading;

	using Ecng.ComponentModel;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	//using StockSharp.Plaza;
	using StockSharp.Quik;
	using StockSharp.Algo.Derivatives;
	using StockSharp.Algo.Strategies.Derivatives;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;

	public partial class MainWindow
	{
		private class FakeConnector : Connector, IMarketDataProvider
		{
			public FakeConnector(IEnumerable<Security> securities)
			{
				Securities = securities;
			}

			public override IEnumerable<Security> Securities { get; }

			public override DateTimeOffset CurrentTime => DateTime.Now;

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

		private readonly ThreadSafeObservableCollection<Security> _options;
		private readonly ThreadSafeObservableCollection<Security> _assets;
		//private QuikTrader _trader;
		//private PlazaTrader _trader;
		private bool _isDirty;

		public IConnector Connector;

		public MainWindow()
		{
			InitializeComponent();

			var assetsSource = new ObservableCollectionEx<Security>();
			var optionsSource = new ObservableCollectionEx<Security>();

			Options.ItemsSource = optionsSource;
			Assets.ItemsSource = assetsSource;

			_assets = new ThreadSafeObservableCollection<Security>(assetsSource);
			_options = new ThreadSafeObservableCollection<Security>(optionsSource);

			var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
			timer.Tick += (sender, args) =>
			{
				if (!_isDirty)
					return;

				_isDirty = false;
				RefreshChart();
			};
			timer.Start();

			//
			// draw test data on the pos chart

			var asset = new Security { Id = "RIM4@FORTS" };

			Connector = new FakeConnector(new[] { asset });

			PosChart.AssetPosition = new Position
			{
				Security = asset,
				CurrentValue = -1,
			};

			PosChart.MarketDataProvider = Connector;
			PosChart.SecurityProvider = Connector;

			var expDate = new DateTime(2014, 6, 14);

			PosChart.Positions.Add(new Position
			{
				Security = new Security { Code = "RI C 110000", Strike = 110000, ImpliedVolatility = 45, OptionType = OptionTypes.Call, ExpiryDate = expDate, Board = ExchangeBoard.Forts, UnderlyingSecurityId = asset.Id },
				CurrentValue = 10,
			});
			PosChart.Positions.Add(new Position
			{
				Security = new Security { Code = "RI P 95000", Strike = 95000, ImpliedVolatility = 30, OptionType = OptionTypes.Put, ExpiryDate = expDate, Board = ExchangeBoard.Forts, UnderlyingSecurityId = asset.Id },
				CurrentValue = -3,
			});

			PosChart.Refresh(100000, 10, new DateTime(2014, 5, 5), expDate);

			//
			// draw test data on the desk

			var expiryDate = new DateTime(2014, 09, 15);

			var model = new OptionDeskModel
			{
				MarketDataProvider = Connector,
				UnderlyingAsset = asset,
			};

			Desk.Model = model;

			model.Add(CreateStrike(05000, 10, 60, OptionTypes.Call, expiryDate, asset, 100));
			model.Add(CreateStrike(10000, 10, 53, OptionTypes.Call, expiryDate, asset, 343));
			model.Add(CreateStrike(15000, 10, 47, OptionTypes.Call, expiryDate, asset, 3454));
			model.Add(CreateStrike(20000, 78, 42, OptionTypes.Call, expiryDate, asset, null));
			model.Add(CreateStrike(25000, 32, 35, OptionTypes.Call, expiryDate, asset, 100));
			model.Add(CreateStrike(30000, 3245, 32, OptionTypes.Call, expiryDate, asset, 55));
			model.Add(CreateStrike(35000, 3454, 37, OptionTypes.Call, expiryDate, asset, 456));
			model.Add(CreateStrike(40000, 34, 45, OptionTypes.Call, expiryDate, asset, 4));
			model.Add(CreateStrike(45000, 3566, 51, OptionTypes.Call, expiryDate, asset, 67));
			model.Add(CreateStrike(50000, 454, 57, OptionTypes.Call, expiryDate, asset, null));
			model.Add(CreateStrike(55000, 10, 59, OptionTypes.Call, expiryDate, asset, 334));

			model.Add(CreateStrike(05000, 10, 50, OptionTypes.Put, expiryDate, asset, 100));
			model.Add(CreateStrike(10000, 10, 47, OptionTypes.Put, expiryDate, asset, 343));
			model.Add(CreateStrike(15000, 6788, 42, OptionTypes.Put, expiryDate, asset, 3454));
			model.Add(CreateStrike(20000, 10, 37, OptionTypes.Put, expiryDate, asset, null));
			model.Add(CreateStrike(25000, 567, 32, OptionTypes.Put, expiryDate, asset, 100));
			model.Add(CreateStrike(30000, 4577, 30, OptionTypes.Put, expiryDate, asset, 55));
			model.Add(CreateStrike(35000, 67835, 32, OptionTypes.Put, expiryDate, asset, 456));
			model.Add(CreateStrike(40000, 13245, 35, OptionTypes.Put, expiryDate, asset, 4));
			model.Add(CreateStrike(45000, 10, 37, OptionTypes.Put, expiryDate, asset, 67));
			model.Add(CreateStrike(50000, 454, 39, OptionTypes.Put, expiryDate, asset, null));
			model.Add(CreateStrike(55000, 10, 41, OptionTypes.Put, expiryDate, asset, 334));

			model.Refresh(new DateTime(2014, 08, 15));

			//
			// draw test data on the smile chart

			var puts = SmileChart.CreateSmile("RIM4 (Put)", Colors.DarkRed);
			var calls = SmileChart.CreateSmile("RIM4 (Call)", Colors.DarkGreen);

			foreach (var option in model.Options)
			{
				if (option.Strike == null || option.ImpliedVolatility == null)
					continue;

				(option.OptionType == OptionTypes.Put ? puts : calls).Add(new LineData<double> { X = (double)option.Strike.Value, Y = option.ImpliedVolatility.Value });
			}

			Instance = this;
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

		public static MainWindow Instance { get; private set; }

		protected override void OnClosing(CancelEventArgs e)
		{
			Connector?.Dispose();

			base.OnClosing(e);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (Connector != null && !(Connector is FakeConnector))
				return;

			PosChart.Positions.Clear();
			PosChart.AssetPosition = null;
			PosChart.Refresh(1, 1, default(DateTimeOffset), default(DateTimeOffset));

			// create connection
			Connector = new QuikTrader();

			//_trader = new PlazaTrader { IsCGate = true };
			//_trader.Tables.Add(_trader.TableRegistry.Volatility);

			Portfolio.Portfolios = new PortfolioDataSource(Connector);

			PosChart.MarketDataProvider = Connector;
			PosChart.SecurityProvider = Connector;

			// fill underlying asset's list
			Connector.NewSecurity += security =>
			{
				if (security.Type == SecurityTypes.Future)
					_assets.Add(security);
			};

			Connector.SecurityChanged += security =>
			{
				if ((PosChart.AssetPosition != null && PosChart.AssetPosition.Security == security) || PosChart.Positions.Cache.Select(p => p.Security).Contains(security))
					_isDirty = true;
			};

			// subscribing on tick prices and updating asset price
			Connector.NewTrade += trade =>
			{
				var assetPos = PosChart.AssetPosition;
				if (assetPos != null && trade.Security == assetPos.Security)
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

			Connector.Connect();
		}

		private void RefreshChart()
		{
			var asset = SelectedAsset;
			var trade = asset.LastTrade;

			if (trade != null)
				PosChart.Refresh(trade.Price, asset.PriceStep ?? 1m, TimeHelper.NowWithOffset, asset.ExpiryDate ?? DateTimeOffset.Now.Date + TimeSpan.FromDays(1));
		}

		private Security SelectedOption => (Security)Options.SelectedItem;

		private Security SelectedAsset => (Security)Assets.SelectedItem;

		private void Assets_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var asset = SelectedAsset;

			_options.Clear();
			_options.AddRange(asset.GetDerivatives(Connector));

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

			PosChart.Positions.AddRange(_options.Select(s => Connector.GetPosition(portfolio, s)));

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