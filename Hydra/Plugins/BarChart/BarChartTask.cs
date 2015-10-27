namespace StockSharp.Hydra.BarChart
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Security;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.BarChart;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	// TODO
	//[Doc("")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime | TaskCategories.History |
		TaskCategories.Paid | TaskCategories.Ticks | TaskCategories.MarketDepth |
		TaskCategories.Stock | TaskCategories.Level1 | TaskCategories.Candles)]
	class BarChartTask : ConnectorHydraTask<BarChartMessageAdapter>
	{
		private const string _sourceName = "BarChart";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class BarChartSettings : ConnectorHydraTaskSettings
		{
			private const string _category = _sourceName;

			public BarChartSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.Str2535Key)]
			[DescriptionLoc(LocalizedStrings.Str2536Key)]
			[PropertyOrder(0)]
			public bool IsRealTime
			{
				get { return ExtensionInfo["IsRealTime"].To<bool>(); }
				set { ExtensionInfo["IsRealTime"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int Offset
			{
				get { return ExtensionInfo["Offset"].To<int>(); }
				set { ExtensionInfo["Offset"] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(2)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo["IgnoreWeekends"]; }
				set { ExtensionInfo["IgnoreWeekends"] = value; }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		private BarChartSettings _settings;

		public BarChartTask()
			: base(new BarChartTrader())
		{
			_supportedCandleSeries = BarChartMessageAdapter.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private readonly Type[] _supportedMarketDataTypes = { typeof(Candle), typeof(QuoteChangeMessage), typeof(Level1ChangeMessage) };

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new BarChartSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Offset = 0;
			_settings.StartFrom = DateTime.Today;
			_settings.Login = null;
			_settings.Password = null;
			_settings.IsDownloadNews = true;
			_settings.IsRealTime = false;
			_settings.Interval = TimeSpan.FromDays(1);
			_settings.IgnoreWeekends = true;
		}

		protected override BarChartMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new BarChartMessageAdapter(generator)
			{
				Login = _settings.Login,
				Password = _settings.Password
			};
		}

		protected override TimeSpan OnProcess()
		{
			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			if (this.GetAllSecurity() != null)
			{
				this.AddWarningLog(LocalizedStrings.Str2549);
				return TimeSpan.MaxValue;
			}

			if (_settings.IsRealTime)
				return base.OnProcess();
			
			var startDate = _settings.StartFrom;
			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.Offset);

			var allDates = startDate.Range(endDate, TimeSpan.FromDays(1)).ToArray();

			var hasSecurities = false;

			foreach (var security in GetWorkingSecurities())
			{
				hasSecurities = true;

				if (!CanProcess())
					break;

				if (security.MarketDataTypesSet.Contains(typeof(Level1ChangeMessage)))
				{
					var tradeStorage = StorageRegistry.GetTickMessageStorage(security.Security, _settings.Drive, _settings.StorageFormat);

					foreach (var date in allDates.Except(tradeStorage.Dates))
					{
						if (!CanProcess())
							break;

						if (_settings.IgnoreWeekends && !security.IsTradeDate(date))
						{
							this.AddDebugLog(LocalizedStrings.WeekEndDate, date);
							continue;
						}

						this.AddInfoLog(LocalizedStrings.Str2294Params, date, security.Security.Id);

						bool isSuccess;
						var trades = ((BarChartTrader)Connector).GetHistoricalTicks(security.Security, _settings.StartFrom, _settings.StartFrom.EndOfDay(), out isSuccess);

						if (isSuccess)
						{
							if (trades.Any())
								SaveTicks(security, trades);
							else
								this.AddDebugLog(LocalizedStrings.NoData);
						}
						else
							this.AddErrorLog(LocalizedStrings.Str2550);
					}
				}
				else
					this.AddDebugLog(LocalizedStrings.MarketDataNotEnabled, security.Security.Id, typeof(Level1ChangeMessage).Name);

				foreach (var series in security.CandleSeries)
				{
					if (!CanProcess())
						break;

					if (series.CandleType != typeof(TimeFrameCandle))
					{
						this.AddWarningLog(LocalizedStrings.Str2296Params, series);
						continue;
					}

					var candleStorage = StorageRegistry.GetCandleMessageStorage(series.CandleType.ToCandleMessageType(), security.Security, series.Arg, _settings.Drive, _settings.StorageFormat);

					foreach (var date in allDates.Except(candleStorage.Dates))
					{
						if (!CanProcess())
							break;

						if (_settings.IgnoreWeekends && !security.IsTradeDate(date))
						{
							this.AddDebugLog(LocalizedStrings.WeekEndDate, date);
							continue;
						}

						this.AddInfoLog(LocalizedStrings.Str2298Params, series, date, security.Security.Id);

						bool isSuccess;
						var candles = ((BarChartTrader)Connector).GetHistoricalCandles(security.Security, series.CandleType, series.Arg, date, date.EndOfDay(), out isSuccess);

						if (isSuccess)
						{
							if (candles.Any())
								SaveCandles(security, candles);
							else
								this.AddDebugLog(LocalizedStrings.NoData);
						}
						else
							this.AddErrorLog(LocalizedStrings.Str2550);
					}
				}
			}

			if (!hasSecurities)
			{
				this.AddWarningLog(LocalizedStrings.Str2292);
				return TimeSpan.MaxValue;
			}

			if (CanProcess())
			{
				this.AddInfoLog(LocalizedStrings.Str2300);

				_settings.StartFrom = endDate;
				SaveSettings();
			}

			return _settings.Interval;
		}
	}
}