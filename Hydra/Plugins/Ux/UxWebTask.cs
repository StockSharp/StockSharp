#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Ux.UxPublic
File: UxWebTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Ux
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.History;
	using StockSharp.Algo.History.Russian;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/184da0f9-29db-4397-8497-1ed4c8f7ea0d.htm")]
	[Icon("ux_logo.png")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.History |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Ticks)]
	class UxWebTask : BaseHydraTask, ISecurityDownloader
	{
		private const string _sourceName = LocalizedStrings.Str2830Key;

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class UxWebSettings : HydraTaskSettings
		{
			public UxWebSettings(HydraTaskSettings settings)
				: base(settings)
			{
				CollectionHelper.TryAdd(ExtensionInfo, "CandleDayStep", 30);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo[nameof(StartFrom)].To<DateTime>(); }
				set { ExtensionInfo[nameof(StartFrom)] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int DayOffset
			{
				get { return ExtensionInfo[nameof(DayOffset)].To<int>(); }
				set { ExtensionInfo[nameof(DayOffset)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(2)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo[nameof(IgnoreWeekends)]; }
				set { ExtensionInfo[nameof(IgnoreWeekends)] = value; }
			}

			[CategoryLoc(_sourceName)]
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
		}

		private UxWebSettings _settings;

		public UxWebTask()
		{
			SupportedDataTypes = UxHistorySource
				.TimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.Concat(new[]
				{
					DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Tick),
				})
				.ToArray();
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new UxWebSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.DayOffset = 1;
			_settings.StartFrom = new DateTime(2001, 1, 1);
			_settings.Interval = TimeSpan.FromDays(1);
			_settings.IgnoreWeekends = true;
			_settings.CandleDayStep = 30;
		}

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override TimeSpan OnProcess()
		{
			var allSecurity = this.GetAllSecurity();

			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			IEnumerable<HydraTaskSecurity> selectedSecurities = (allSecurity != null
				? this.ToHydraSecurities(EntityRegistry.Securities.Filter(ExchangeBoard.Ux).Concat(EntityRegistry.Securities.Filter(ExchangeBoard.UxStock)))
				: Settings.Securities
					).ToArray();

			var source = new UxHistorySource();

			if (selectedSecurities.IsEmpty())
			{
				this.AddWarningLog(LocalizedStrings.Str2289);

				source.Refresh(EntityRegistry.Securities, new Security(), SaveSecurity, () => !CanProcess(false));

				selectedSecurities = this.ToHydraSecurities(EntityRegistry.Securities.Filter(ExchangeBoard.Ux).Concat(EntityRegistry.Securities.Filter(ExchangeBoard.UxStock)));
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

				#region LoadTicks
				if ((allSecurity ?? security).IsTicksEnabled())
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
							{
								this.AddDebugLog(LocalizedStrings.WeekEndDate, emptyDate);
								continue;
							}

							try
							{
								this.AddInfoLog(LocalizedStrings.Str2294Params, emptyDate, security.Security.Id);
								var trades = source.GetTicks(security.Security, emptyDate, emptyDate);

								if (trades.Any())
									SaveTicks(security, trades);
								else
									this.AddDebugLog(LocalizedStrings.NoData);
							}
							catch (Exception ex)
							{
								HandleError(new InvalidOperationException(LocalizedStrings.Str2295Params
									.Put(emptyDate, security.Security.Id), ex));
							}
						}
					}
				}
				else
					this.AddDebugLog(LocalizedStrings.MarketDataNotEnabled, security.Security.Id, typeof(Trade).Name);

				#endregion

				if (!CanProcess())
					break;

				#region LoadCandles
				foreach (var pair in (allSecurity ?? security).GetCandleSeries())
				{
					if (!CanProcess())
						break;

					if (pair.MessageType != typeof(TimeFrameCandleMessage))
					{
						this.AddWarningLog(LocalizedStrings.Str2296Params, pair);
						continue;
					}

					var tf = (TimeSpan)pair.Arg;

					var storage = StorageRegistry.GetCandleMessageStorage(pair.MessageType, security.Security, tf, _settings.Drive, _settings.StorageFormat);
					var emptyDates = allDates.Except(storage.Dates).ToArray();

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

						try
						{
							var till = currDate.AddDays(_settings.CandleDayStep - 1);
							this.AddInfoLog(LocalizedStrings.Str2298Params, tf, currDate, till, security.Security.Id);
							
							var candles = source.GetCandles(security.Security, tf, currDate, till);

							if (candles.Any())
								SaveCandles(security, candles);
							else
								this.AddDebugLog(LocalizedStrings.NoData);
						}
						catch (Exception ex)
						{
							HandleError(new InvalidOperationException(LocalizedStrings.Str2299Params
								.Put(tf, currDate, security.Security.Id), ex));
						}

						currDate = currDate.AddDays(_settings.CandleDayStep);
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
			new UxHistorySource().Refresh(storage, criteria, newSecurity, isCancelled);
		}
	}
}