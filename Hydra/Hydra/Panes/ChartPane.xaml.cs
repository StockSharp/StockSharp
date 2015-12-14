#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: ChartPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Configuration;
	using StockSharp.Logging;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Charting.IndicatorPainters;
	using StockSharp.Localization;

	public partial class ChartPane : IPane
	{
		private readonly SynchronizedDictionary<ChartIndicatorElement, IIndicator> _indicators = new SynchronizedDictionary<ChartIndicatorElement, IIndicator>();
		private readonly CachedSynchronizedList<RefPair<IChartElement, int>> _elements = new CachedSynchronizedList<RefPair<IChartElement, int>>();
		private readonly ResettableTimer _drawTimer;

		private ChartCandleElement _candlesElem;
		private ChartIndicatorElement _volumeElem;
		private IEnumerable<Candle> _candles;

		public ChartPane()
		{
			InitializeComponent();

			_drawTimer = new ResettableTimer(TimeSpan.FromSeconds(2), "Chart");
			_drawTimer.Elapsed += DrawTimerOnElapsed;

			ChartPanel.MinimumRange = 200;
			ChartPanel.IsInteracted = true;
			ChartPanel.FillIndicators();

			ChartPanel.SubscribeCandleElement += OnChartPanelSubscribeCandleElement;
			ChartPanel.SubscribeIndicatorElement += OnChartPanelSubscribeIndicatorElement;
			ChartPanel.UnSubscribeElement += ChartPanelOnUnSubscribeElement;
		}

		private void OnChartPanelSubscribeCandleElement(ChartCandleElement element, CandleSeries candleSeries)
		{
			_drawTimer.Cancel();

			_elements.Add(new RefPair<IChartElement, int>(element, 0));
			_drawTimer.Activate();
		}

		private void OnChartPanelSubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries candleSeries, IIndicator indicator)
		{
			_drawTimer.Cancel();

			_elements.Add(new RefPair<IChartElement, int>(element, 0));
			_indicators.Add(element, indicator);
			_drawTimer.Activate();
		}

		private void ChartPanelOnUnSubscribeElement(IChartElement element)
		{
			lock (_elements.SyncRoot)
				_elements.RemoveWhere(p => p.First == element);
		}

		private void DrawTimerOnElapsed(Func<bool> canProcess)
		{
			try
			{
				GuiDispatcher.GlobalDispatcher.AddAction(() => CancelButton.Visibility = Visibility.Visible);

				var index = 0;

				foreach (var batch in _candles.Batch(50))
				{
					if (!canProcess())
						break;

					var values = new List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>>();

					foreach (var c in batch)
					{
						var candle = c;

						var pair = new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(candle.OpenTime, new Dictionary<IChartElement, object>());

						foreach (var elemPair in _elements.Cache)
						{
							if (elemPair.Second >= index)
								continue;

							elemPair.First.DoIf<IChartElement, ChartCandleElement>(e => pair.Second.Add(e, candle));
							elemPair.First.DoIf<IChartElement, ChartIndicatorElement>(e => pair.Second.Add(e, CreateIndicatorValue(e, candle)));

							elemPair.Second = index;
						}

						values.Add(pair);

						index++;
					}

					ChartPanel.Draw(values);
				}

				GuiDispatcher.GlobalDispatcher.AddAction(() => CancelButton.Visibility = Visibility.Collapsed);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		private IIndicatorValue CreateIndicatorValue(ChartIndicatorElement element, Candle candle)
		{
			var indicator = _indicators.TryGetValue(element);

			if (indicator == null)
				throw new InvalidOperationException(LocalizedStrings.IndicatorNotFound.Put(element));

			return indicator.Process(candle);
		}

		public void Draw(CandleSeries series, IEnumerable<Candle> candles)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (candles == null)
				throw new ArgumentNullException(nameof(candles));

			Series = series;
			_candles = candles;

			//_candlesCount = 0;

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
			ChartPanel.AddElement(volumeArea, _volumeElem, series, indicator);

			CancelButton.Visibility = Visibility.Visible;

			_drawTimer.Activate();
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

		string IPane.Title => LocalizedStrings.Str3200 + " " + Series;

		Uri IPane.Icon => null;

		bool IPane.IsValid => false;

		void IDisposable.Dispose()
		{
			_drawTimer.Dispose();
		}

		private void Cancel_OnClick(object sender, RoutedEventArgs e)
		{
			_drawTimer.Cancel();
		}
	}
}