#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Quandl.QuandlPublic
File: QuandlTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Quandl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;
	using System.IO;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2288ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/bb5fedcc-9226-448e-8bb9-42969fba227e.htm")]
	[Icon("quandl_logo.png")]
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
				CollectionHelper.TryAdd(ExtensionInfo, "CandleDayStep", 30);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3451Key)]
			[DescriptionLoc(LocalizedStrings.Str3451Key, true)]
			[PropertyOrder(0)]
			public SecureString AuthToken
			{
				get { return ExtensionInfo[nameof(AuthToken)].To<SecureString>(); }
				set { ExtensionInfo[nameof(AuthToken)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(1)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo[nameof(StartFrom)].To<DateTime>(); }
				set { ExtensionInfo[nameof(StartFrom)] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(2)]
			public int DayOffset
			{
				get { return ExtensionInfo[nameof(DayOffset)].To<int>(); }
				set { ExtensionInfo[nameof(DayOffset)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(3)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo[nameof(IgnoreWeekends)]; }
				set { ExtensionInfo[nameof(IgnoreWeekends)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.TemporaryFilesKey)]
			[DescriptionLoc(LocalizedStrings.TemporaryFilesKey, true)]
			[PropertyOrder(4)]
			public TempFiles UseTemporaryFiles
			{
				get { return ExtensionInfo[nameof(UseTemporaryFiles)].To<TempFiles>(); }
				set { ExtensionInfo[nameof(UseTemporaryFiles)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
			[DescriptionLoc(LocalizedStrings.CandleTimeIntervalKey)]
			[PropertyOrder(5)]
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

		private QuandlSecurityStorage _quandlSecurityStorage;

		public QuandlTask()
		{
			SupportedDataTypes = QuandlHistorySource
				.TimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.ToArray();
		}

		private QuandlSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_quandlSecurityStorage = new QuandlSecurityStorage(EntityRegistry);

			_settings = new QuandlSettings(settings);

			if (!_settings.IsDefault)
				return;

			_settings.DayOffset = 1;
			_settings.StartFrom = new DateTime(1980, 1, 1);
			_settings.Interval = TimeSpan.FromDays(1);
			_settings.IgnoreWeekends = true;
			_settings.AuthToken = new SecureString();
			_settings.UseTemporaryFiles = TempFiles.UseAndDelete;
			_settings.CandleDayStep = 30;
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

				foreach (var series in (allSecurity ?? security).GetCandleSeries())
				{
					if (!CanProcess())
						break;

					if (series.MessageType != typeof(TimeFrameCandleMessage))
					{
						this.AddWarningLog(LocalizedStrings.Str2296Params, series);
						continue;
					}

					var tf = (TimeSpan)series.Arg;

					var storage = StorageRegistry.GetCandleMessageStorage(series.MessageType, security.Security, tf, _settings.Drive, _settings.StorageFormat);
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

							if (_settings.UseTemporaryFiles == TempFiles.UseAndDelete)
								File.Delete(source.GetDumpFile(security.Security, currDate, till, typeof(TimeFrameCandleMessage), tf));
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