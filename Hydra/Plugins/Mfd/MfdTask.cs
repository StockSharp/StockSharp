namespace StockSharp.Hydra.Mfd
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History;
	using StockSharp.Algo.History.Russian;
	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.Russian)]
	[TaskDisplayName(_sourceName)]
	class MfdTask : BaseHydraTask, ISecurityDownloader
	{
		private const string _sourceName = "MFD";

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class MfdSettings : HydraTaskSettings
		{
			public MfdSettings(HydraTaskSettings settings)
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
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
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
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(2)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo["IgnoreWeekends"]; }
				set { ExtensionInfo["IgnoreWeekends"] = value; }
			}
		}

		private MfdSettings _settings;
		private MfdSecurityStorage _mfdSecurityStorage;

		public MfdTask()
		{
			_supportedCandleSeries = MfdHistorySource.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new MfdSettings(settings);

			if (settings.IsDefault)
			{
				_settings.DayOffset = 1;
				_settings.StartFrom = new DateTime(2001, 1, 1);
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.IgnoreWeekends = true;
			}

			_mfdSecurityStorage = new MfdSecurityStorage(EntityRegistry);
		}

		public override Uri Icon
		{
			get { return "mfd_logo.png".GetResourceUrl(GetType()); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str3807Params.Put(_sourceName); }
		}

		public override TaskTypes Type
		{
			get { return TaskTypes.Source; }
		}

		private readonly Type[] _supportedMarketDataTypes = { typeof(Trade), typeof(Candle) };

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		private static bool IsMfd(HydraTaskSecurity taskSecurity)
		{
			var security = taskSecurity.Security;

			return security.ExtensionInfo.ContainsKey(MfdHistorySource.MarketIdField)
				&& security.ExtensionInfo.ContainsKey(MfdHistorySource.SecurityIdField);
		}

		protected override TimeSpan OnProcess()
		{
			var source = new MfdHistorySource { DumpFolder = GetTempPath() };

			var allSecurity = this.GetAllSecurity();

			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			var selectedSecurities = (allSecurity != null ? this.ToHydraSecurities(_mfdSecurityStorage.Securities) : Settings.Securities).ToArray();

			var hasNonMfd = selectedSecurities.Any(s => !IsMfd(s));

			if (selectedSecurities.IsEmpty() || hasNonMfd)
			{
				this.AddWarningLog(selectedSecurities.IsEmpty()
					? LocalizedStrings.Str2289
					: LocalizedStrings.Str3808);

				source.Refresh(_mfdSecurityStorage, new Security(), SaveSecurity, () => !CanProcess(false));

				selectedSecurities = (allSecurity != null ? this.ToHydraSecurities(_mfdSecurityStorage.Securities) : Settings.Securities)
					.Where(s =>
					{
						var retVal = IsMfd(s);

						if (!retVal)
							this.AddWarningLog(LocalizedStrings.Str3809Params, s.Security.Id);

						return retVal;
					}).ToArray();
			}

			if (!CanProcess())
				return base.OnProcess();

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

				#region LoadTrades
				if ((allSecurity ?? security).MarketDataTypesSet.Contains(typeof(Trade)))
				{
					var storage = StorageRegistry.GetTradeStorage(security.Security, _settings.Drive, _settings.StorageFormat);
					var emptyDates = allDates.Except(storage.Dates).ToArray();

					if (emptyDates.IsEmpty())
					{
						this.AddInfoLog(LocalizedStrings.Str2293Params, security.Security.Id);
					}
					else
					{
						foreach (var emptyDate in emptyDates)
						{
							if (!CanProcess())
								break;

							if (_settings.IgnoreWeekends && !security.IsTradeDate(emptyDate))
								continue;

							try
							{
								this.AddInfoLog(LocalizedStrings.Str2294Params, emptyDate, security.Security.Id);
								var trades = source.GetTrades(security.Security, emptyDate, emptyDate);
								SaveTrades(security, trades);
							}
							catch (Exception ex)
							{
								HandleError(new InvalidOperationException(LocalizedStrings.Str2295Params
									.Put(emptyDate, security.Security.Id), ex));
							}
						}
					}
				}
				#endregion

				if (!CanProcess())
					break;

				#region LoadCandles
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

					foreach (var emptyDate in emptyDates)
					{
						if (!CanProcess())
							break;

						if (_settings.IgnoreWeekends && !security.IsTradeDate(emptyDate))
							continue;

						try
						{
							this.AddInfoLog(LocalizedStrings.Str2298Params, series.Arg, emptyDate, security.Security.Id);
							var candles = source.GetCandles(security.Security, (TimeSpan)series.Arg, emptyDate, emptyDate);
							SaveCandles(security, candles);

							File.Delete(source.GetDumpFile(security.Security, emptyDate, emptyDate, typeof(TimeFrameCandle), series.Arg));
						}
						catch (Exception ex)
						{
							HandleError(new InvalidOperationException(LocalizedStrings.Str2299Params
								.Put(series.Arg, emptyDate, security.Security.Id), ex));
						}
					}
				}
				#endregion
			}

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}

		void ISecurityDownloader.Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			new MfdHistorySource().Refresh(_mfdSecurityStorage, criteria, newSecurity, isCancelled);
		}
	}
}
