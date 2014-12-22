namespace SampleAlfaCandles
{
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.AlfaDirect;
	using StockSharp.Logging;
	using StockSharp.Localization;

	partial class MainWindow
	{
		private AlfaTrader _trader;
		private readonly ObservableCollection<Security> _securitiesSource = new ObservableCollection<Security>();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();

			HistoryInterval.ItemsSource = AlfaTimeFrames.AllTimeFrames;

			HistoryInterval.SelectedIndex = 2;
			From.Value = DateTime.Today - TimeSpan.FromDays(7);
			To.Value = DateTime.Now;

			Security.ItemsSource = _securitiesSource;

			_logManager.Listeners.Add(new FileLogListener());
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			// создаем подключение
			_trader = new AlfaTrader { LogLevel = LogLevels.Debug };

			_logManager.Sources.Add(_trader);

			// подписываемся на ошибку обработки данных (транзакций и маркет)
			_trader.ProcessDataError += error =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// подписываемся на ошибку подписки маркет-данных
			_trader.MarketDataSubscriptionFailed += (security, type, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));
			
			_trader.NewSecurities += securities =>
			{
				this.GuiAsync(() => _securitiesSource.AddRange(securities));

				// начинаем получать текущие сделки (для построения свечек в реальном времени)

				// альфа не выдержит нагрузки получения сделок по всем инструментам
				// нужно подписываться только на те, которые необходимы
				// securities.ForEach(_trader.RegisterTrades);
			};

			_trader.NewPortfolios += portfolios => portfolios.ForEach(_trader.RegisterPortfolio);

			_trader.Connected += () =>
			{
				_trader.StartExport();
				this.GuiAsync(() => ConnectBtn.IsEnabled = false);
			};

			_trader.Connect();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (_trader != null)
			{
				_logManager.Sources.Remove(_trader);
				_trader.Dispose();
			}

			base.OnClosing(e);
		}

		private Security SelectedSecurity
		{
			get { return (Security)Security.SelectedValue; }
		}

		private void ShowChartClick(object sender, RoutedEventArgs e)
		{
			var security = SelectedSecurity;
			_trader.RegisterTrades(security);

			var timeFrame = (TimeSpan)HistoryInterval.SelectedItem;

			var from = From.Value.Value;
			var to = To.Value.Value;

			if (from > to)
			{
				return;
			}

			var wnd = new ChartWindow
			{
				Title = "{0}, {1}, {2} - {3}".Put(security.Code, HistoryInterval.SelectedItem, from, to)
			};

			wnd.Show();

			var series = new CandleSeries
			{
				Security = security,
				Arg = timeFrame,
				CandleType = typeof(TimeFrameCandle)
			};

			_trader.NewCandles += (candleSeries, candles) =>
			{
				_trader.AddInfoLog("newcandles({0}):\n{1}", candles.Count(), candles.Select(c => c.ToString()).Join("\n"));

				if (candleSeries == series)
					wnd.DrawCandles(candles);
			};

			_trader.SubscribeCandles(series, from, to);
		}

		private void SecuritySelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ShowChart.IsEnabled = SelectedSecurity != null;
		}
	}
}