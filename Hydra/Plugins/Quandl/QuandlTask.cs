namespace StockSharp.Hydra.Quandl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;
	using System.IO;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Logging;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2288ParamsKey, _sourceName)]
	[TaskDoc("http://stocksharp.com/doc/html/bb5fedcc-9226-448e-8bb9-42969fba227e.htm")]
	[TaskIcon("quandl_logo.png")]
	[TaskCategory(TaskCategories.America | TaskCategories.History | TaskCategories.Forex |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Candles)]
	class QuandlTask : BaseHydraTask, ISecurityDownloader
    {
		private const string _sourceName = "Quandl";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class QuandlSettings : HydraTaskSettings
		{
			public QuandlSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3451Key)]
			[DescriptionLoc(LocalizedStrings.Str3451Key, true)]
			[PropertyOrder(0)]
			public SecureString AuthToken
			{
				get { return ExtensionInfo["AuthToken"].To<SecureString>(); }
				set { ExtensionInfo["AuthToken"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(1)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(2)]
			public int DayOffset
			{
				get { return ExtensionInfo["DayOffset"].To<int>(); }
				set { ExtensionInfo["DayOffset"] = value; }
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

		private QuandlSettings _settings;
		private QuandlSecurityStorage _quandlSecurityStorage;

		public QuandlTask()
		{
			_supportedCandleSeries = QuandlHistorySource.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
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

		private readonly Type[] _supportedMarketDataTypes = { typeof(Candle) };

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new QuandlSettings(settings);

			if (_settings.IsDefault)
			{
				_settings.DayOffset = 1;
				_settings.StartFrom = new DateTime(1980, 1, 1);
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.IgnoreWeekends = true;
				_settings.AuthToken = new SecureString();
				_settings.UseTemporaryFiles = TempFiles.UseAndDelete;
			}

			_quandlSecurityStorage = new QuandlSecurityStorage(EntityRegistry);
		}

		public void Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			CreateSource().Refresh(_quandlSecurityStorage, criteria, newSecurity, isCancelled);
		}

		private static bool IsQuandl(HydraTaskSecurity taskSecurity)
		{
			var security = taskSecurity.Security;

			return security.ExtensionInfo.ContainsKey(QuandlHistorySource.SecurityCodeField)
				&& security.ExtensionInfo.ContainsKey(QuandlHistorySource.SourceCodeField);
		}

		protected override TimeSpan OnProcess()
		{
			var source = CreateSource();

			var allSecurity = this.GetAllSecurity();

			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			var selectedSecurities = (allSecurity != null ? this.ToHydraSecurities(_quandlSecurityStorage.Securities) : Settings.Securities).ToArray();

			var hasNonQuandl = selectedSecurities.Any(s => !IsQuandl(s));

			if (selectedSecurities.IsEmpty() || hasNonQuandl)
			{
				this.AddWarningLog(selectedSecurities.IsEmpty()
					? LocalizedStrings.Str2289
					: LocalizedStrings.Str2290.Put("Quandl"));

				source.Refresh(_quandlSecurityStorage, new Security(), SaveSecurity, () => !CanProcess(false));

				selectedSecurities = (allSecurity != null ? this.ToHydraSecurities(_quandlSecurityStorage.Securities) : Settings.Securities)
					.Where(s =>
					{
						var retVal = IsQuandl(s);

						if (!retVal)
							this.AddWarningLog(LocalizedStrings.Str2291Params, s.Security.Id, "Quandl");

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

			if (_settings.UseTemporaryFiles != TempFiles.NotUse)
				source.DumpFolder = GetTempPath();

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

					var tf = (TimeSpan)series.Arg;

					var storage = StorageRegistry.GetCandleStorage(series.CandleType, security.Security, tf, _settings.Drive, _settings.StorageFormat);
					var emptyDates = allDates.Except(storage.Dates).ToArray();

					if (emptyDates.IsEmpty())
					{
						this.AddInfoLog(LocalizedStrings.Str2297Params, tf, security.Security.Id);
						continue;
					}

					foreach (var emptyDate in emptyDates)
					{
						if (!CanProcess())
							break;

						if (_settings.IgnoreWeekends && !security.IsTradeDate(emptyDate))
						{
							this.AddDebugLog(LocalizedStrings.WeekEndDate, emptyDate);
							continue;
						}

						try
						{
							this.AddInfoLog(LocalizedStrings.Str2298Params, tf, emptyDate, security.Security.Id);
							var candles = source.GetCandles(security.Security, tf, emptyDate, emptyDate + tf);

							if (candles.Any())
								SaveCandles(security, candles);
							else
								this.AddDebugLog(LocalizedStrings.NoData);

							if (_settings.UseTemporaryFiles == TempFiles.UseAndDelete)
								File.Delete(source.GetDumpFile(security.Security, emptyDate, emptyDate, typeof(TimeFrameCandle), series.Arg));
						}
						catch (Exception ex)
						{
							HandleError(new InvalidOperationException(LocalizedStrings.Str2299Params
								.Put(series.Arg, emptyDate, security.Security.Id), ex));
						}
					}
				}
			}

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}

		private QuandlHistorySource CreateSource()
		{
			return new QuandlHistorySource
			{
				AuthToken = _settings.AuthToken.To<string>()
			};
		}
    }
}