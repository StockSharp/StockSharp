namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Logging;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Charting.IndicatorPainters;
	using StockSharp.Localization;

	public partial class ChartPane : IPane
	{
		private readonly Dictionary<ChartIndicatorElement, IIndicator> _indicators = new Dictionary<ChartIndicatorElement, IIndicator>();
		private readonly HashSet<IChartElement> _elements = new HashSet<IChartElement>();
		private readonly SyncObject _syncObject = new SyncObject();
		private readonly ResettableTimer _drawTimer;

		private ChartCandleElement _candlesElem;
		private ChartIndicatorElement _volumeElem;
		private IEnumerable<Candle> _candles;
		private bool _isDisposed;
		private bool _isStopped;
		private int _candlesCount;

		public ChartPane()
		{
			InitializeComponent();

			_drawTimer = new ResettableTimer(TimeSpan.FromSeconds(2));
			_drawTimer.Elapsed += DrawTimerOnElapsed;

			ChartPanel.MinimumRange = 200;
			ChartPanel.IsInteracted = true;
			ChartPanel.IndicatorTypes.AddRange(AppConfig.Instance.Indicators);

			ChartPanel.SubscribeCandleElement += OnChartPanelSubscribeCandleElement;
			ChartPanel.SubscribeIndicatorElement += OnChartPanelSubscribeIndicatorElement;
		}

		private void OnChartPanelSubscribeCandleElement(ChartCandleElement element, CandleSeries candleSeries)
		{
			_elements.Add(element);
			_drawTimer.Reset();
		}

		private void OnChartPanelSubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries candleSeries, IIndicator indicator)
		{
			_elements.Add(element);
			_drawTimer.Reset();
		}

		private void DrawTimerOnElapsed()
		{
			try
			{
				if (_isDisposed)
					return;

				_isStopped = false;

				var elements = _elements.CopyAndClear();

				var candleElement = elements.OfType<ChartCandleElement>().FirstOrDefault();

				if (candleElement == null)
				{
					foreach (var indicatorElement in elements.OfType<ChartIndicatorElement>())
						ProcessIndicator(indicatorElement, _candles);
				}
				else
					ProcessCandleElement(_candles);

				GuiDispatcher.GlobalDispatcher.AddAction(() => CancelButton.Visibility = Visibility.Collapsed);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		private void ProcessCandleElement(IEnumerable<Candle> candles)
		{
			foreach (var batch in candles.Batch(50))
			{
				if (_isStopped || _isDisposed)
					break;

				var values = new List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>>();

				foreach (var c in batch)
				{
					var candle = c;

					var pair = new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(candle.OpenTime, new Dictionary<IChartElement, object>());

					_candlesCount++;

					// ограничиваем кол-во передаваемых свечек, чтобы не фризился интерфейс
					if (_candlesCount % 100 == 0)
						System.Threading.Thread.Sleep(200);

					foreach (var el in ChartPanel.Elements)
					{
						el.DoIf<IChartElement, ChartCandleElement>(e => pair.Second.Add(e, candle));
						el.DoIf<IChartElement, ChartIndicatorElement>(e => pair.Second.Add(e, CreateIndicatorValue(e, candle)));
					}

					values.Add(pair);
				}

				ChartPanel.Draw(values);
			}
		}

		private void ProcessIndicator(ChartIndicatorElement element, IEnumerable<Candle> candles)
		{
			var allValues = candles
				.Take(_candlesCount)
				.Select(candle => new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(candle.OpenTime, new Dictionary<IChartElement, object>
				{
					{ element, CreateIndicatorValue(element, candle) }
				}))
				.ToList();

			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				ChartPanel.Reset(new[] { element });
				ChartPanel.Draw(allValues);
			});
		}

		private IIndicatorValue CreateIndicatorValue(ChartIndicatorElement element, Candle candle)
		{
			var indicator = _indicators.TryGetValue(element);

			if (indicator == null)
				throw new InvalidOperationException(LocalizedStrings.Str3848.Put(element));

			return indicator.Process(candle);
		}

		public void Draw(CandleSeries series, IEnumerable<Candle> candles)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (candles == null)
				throw new ArgumentNullException("candles");

			Series = series;
			_candles = candles;

			_candlesCount = 0;

			var ohlcArea = new ChartArea { Height = 210 };
			ChartPanel.AddArea(ohlcArea);

			_candlesElem = new ChartCandleElement();
			ChartPanel.AddElement(ohlcArea, _candlesElem, series);

			var volumeArea = new ChartArea { Height = 130 };
			ChartPanel.AddArea(volumeArea);

			_volumeElem = new ChartIndicatorElement
			{
				IndicatorPainter = new VolumePainter(),
			};
			var indicator = new VolumeIndicator();
			_indicators.Add(_volumeElem, indicator);
			ChartPanel.AddElement(volumeArea, _volumeElem, series, indicator);

			CancelButton.Visibility = Visibility.Visible;

			_drawTimer.Flush();
		}

		public CandleSeries Series { get; set; }

		void IPersistable.Load(SettingsStorage storage)
		{
			ChartPanel.Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			ChartPanel.Save(storage);
		}

		string IPane.Title
		{
			get { return LocalizedStrings.Str3200 + " " + Series; }
		}

		Uri IPane.Icon
		{
			get { return null; }
		}

		bool IPane.IsValid
		{
			get { return false; }
		}

		void IDisposable.Dispose()
		{
			_isDisposed = true;
			_drawTimer.Flush();
			_syncObject.Pulse();
		}

		private void Cancel_OnClick(object sender, RoutedEventArgs e)
		{
			_isStopped = true;
		}
	}
}