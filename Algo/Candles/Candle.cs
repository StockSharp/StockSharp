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
	using Ecng.Collections;

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
	public abstract class Candle : Cloneable<Candle>
	{
		/// <summary>
		/// Security.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.SecurityKey, true)]
		public Security Security { get; set; }

		/// <summary>
		/// Open time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleOpenTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleOpenTimeKey, true)]
		public DateTimeOffset OpenTime { get; set; }

		/// <summary>
		/// Close time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleCloseTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleCloseTimeKey, true)]
		public DateTimeOffset CloseTime { get; set; }

		/// <summary>
		/// High time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleHighTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleHighTimeKey, true)]
		public DateTimeOffset HighTime { get; set; }

		/// <summary>
		/// Low time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleLowTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleLowTimeKey, true)]
		public DateTimeOffset LowTime { get; set; }

		/// <summary>
		/// Opening price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str79Key)]
		[DescriptionLoc(LocalizedStrings.Str80Key)]
		public decimal OpenPrice { get; set; }

		/// <summary>
		/// Closing price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ClosingPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str86Key)]
		public decimal ClosePrice { get; set; }

		/// <summary>
		/// Highest price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.HighestPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str82Key)]
		public decimal HighPrice { get; set; }

		/// <summary>
		/// Lowest price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.LowestPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str84Key)]
		public decimal LowPrice { get; set; }

		/// <summary>
		/// Total price size.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TotalPriceKey)]
		public decimal TotalPrice { get; set; }

		/// <summary>
		/// Volume at open.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OpenVolumeKey)]
		public decimal? OpenVolume { get; set; }

		/// <summary>
		/// Volume at close.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CloseVolumeKey)]
		public decimal? CloseVolume { get; set; }

		/// <summary>
		/// Volume at high.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.HighVolumeKey)]
		public decimal? HighVolume { get; set; }

		/// <summary>
		/// Volume at low.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.LowVolumeKey)]
		public decimal? LowVolume { get; set; }

		/// <summary>
		/// Total volume.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.TotalCandleVolumeKey)]
		public decimal TotalVolume { get; set; }

		/// <summary>
		/// Relative volume.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.RelativeVolumeKey)]
		public decimal? RelativeVolume { get; set; }

		/// <summary>
		/// Candle arg.
		/// </summary>
		public abstract object Arg { get; set; }

		/// <summary>
		/// Number of ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TicksKey)]
		[DescriptionLoc(LocalizedStrings.TickCountKey)]
		public int? TotalTicks { get; set; }

		/// <summary>
		/// Number of up trending ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickUpKey)]
		[DescriptionLoc(LocalizedStrings.TickUpCountKey)]
		public int? UpTicks { get; set; }

		/// <summary>
		/// Number of down trending ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickDownKey)]
		[DescriptionLoc(LocalizedStrings.TickDownCountKey)]
		public int? DownTicks { get; set; }

		private CandleStates _state;

		/// <summary>
		/// State.
		/// </summary>
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

		/// <summary>
		/// Price levels.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceLevelsKey)]
		public IEnumerable<CandlePriceLevel> PriceLevels { get; set; }

		/// <summary>
		/// <see cref="PriceLevels"/> with minimum <see cref="CandlePriceLevel.TotalVolume"/>.
		/// </summary>
		public CandlePriceLevel? MinPriceLevel => PriceLevels?.OrderBy(l => l.TotalVolume).FirstOr();

		/// <summary>
		/// <see cref="PriceLevels"/> with maximum <see cref="CandlePriceLevel.TotalVolume"/>.
		/// </summary>
		public CandlePriceLevel? MaxPriceLevel => PriceLevels?.OrderByDescending(l => l.TotalVolume).FirstOr();

		/// <summary>
		/// Open interest.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OIKey)]
		[DescriptionLoc(LocalizedStrings.OpenInterestKey)]
		public decimal? OpenInterest { get; set; }

		/// <summary>
		/// Sequence number.
		/// </summary>
		/// <remarks>Zero means no information.</remarks>
		[DataMember]
		public long SeqNum { get; set; }

		/// <summary>
		/// Determines the message is generated from the specified <see cref="DataType"/>.
		/// </summary>
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
			//destination.VolumeProfileInfo = VolumeProfileInfo;
			destination.PriceLevels = PriceLevels?./*Select(l => l.Clone()).*/ToArray();
			destination.SeqNum = SeqNum;
			destination.BuildFrom = BuildFrom;

			return destination;
		}
	}

	/// <summary>
	/// Time-frame candle.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.TimeFrameCandleKey)]
	public class TimeFrameCandle : Candle
	{
		/// <summary>
		/// Time-frame.
		/// </summary>
		[DataMember]
		public TimeSpan TimeFrame { get; set; }

		/// <inheritdoc />
		public override object Arg
		{
			get => TimeFrame;
			set => TimeFrame = (TimeSpan)value;
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
	public class TickCandle : Candle
	{
		private int _maxTradeCount;

		/// <summary>
		/// Maximum tick count.
		/// </summary>
		[DataMember]
		public int MaxTradeCount
		{
			get => _maxTradeCount;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_maxTradeCount = value;
			}
		}

		/// <inheritdoc />
		public override object Arg
		{
			get => MaxTradeCount;
			set => MaxTradeCount = (int)value;
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
	public class VolumeCandle : Candle
	{
		private decimal _volume;

		/// <summary>
		/// Maximum volume.
		/// </summary>
		[DataMember]
		public decimal Volume
		{
			get => _volume;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_volume = value;
			}
		}

		/// <inheritdoc />
		public override object Arg
		{
			get => Volume;
			set => Volume = (decimal)value;
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
	public class RangeCandle : Candle
	{
		private Unit _priceRange;

		/// <summary>
		/// Range of price.
		/// </summary>
		[DataMember]
		public Unit PriceRange
		{
			get => _priceRange;
			set => _priceRange = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public override object Arg
		{
			get => PriceRange;
			set => PriceRange = (Unit)value;
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
	public class PnFCandle : Candle
	{
		private PnFArg _pnFArg;

		/// <summary>
		/// Value of arguments.
		/// </summary>
		[DataMember]
		public PnFArg PnFArg
		{
			get => _pnFArg;
			set => _pnFArg = value ?? throw new ArgumentNullException(nameof(value));
		}

		///// <summary>
		///// Type of symbols.
		///// </summary>
		//[DataMember]
		//public PnFTypes Type { get; set; }

		/// <inheritdoc />
		public override object Arg
		{
			get => PnFArg;
			set => PnFArg = (PnFArg)value;
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
	public class RenkoCandle : Candle
	{
		private Unit _boxSize;

		/// <summary>
		/// Possible price change range.
		/// </summary>
		[DataMember]
		public Unit BoxSize
		{
			get => _boxSize;
			set => _boxSize = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public override object Arg
		{
			get => BoxSize;
			set => BoxSize = (Unit)value;
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
	public class HeikinAshiCandle : TimeFrameCandle
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
