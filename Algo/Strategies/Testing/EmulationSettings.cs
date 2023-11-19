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
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Testing;
	using StockSharp.Logging;
	using StockSharp.Localization;
	using StockSharp.Algo.Commissions;

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
		[Obsolete("Use specified parameters in Start methods.")]
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
		[Obsolete("Use specified parameters in Start methods.")]
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
			Name = LocalizedStrings.IntervalKey,
			Description = LocalizedStrings.Str1409Key,
			GroupName = LocalizedStrings.BacktestKey,
			Order = 100)]
		public TimeSpan MarketTimeChangedInterval
		{
			get => _marketTimeChangedInterval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

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
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

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

		private LogLevels _logLevel = LogLevels.Info;

		/// <summary>
		/// Logging level.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.LogLevelKey,
			Description = LocalizedStrings.LogLevelKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str12Key,
			Order = 300)]
		[Obsolete("Use external storage.")]
		public LogLevels LogLevel
		{
			get => _logLevel;
			set
			{
				_logLevel = value;
				NotifyPropertyChanged();
			}
		}

		private IEnumerable<ICommissionRule> _commissionRules = Enumerable.Empty<ICommissionRule>();

		/// <summary>
		/// Commission rules.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			GroupName = LocalizedStrings.BacktestKey,
			Name = LocalizedStrings.CommissionKey,
			Description = LocalizedStrings.CommissionDescKey,
			Order = 110)]
		public IEnumerable<ICommissionRule> CommissionRules
		{
			get => _commissionRules;
			set
			{
				_commissionRules = value ?? throw new ArgumentNullException(nameof(value));
				NotifyChanged();
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
#pragma warning disable CS0618 // Type or member is obsolete
				.Set(nameof(StartTime), StartTime)
				.Set(nameof(StopTime), StopTime)
				.Set(nameof(LogLevel), LogLevel.To<string>())
#pragma warning restore CS0618 // Type or member is obsolete
				.Set(nameof(OrderLogDataMode), OrderLogDataMode.To<string>())
				.Set(nameof(DepthDataMode), DepthDataMode.To<string>())
				.Set(nameof(MarketTimeChangedInterval), MarketTimeChangedInterval)
				.Set(nameof(UnrealizedPnLInterval), UnrealizedPnLInterval)
				.Set(nameof(TradeDataMode), TradeDataMode.To<string>())
				.Set(nameof(CheckTradableDates), CheckTradableDates)
				.Set(nameof(CommissionRules), CommissionRules.Select(c => c.SaveEntire(false)).ToArray())
			;
		}

		/// <summary>
		/// To load the state of paper trading parameters.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

#pragma warning disable CS0618 // Type or member is obsolete
			StartTime = storage.GetValue(nameof(StartTime), StartTime);
			StopTime = storage.GetValue(nameof(StopTime), StopTime);
			LogLevel = storage.GetValue(nameof(LogLevel), LogLevel);
#pragma warning restore CS0618 // Type or member is obsolete
			OrderLogDataMode = storage.GetValue(nameof(OrderLogDataMode), OrderLogDataMode);
			DepthDataMode = storage.GetValue(nameof(DepthDataMode), DepthDataMode);
			MarketTimeChangedInterval = storage.GetValue(nameof(MarketTimeChangedInterval), MarketTimeChangedInterval);
			UnrealizedPnLInterval = storage.GetValue(nameof(UnrealizedPnLInterval), UnrealizedPnLInterval);
			TradeDataMode = storage.GetValue(nameof(TradeDataMode), TradeDataMode);
			CheckTradableDates = storage.GetValue(nameof(CheckTradableDates), CheckTradableDates);

			var commRules = storage.GetValue<SettingsStorage[]>(nameof(CommissionRules));
			if (commRules is not null)
				CommissionRules = commRules.Select(i => i.LoadEntire<ICommissionRule>()).ToArray();
		}
	}
}