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
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

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
		}

		private bool _matchOnTouch = true;

		/// <summary>
		/// At emulation of clearing by trades, to perform clearing of orders, when trade price touches the order price (is equal to order price), rather than only when the trade price is better than order price. Is On by default (optimistic scenario).
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1176Key,
			Description = LocalizedStrings.Str1177Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 200)]
		public bool MatchOnTouch
		{
			get => _matchOnTouch;
			set
			{
				if (_matchOnTouch == value)
					return;

				_matchOnTouch = value;
				NotifyChanged(nameof(MatchOnTouch));
			}
		}

		private TimeSpan _depthExpirationTime = TimeSpan.FromDays(1);

		/// <summary>
		/// The maximal time, during which the order book is in the emulator, if no renewal during this time, the order book is deleted, This feature may be used to remove old order books if the are holes in data. By default is equal to 1 day.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1178Key,
			Description = LocalizedStrings.Str1179Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 201)]
		public TimeSpan DepthExpirationTime
		{
			get => _depthExpirationTime;
			set
			{
				if (_depthExpirationTime == value)
					return;

				_depthExpirationTime = value;
				NotifyChanged(nameof(DepthExpirationTime));
			}
		}

		private double _failing;

		/// <summary>
		/// The percentage value of new orders registration error. The value may be from 0 (not a single error) to 100. By default is Off.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1180Key,
			Description = LocalizedStrings.Str1181Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 202)]
		public double Failing
		{
			get => _failing;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1182);

				if (value > 100)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1183);

				_failing = value;
				NotifyChanged(nameof(Failing));
			}
		}

		private TimeSpan _latency;

		/// <summary>
		/// The minimal value of the registered orders delay. By default, it is <see cref="TimeSpan.Zero"/>, which means instant adoption of registered orders by  exchange.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str161Key,
			Description = LocalizedStrings.Str1184Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 203)]
		public TimeSpan Latency
		{
			get => _latency;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1185);

				_latency = value;
				NotifyChanged(nameof(Latency));
			}
		}

		private bool _isSupportAtomicReRegister = true;

		/// <summary>
		/// Gets a value indicating whether the re-registration orders as a single transaction. By default is enabled.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ReregisteringKey,
			Description = LocalizedStrings.Str60Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 204)]
		public bool IsSupportAtomicReRegister
		{
			get => _isSupportAtomicReRegister;
			set
			{
				_isSupportAtomicReRegister = value;
				NotifyChanged(nameof(_isSupportAtomicReRegister));
			}
		}

		private TimeSpan _bufferTime;

		/// <summary>
		/// Responses shall be sent in intervals by whole package. The network delay and buffered operation of exchange are emulated. The default is 0 ms.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1186Key,
			Description = LocalizedStrings.Str1187Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 205)]
		public TimeSpan BufferTime
		{
			get => _bufferTime;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str940);

				_bufferTime = value;
				NotifyChanged(nameof(BufferTime));
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
		//			throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1189);

		//		_useCandlesTimeFrame = value;
		//		NotifyChanged(nameof(UseCandlesTimeFrame));
		//	}
		//}

		private long _initialOrderId;

		/// <summary>
		/// The number, starting at which the emulator will generate identifiers for orders <see cref="Order.Id"/>.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1190Key,
			Description = LocalizedStrings.Str1191Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 206)]
		public long InitialOrderId
		{
			get => _initialOrderId;
			set
			{
				_initialOrderId = value;
				NotifyChanged(nameof(InitialOrderId));
			}
		}

		private long _initialTradeId;

		/// <summary>
		/// The number, starting at which the emulator will generate identifiers fir trades <see cref="Trade.Id"/>.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1192Key,
			Description = LocalizedStrings.Str1193Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 207)]
		public long InitialTradeId
		{
			get => _initialTradeId;
			set
			{
				_initialTradeId = value;
				NotifyChanged(nameof(InitialTradeId));
			}
		}

		private long _initialTransactionId;

		/// <summary>
		/// The number, starting at which the emulator will generate numbers for order trades <see cref="Order.TransactionId"/>.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TransactionKey,
			Description = LocalizedStrings.Str1194Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 208)]
		public long InitialTransactionId
		{
			get => _initialTransactionId;
			set
			{
				_initialTransactionId = value;
				NotifyChanged(nameof(InitialTransactionId));
			}
		}

		private int _spreadSize = 2;

		/// <summary>
		/// The size of spread in price increments. It used at determination of spread for generation of order book from tick trades. By default equals to 2.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1195Key,
			Description = LocalizedStrings.Str1196Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 209)]
		public int SpreadSize
		{
			get => _spreadSize;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_spreadSize = value;
				NotifyChanged(nameof(SpreadSize));
			}
		}

		private int _maxDepth = 5;

		/// <summary>
		/// The maximal depth of order book, which will be generated from ticks. It used, if there is no order book history. By default equals to 5.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1197Key,
			Description = LocalizedStrings.Str1198Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 210)]
		public int MaxDepth
		{
			get => _maxDepth;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_maxDepth = value;
				NotifyChanged(nameof(MaxDepth));
			}
		}

		private int _volumeMultiplier = 2;

		/// <summary>
		/// The number of volume increments, at which the order exceeds the tick trade. It used at testing on tick trades. By default equals to 2.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1199Key,
			Description = LocalizedStrings.Str1200Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 211)]
		public int VolumeMultiplier
		{
			get => _volumeMultiplier;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_volumeMultiplier = value;
				NotifyChanged(nameof(VolumeMultiplier));
			}
		}

		private TimeSpan _portfolioRecalcInterval = TimeSpan.Zero;

		/// <summary>
		/// The interval for recalculation of data on portfolios. If interval equals <see cref="TimeSpan.Zero"/>, recalculation is not performed.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1201Key,
			Description = LocalizedStrings.Str1202Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 212)]
		public TimeSpan PortfolioRecalcInterval
		{
			get => _portfolioRecalcInterval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str940);

				_portfolioRecalcInterval = value;
				NotifyChanged(nameof(PortfolioRecalcInterval));
			}
		}

		private bool _convertTime;

		/// <summary>
		/// To convert time for orders and trades into exchange time. By default, it is disabled.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1203Key,
			Description = LocalizedStrings.Str1204Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 213)]
		public bool ConvertTime
		{
			get => _convertTime;
			set
			{
				_convertTime = value;
				NotifyChanged(nameof(ConvertTime));
			}
		}

		private TimeZoneInfo _timeZone;

		/// <summary>
		/// Information about the time zone where the exchange is located.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TimeZoneKey,
			Description = LocalizedStrings.Str68Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 214)]
		public TimeZoneInfo TimeZone
		{
			get => _timeZone;
			set
			{
				_timeZone = value;
				NotifyChanged(nameof(TimeZone));
			}
		}

		private Unit _priceLimitOffset = new Unit(40, UnitTypes.Percent);

		/// <summary>
		/// The price shift from the previous trade, determining boundaries of maximal and minimal prices for the next session. Used only if there is no saved information <see cref="Level1ChangeMessage"/>. By default, it equals to 40%.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1205Key,
			Description = LocalizedStrings.Str1206Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 215)]
		public Unit PriceLimitOffset
		{
			get => _priceLimitOffset;
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
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1207Key,
			Description = LocalizedStrings.Str1208Key,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 216)]
		public bool IncreaseDepthVolume
		{
			get => _increaseDepthVolume;
			set
			{
				_increaseDepthVolume = value;
				NotifyChanged(nameof(IncreaseDepthVolume));
			}
		}

		private bool _checkTradingState;

		/// <summary>
		/// Check trading state.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SessionStateKey,
			Description = LocalizedStrings.CheckTradingStateKey,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 217)]
		public bool CheckTradingState
		{
			get => _checkTradingState;
			set
			{
				_checkTradingState = value;
				NotifyChanged(nameof(CheckTradingState));
			}
		}

		private bool _checkMoney;

		/// <summary>
		/// Check money balance.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1543Key,
			Description = LocalizedStrings.CheckMoneyKey,
			GroupName = LocalizedStrings.Str1175Key,
			Order = 218)]
		public bool CheckMoney
		{
			get => _checkMoney;
			set
			{
				_checkMoney = value;
				NotifyChanged(nameof(CheckMoney));
			}
		}

		/// <summary>
		/// To save the state of paper trading parameters.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(DepthExpirationTime), DepthExpirationTime);
			storage.SetValue(nameof(MatchOnTouch), MatchOnTouch);
			storage.SetValue(nameof(Failing), Failing);
			storage.SetValue(nameof(Latency), Latency);
			storage.SetValue(nameof(IsSupportAtomicReRegister), IsSupportAtomicReRegister);
			storage.SetValue(nameof(BufferTime), BufferTime);
			//storage.SetValue(nameof(UseCandlesTimeFrame), UseCandlesTimeFrame);
			storage.SetValue(nameof(InitialOrderId), InitialOrderId);
			storage.SetValue(nameof(InitialTradeId), InitialTradeId);
			storage.SetValue(nameof(InitialTransactionId), InitialTransactionId);
			storage.SetValue(nameof(SpreadSize), SpreadSize);
			storage.SetValue(nameof(MaxDepth), MaxDepth);
			storage.SetValue(nameof(VolumeMultiplier), VolumeMultiplier);
			storage.SetValue(nameof(PortfolioRecalcInterval), PortfolioRecalcInterval);
			storage.SetValue(nameof(ConvertTime), ConvertTime);
			storage.SetValue(nameof(PriceLimitOffset), PriceLimitOffset);
			storage.SetValue(nameof(IncreaseDepthVolume), IncreaseDepthVolume);
			storage.SetValue(nameof(CheckTradingState), CheckTradingState);
			storage.SetValue(nameof(CheckMoney), CheckMoney);

			if (TimeZone != null)
				storage.SetValue(nameof(TimeZone), TimeZone);
		}

		/// <summary>
		/// To load the state of paper trading parameters.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			DepthExpirationTime = storage.GetValue(nameof(DepthExpirationTime), DepthExpirationTime);
			MatchOnTouch = storage.GetValue(nameof(MatchOnTouch), MatchOnTouch);
			Failing = storage.GetValue(nameof(Failing), Failing);
			Latency = storage.GetValue(nameof(Latency), Latency);
			IsSupportAtomicReRegister = storage.GetValue(nameof(IsSupportAtomicReRegister), IsSupportAtomicReRegister);
			BufferTime = storage.GetValue(nameof(BufferTime), BufferTime);
			//UseCandlesTimeFrame = storage.GetValue(nameof(UseCandlesTimeFrame), UseCandlesTimeFrame);
			InitialOrderId = storage.GetValue(nameof(InitialOrderId), InitialOrderId);
			InitialTradeId = storage.GetValue(nameof(InitialTradeId), InitialTradeId);
			InitialTransactionId = storage.GetValue(nameof(InitialTransactionId), InitialTransactionId);
			SpreadSize = storage.GetValue(nameof(SpreadSize), SpreadSize);
			MaxDepth = storage.GetValue(nameof(MaxDepth), MaxDepth);
			VolumeMultiplier = storage.GetValue(nameof(VolumeMultiplier), VolumeMultiplier);
			PortfolioRecalcInterval = storage.GetValue(nameof(PortfolioRecalcInterval), PortfolioRecalcInterval);
			ConvertTime = storage.GetValue(nameof(ConvertTime), ConvertTime);
			PriceLimitOffset = storage.GetValue(nameof(PriceLimitOffset), PriceLimitOffset);
			IncreaseDepthVolume = storage.GetValue(nameof(IncreaseDepthVolume), IncreaseDepthVolume);
			CheckTradingState = storage.GetValue(nameof(CheckTradingState), CheckTradingState);
			CheckMoney = storage.GetValue(nameof(CheckMoney), CheckMoney);

			if (storage.Contains(nameof(TimeZone)))
				TimeZone = storage.GetValue<TimeZoneInfo>(nameof(TimeZone));
		}
	}
}