namespace StockSharp.Studio.Controls
{
	using System;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Charting.IndicatorPainters;

	[DisplayNameLoc(LocalizedStrings.Str3200Key)]
	[DescriptionLoc(LocalizedStrings.Str3201Key)]
	[Icon("images/chart_24x24.png")]
	public partial class CandleChartPanel
	{
		private readonly BufferedChart _bufferedChart;

		private SettingsStorage _settingsStorage;
		private StrategyContainer _strategy;
		private Timer _timer;

		public CandleChartPanel()
		{
			InitializeComponent();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<ChartDrawCommand>(this, false, cmd => _bufferedChart.Draw(cmd.Values));
			cmdSvc.Register<ChartAddAreaCommand>(this, false, cmd => _bufferedChart.AddArea(cmd.Area));
			cmdSvc.Register<ChartRemoveAreaCommand>(this, false, cmd => _bufferedChart.RemoveArea(cmd.Area));
			cmdSvc.Register<ChartAddElementCommand>(this, false, cmd => _bufferedChart.AddElement(cmd.Area, cmd.Element));
			cmdSvc.Register<ChartRemoveElementCommand>(this, false, cmd => _bufferedChart.RemoveElement(cmd.Area, cmd.Element));
			cmdSvc.Register<ChartClearAreasCommand>(this, false, cmd => _bufferedChart.ClearAreas());
			cmdSvc.Register<ChartResetElementsCommand>(this, false, cmd => _bufferedChart.Reset(cmd.Elements));
			cmdSvc.Register<ChartAutoRangeCommand>(this, false, cmd => _bufferedChart.IsAutoRange = cmd.AutoRange);
			cmdSvc.Register<ResetedCommand>(this, true, cmd => OnReseted());
			cmdSvc.Register<BindStrategyCommand>(this, true, cmd =>
			{
				if (!cmd.CheckControl(this))
					return;

				if (_strategy == cmd.Source)
					return;

				_strategy = cmd.Source;

				SetChart(true);

				ChartPanel.IsInteracted = _strategy != null && _strategy.GetIsInteracted();

				if (_settingsStorage != null)
					ChartPanel.Load(_settingsStorage);

				TryCreateDefaultSeries();
			});
			
			ChartPanel.SettingsChanged += () => new ControlChangedCommand(this).Process(this);
			ChartPanel.RegisterOrder += order => new RegisterOrderCommand(order).Process(this);
			ChartPanel.SubscribeCandleElement += OnChartPanelSubscribeCandleElement;
			ChartPanel.SubscribeIndicatorElement += OnChartPanelSubscribeIndicatorElement;
			ChartPanel.SubscribeOrderElement += OnChartPanelSubscribeOrderElement;
			ChartPanel.SubscribeTradeElement += OnChartPanelSubscribeTradeElement;
			ChartPanel.UnSubscribeElement += OnChartPanelUnSubscribeElement;

			var indicatorTypes = ConfigManager
				.GetService<IAlgoService>()
				.IndicatorTypes;

			ChartPanel.MinimumRange = 200;
			ChartPanel.IndicatorTypes.AddRange(indicatorTypes);

			_bufferedChart = new BufferedChart(ChartPanel);

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		public override void Dispose()
		{
			if (_strategy != null)
				SetChart(false);

			if (_timer != null)
			{
				_timer.Dispose();
				_timer = null;
			}

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<ChartDrawCommand>(this);
			cmdSvc.UnRegister<ChartAddAreaCommand>(this);
			cmdSvc.UnRegister<ChartRemoveAreaCommand>(this);
			cmdSvc.UnRegister<ChartAddElementCommand>(this);
			cmdSvc.UnRegister<ChartRemoveElementCommand>(this);
			cmdSvc.UnRegister<ChartClearAreasCommand>(this);
			cmdSvc.UnRegister<ChartResetElementsCommand>(this);
			cmdSvc.UnRegister<ChartAutoRangeCommand>(this);
			cmdSvc.UnRegister<ResetedCommand>(this);
			cmdSvc.UnRegister<BindStrategyCommand>(this);
		}

		private void OnReseted()
		{
			// если у элемента нет "источника" созданного руками значит это творчество стратегии
			foreach (var area in ChartPanel.Areas.ToArray())
			{
				if (_strategy != null && _strategy.GetIsInteracted())
				{
					foreach (var e in area.Elements.ToArray())
					{
						if (ChartPanel.Elements.All(el => el != e))
							area.Elements.Remove(e);
					}
				}
				
				if (area.Elements.IsEmpty())
					ChartPanel.Areas.Remove(area);
			}

			ChartPanel.Reset(ChartPanel.Elements);
			ChartPanel.ReSubscribeElements();

			//TryCreateDefaultSeries();
		}

		private void TryCreateDefaultSeries()
		{
			if (_strategy == null || _strategy.Security == null || !_strategy.GetIsInteracted() || ChartPanel.Areas.Count != 0)
				return;

			var series = _strategy.Security.TimeFrame(TimeSpan.FromMinutes(5));

			var area = new ChartArea { Title = LocalizedStrings.Str3080 + " 1" };
			ChartPanel.AddArea(area);
			ChartPanel.AddElement(area, new ChartCandleElement(), series);
			ChartPanel.AddElement(area, new ChartIndicatorElement(), series, new SimpleMovingAverage { Length = 50 });
			ChartPanel.AddElement(area, new ChartOrderElement(), _strategy.Security);
			ChartPanel.AddElement(area, new ChartTradeElement(), _strategy.Security);

			var adxArea = new ChartArea { Title = LocalizedStrings.Str3080 + " 2" };
			ChartPanel.AddArea(adxArea);
			ChartPanel.AddElement(adxArea, new ChartIndicatorElement { IndicatorPainter = new AverageDirectionalIndexPainter() }, series, new AverageDirectionalIndex());

			var volumeArea = new ChartArea { Title = LocalizedStrings.Str3080 + " 3" };
			ChartPanel.AddArea(volumeArea);
			ChartPanel.AddElement(volumeArea, new ChartIndicatorElement { IndicatorPainter = new VolumePainter() }, series, new VolumeIndicator());
		}

		private void OnChartPanelSubscribeCandleElement(ChartCandleElement element, CandleSeries candleSeries)
		{
			new SubscribeCandleElementCommand(element, candleSeries).Process(this);
		}

		private void OnChartPanelSubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries candleSeries, IIndicator indicator)
		{
			new SubscribeIndicatorElementCommand(element, candleSeries, indicator).Process(this);
		}

