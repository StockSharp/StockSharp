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
	/// Настройки эмуляции.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.SettingsKey)]
	[DescriptionLoc(LocalizedStrings.Str1408Key)]
	public class EmulationSettings : MarketEmulatorSettings
	{
		private DateTime _startTime = DateTime.Today.AddYears(-1);

		/// <summary>
		/// Дата в истории, с которой необходимо начать эмуляцию.
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
		/// Дата в истории, на которой необходимо закончить эмуляцию (дата включается).
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
		/// Интервал изменения времени.
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
		/// Интервал пересчета нереализованной прибыли.
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
		/// Какие использовать сделки.
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
		/// Какие использовать стаканы.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(30)]
		[DisplayNameLoc(LocalizedStrings.Str1414Key)]
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
		/// Использовать лог заявок.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(40)]
		[DisplayNameLoc(LocalizedStrings.Str1416Key)]
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
		/// Количество одновременно тестируемых стратегий.
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
		/// Уровень логирования.
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
		/// Создать <see cref="EmulationSettings"/>.
		/// </summary>
		public EmulationSettings()
		{
			MarketTimeChangedInterval = TimeSpan.FromMinutes(1);
			LogLevel = LogLevels.Info;
			UseCandlesTimeFrame = TimeSpan.FromMinutes(5);
		}

		/// <summary>
		/// Сохранить состояние параметров эмуляции.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
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
		/// Загрузить состояние параметров эмуляции.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
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