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
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SettingsKey,
		Description = LocalizedStrings.Str1408Key)]
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
				NotifyPropertyChanged();
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
				NotifyPropertyChanged();
			}
		}

		private TimeSpan _marketTimeChangedInterval = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Time change interval. <see cref="TimeSpan.Zero"/> means interval disabled.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str175Key,
			Description = LocalizedStrings.Str1409Key,
			GroupName = LocalizedStrings.BacktestKey,
			Order = 100)]
		public TimeSpan MarketTimeChangedInterval
		{
			get => _marketTimeChangedInterval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_marketTimeChangedInterval = value;
				NotifyPropertyChanged();
			}
		}

		private TimeSpan _unrealizedPnLInterval = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Unrealized profit recalculation interval. <see cref="TimeSpan.Zero"/> means interval disabled.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1410Key,
			Description = LocalizedStrings.Str1411Key,
			GroupName = LocalizedStrings.BacktestKey,
			Order = 101)]
		public TimeSpan UnrealizedPnLInterval
		{
			get => _unrealizedPnLInterval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_unrealizedPnLInterval = value;
				NotifyPropertyChanged();
			}
		}

		private EmulationMarketDataModes _tradeDataMode;

		/// <summary>
		/// What trades to use.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TradesKey,
			Description = LocalizedStrings.Str1413Key,
			GroupName = LocalizedStrings.BacktestKey,
			Order = 102)]
		public EmulationMarketDataModes TradeDataMode
		{
			get => _tradeDataMode;
			set
			{
				_tradeDataMode = value;
				NotifyPropertyChanged();
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
			GroupName = LocalizedStrings.BacktestKey,
			Order = 103)]
		public EmulationMarketDataModes DepthDataMode
		{
			get => _depthDataMode;
			set
			{
				_depthDataMode = value;
				NotifyPropertyChanged();
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
			GroupName = LocalizedStrings.BacktestKey,
			Order = 104)]
		public EmulationMarketDataModes OrderLogDataMode
		{
			get => _orderLogDataMode;
			set
			{
				_orderLogDataMode = value;
				NotifyPropertyChanged();
			}
		}

		private bool _checkTradableDates;

		/// <summary>
		/// Check loading dates are they tradable.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CheckDatesKey,
			Description = LocalizedStrings.CheckDatesDescKey,
			GroupName = LocalizedStrings.BacktestKey,
			Order = 106)]
		public bool CheckTradableDates
		{
			get => _checkTradableDates;
			set
			{
				_checkTradableDates = value;
				NotifyPropertyChanged();
			}
		}

		private int _batchSize = Environment.ProcessorCount * 2;

		/// <summary>
		/// Number of simultaneously tested strategies.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ParallelKey,
			Description = LocalizedStrings.Str1419Key,
			GroupName = LocalizedStrings.OptimizationKey,
			Order = 200)]
		public int BatchSize
		{
			get => _batchSize;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_batchSize = value;
				NotifyPropertyChanged();
			}
		}

		private int _maxIterations;

		/// <summary>
		/// Maximum possible iterations count. Zero means the option is ignored.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IterationsKey,
			Description = LocalizedStrings.MaxIterationsKey,
			GroupName = LocalizedStrings.OptimizationKey,
			Order = 201)]
		public int MaxIterations
		{
			get => _maxIterations;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_maxIterations = value;
				NotifyPropertyChanged();
			}
		}

		private LogLevels _logLevel = LogLevels.Info;

		/// <summary>
		/// Logging level.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str9Key,
			Description = LocalizedStrings.Str9Key + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str12Key,
			Order = 300)]
		public LogLevels LogLevel
		{
			get => _logLevel;
			set
			{
				_logLevel = value;
				NotifyPropertyChanged();
			}
		}

		/// <summary>
		/// To save the state of paper trading parameters.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage
				.Set(nameof(StartTime), StartTime)
				.Set(nameof(StopTime), StopTime)
				.Set(nameof(OrderLogDataMode), OrderLogDataMode.To<string>())
				.Set(nameof(DepthDataMode), DepthDataMode.To<string>())
				.Set(nameof(MarketTimeChangedInterval), MarketTimeChangedInterval)
				.Set(nameof(UnrealizedPnLInterval), UnrealizedPnLInterval)
				.Set(nameof(TradeDataMode), TradeDataMode.To<string>())
				.Set(nameof(CheckTradableDates), CheckTradableDates)
				.Set(nameof(BatchSize), BatchSize)
				.Set(nameof(MaxIterations), MaxIterations)
				.Set(nameof(LogLevel), LogLevel.To<string>())
			;
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
			TradeDataMode = storage.GetValue(nameof(TradeDataMode), TradeDataMode);
			CheckTradableDates = storage.GetValue(nameof(CheckTradableDates), CheckTradableDates);
			BatchSize = storage.GetValue(nameof(BatchSize), BatchSize);
			MaxIterations = storage.GetValue(nameof(MaxIterations), MaxIterations);
			LogLevel = storage.GetValue(nameof(LogLevel), LogLevel);
		}
	}
}