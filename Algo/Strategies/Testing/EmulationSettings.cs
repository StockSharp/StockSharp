#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Testing.Algo
File: EmulationSettings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies.Testing
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Testing;
	using StockSharp.Logging;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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
			get { return _startTime; }
			set
			{
				_startTime = value;
				NotifyPropertyChanged("StartTime");
			}
		}

		private DateTime _stopTime = DateTime.Today;

		/// <summary>
		/// Date in history to stop the paper trading (date is included).
		/// </summary>
		[Browsable(false)]
		public DateTime StopTime
		{
			get { return _stopTime; }
			set
			{
				_stopTime = value;
				NotifyPropertyChanged("StopTime");
			}
		}

		#region Эмуляция

		private TimeSpan _marketTimeChangedInterval;

		/// <summary>
		/// Time change interval.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(1)]
		[DisplayNameLoc(LocalizedStrings.Str175Key)]
		[DescriptionLoc(LocalizedStrings.Str1409Key)]
		public TimeSpan MarketTimeChangedInterval
		{
			get { return _marketTimeChangedInterval; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();

				_marketTimeChangedInterval = value;
				NotifyPropertyChanged("MarketTimeChangedInterval");
			}
		}

		private TimeSpan? _unrealizedPnLInterval;

		/// <summary>
		/// Unrealized profit recalculation interval.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(1)]
		[DisplayNameLoc(LocalizedStrings.Str1410Key)]
		[DescriptionLoc(LocalizedStrings.Str1411Key)]
		[DefaultValue(typeof(TimeSpan), "00:01:00")]
		public TimeSpan? UnrealizedPnLInterval
		{
			get { return _unrealizedPnLInterval; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();

				_unrealizedPnLInterval = value;
				NotifyPropertyChanged("UnrealizedPnLInterval");
			}
		}

		private EmulationMarketDataModes _tradeDataMode;

		/// <summary>
		/// What trades to use.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(25)]
		[DisplayNameLoc(LocalizedStrings.Str985Key)]
		[DescriptionLoc(LocalizedStrings.Str1413Key)]
		public EmulationMarketDataModes TradeDataMode
		{
			get { return _tradeDataMode; }
			set
			{
				_tradeDataMode = value;
				NotifyPropertyChanged("TradeMode");
			}
		}

		private EmulationMarketDataModes _depthDataMode;

		/// <summary>
		/// What market depths to use.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(30)]
		[DisplayNameLoc(LocalizedStrings.MarketDepthsKey)]
		[DescriptionLoc(LocalizedStrings.Str1415Key)]
		public EmulationMarketDataModes DepthDataMode
		{
			get { return _depthDataMode; }
			set
			{
				_depthDataMode = value;
				NotifyPropertyChanged("DepthMode");
			}
		}

		private EmulationMarketDataModes _orderLogDataMode;

		/// <summary>
		/// Use orders log.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(40)]
		[DisplayNameLoc(LocalizedStrings.OrderLogKey)]
		[DescriptionLoc(LocalizedStrings.Str1417Key)]
		public EmulationMarketDataModes OrderLogDataMode
		{
			get { return _orderLogDataMode; }
			set
			{
				_orderLogDataMode = value;
				NotifyPropertyChanged("OrderLogMode");
			}
		}

		private int _batchSize = 10;

		/// <summary>
		/// Number of simultaneously tested strategies.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(50)]
		[DisplayNameLoc(LocalizedStrings.Str1418Key)]
		[DescriptionLoc(LocalizedStrings.Str1419Key)]
		public int BatchSize
		{
			get { return _batchSize; }
			set
			{
				_batchSize = value;
				NotifyPropertyChanged("BatchSize");
			}
		}

		#endregion

		#region Отладка

		private LogLevels _logLevel;

		/// <summary>
		/// Logging level.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str12Key)]
		[PropertyOrder(1)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str9Key, true)]
		public LogLevels LogLevel
		{
			get { return _logLevel; }
			set
			{
				_logLevel = value;
				NotifyPropertyChanged("LogLevel");
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

			storage.SetValue("StartTime", StartTime);
			storage.SetValue("StopTime", StopTime);
			storage.SetValue("OrderLogMode", OrderLogDataMode.To<string>());
			storage.SetValue("DepthMode", DepthDataMode.To<string>());
			storage.SetValue("MarketTimeChangedInterval", MarketTimeChangedInterval);
			storage.SetValue("UnrealizedPnLInterval", UnrealizedPnLInterval);
			storage.SetValue("LogLevel", LogLevel.To<string>());
			storage.SetValue("TradeMode", TradeDataMode.To<string>());
			storage.SetValue("BatchSize", BatchSize);
		}

		/// <summary>
		/// To load the state of paper trading parameters.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			StartTime = storage.GetValue("StartTime", StartTime);
			StopTime = storage.GetValue("StopTime", StopTime);
			OrderLogDataMode = storage.GetValue("OrderLogMode", OrderLogDataMode);
			DepthDataMode = storage.GetValue("DepthMode", DepthDataMode);
			MarketTimeChangedInterval = storage.GetValue("MarketTimeChangedInterval", MarketTimeChangedInterval);
			UnrealizedPnLInterval = storage.GetValue("UnrealizedPnLInterval", UnrealizedPnLInterval);
			LogLevel = storage.GetValue("LogLevel", LogLevel);
			TradeDataMode = storage.GetValue("TradeMode", TradeDataMode);
			BatchSize = storage.GetValue("BatchSize", BatchSize);
		}
	}
}