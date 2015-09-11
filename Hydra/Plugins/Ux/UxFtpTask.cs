namespace StockSharp.Hydra.Ux
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Localization;

	using StockSharp.Algo;
	using StockSharp.Algo.History.Russian.Rts;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[TaskDoc("http://stocksharp.com/doc/html/184da0f9-29db-4397-8497-1ed4c8f7ea0d.htm")]
	[TaskIcon("ux_logo.png")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.History |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Ticks)]
	class UxFtpTask : BaseHydraTask
	{
		private const string _sourceName = "UX (FTP)";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class UxFtpSettings : HydraTaskSettings
		{
			public UxFtpSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("UseTemporaryFiles", TempFiles.UseAndDelete.To<string>());
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int DayOffset
			{
				get { return ExtensionInfo["DayOffset"].To<int>(); }
				set { ExtensionInfo["DayOffset"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2617Key)]
			[DescriptionLoc(LocalizedStrings.Str2813Key)]
			[PropertyOrder(2)]
			public bool IsSystemOnly
			{
				get { return (bool)ExtensionInfo["IsSystemOnly"]; }
				set { ExtensionInfo["IsSystemOnly"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(3)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo["IgnoreWeekends"]; }
				set { ExtensionInfo["IgnoreWeekends"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.TemporaryFilesKey)]
			[DescriptionLoc(LocalizedStrings.TemporaryFilesKey, true)]
			[PropertyOrder(4)]
			public TempFiles UseTemporaryFiles
			{
				get { return ExtensionInfo["UseTemporaryFiles"].To<TempFiles>(); }
				set { ExtensionInfo["UseTemporaryFiles"] = value.To<string>(); }
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
			_settings.UseTemporaryFiles = TempFiles.UseAndDelete;
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override TimeSpan OnProcess()
		{
			var source = new RtsHistorySource
			{
				ExchangeBoard = ExchangeBoard.Ux,
				IsSystemOnly = _settings.IsSystemOnly,
			};

			if (_settings.UseTemporaryFiles != TempFiles.NotUse)
				source.DumpFolder = GetTempPath();

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

				if (_settings.IgnoreWeekends && !ExchangeBoard.Ux.IsTradeDate(date.ApplyTimeZone(Exchange.Ux.TimeZoneInfo), true))
				{
					this.AddDebugLog(LocalizedStrings.WeekEndDate, date);
					continue;
				}

				this.AddInfoLog(LocalizedStrings.Str2823Params, date);

				var trades = source.LoadTrades(EntityRegistry.Securities, date);

				if (trades.Count == 0)
				{
					this.AddDebugLog(LocalizedStrings.NoData);
				}
				else
				{
					if (allSecurity == null)
						trades = trades.Where(p => secMap.Contains(p.Key)).ToDictionary();

					foreach (var pair in trades)
					{
						SaveSecurity(pair.Key);
						SaveTrades(pair.Key, pair.Value);
					}
				}

				if (_settings.UseTemporaryFiles == TempFiles.UseAndDelete)
				{
					var dir = source.GetDumpFile(null, date, date, typeof(Trade), null);

					if (Directory.Exists(dir))
						Directory.Delete(dir, true);
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