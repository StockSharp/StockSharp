#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Google.GooglePublic
File: GoogleTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Google
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.History;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2288ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/0166d686-9fb0-4e78-8578-f5b706ad6a07.htm")]
	[Icon("google_logo.png")]
	[TaskCategory(TaskCategories.America | TaskCategories.History |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Candles)]
	class GoogleTask : BaseHydraTask
    {
		private const string _sourceName = "Google";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class GoogleSettings : HydraTaskSettings
		{
			public GoogleSettings(HydraTaskSettings settings)
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

		private GoogleSettings _settings;

		public GoogleTask()
		{
			SupportedDataTypes = GoogleHistorySource
				.TimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.ToArray();
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new GoogleSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.DayOffset = 1;
			_settings.StartFrom = new DateTime(2000, 1, 1);
			_settings.Interval = TimeSpan.FromDays(1);
			_settings.IgnoreWeekends = true;
			_settings.CandleDayStep = 30;
		}

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override TimeSpan OnProcess()
		{
			var allSecurity = this.GetAllSecurity();

			var source = new GoogleHistorySource();

			var startDate = _settings.StartFrom;
			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.DayOffset);

			var allDates = startDate.Range(endDate, TimeSpan.FromDays(1)).ToArray();

			var hasSecurities = false;

			foreach (var security in GetWorkingSecurities())
			{
				hasSecurities = true;

				if (!CanProcess())
					break;

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
							var till = currDate + TimeSpan.FromDays(_settings.CandleDayStep).Max(tf);
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
			}

			if (!hasSecurities)
			{
				this.AddWarningLog(LocalizedStrings.Str2292);
				return TimeSpan.MaxValue;
			}

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}
    }
}