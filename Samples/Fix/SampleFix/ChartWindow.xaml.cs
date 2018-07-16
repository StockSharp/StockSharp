namespace SampleFix
{
	using System;
	using System.ComponentModel;
	using System.Windows.Media;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Fix;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;

	partial class ChartWindow
	{
		private readonly FixTrader _trader;
		private readonly CandleSeries _candleSeries;
		private readonly ChartCandleElement _candleElem;
		private readonly long _transactionId;

		public ChartWindow(CandleSeries candleSeries, DateTimeOffset? from = null, DateTimeOffset? to = null)
		{
			InitializeComponent();

			_candleSeries = candleSeries ?? throw new ArgumentNullException(nameof(candleSeries));
			_trader = MainWindow.Instance.Trader;

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

			_trader.NewMessage += ProcessNewMessage;

			_trader.SubscribeMarketData(candleSeries.Security, new MarketDataMessage
			{
				TransactionId = _transactionId = _trader.TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.CandleTimeFrame,
				//SecurityId = GetSecurityId(series.Security),
				Arg = candleSeries.Arg,
				IsSubscribe = true,
				From = from,
				To = to,
			}.ValidateBounds());
		}

		private void ProcessNewMessage(Message message)
		{
			var candleMsg = message as CandleMessage;

			if (candleMsg?.OriginalTransactionId != _transactionId)
				return;

			Chart.Draw(_candleElem, candleMsg.ToCandle(_candleSeries));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_trader.NewMessage -= ProcessNewMessage;
			base.OnClosing(e);
		}
	}
}