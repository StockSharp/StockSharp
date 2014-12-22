namespace StockSharp.Hydra.Ux
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.History.Russian.Rts;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.Russian)]
	[TaskDisplayName(_sourceName)]
	class UxFtpTask : BaseHydraTask
	{
		private const string _sourceName = "UX (FTP)";

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class UxFtpSettings : HydraTaskSettings
		{
			public UxFtpSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("IgnoreWeekends", true);
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int DayOffset
			{
				get { return ExtensionInfo["DayOffset"].To<int>(); }
				set { ExtensionInfo["DayOffset"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2617Key)]
			[DescriptionLoc(LocalizedStrings.Str2813Key)]
			[PropertyOrder(2)]
			public bool IsSystemOnly
			{
				get { return (bool)ExtensionInfo["IsSystemOnly"]; }
				set { ExtensionInfo["IsSystemOnly"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(3)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo["IgnoreWeekends"]; }
				set { ExtensionInfo["IgnoreWeekends"] = value; }
			}
		}

		private UxFtpSettings _settings;

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new UxFtpSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.DayOffset = 3;
			_settings.StartFrom = RtsHistorySource.UxMinAvaliableTime;
			_settings.IsSystemOnly = true;
			_settings.Interval = TimeSpan.FromDays(1);
			_settings.IgnoreWeekends = true;
		}

		public override Uri Icon
		{
			get { return "ux_logo.png".GetResourceUrl(GetType()); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override TaskTypes Type
		{
			get { return TaskTypes.Source; }
		}

		protected override TimeSpan OnProcess()
		{
			var source = new RtsHistorySource
			{
				ExchangeBoard = ExchangeBoard.Ux,
				DumpFolder = GetTempPath(),
				IsSystemOnly = _settings.IsSystemOnly,
			};

			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			var allSecurity = this.GetAllSecurity();

			var startDate = _settings.StartFrom;
			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.DayOffset);

			var allDates = startDate.Range(endDate, TimeSpan.FromDays(1)).ToArray();

			var secMap = new HashSet<Security>();

			if (allSecurity == null)
				secMap.AddRange(Settings.Securities.Select(s => s.Security));

			foreach (var date in allDates)
			{
				if (!CanProcess())
					break;

				if (_settings.IgnoreWeekends && !ExchangeBoard.Ux.WorkingTime.IsTradeDate(date, true))
					continue;

				this.AddInfoLog(LocalizedStrings.Str2823Params, date.ToShortDateString());

				var trades = source.LoadTrades(EntityRegistry.Securities, date);

				if (allSecurity == null)
					trades = trades.Where(p => secMap.Contains(p.Key)).ToDictionary();

				foreach (var pair in trades)
				{
					SaveSecurity(pair.Key);
					SaveTrades(pair.Key, pair.Value);
				}

				_settings.StartFrom = date.AddDays(1);
				SaveSettings();
			}

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}
	}
}