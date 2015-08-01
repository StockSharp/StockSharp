namespace StockSharp.Hydra.Converters
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.PropertyGrid;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(LocalizedStrings.Str3131Key)]
	class ConvertTask : BaseHydraTask
	{
		private enum ConvertModes
		{
			[EnumDisplayNameLoc(LocalizedStrings.Str3773Key)]
			OrderLogToTicks,

			[EnumDisplayNameLoc(LocalizedStrings.Str3774Key)]
			OrderLogToDepths,

			[EnumDisplayNameLoc(LocalizedStrings.Str3775Key)]
			OrderLogToCandles,

			[EnumDisplayNameLoc(LocalizedStrings.Str3776Key)]
			TicksToCandles,

			[EnumDisplayNameLoc(LocalizedStrings.Str3777Key)]
			DepthsToCandles,
		}

		[TaskSettingsDisplayName(LocalizedStrings.Str3131Key, true)]
		//[CategoryOrder(_sourceName, 0)]
		private sealed class ConvertTaskSettings : HydraTaskSettings
		{
			public ConvertTaskSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("DestinationStorageFormat", StorageFormats.Binary.To<string>());
			}

			[CategoryLoc(LocalizedStrings.Str3131Key)]
			[DisplayNameLoc(LocalizedStrings.Str3131Key)]
			[DescriptionLoc(LocalizedStrings.Str3131Key, true)]
			[PropertyOrder(0)]
			public ConvertModes ConvertMode
			{
				get { return ExtensionInfo["ConvertMode"].To<ConvertModes>(); }
				set { ExtensionInfo["ConvertMode"] = value.To<string>(); }
			}

			/// <summary>
			/// Формат данных.
			/// </summary>
			[CategoryLoc(LocalizedStrings.Str3131Key)]
			[DisplayNameLoc(LocalizedStrings.Str2239Key)]
			[DescriptionLoc(LocalizedStrings.Str2240Key)]
			[PropertyOrder(1)]
			[Ignore]
			public StorageFormats DestinationStorageFormat
			{
				get { return ExtensionInfo["DestinationStorageFormat"].To<StorageFormats>(); }
				set { ExtensionInfo["DestinationStorageFormat"] = value.To<string>(); }
			}

			[CategoryLoc(LocalizedStrings.Str3131Key)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str3779Key)]
			[PropertyOrder(1)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[CategoryLoc(LocalizedStrings.Str3131Key)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str3778Key)]
			[PropertyOrder(2)]
			public int Offset
			{
				get { return ExtensionInfo["Offset"].To<int>(); }
				set { ExtensionInfo["Offset"] = value; }
			}

			[CategoryLoc(LocalizedStrings.CandlesKey)]
			[DisplayNameLoc(LocalizedStrings.Str225Key)]
			[DescriptionLoc(LocalizedStrings.Str3761Key)]
			[PropertyOrder(0)]
			[Editor(typeof(CandleSettingsEditor), typeof(CandleSettingsEditor))]
			public CandleSeries CandleSettings
			{
				get { return (CandleSeries)ExtensionInfo["CandleSettings"]; }
				set { ExtensionInfo["CandleSettings"] = value; }
			}

			[CategoryLoc(LocalizedStrings.MarketDepthsKey)]
			[DisplayNameLoc(LocalizedStrings.Str175Key)]
			[DescriptionLoc(LocalizedStrings.Str3781Key)]
			[PropertyOrder(0)]
			public TimeSpan MarketDepthInterval
			{
				get { return (TimeSpan)ExtensionInfo["MarketDepthInterval"]; }
				set { ExtensionInfo["MarketDepthInterval"] = value; }
			}

			[CategoryLoc(LocalizedStrings.MarketDepthsKey)]
			[DisplayNameLoc(LocalizedStrings.Str1660Key)]
			[DescriptionLoc(LocalizedStrings.Str3782Key)]
			[PropertyOrder(1)]
			public int MarketDepthMaxDepth
			{
				get { return (int)ExtensionInfo["MarketDepthMaxDepth"]; }
				set { ExtensionInfo["MarketDepthMaxDepth"] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3131Key)]
			[DisplayNameLoc(LocalizedStrings.Str3783Key)]
			[DescriptionLoc(LocalizedStrings.Str3784Key)]
			[PropertyOrder(2)]
			[Editor(typeof(DriveComboBoxEditor), typeof(DriveComboBoxEditor))]
			public IMarketDataDrive DestinationDrive
			{
				get
				{
					return DriveCache.Instance.GetDrive((string)ExtensionInfo.TryGetValue("DestinationDrive") ?? string.Empty);
				}
				set
				{
					ExtensionInfo["DestinationDrive"] = value == null ? null : value.Path;
				}
			}

			public override HydraTaskSettings Clone()
			{
				var clone = (ConvertTaskSettings)base.Clone();
				clone.CandleSettings = CandleSettings.Clone();
				return clone;
			}
		}

		public override string Description
		{
			get { return LocalizedStrings.Str3785; }
		}

		public override Uri Icon
		{
			get { return "convert_logo.png".GetResourceUrl(GetType()); }
		}

		public override TaskTypes Type
		{
			get { return TaskTypes.Converter; }
		}

		private ConvertTaskSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new ConvertTaskSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Offset = 1;
				_settings.CandleSettings = new CandleSeries { CandleType = typeof(TimeFrameCandle), Arg = TimeSpan.FromMinutes(1) };
				_settings.StartFrom = DateTime.Today.Subtract(TimeSpan.FromDays(30));
				_settings.ConvertMode = ConvertModes.TicksToCandles;
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.MarketDepthInterval = TimeSpan.FromMilliseconds(10);
				_settings.MarketDepthMaxDepth = 50;
				_settings.DestinationDrive = null;
				_settings.DestinationStorageFormat = StorageFormats.Binary;
			}
		}

		protected override TimeSpan OnProcess()
		{
			var hasSecurities = false;

			this.AddInfoLog(LocalizedStrings.Str2306Params.Put(_settings.StartFrom));

			foreach (var security in GetWorkingSecurities())
			{
				hasSecurities = true;

				if (!CanProcess())
					break;

				//this.AddInfoLog("Обработка инструмента {0}. Конвертация {1}.".Put(security.Security.Id, mode));

				IMarketDataStorage fromStorage;
				IMarketDataStorage toStorage;

				switch (_settings.ConvertMode)
				{
					case ConvertModes.OrderLogToTicks:
						fromStorage = StorageRegistry.GetOrderLogMessageStorage(security.Security, _settings.Drive, _settings.StorageFormat);
						toStorage = StorageRegistry.GetTickMessageStorage(security.Security, _settings.DestinationDrive, _settings.DestinationStorageFormat);
						break;
					case ConvertModes.OrderLogToDepths:
						fromStorage = StorageRegistry.GetOrderLogMessageStorage(security.Security, _settings.Drive, _settings.StorageFormat);
						toStorage = StorageRegistry.GetQuoteMessageStorage(security.Security, _settings.DestinationDrive, _settings.DestinationStorageFormat);
						break;
					case ConvertModes.OrderLogToCandles:
						fromStorage = StorageRegistry.GetOrderLogMessageStorage(security.Security, _settings.Drive, _settings.StorageFormat);
						toStorage = StorageRegistry.GetCandleMessageStorage(_settings.CandleSettings.CandleType.ToCandleMessageType(), security.Security, _settings.CandleSettings.Arg, _settings.DestinationDrive, _settings.DestinationStorageFormat);
						break;
					case ConvertModes.TicksToCandles:
						fromStorage = StorageRegistry.GetTickMessageStorage(security.Security, _settings.Drive, _settings.StorageFormat);
						toStorage = StorageRegistry.GetCandleMessageStorage(_settings.CandleSettings.CandleType.ToCandleMessageType(), security.Security, _settings.CandleSettings.Arg, _settings.DestinationDrive, _settings.DestinationStorageFormat);
						break;
					case ConvertModes.DepthsToCandles:
						fromStorage = StorageRegistry.GetQuoteMessageStorage(security.Security, _settings.Drive, _settings.StorageFormat);
						toStorage = StorageRegistry.GetCandleMessageStorage(_settings.CandleSettings.CandleType.ToCandleMessageType(), security.Security, _settings.CandleSettings.Arg, _settings.DestinationDrive, _settings.DestinationStorageFormat);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				var from = fromStorage.GetFromDate();
				var to = fromStorage.GetToDate();

				if (from == null || to == null)
				{
					//this.AddInfoLog("Нет данных для конвертации.");
					continue;
				}

				from = _settings.StartFrom.Max(from.Value);
				to = (DateTime.Today - TimeSpan.FromDays(_settings.Offset)).Min(to.Value);

				foreach (var date in from.Value.Range(to.Value, TimeSpan.FromDays(1)).Except(toStorage.Dates))
				{
					if (!CanProcess())
						break;

					this.AddInfoLog(LocalizedStrings.Str3786Params.Put(security.Security.Id, _settings.ConvertMode, date));

					try
					{
						switch (_settings.ConvertMode)
						{
							case ConvertModes.OrderLogToTicks:
							{
								var ticks = ((IMarketDataStorage<ExecutionMessage>)fromStorage)
									.Load(date)
									.ToTicks();

								toStorage.Save(ticks);
								RaiseDataLoaded(security.Security, typeof(ExecutionMessage), ExecutionTypes.Tick, date, ticks.Count);
								break;
							}
							case ConvertModes.OrderLogToDepths:
							{
								var depths = ((IMarketDataStorage<ExecutionMessage>)fromStorage)
									.Load(date)
									.ToMarketDepths(_settings.MarketDepthInterval, _settings.MarketDepthMaxDepth);

								toStorage.Save(depths);
								RaiseDataLoaded(security.Security, typeof(QuoteChangeMessage), null, date, depths.Count);
								break;
							}
							case ConvertModes.OrderLogToCandles:
							{
								var candles = ((IMarketDataStorage<ExecutionMessage>)fromStorage)
									.Load(date)
									.ToTicks()
									.ToCandles(new CandleSeries(_settings.CandleSettings.CandleType, security.Security, _settings.CandleSettings.Arg));

								toStorage.Save(candles);
								RaiseDataLoaded(security.Security, _settings.CandleSettings.CandleType, _settings.CandleSettings.Arg, date, candles.Count);
								break;
							}
							case ConvertModes.TicksToCandles:
							{
								var candles = ((IMarketDataStorage<ExecutionMessage>)fromStorage)
									.Load(date)
									.ToCandles(new CandleSeries(_settings.CandleSettings.CandleType, security.Security, _settings.CandleSettings.Arg));

								toStorage.Save(candles);
								RaiseDataLoaded(security.Security, _settings.CandleSettings.CandleType, _settings.CandleSettings.Arg, date, candles.Count);
								break;
							}
							case ConvertModes.DepthsToCandles:
							{
								var candles = ((IMarketDataStorage<QuoteChangeMessage>)fromStorage)
									.Load(date)
									.ToCandles(new CandleSeries(_settings.CandleSettings.CandleType, security.Security, _settings.CandleSettings.Arg));

								toStorage.Save(candles);
								RaiseDataLoaded(security.Security, _settings.CandleSettings.CandleType, _settings.CandleSettings.Arg, date, candles.Count);
								break;
							}
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
					catch (Exception ex)
					{
						HandleError(ex);
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