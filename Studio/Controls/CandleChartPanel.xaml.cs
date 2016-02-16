#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: CandleChartPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace StockSharp.Studio.Controls
{
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml.Charting;

	[DisplayNameLoc(LocalizedStrings.Str3200Key)]
	[DescriptionLoc(LocalizedStrings.Str3201Key)]
	[Icon("images/chart_24x24.png")]
	public partial class CandleChartPanel
	{
		private Timer _timer;

		public CandleChartPanel()
		{
			InitializeComponent();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<ChartDrawCommand>(this, true, cmd => ChartPanel.Draw(cmd.Values));
			cmdSvc.Register<ChartAddAreaCommand>(this, true, cmd => ChartPanel.AddArea(cmd.Area));
			cmdSvc.Register<ChartRemoveAreaCommand>(this, true, cmd => ChartPanel.RemoveArea(cmd.Area));
			cmdSvc.Register<ChartAddElementCommand>(this, true, cmd =>
			{
				var celem = cmd.Element as ChartCandleElement;

				if (celem != null && cmd.Series != null)
				{
					ChartPanel.AddElement(cmd.Area, celem, cmd.Series);
					OnChartPanelSubscribeCandleElement(celem, cmd.Series);
				}
				else
					ChartPanel.AddElement(cmd.Area, cmd.Element);
			});
			cmdSvc.Register<ChartRemoveElementCommand>(this, true, cmd =>
			{
				ChartPanel.RemoveElement(cmd.Area, cmd.Element);
				OnChartPanelUnSubscribeElement(cmd.Element);
			});

			cmdSvc.Register<ChartClearAreasCommand>(this, true, cmd => ChartPanel.ClearAreas());
			cmdSvc.Register<ChartResetElementsCommand>(this, true, cmd => ChartPanel.Reset(cmd.Elements));
			cmdSvc.Register<ChartAutoRangeCommand>(this, true, cmd => ChartPanel.IsAutoRange = cmd.AutoRange);
			cmdSvc.Register<ResetedCommand>(this, true, cmd => OnReseted());
			
			//ChartPanel.IsInteracted = true;
			ChartPanel.SettingsChanged += () => new ControlChangedCommand(this).Process(this);
			ChartPanel.RegisterOrder += order => new RegisterOrderCommand(order).Process(this);
			ChartPanel.SubscribeCandleElement += OnChartPanelSubscribeCandleElement;
			ChartPanel.SubscribeIndicatorElement += OnChartPanelSubscribeIndicatorElement;
			ChartPanel.SubscribeOrderElement += OnChartPanelSubscribeOrderElement;
			ChartPanel.SubscribeTradeElement += OnChartPanelSubscribeTradeElement;
			ChartPanel.UnSubscribeElement += OnChartPanelUnSubscribeElement;

			ChartPanel.MinimumRange = 200;
			ChartPanel.FillIndicators();

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		public override void Dispose()
		{
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
			cmdSvc.UnRegister<SelectCommand>(this);
		}

		private void OnReseted()
		{
			foreach (var area in ChartPanel.Areas.ToArray())
			{
				foreach (var e in area.Elements.ToArray())
				{
					if (ChartPanel.Elements.All(el => el != e))
						area.Elements.Remove(e);
				}
				
				if (area.Elements.IsEmpty())
					ChartPanel.Areas.Remove(area);
			}

			ChartPanel.Reset(ChartPanel.Elements);
			ChartPanel.ReSubscribeElements();

			//TryCreateDefaultSeries();
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

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

//			var savedPanel = storage.GetValue<SettingsStorage>("ChartPanel");
//
//			if(savedPanel != null)
//				ChartPanel.Load(savedPanel);
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

//			var s = new SettingsStorage();
//			ChartPanel.Save(s);
//
//			storage.SetValue("ChartPanel", s);
		}
	}
}