#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.BarChart.BarChartPublic
File: BarChartTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.BarChart
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Algo;
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
				CollectionHelper.TryAdd(ExtensionInfo, "CandleDayStep", 30);
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.Str2535Key)]
			[DescriptionLoc(LocalizedStrings.Str2536Key)]
			[PropertyOrder(0)]
			public bool IsRealTime
			{
				get { return ExtensionInfo[nameof(IsRealTime)].To<bool>(); }
				set { ExtensionInfo[nameof(IsRealTime)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo[nameof(Login)]; }
				set { ExtensionInfo[nameof(Login)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo[nameof(Password)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Password)] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo[nameof(StartFrom)].To<DateTime>(); }
				set { ExtensionInfo[nameof(StartFrom)] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int Offset
			{
				get { return ExtensionInfo[nameof(Offset)].To<int>(); }
				set { ExtensionInfo[nameof(Offset)] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(2)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo[nameof(IgnoreWeekends)]; }
				set { ExtensionInfo[nameof(IgnoreWeekends)] = value; }
			}

			[CategoryLoc(LocalizedStrings.HistoryKey)]
			[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
			[DescriptionLoc(LocalizedStrings.CandleTimeIntervalKey)]
			[PropertyOrder(4)]
			public int CandleDayStep
			{
				get { return ExtensionInfo[nameof(CandleDayStep)].To<int>(); }
				set
				{
					if (value < 1)
						throw new ArgumentOutOfRangeException();

					ExtensionInfo[nameof(CandleDayStep)] = value;
				}
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
			SupportedDataTypes = BarChartMessageAdapter
				.TimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.Concat(new[]
				{
					DataType.Create(typeof(QuoteChangeMessage), null),
					DataType.Create(typeof(Level1ChangeMessage), null),
				})
				.ToArray();
		}

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		public override HydraTaskSettings Settings => _settings;

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
			_settings.CandleDayStep = 30;
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
			var anyData = false;

			var hasSecurities = false;

			foreach (var security in GetWorkingSecurities())
			{
				hasSecurities = true;

				if (!CanProcess())
					break;

				if (security.IsLevel1Enabled())
				{
					anyData = true;

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

				foreach (var pair in security.GetCandleSeries())
				{
					anyData = true;

					if (!CanProcess())
						break;

					if (pair.MessageType != typeof(TimeFrameCandleMessage))
					{
						this.AddWarningLog(LocalizedStrings.Str2296Params, pair);
						continue;
					}

					var tf = (TimeSpan)pair.Arg;

					var candleStorage = StorageRegistry.GetCandleMessageStorage(pair.MessageType, security.Security, tf, _settings.Drive, _settings.StorageFormat);
					var emptyDates = allDates.Except(candleStorage.Dates).ToArray();

					if (emptyDates.IsEmpty())
					{
						this.AddInfoLog(LocalizedStrings.Str2297Params, tf, security.Security.Id);
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

						var till = currDate.AddDays(_settings.CandleDayStep - 1).EndOfDay();
						this.AddInfoLog(LocalizedStrings.Str2298Params, pair, currDate, till, security.Security.Id);

						bool isSuccess;
						var candles = ((BarChartTrader)Connector).GetHistoricalCandles(security.Security, pair.MessageType, tf, currDate, till, out isSuccess);

						if (isSuccess)
						{
							if (candles.Any())
								SaveCandles(security, candles);
							else
								this.AddDebugLog(LocalizedStrings.NoData);
						}
						else
							this.AddErrorLog(LocalizedStrings.Str2550);

						currDate = currDate.AddDays(_settings.CandleDayStep);
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
				if (anyData)
				{
					this.AddInfoLog(LocalizedStrings.Str2300);

					_settings.StartFrom = endDate;
					SaveSettings();
				}
				else
					this.AddWarningLog(LocalizedStrings.Str2913);
			}

			return _settings.Interval;
		}
	}
}