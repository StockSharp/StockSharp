namespace StockSharp.Hydra.Yahoo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2288ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/c60b2dd2-518c-49e5-8ba4-edabbda04222.htm")]
	[Icon("yahoo_logo.png")]
	[TaskCategory(TaskCategories.America | TaskCategories.History | TaskCategories.Stock |
		TaskCategories.Free | TaskCategories.Candles | TaskCategories.Level1)]
	class YahooTask : BaseHydraTask, ISecurityDownloader
    {
		private const string _sourceName = "Yahoo";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class YahooSettings : HydraTaskSettings
		{
			public YahooSettings(HydraTaskSettings settings)
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
			public int DayOffset
			{
				get { return ExtensionInfo["DayOffset"].To<int>(); }
				set { ExtensionInfo["DayOffset"] = value; }
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

		private YahooSettings _settings;

		public YahooTask()
		{
			_supportedCandleSeries = YahooHistorySource.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new YahooSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.DayOffset = 1;
			_settings.StartFrom = new DateTime(2000, 1, 1);
			_settings.Interval = TimeSpan.FromDays(1);
			_settings.IgnoreWeekends = true;
			_settings.CandleDayStep = 30;
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		private readonly Type[] _supportedMarketDataTypes = { typeof(Candle), typeof(Level1ChangeMessage) };

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		protected override TimeSpan OnProcess()
		{
			var allSecurity = this.GetAllSecurity();

			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			IEnumerable<HydraTaskSecurity> selectedSecurities = (allSecurity != null
				? this.ToHydraSecurities(EntityRegistry.Securities)
				: Settings.Securities
			).ToArray();

			var source = new YahooHistorySource();

			if (selectedSecurities.IsEmpty())
			{
				this.AddWarningLog(LocalizedStrings.Str2289);

				source.Refresh(EntityRegistry.Securities, new Security(), SaveSecurity, () => !CanProcess(false));
				selectedSecurities = this.ToHydraSecurities(EntityRegistry.Securities);
			}

			if (selectedSecurities.IsEmpty())
			{
				this.AddWarningLog(LocalizedStrings.Str2292);
				return TimeSpan.MaxValue;
			}

			var startDate = _settings.StartFrom;
			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.DayOffset);

			var allDates = startDate.Range(endDate, TimeSpan.FromDays(1)).ToArray();

			foreach (var security in selectedSecurities)
			{
				if (!CanProcess())
					break;

				foreach (var series in (allSecurity ?? security).CandleSeries)
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
							var till = currDate + TimeSpan.FromDays(_settings.CandleDayStep).Max((TimeSpan)series.Arg);
							this.AddInfoLog(LocalizedStrings.Str2298Params, series.Arg, currDate, till, security.Security.Id);
							
							var candles = source.GetCandles(security.Security, (TimeSpan)series.Arg, currDate, till);

							if (candles.Any())
								SaveCandles(security, candles);
							else
								this.AddDebugLog(LocalizedStrings.NoData);
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

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}

		void ISecurityDownloader.Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			new YahooHistorySource().Refresh(storage, criteria, newSecurity, isCancelled);
		}
    }
}