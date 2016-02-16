#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: EmulationControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Configuration;
using Ecng.Serialization;
using MoreLinq;
using StockSharp.Algo.Candles;
using StockSharp.Terminal.Layout;
using StockSharp.BusinessEntities;
using StockSharp.Studio.Controls;
using StockSharp.Studio.Core.Commands;
using StockSharp.Terminal.Services;
using StockSharp.Xaml.Charting;

namespace StockSharp.Terminal.Controls
{
	public partial class WorkAreaControl
	{
		private readonly LayoutManager _layoutManager;
		private Security _lastSelectedSecurity;
		ChartCandleElement _candleElement;
		ChartArea _chartArea;
		CandleSeries _series;

		public WorkAreaControl()
		{
			InitializeCommands();
			InitializeComponent();

			_layoutManager = new LayoutManager(DockingManager);
		}

		private void InitializeCommands()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			cmdSvc.Register<SelectCommand>(this, true, cmd =>
			{
				var sec = cmd.Instance as Security;
				if(sec == null)
					return;

				var oldSec = _lastSelectedSecurity;
				_lastSelectedSecurity = sec;

				OnSelectedSecurityChanged(oldSec);
			});

			cmdSvc.Register<NewCandlesCommand>(this, true, cmd =>
			{
				if(_lastSelectedSecurity == null || _candleElement == null)
					return;

				var candles = cmd.Candles.Where(c => c.Security.Id == _lastSelectedSecurity.Id).ToArray();

				if(candles.Length == 0)
					return;

				var values = new List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>>();
				candles.ForEach(c =>
				{
					values.Add(new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(c.OpenTime, new Dictionary<IChartElement, object>
					{
						{_candleElement, c}
					}));
				});

				new ChartDrawCommand(values).Process(this);
			});
		}

		public void HandleNewPanelSelection(Type controlType)
		{
			if(controlType == null || !typeof(BaseStudioControl).IsAssignableFrom(controlType))
				return;

			var control = (BaseStudioControl) Activator.CreateInstance(controlType);

			var depthControl = control as ScalpingMarketDepthControl;
			if (depthControl != null && _lastSelectedSecurity != null)
				depthControl.Settings.Security = _lastSelectedSecurity;

			control.Title = control.GetType().GetDisplayName();

			_layoutManager.OpenToolWindow(control);
		}

		void OnSelectedSecurityChanged(Security oldSec)
		{
			ResetChart();
			ResetMarketDepth();

			if(_lastSelectedSecurity == null)
				return;

			if (_chartArea == null)
			{
				_chartArea = new ChartArea { Title = "Candles chart" };
				_chartArea.YAxises.First().AutoRange = true;

				new ChartAddAreaCommand(_chartArea).Process(this);
			}

			var today = DateTimeOffset.Now.Date;
			_candleElement = new ChartCandleElement();
			_series = new CandleSeries(typeof(TimeFrameCandle), _lastSelectedSecurity, TimeSpan.FromMinutes(5))
			{
				From = today - TimeSpan.FromDays(5),
				To = today + TimeSpan.FromDays(1),
			};

			new ChartAddElementCommand(_chartArea, _candleElement, _series).Process(this);
		}

		void ResetChart()
		{
			if (_chartArea != null)
			{
				if (_candleElement != null)
					new ChartRemoveElementCommand(_chartArea, _candleElement).Process(this);

				new ChartRemoveAreaCommand(_chartArea).Process(this);
				_chartArea = null;
			}
		}

		void ResetMarketDepth()
		{
			new ClearMarketDepthCommand(_lastSelectedSecurity).Process(this);
		}

		#region IPersistable

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			_layoutManager.Load(storage.GetValue<SettingsStorage>("LayoutManager"));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("LayoutManager", _layoutManager.Save());
		}

		#endregion
	}
}
