namespace StockSharp.Hydra.DukasCopy
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History;
	using StockSharp.Algo.History.Forex;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2288ParamsKey, _sourceName)]
	[TaskDoc("http://stocksharp.com/doc/html/bba0059b-526b-406c-ba6e-a80d6c9e4ae0.htm")]
	[TaskIcon("dukas_logo.png")]
	[TaskCategory(TaskCategories.Forex | TaskCategories.History |
		TaskCategories.Free | TaskCategories.Level1 | TaskCategories.Candles)]
	class DukasCopyTask : BaseHydraTask, ISecurityDownloader
    {
		private const string _sourceName = "DukasCopy";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class DukasCopySettings : HydraTaskSettings
		{
			public DukasCopySettings(HydraTaskSettings settings)
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
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str273Key)]
			[DescriptionLoc(LocalizedStrings.Str3789Key)]
			[PropertyOrder(1)]
			public Sides Side
			{
				get { return (Sides)ExtensionInfo["Side"].To<int>(); }
				set { ExtensionInfo["Side"] = (int)value; }
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
			[DisplayNameLoc(LocalizedStrings.TemporaryFilesKey)]
			[DescriptionLoc(LocalizedStrings.TemporaryFilesKey, true)]
			[PropertyOrder(3)]
			public TempFiles UseTemporaryFiles
			{
				get { return ExtensionInfo["UseTemporaryFiles"].To<TempFiles>(); }
				set { ExtensionInfo["UseTemporaryFiles"] = value.To<string>(); }
			}
		}

		private DukasCopySettings _settings;

		public DukasCopyTask()
		{
			_supportedCandleSeries = DukasCopySource.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new DukasCopySettings(settings);

			if (settings.IsDefault)
			{
				_settings.DayOffset = 1;
				_settings.StartFrom = new DateTime(2006, 1, 1);
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.Side = Sides.Buy;
				_settings.UseTemporaryFiles = TempFiles.UseAndDelete;
			}
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly Type[] _supportedMarketDataTypes = { typeof(Level1ChangeMessage), typeof(Candle) };

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
			var allSecurity = this.GetAllSecurity();

			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			IEnumerable<HydraTaskSecurity> selectedSecurities = (allSecurity != null
				? this.ToHydraSecurities(EntityRegistry.Securities.Filter(ExchangeBoard.DukasCopy))
				: Settings.Securities
					).ToArray();

			var source = new DukasCopySource();

			if (_settings.UseTemporaryFiles != TempFiles.NotUse)
				source.DumpFolder = GetTempPath();

			if (selectedSecurities.IsEmpty())
			{
				this.AddWarningLog(LocalizedStrings.Str2289);

				source.Refresh(EntityRegistry.Securities, new Security(), SaveSecurity, () => !CanProcess(false));
			
				selectedSecurities = this.ToHydraSecurities(EntityRegistry.Securities.Filter(ExchangeBoard.DukasCopy));
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
				if ((allSecurity ?? security).MarketDataTypesSet.Contains(typeof(Level1ChangeMessage)))
				{
					var storage = StorageRegistry.GetLevel1MessageStorage(security.Security, _settings.Drive, _settings.StorageFormat);
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

							try
							{
								this.AddInfoLog(LocalizedStrings.Str2294Params, emptyDate, security.Security.Id);
								var ticks = source.LoadTickMessages(security.Security, emptyDate);

								if (ticks.Any())
									SaveLevel1Changes(security, ticks);
								else
									this.AddDebugLog(LocalizedStrings.NoData);

								if (_settings.UseTemporaryFiles == TempFiles.UseAndDelete)
									File.Delete(source.GetDumpFile(security.Security, emptyDate, emptyDate, typeof(Level1ChangeMessage), null));
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
					this.AddDebugLog(LocalizedStrings.MarketDataNotEnabled, security.Security.Id, typeof(Level1ChangeMessage).Name);

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

						try
						{
							this.AddInfoLog(LocalizedStrings.Str2298Params, series.Arg, emptyDate, security.Security.Id);
							var candles = source.LoadCandles(security.Security, (TimeSpan)series.Arg, emptyDate, _settings.Side);
							
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
				#endregion
			}

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}

		void ISecurityDownloader.Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			new DukasCopySource().Refresh(EntityRegistry.Securities, criteria, newSecurity, isCancelled);
		}
    }
}