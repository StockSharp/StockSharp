#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: MarketEmulatorSettings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;
	using System.ComponentModel;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Settings of exchange emulator.
	/// </summary>
	public class MarketEmulatorSettings : NotifiableObject, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MarketEmulatorSettings"/>.
		/// </summary>
		public MarketEmulatorSettings()
		{
			DepthExpirationTime = TimeSpan.FromDays(1);
			MatchOnTouch = true;
			IsSupportAtomicReRegister = true;
		}

		private bool _matchOnTouch;

		/// <summary>
		/// At emulation of clearing by trades, to perform clearing of orders, when trade price touches the order price (is equal to order price), rather than only when the trade price is better than order price. Is On by default (optimistic scenario).
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(50)]
		[DisplayNameLoc(LocalizedStrings.Str1176Key)]
		[DescriptionLoc(LocalizedStrings.Str1177Key)]
		public bool MatchOnTouch
		{
			get { return _matchOnTouch; }
			set
			{
				if (_matchOnTouch == value)
					return;

				_matchOnTouch = value;
				NotifyChanged("MatchOnTouch");
			}
		}

		private TimeSpan _depthExpirationTime;

		/// <summary>
		/// The maximal time, during which the order book is in the emulator, If no renewal during this time, the order book is deleted, This feature may be used to remove old order books if the are holes in data. By default is equal to 1 day.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(200)]
		[DisplayNameLoc(LocalizedStrings.Str1178Key)]
		[DescriptionLoc(LocalizedStrings.Str1179Key)]
		public TimeSpan DepthExpirationTime
		{
			get { return _depthExpirationTime; }
			set
			{
				if (_depthExpirationTime == value)
					return;

				_depthExpirationTime = value;
				NotifyChanged("DepthExpirationTime");
			}
		}

		private double _failing;

		/// <summary>
		/// The percentage value of new orders registration error. The value may be from 0 (not a single error) to 100. By default is Off.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(60)]
		[DisplayNameLoc(LocalizedStrings.Str1180Key)]
		[DescriptionLoc(LocalizedStrings.Str1181Key)]
		public double Failing
		{
			get { return _failing; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1182);

				if (value > 100)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1183);

				_failing = value;
				NotifyChanged("Failing");
			}
		}

		private TimeSpan _latency;

		/// <summary>
		/// The minimal value of the registered orders delay. By default, it is <see cref="TimeSpan.Zero"/>, which means instant adoption of registered orders by  exchange.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(20)]
		[DisplayNameLoc(LocalizedStrings.Str161Key)]
		[DescriptionLoc(LocalizedStrings.Str1184Key)]
		public TimeSpan Latency
		{
			get { return _latency; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1185);

				_latency = value;
				NotifyChanged("Latency");
			}
		}

		private bool _isSupportAtomicReRegister;

		/// <summary>
		/// Gets a value indicating whether the re-registration orders as a single transaction. By default is enabled.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(30)]
		[DisplayName("MOVE")]
		[DescriptionLoc(LocalizedStrings.Str60Key)]
		public bool IsSupportAtomicReRegister
		{
			get { return _isSupportAtomicReRegister; }
			set
			{
				_isSupportAtomicReRegister = value;
				NotifyChanged("_isSupportAtomicReRegister");
			}
		}

		private TimeSpan _bufferTime;

		/// <summary>
		/// Responses shall be sent in intervals by whole package. The network delay and bufferized operation of exchange are emulated. The default is 0 ms.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(80)]
		[DisplayNameLoc(LocalizedStrings.Str1186Key)]
		[DescriptionLoc(LocalizedStrings.Str1187Key)]
		public TimeSpan BufferTime
		{
			get { return _bufferTime; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str940);

				_bufferTime = value;
				NotifyChanged("BufferTime");
			}
		}

		//private TimeSpan? _useCandlesTimeFrame;

		///// <summary>
		///// Использовать свечи с заданным тайм-фреймом. Если тайм-фрейм равен <see langword="null"/>, свечи не используются.
		///// </summary>
		//[CategoryLoc(LocalizedStrings.Str1174Key)]
		//[PropertyOrder(10)]
		//[DisplayNameLoc(LocalizedStrings.CandlesKey)]
		//[DescriptionLoc(LocalizedStrings.Str1188Key)]
		//[Nullable]
		//[DefaultValue(typeof(TimeSpan), "00:05:00")]
		//public TimeSpan? UseCandlesTimeFrame
		//{
		//	get { return _useCandlesTimeFrame; }
		//	set
		//	{
		//		if (value <= TimeSpan.Zero)
		//			throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1189);

		//		_useCandlesTimeFrame = value;
		//		NotifyChanged("UseCandlesTimeFrame");
		//	}
		//}

		private long _initialOrderId;

		/// <summary>
		/// The number, starting at which the emulator will generate identifiers for orders <see cref="Order.Id"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(70)]
		[DisplayNameLoc(LocalizedStrings.Str1190Key)]
		[DescriptionLoc(LocalizedStrings.Str1191Key)]
		public long InitialOrderId
		{
			get { return _initialOrderId; }
			set
			{
				_initialOrderId = value;
				NotifyChanged("InitialOrderId");
			}
		}

		private long _initialTradeId;

		/// <summary>
		/// The number, starting at which the emulator will generate identifiers fir trades <see cref="Trade.Id"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(80)]
		[DisplayNameLoc(LocalizedStrings.Str1192Key)]
		[DescriptionLoc(LocalizedStrings.Str1193Key)]
		public long InitialTradeId
		{
			get { return _initialTradeId; }
			set
			{
				_initialTradeId = value;
				NotifyChanged("InitialTradeId");
			}
		}

		private long _initialTransactionId;

		/// <summary>
		/// The number, starting at which the emulator will generate numbers for order trades <see cref="Order.TransactionId"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(80)]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.Str1194Key)]
		public long InitialTransactionId
		{
			get { return _initialTransactionId; }
			set
			{
				_initialTransactionId = value;
				NotifyChanged("InitialTransactionId");
			}
		}

		private int _spreadSize = 2;

		/// <summary>
		/// The size of spread in price increments. It used at determination of spread for generation of order book from tick trades. By default equals to 2.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(90)]
		[DisplayNameLoc(LocalizedStrings.Str1195Key)]
		[DescriptionLoc(LocalizedStrings.Str1196Key)]
		public int SpreadSize
		{
			get { return _spreadSize; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_spreadSize = value;
				NotifyChanged("SpreadSize");
			}
		}

		private int _maxDepth = 5;

		/// <summary>
		/// The maximal depth of order book, which will be generated from ticks. It used, if there is no order book history. By default equals to 5.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(100)]
		[DisplayNameLoc(LocalizedStrings.Str1197Key)]
		[DescriptionLoc(LocalizedStrings.Str1198Key)]
		public int MaxDepth
		{
			get { return _maxDepth; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_maxDepth = value;
				NotifyChanged("MaxDepth");
			}
		}

		private int _volumeMultiplier = 2;

		/// <summary>
		/// The number of volume increments, at which the order exceeds the tick trade. It used at testing on tick trades. By default equals to 2.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(110)]
		[DisplayNameLoc(LocalizedStrings.Str1199Key)]
		[DescriptionLoc(LocalizedStrings.Str1200Key)]
		public int VolumeMultiplier
		{
			get { return _volumeMultiplier; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_volumeMultiplier = value;
				NotifyChanged("VolumeMultiplier");
			}
		}

		private TimeSpan _portfolioRecalcInterval = TimeSpan.Zero;

		/// <summary>
		/// The interval for recalculation of data on portfolios. If interval equals <see cref="TimeSpan.Zero"/>, recalculation is not performed.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(120)]
		[DisplayNameLoc(LocalizedStrings.Str1201Key)]
		[DescriptionLoc(LocalizedStrings.Str1202Key)]
		public TimeSpan PortfolioRecalcInterval
		{
			get { return _portfolioRecalcInterval; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str940);

				_portfolioRecalcInterval = value;
				NotifyChanged("PortfolioRecalcInterval");
			}
		}

		private bool _convertTime;

		/// <summary>
		/// To convert time for orders and trades into exchange time. By default, it is disabled.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(130)]
		[DisplayNameLoc(LocalizedStrings.Str1203Key)]
		[DescriptionLoc(LocalizedStrings.Str1204Key)]
		public bool ConvertTime
		{
			get { return _convertTime; }
			set
			{
				_convertTime = value;
				NotifyChanged("ConvertTime");
			}
		}

		private TimeZoneInfo _timeZone;

		/// <summary>
		/// Information about the time zone where the exchange is located.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(140)]
		[DisplayNameLoc(LocalizedStrings.TimeZoneKey)]
		[DescriptionLoc(LocalizedStrings.Str68Key)]
		public TimeZoneInfo TimeZone
		{
			get { return _timeZone; }
			set
			{
				_timeZone = value;
				NotifyChanged("TimeZone");
			}
		}

		private Unit _priceLimitOffset = new Unit(40, UnitTypes.Percent);

		/// <summary>
		/// The price shift from the previous trade, determining boundaries of maximal and minimal prices for the next session. Used only if there is no saved information <see cref="Level1ChangeMessage"/>. By default, it equals to 40%.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(150)]
		[DisplayNameLoc(LocalizedStrings.Str1205Key)]
		[DescriptionLoc(LocalizedStrings.Str1206Key)]
		public Unit PriceLimitOffset
		{
			get { return _priceLimitOffset; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_priceLimitOffset = value;
			}
		}

		private bool _increaseDepthVolume = true;

		/// <summary>
		/// To add the additional volume into order book at registering orders with greater volume. By default, it is enabled.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(150)]
		[DisplayNameLoc(LocalizedStrings.Str1207Key)]
		[DescriptionLoc(LocalizedStrings.Str1208Key)]
		public bool IncreaseDepthVolume
		{
			get { return _increaseDepthVolume; }
			set
			{
				_increaseDepthVolume = value;
				NotifyChanged("IncreaseDepthVolume");
			}
		}

		/// <summary>
		/// To save the state of paper trading parameters.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("DepthExpirationTime", DepthExpirationTime);
			storage.SetValue("MatchOnTouch", MatchOnTouch);
			storage.SetValue("Failing", Failing);
			storage.SetValue("Latency", Latency);
			storage.SetValue("IsSupportAtomicReRegister", IsSupportAtomicReRegister);
			storage.SetValue("BufferTime", BufferTime);
			//storage.SetValue("UseCandlesTimeFrame", UseCandlesTimeFrame);
			storage.SetValue("InitialOrderId", InitialOrderId);
			storage.SetValue("InitialTradeId", InitialTradeId);
			storage.SetValue("InitialTransactionId", InitialTransactionId);
			storage.SetValue("SpreadSize", SpreadSize);
			storage.SetValue("MaxDepth", MaxDepth);
			storage.SetValue("VolumeMultiplier", VolumeMultiplier);
			storage.SetValue("PortfolioRecalcInterval", PortfolioRecalcInterval);
			storage.SetValue("ConvertTime", ConvertTime);
			storage.SetValue("PriceLimitOffset", PriceLimitOffset);
			storage.SetValue("IncreaseDepthVolume", IncreaseDepthVolume);

			if (TimeZone != null)
				storage.SetValue("TimeZone", TimeZone.ToSerializedString());
		}

		/// <summary>
		/// To load the state of paper trading parameters.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			DepthExpirationTime = storage.GetValue("DepthExpirationTime", DepthExpirationTime);
			MatchOnTouch = storage.GetValue("MatchOnTouch", MatchOnTouch);
			Failing = storage.GetValue("Failing", Failing);
			Latency = storage.GetValue("Latency", Latency);
			IsSupportAtomicReRegister = storage.GetValue("IsSupportAtomicReRegister", IsSupportAtomicReRegister);
			BufferTime = storage.GetValue("BufferTime", BufferTime);
			//UseCandlesTimeFrame = storage.GetValue("UseCandlesTimeFrame", UseCandlesTimeFrame);
			InitialOrderId = storage.GetValue("InitialOrderId", InitialOrderId);
			InitialTradeId = storage.GetValue("InitialTradeId", InitialTradeId);
			InitialTransactionId = storage.GetValue("InitialTransactionId", InitialTransactionId);
			SpreadSize = storage.GetValue("SpreadSize", SpreadSize);
			MaxDepth = storage.GetValue("MaxDepth", MaxDepth);
			VolumeMultiplier = storage.GetValue("VolumeMultiplier", VolumeMultiplier);
			PortfolioRecalcInterval = storage.GetValue("PortfolioRecalcInterval", PortfolioRecalcInterval);
			ConvertTime = storage.GetValue("ConvertTime", ConvertTime);
			PriceLimitOffset = storage.GetValue("PriceLimitOffset", PriceLimitOffset);
			IncreaseDepthVolume = storage.GetValue("IncreaseDepthVolume", IncreaseDepthVolume);

			if (storage.Contains("TimeZone"))
				TimeZone = TimeZoneInfo.FromSerializedString(storage.GetValue<string>("TimeZone"));
		}
	}
}