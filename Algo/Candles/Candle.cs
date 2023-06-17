#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: Candle.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Base candle class (contains main parameters).
	/// </summary>
	[DataContract]
	[Serializable]
	[KnownType(typeof(TickCandle))]
	[KnownType(typeof(VolumeCandle))]
	[KnownType(typeof(RangeCandle))]
	[KnownType(typeof(TimeFrameCandle))]
	[KnownType(typeof(PnFCandle))]
	[KnownType(typeof(RenkoCandle))]
	[Obsolete("Use ICandleMessage.")]
	public abstract class Candle : Cloneable<Candle>, ICandleMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Candle"/>.
		/// </summary>
		protected Candle()
		{
		}

		private SecurityId? _securityId;

		SecurityId ISecurityIdMessage.SecurityId
		{
			get => _securityId ??= Security?.Id.ToSecurityId() ?? default;
			set => throw new NotSupportedException();
		}

		DateTimeOffset IServerTimeMessage.ServerTime
		{
			get => OpenTime;
			set => OpenTime = value;
		}

		DateTimeOffset ILocalTimeMessage.LocalTime => OpenTime;

		/// <summary>
		/// Security.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.SecurityKey, true)]
		public Security Security { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleOpenTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleOpenTimeKey, true)]
		public DateTimeOffset OpenTime { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleCloseTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleCloseTimeKey, true)]
		public DateTimeOffset CloseTime { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleHighTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleHighTimeKey, true)]
		public DateTimeOffset HighTime { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleLowTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleLowTimeKey, true)]
		public DateTimeOffset LowTime { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str79Key)]
		[DescriptionLoc(LocalizedStrings.Str80Key)]
		public decimal OpenPrice { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ClosingPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str86Key)]
		public decimal ClosePrice { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.HighestPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str82Key)]
		public decimal HighPrice { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.LowestPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str84Key)]
		public decimal LowPrice { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TotalPriceKey)]
		public decimal TotalPrice { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OpenVolumeKey)]
		public decimal? OpenVolume { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CloseVolumeKey)]
		public decimal? CloseVolume { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.HighVolumeKey)]
		public decimal? HighVolume { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.LowVolumeKey)]
		public decimal? LowVolume { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.TotalCandleVolumeKey)]
		public decimal TotalVolume { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.RelativeVolumeKey)]
		public decimal? RelativeVolume { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.XamlStr493Key)]
		public decimal? BuyVolume { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.XamlStr579Key)]
		public decimal? SellVolume { get; set; }

		/// <inheritdoc />
		public abstract object Arg { get; set; }

		private DataType _dataType;

		/// <inheritdoc />
		public DataType DataType
		{
			get
			{
				if (_dataType is null)
				{
					var arg = Arg;

					if (!arg.IsNull(true))
						_dataType = DataType.Create(GetType().ToCandleMessageType(), arg);
				}

				return _dataType;
			}
		}

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TicksKey)]
		[DescriptionLoc(LocalizedStrings.TickCountKey)]
		public int? TotalTicks { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickUpKey)]
		[DescriptionLoc(LocalizedStrings.TickUpCountKey)]
		public int? UpTicks { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickDownKey)]
		[DescriptionLoc(LocalizedStrings.TickDownCountKey)]
		public int? DownTicks { get; set; }

		private CandleStates _state;

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.CandleStateKey, true)]
		public CandleStates State
		{
			get => _state;
			set
			{
				ThrowIfFinished();
				_state = value;
			}
		}

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceLevelsKey)]
		public IEnumerable<CandlePriceLevel> PriceLevels { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OIKey)]
		[DescriptionLoc(LocalizedStrings.OpenInterestKey)]
		public decimal? OpenInterest { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long SeqNum { get; set; }

		/// <inheritdoc />
		[DataMember]
		public DataType BuildFrom { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return "{0:HH:mm:ss} {1} (O:{2}, H:{3}, L:{4}, C:{5}, V:{6})"
				.Put(OpenTime, GetType().Name + "_" + Security + "_" + Arg, OpenPrice, HighPrice, LowPrice, ClosePrice, TotalVolume);
		}

		private void ThrowIfFinished()
		{
			if (State == CandleStates.Finished)
				throw new InvalidOperationException(LocalizedStrings.Str649);
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <typeparam name="TCandle">The candle type.</typeparam>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected TCandle CopyTo<TCandle>(TCandle destination)
			where TCandle : Candle
		{
			destination.Arg = Arg;
			destination.ClosePrice = ClosePrice;
			destination.CloseTime = CloseTime;
			destination.CloseVolume = CloseVolume;
			destination.DownTicks = DownTicks;
			destination.HighPrice = HighPrice;
			destination.HighTime = HighTime;
			destination.HighVolume = HighVolume;
			destination.LowPrice = LowPrice;
			destination.LowTime = LowTime;
			destination.LowVolume = LowVolume;
			destination.OpenInterest = OpenInterest;
			destination.OpenPrice = OpenPrice;
			destination.OpenTime = OpenTime;
			destination.OpenVolume = OpenVolume;
			destination.RelativeVolume = RelativeVolume;
			destination.Security = Security;
			//destination.Series = Series;
			//destination.Source = Source;
			//destination.State = State;
			destination.TotalPrice = TotalPrice;
			destination.TotalTicks = TotalTicks;
			destination.TotalVolume = TotalVolume;
			destination.BuyVolume = BuyVolume;
			destination.SellVolume = SellVolume;
			//destination.VolumeProfileInfo = VolumeProfileInfo;
			destination.PriceLevels = PriceLevels?./*Select(l => l.Clone()).*/ToArray();
			destination.SeqNum = SeqNum;
			destination.BuildFrom = BuildFrom;

			return destination;
		}
	}

	/// <summary>
	/// Base candle class (contains main parameters).
	/// </summary>
	/// <typeparam name="TArg"></typeparam>
	[Obsolete("Use ICandleMessage.")]
	public abstract class Candle<TArg> : Candle
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Candle"/>.
		/// </summary>
		protected Candle()
		{
		}

		private TArg _typedArg;

		/// <summary>
		/// Arg.
		/// </summary>
		public TArg TypedArg
		{
			get => _typedArg;
			set
			{
				Validate(value);
				_typedArg = value;
			}
		}

		/// <summary>
		/// Validate value.
		/// </summary>
		/// <param name="value">Value.</param>
		protected virtual void Validate(TArg value) { }

		/// <inheritdoc />
		public override object Arg
		{
			get => TypedArg;
			set => TypedArg = (TArg)value;
		}
	}

	/// <summary>
	/// Time-frame candle.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.TimeFrameCandleKey)]
	[Obsolete("Use TimeFrameCandleMessage.")]
	public class TimeFrameCandle : Candle<TimeSpan>, ITimeFrameCandleMessage
	{
		/// <summary>
		/// Time-frame.
		/// </summary>
		[DataMember]
		public TimeSpan TimeFrame
		{
			get => TypedArg;
			set => TypedArg = value;
		}

		/// <inheritdoc />
		protected override void Validate(TimeSpan value)
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value));
		}

		/// <summary>
		/// Create a copy of <see cref="TimeFrameCandle"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Candle Clone()
		{
			return CopyTo(new TimeFrameCandle());
		}
	}

	/// <summary>
	/// Tick candle.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.TickCandleKey)]
	[Obsolete("Use TickCandleMessage.")]
	public class TickCandle : Candle<int>, ITickCandleMessage
	{
		/// <summary>
		/// Maximum tick count.
		/// </summary>
		[DataMember]
		public int MaxTradeCount
		{
			get => TypedArg;
			set => TypedArg = value;
		}

		/// <inheritdoc />
		protected override void Validate(int value)
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value));
		}

		/// <summary>
		/// Create a copy of <see cref="TickCandle"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Candle Clone()
		{
			return CopyTo(new TickCandle());
		}
	}

	/// <summary>
	/// Volume candle.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.VolumeCandleKey)]
	[Obsolete("Use VolumeCandleMessage.")]
	public class VolumeCandle : Candle<decimal>, IVolumeCandleMessage
	{
		/// <summary>
		/// Maximum volume.
		/// </summary>
		[DataMember]
		public decimal Volume
		{
			get => TypedArg;
			set => TypedArg = value;
		}

		/// <inheritdoc />
		protected override void Validate(decimal value)
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value));
		}

		/// <summary>
		/// Create a copy of <see cref="VolumeCandle"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Candle Clone()
		{
			return CopyTo(new VolumeCandle());
		}
	}

	/// <summary>
	/// Range candle.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.RangeCandleKey)]
	[Obsolete("Use RangeCandleMessage.")]
	public class RangeCandle : Candle<Unit>, IRangeCandleMessage
	{
		/// <summary>
		/// Range of price.
		/// </summary>
		[DataMember]
		public Unit PriceRange
		{
			get => TypedArg;
			set => TypedArg = value;
		}

		/// <inheritdoc />
		protected override void Validate(Unit value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Create a copy of <see cref="RangeCandle"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Candle Clone()
		{
			return CopyTo(new RangeCandle());
		}
	}

	/// <summary>
	/// The candle of point-and-figure chart (tac-toe chart).
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.PnFCandleKey)]
	[Obsolete("Use PnFCandleMessage.")]
	public class PnFCandle : Candle<PnFArg>, IPnFCandleMessage
	{
		/// <summary>
		/// Value of arguments.
		/// </summary>
		[DataMember]
		public PnFArg PnFArg
		{
			get => TypedArg;
			set => TypedArg = value;
		}

		/// <inheritdoc />
		protected override void Validate(PnFArg value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Create a copy of <see cref="PnFCandle"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Candle Clone()
		{
			return CopyTo(new PnFCandle());
		}
	}

	/// <summary>
	/// Renko candle.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.RenkoCandleKey)]
	[Obsolete("Use RenkoCandleMessage.")]
	public class RenkoCandle : Candle<Unit>, IRenkoCandleMessage
	{
		/// <summary>
		/// Possible price change range.
		/// </summary>
		[DataMember]
		public Unit BoxSize
		{
			get => TypedArg;
			set => TypedArg = value;
		}

		/// <inheritdoc />
		protected override void Validate(Unit value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Create a copy of <see cref="RenkoCandle"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Candle Clone()
		{
			return CopyTo(new RenkoCandle());
		}
	}

	/// <summary>
	/// Heikin ashi candle.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.HeikinAshiKey)]
	[Obsolete("Use HeikinAshiCandleMessage.")]
	public class HeikinAshiCandle : TimeFrameCandle, IHeikinAshiCandleMessage
	{
		/// <summary>
		/// Create a copy of <see cref="HeikinAshiCandle"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Candle Clone()
		{
			return CopyTo(new HeikinAshiCandle());
		}
	}
}
