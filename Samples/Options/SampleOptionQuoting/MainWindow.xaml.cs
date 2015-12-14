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
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private class FakeConnector : Connector, IMarketDataProvider
		{
			private readonly IEnumerable<Security> _securities;

			public FakeConnector(IEnumerable<Security> securities)
			{
				_securities = securities;
			}

			public override IEnumerable<Security> Securities
			{
				get { return _securities; }
			}

			public override DateTimeOffset CurrentTime
			{
				get { return DateTime.Now; }
			}

			object IMarketDataProvider.GetSecurityValue(Security security, Level1Fields field)
			{
				switch (field)
				{
					case Level1Fields.ImpliedVolatility:
						return security.ImpliedVolatility;
				}

				return null;
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

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			QuikPath.Folder = QuikTerminal.GetDefaultPath();

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
			// добавляем тестовый данные для отображения графика

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

			Instance = this;
		}

		public static MainWindow Instance { get; private set; }

		protected override void OnClosing(CancelEventArgs e)
		{
			if (Connector != null)
			{
				Connector.Dispose();
			}

			base.OnClosing(e);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			var isDde = IsDde.IsChecked == true;

			if (isDde && QuikPath.Folder.IsEmpty())
			{
				MessageBox.Show(this, LocalizedStrings.Str2969);
				return;
			}

			if (Connector != null && !(Connector is FakeConnector))
				return;

			PosChart.Positions.Clear();
			PosChart.AssetPosition = null;
			PosChart.Refresh(1, 1, default(DateTimeOffset), default(DateTimeOffset));

			// создаем подключение
			Connector = new QuikTrader(QuikPath.Folder)
			{
				IsDde = isDde
			};

			if (isDde)
			{
				// изменяем метаданные так, чтобы начали обрабатывать дополнительные колонки опционов
				var columns = ((QuikTrader)Connector).SecuritiesTable.Columns;
				columns.Add(DdeSecurityColumns.Strike);
				columns.Add(DdeSecurityColumns.ImpliedVolatility);
				columns.Add(DdeSecurityColumns.UnderlyingSecurity);
				columns.Add(DdeSecurityColumns.TheorPrice);
				columns.Add(DdeSecurityColumns.OptionType);
				columns.Add(DdeSecurityColumns.ExpiryDate);
			}

			//_trader = new PlazaTrader { IsCGate = true };
			//_trader.Tables.Add(_trader.TableRegistry.Volatility);

			Portfolio.Portfolios = new PortfolioDataSource(Connector);

			PosChart.MarketDataProvider = Connector;
			PosChart.SecurityProvider = Connector;

			// добавляем базовые активы в список
			Connector.NewSecurities += securities =>
				_assets.AddRange(securities.Where(s => s.Type == SecurityTypes.Future));

			Connector.SecuritiesChanged += securities =>
			{
				if ((PosChart.AssetPosition != null && securities.Contains(PosChart.AssetPosition.Security)) || PosChart.Positions.Cache.Select(p => p.Security).Intersect(securities).Any())
					_isDirty = true;
			};

			// подписываемся на событие новых сделок чтобы обновить текущую цену фьючерса
			Connector.NewTrades += trades =>
			{
				var assetPos = PosChart.AssetPosition;
				if (assetPos != null && trades.Any(t => t.Security == assetPos.Security))
					_isDirty = true;
			};

			Connector.NewPositions += positions => this.GuiAsync(() =>
			{
				var asset = SelectedAsset;

				if (asset == null)
					return;

				var assetPos = positions.FirstOrDefault(p => p.Security == asset);
				var newPos = positions.Where(p => p.Security.UnderlyingSecurityId == asset.Id).ToArray();

				if (assetPos == null && newPos.Length == 0)
					return;

				if (assetPos != null)
					PosChart.AssetPosition = assetPos;

				if (newPos.Length > 0)
					PosChart.Positions.AddRange(newPos);

				RefreshChart();
			});

			Connector.PositionsChanged += positions => this.GuiAsync(() =>
			{
				if ((PosChart.AssetPosition != null && positions.Contains(PosChart.AssetPosition)) || positions.Intersect(PosChart.Positions.Cache).Any())
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

		private Security SelectedOption
		{
			get { return (Security)Options.SelectedItem; }
		}

		private Security SelectedAsset
		{
			get { return (Security)Assets.SelectedItem; }
		}

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

			// создаем окно для отображения стакана
			var wnd = new QuotesWindow { Title = option.Name };
			wnd.Init(option);

			// создаем дельта-хеджирование, передав в него опционные стратегии для отслеживания их позиции
			var hedge = new DeltaHedgeStrategy
			{
				Security = option.GetUnderlyingAsset(Connector),
				Portfolio = Portfolio.SelectedPortfolio,
				Connector = Connector,
			};

			// создаем котирование на покупку 20-ти контрактов
			var quoting = new VolatilityQuotingStrategy(Sides.Buy, 20,
					new Range<decimal>(ImpliedVolatilityMin.Value ?? 0, ImpliedVolatilityMax.Value ?? 100))
			{
				// указываем, что котирование работает с объемом в 1 контракт
				Volume = 1,
				Security = option,
				Portfolio = Portfolio.SelectedPortfolio,
				Connector = Connector,
			};

			// добавляем стратегию, которую необходимо хеджировать
			hedge.ChildStrategies.Add(quoting);

			// запускаем дельта-хеджирование
			hedge.Start();

			wnd.Closed += (s1, e1) =>
			{
				// принудительная остановка стратегии при закрытие окна со стаканом
				hedge.Stop();
			};

			// показываем окно
			wnd.Show();
		}
	}
}