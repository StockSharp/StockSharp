namespace StockSharp.Hydra.AlorHistory
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History.Russian;
	using StockSharp.Logging;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2288ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/cec19cef-4bf7-4dcb-90fb-fccba4c0248c.htm")]
	[Icon("alor_logo.png")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.History |
		TaskCategories.Stock | TaskCategories.Candles | TaskCategories.Free)]
	class AlorHistoryTask : BaseHydraTask
	{
		private const string _sourceName = LocalizedStrings.AlorHistoryKey;

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class AlorHistorySettings : HydraTaskSettings
		{
			public AlorHistorySettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("CandleDayStep", 30);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int Offset
			{
				get { return ExtensionInfo["Offset"].To<int>(); }
				set { ExtensionInfo["Offset"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(2)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo["IgnoreWeekends"]; }
				set { ExtensionInfo["IgnoreWeekends"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.TemporaryFilesKey)]
			[DescriptionLoc(LocalizedStrings.TemporaryFilesKey, true)]
			[PropertyOrder(3)]
			public TempFiles UseTemporaryFiles
			{
				get { return ExtensionInfo["UseTemporaryFiles"].To<TempFiles>(); }
				set { ExtensionInfo["UseTemporaryFiles"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
			[DescriptionLoc(LocalizedStrings.CandleTimeIntervalKey)]
			[PropertyOrder(4)]
			public int CandleDayStep
			{
				get { return ExtensionInfo["CandleDayStep"].To<int>(); }
				set
				{
					if (value < 1)
						throw new ArgumentOutOfRangeException();

					ExtensionInfo["CandleDayStep"] = value;
				}
			}
		}

		private AlorHistorySettings _settings;

		public AlorHistoryTask()
		{
			_supportedCandleSeries = AlorHistorySource.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new AlorHistorySettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Offset = 1;
			_settings.StartFrom = new DateTime(2001, 1, 1);
			_settings.Interval = TimeSpan.FromDays(1);
			_settings.IgnoreWeekends = true;
			_settings.UseTemporaryFiles = TempFiles.UseAndDelete;
			_settings.CandleDayStep = 30;
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly Type[] _supportedMarketDataTypes = { typeof(Candle) };

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override TimeSpan OnProcess()
		{
			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			if (this.GetAllSecurity() != null)
			{
				//throw new InvalidOperationException("Источник не поддерживает закачку данных по всем инструментам.");
				this.AddWarningLog(LocalizedStrings.Str2292);
				return TimeSpan.MaxValue;
			}

			var source = new AlorHistorySource();

			if (_settings.UseTemporaryFiles != TempFiles.NotUse)
				source.DumpFolder = GetTempPath();

			var startDate = _settings.StartFrom;
			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.Offset);

			var allDates = startDate.Range(endDate, TimeSpan.FromDays(1)).ToArray();

			foreach (var security in GetWorkingSecurities())
			{
				if (!CanProcess())
					break;

				#region LoadCandles
				if (security.CandleSeries.Any())
				{
					foreach (var series in security.CandleSeries)
					{
						if (!CanProcess())
							break;

						if (series.CandleType != typeof(TimeFrameCandle))
						{
							this.AddWarningLog(LocalizedStrings.Str2296Params, series);
							continue;
						}

						var storage = StorageRegistry.GetCandleStorage(series.CandleType, security.Security, series.Arg, _settings.Drive, _settings.StorageFormat);
						var emptyDates = allDates.Except(storage.Dates).ToArray();

						if (emptyDates.IsEmpty())
						{
							this.AddInfoLog(LocalizedStrings.Str2297Params, series.Arg, security.Security.Id);
							continue;
						}

						var currDate = emptyDates.First();
						var lastDate = emptyDates.Last();

						while (currDate <= lastDate)
						{
							if (!CanProcess())
								break;

							if (_settings.IgnoreWeekends && !security.IsTradeDate(currDate))
							{
								this.AddDebugLog(LocalizedStrings.WeekEndDate, currDate);
								currDate = currDate.AddDays(1);
								continue;
							}

							try
							{
								var till = currDate.AddDays(_settings.CandleDayStep - 1);
								this.AddInfoLog(LocalizedStrings.Str2298Params, series.Arg, currDate, till, security.Security.Id);
								
								var candles = source.GetCandles(security.Security, (TimeSpan)series.Arg, currDate, till);
								
								if (candles.Any())
									SaveCandles(security, candles);
								else
									this.AddDebugLog(LocalizedStrings.NoData);

								if (_settings.UseTemporaryFiles == TempFiles.UseAndDelete)
									File.Delete(source.GetDumpFile(security.Security, currDate, till, typeof(TimeFrameCandleMessage), series.Arg));
							}
							catch (Exception ex)
							{
								HandleError(new InvalidOperationException(LocalizedStrings.Str2299Params
									.Put(series.Arg, currDate, security.Security.Id), ex));
							}

							currDate = currDate.AddDays(_settings.CandleDayStep);
						}
					}
				}
				#endregion
			}

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}
	}
}