		private void OnChartPanelSubscribeOrderElement(ChartOrderElement element, Security security)
		{
			new SubscribeOrderElementCommand(element, security).Process(this);
		}

		private void OnChartPanelSubscribeTradeElement(ChartTradeElement element, Security security)
		{
			new SubscribeTradeElementCommand(element, security).Process(this);
		}

		private void OnChartPanelUnSubscribeElement(IChartElement element)
		{
			element.DoIf<IChartElement, ChartCandleElement>(e => new UnSubscribeCandleElementCommand(e).Process(this));
			element.DoIf<IChartElement, ChartIndicatorElement>(e => new UnSubscribeIndicatorElementCommand(e).Process(this));
			element.DoIf<IChartElement, ChartOrderElement>(e => new UnSubscribeOrderElementCommand(e).Process(this));
			element.DoIf<IChartElement, ChartTradeElement>(e => new UnSubscribeTradeElementCommand(e).Process(this));
		}

		private void SetChart(bool valid)
		{
			_strategy.SetChart(valid ? _bufferedChart : null);
		}

		public override void Load(SettingsStorage storage)
		{
			_settingsStorage = storage;
		}

		public override void Save(SettingsStorage storage)
		{
			if (_strategy != null)
				ChartPanel.Save(storage);
			else
				storage.AddRange(_settingsStorage);
		}
	}
}