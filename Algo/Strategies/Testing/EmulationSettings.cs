#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Testing.Algo
File: EmulationSettings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies.Testing
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Testing;
	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// Emulation settings.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.SettingsKey)]
	[DescriptionLoc(LocalizedStrings.Str1408Key)]
	public class EmulationSettings : MarketEmulatorSettings
	{
		private DateTime _startTime = DateTime.Today.AddYears(-1);

		/// <summary>
		/// Date in history for starting the paper trading.
		/// </summary>
		[Browsable(false)]
		public DateTime StartTime
		{
			get => _startTime;
			set
			{
				_startTime = value;
				NotifyPropertyChanged(nameof(StartTime));
			}
		}

		private DateTime _stopTime = DateTime.Today;

		/// <summary>
		/// Date in history to stop the paper trading (date is included).
		/// </summary>
		[Browsable(false)]
		public DateTime StopTime
		{
			get => _stopTime;
			set
			{
				_stopTime = value;
				NotifyPropertyChanged(nameof(StopTime));
			}
		}

		#region Emulation

		private TimeSpan _marketTimeChangedInterval;

		/// <summary>
		/// Time change interval.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str175Key,
			Description = LocalizedStrings.Str1409Key,
			GroupName = LocalizedStrings.Str1174Key,
			Order = 100)]
		public TimeSpan MarketTimeChangedInterval
		{
			get => _marketTimeChangedInterval;
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();

				_marketTimeChangedInterval = value;
				NotifyPropertyChanged(nameof(MarketTimeChangedInterval));
			}
		}

		private TimeSpan? _unrealizedPnLInterval;

		/// <summary>
		/// Unrealized profit recalculation interval.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1410Key,
			Description = LocalizedStrings.Str1411Key,
			GroupName = LocalizedStrings.Str1174Key,
			Order = 101)]
		[DefaultValue(typeof(TimeSpan), "00:01:00")]
		public TimeSpan? UnrealizedPnLInterval
		{
			get => _unrealizedPnLInterval;
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();

				_unrealizedPnLInterval = value;
				NotifyPropertyChanged(nameof(UnrealizedPnLInterval));
			}
		}

		private EmulationMarketDataModes _tradeDataMode;

		/// <summary>
		/// What trades to use.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str985Key,
			Description = LocalizedStrings.Str1413Key,
			GroupName = LocalizedStrings.Str1174Key,
			Order = 102)]
		public EmulationMarketDataModes TradeDataMode
		{
			get => _tradeDataMode;
			set
			{
				_tradeDataMode = value;
				NotifyPropertyChanged(nameof(TradeDataMode));
			}
		}

		private EmulationMarketDataModes _depthDataMode;

		/// <summary>
		/// What market depths to use.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.MarketDepthsKey,
			Description = LocalizedStrings.Str1415Key,
			GroupName = LocalizedStrings.Str1174Key,
			Order = 103)]
		public EmulationMarketDataModes DepthDataMode
		{
			get => _depthDataMode;
			set
			{
				_depthDataMode = value;
				NotifyPropertyChanged(nameof(DepthDataMode));
			}
		}

		private EmulationMarketDataModes _orderLogDataMode;

		/// <summary>
		/// Use orders log.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.OrderLogKey,
			Description = LocalizedStrings.Str1417Key,
			GroupName = LocalizedStrings.Str1174Key,
			Order = 104)]
		public EmulationMarketDataModes OrderLogDataMode
		{
			get => _orderLogDataMode;
			set
			{
				_orderLogDataMode = value;
				NotifyPropertyChanged(nameof(OrderLogDataMode));
			}
		}

		private int _batchSize = 10;

		/// <summary>
		/// Number of simultaneously tested strategies.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1418Key,
			Description = LocalizedStrings.Str1419Key,
			GroupName = LocalizedStrings.Str1174Key,
			Order = 105)]
		public int BatchSize
		{
			get => _batchSize;
			set
			{
				_batchSize = value;
				NotifyPropertyChanged(nameof(BatchSize));
			}
		}

		private bool _checkTradableDates = true;

		/// <summary>
		/// Check loading dates are they tradable.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CheckDatesKey,
			Description = LocalizedStrings.CheckDatesDescKey,
			GroupName = LocalizedStrings.Str1174Key,
			Order = 106)]
		public bool CheckTradableDates
		{
			get => _checkTradableDates;
			set
			{
				_checkTradableDates = value;
				NotifyPropertyChanged(nameof(CheckTradableDates));
			}
		}

		#endregion

		#region Debug

		private LogLevels _logLevel;

		/// <summary>
		/// Logging level.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str12Key)]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str9Key,
			Description = LocalizedStrings.Str9Key + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str12Key,
			Order = 200)]
		public LogLevels LogLevel
		{
			get => _logLevel;
			set
			{
				_logLevel = value;
				NotifyPropertyChanged(nameof(LogLevel));
			}
		}

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="EmulationSettings"/>.
		/// </summary>
		public EmulationSettings()
		{
			MarketTimeChangedInterval = TimeSpan.FromMinutes(1);
			LogLevel = LogLevels.Info;
		}

		/// <summary>
		/// To save the state of paper trading parameters.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(StartTime), StartTime);
			storage.SetValue(nameof(StopTime), StopTime);
			storage.SetValue(nameof(OrderLogDataMode), OrderLogDataMode.To<string>());
			storage.SetValue(nameof(DepthDataMode), DepthDataMode.To<string>());
			storage.SetValue(nameof(MarketTimeChangedInterval), MarketTimeChangedInterval);
			storage.SetValue(nameof(UnrealizedPnLInterval), UnrealizedPnLInterval);
			storage.SetValue(nameof(LogLevel), LogLevel.To<string>());
			storage.SetValue(nameof(TradeDataMode), TradeDataMode.To<string>());
			storage.SetValue(nameof(BatchSize), BatchSize);
			storage.SetValue(nameof(CheckTradableDates), CheckTradableDates);
		}

		/// <summary>
		/// To load the state of paper trading parameters.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			StartTime = storage.GetValue(nameof(StartTime), StartTime);
			StopTime = storage.GetValue(nameof(StopTime), StopTime);
			OrderLogDataMode = storage.GetValue(nameof(OrderLogDataMode), OrderLogDataMode);
			DepthDataMode = storage.GetValue(nameof(DepthDataMode), DepthDataMode);
			MarketTimeChangedInterval = storage.GetValue(nameof(MarketTimeChangedInterval), MarketTimeChangedInterval);
			UnrealizedPnLInterval = storage.GetValue(nameof(UnrealizedPnLInterval), UnrealizedPnLInterval);
			LogLevel = storage.GetValue(nameof(LogLevel), LogLevel);
			TradeDataMode = storage.GetValue(nameof(TradeDataMode), TradeDataMode);
			BatchSize = storage.GetValue(nameof(BatchSize), BatchSize);
			CheckTradableDates = storage.GetValue(nameof(CheckTradableDates), CheckTradableDates);
		}
	}
}