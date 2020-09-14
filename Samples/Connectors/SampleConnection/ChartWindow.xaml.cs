namespace SampleConnection
{
	using System;
	using System.ComponentModel;
	using System.Windows.Media;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;

	partial class ChartWindow
	{
		private readonly Connector _connector;
		private readonly ChartCandleElement _candleElem;
		private readonly Subscription _subscription;

		public ChartWindow(MarketDataMessage mdMsg)
		{
			if (mdMsg is null)
				throw new ArgumentNullException(nameof(mdMsg));

			InitializeComponent();

			Title = mdMsg.SecurityId + " - " + mdMsg.DataType2.ToString();

			_connector = MainWindow.Instance.MainPanel.Connector;

			Chart.ChartTheme = ChartThemes.ExpressionDark;

			var area = new ChartArea();
			Chart.Areas.Add(area);

			_candleElem = new ChartCandleElement
			{
				AntiAliasing = false,
				UpFillColor = Colors.White,
				UpBorderColor = Colors.Black,
				DownFillColor = Colors.Black,
				DownBorderColor = Colors.Black,
			};

			area.Elements.Add(_candleElem);

			_connector.CandleReceived += OnCandleReceived;
			_subscription = _connector.SubscribeMarketData(mdMsg);
		}

		public bool SeriesInactive { get; set; }

		private void OnCandleReceived(Subscription subscription, Candle candle)
		{
			if (subscription != _subscription)
				return;

			Chart.Draw(_candleElem, candle);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_connector.CandleReceived -= OnCandleReceived;

			if (!SeriesInactive && _subscription.State.IsActive())
				_connector.UnSubscribe(_subscription);

			base.OnClosing(e);
		}
	}
}
