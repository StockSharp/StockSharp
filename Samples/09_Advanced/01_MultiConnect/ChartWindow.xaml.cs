namespace StockSharp.Samples.Advanced.MultiConnect;

using System;
using System.ComponentModel;
using System.Drawing;

using StockSharp.BusinessEntities;
using StockSharp.Algo;
using StockSharp.Messages;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;

partial class ChartWindow
{
	private readonly Connector _connector;
	private readonly IChartCandleElement _candleElem;
	private readonly Subscription _subscription;

	public ChartWindow(MarketDataMessage mdMsg)
	{
		if (mdMsg is null)
			throw new ArgumentNullException(nameof(mdMsg));

		InitializeComponent();

		Title = mdMsg.SecurityId + " - " + mdMsg.DataType2.ToString();

		_connector = MainWindow.Instance.MainPanel.Connector;

		Chart.ChartTheme = ChartThemes.ExpressionDark;

		var area = Chart.AddArea();

		_candleElem = Chart.CreateCandleElement();
		_candleElem.AntiAliasing = false;
		_candleElem.UpFillColor = Color.White;
		_candleElem.UpBorderColor = Color.Black;
		_candleElem.DownFillColor = Color.Black;
		_candleElem.DownBorderColor = Color.Black;

		area.Elements.Add(_candleElem);

		_connector.CandleReceived += OnCandleReceived;
		_subscription = new(mdMsg);
		_connector.Subscribe(_subscription);
	}

	public bool SeriesInactive { get; set; }

	private void OnCandleReceived(Subscription subscription, ICandleMessage candle)
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
