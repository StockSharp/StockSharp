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
	public abstract class Candle : Cloneable<Candle>
	{
		/// <summary>
		/// Security.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.SecurityKey, true)]
		public Security Security { get; set; }

		private DateTimeOffset _openTime;

		/// <summary>
		/// Open time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleOpenTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleOpenTimeKey, true)]
		public DateTimeOffset OpenTime
		{
			get => _openTime;
			set => _openTime = value;
		}

		private DateTimeOffset _closeTime;

		/// <summary>
		/// Close time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleCloseTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleCloseTimeKey, true)]
		public DateTimeOffset CloseTime
		{
			get => _closeTime;
			set => _closeTime = value;
		}

		private DateTimeOffset _highTime;

		/// <summary>
		/// High time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleHighTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleHighTimeKey, true)]
		public DateTimeOffset HighTime
		{
			get => _highTime;
			set => _highTime = value;
		}

		private DateTimeOffset _lowTime;

		/// <summary>
		/// Low time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleLowTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleLowTimeKey, true)]
		public DateTimeOffset LowTime
		{
			get => _lowTime;
			set => _lowTime = value;
		}

		private decimal _openPrice;

		/// <summary>
		/// Opening price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str79Key)]
		[DescriptionLoc(LocalizedStrings.Str80Key)]
		public decimal OpenPrice
		{
			get => _openPrice;
			set => _openPrice = value;
		}

		private decimal _closePrice;

		/// <summary>
		/// Closing price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ClosingPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str86Key)]
		public decimal ClosePrice
		{
			get => _closePrice;
			set => _closePrice = value;
		}

		private decimal _highPrice;

		/// <summary>
		/// Highest price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.HighestPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str82Key)]
		public decimal HighPrice
		{
			get => _highPrice;
			set => _highPrice = value;
		}

		private decimal _lowPrice;

		/// <summary>
		/// Lowest price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.LowestPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str84Key)]
		public decimal LowPrice
		{
			get => _lowPrice;
			set => _lowPrice = value;
		}

		private decimal _totalPrice;

		/// <summary>
		/// Total price size.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TotalPriceKey)]
		public decimal TotalPrice
		{
			get => _totalPrice;
			set => _totalPrice = value;
		}

		private decimal? _openVolume;

		/// <summary>
		/// Volume at open.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OpenVolumeKey)]
		public decimal? OpenVolume
		{
			get => _openVolume;
			set => _openVolume = value;
		}

		private decimal? _closeVolume;

		/// <summary>
		/// Volume at close.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CloseVolumeKey)]
		public decimal? CloseVolume
		{
			get => _closeVolume;
			set => _closeVolume = value;
		}

		private decimal? _highVolume;

		/// <summary>
		/// Volume at high.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.HighVolumeKey)]
		public decimal? HighVolume
		{
			get => _highVolume;
			set => _highVolume = value;
		}

		private decimal? _lowVolume;

		/// <summary>
		/// Volume at low.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.LowVolumeKey)]
		public decimal? LowVolume
		{
			get => _lowVolume;
			set => _lowVolume = value;
		}

		private decimal _totalVolume;

		/// <summary>
		/// Total volume.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.TotalCandleVolumeKey)]
		public decimal TotalVolume
		{
			get => _totalVolume;
			set => _totalVolume = value;
		}

		private decimal? _relativeVolume;

		/// <summary>
		/// Relative volume.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.RelativeVolumeKey)]
		public decimal? RelativeVolume
		{
			get => _relativeVolume;
			set => _relativeVolume = value;
		}

		//[field: NonSerialized]
		//private CandleSeries _series;

		///// <summary>
		///// Candles series.
		///// </summary>
		//public CandleSeries Series
		//{
		//	get { return _series; }
		//	set { _series = value; }
		//}

		//[field: NonSerialized]
		//private ICandleManagerSource _source;

		///// <summary>
		///// Candle's source.
		///// </summary>
		//public ICandleManagerSource Source
		//{
		//	get { return _source; }
		//	set { _source = value; }
		//}

		/// <summary>
		/// Candle arg.
		/// </summary>
		public abstract object Arg { get; set; }

		private int? _totalTicks;

		/// <summary>
		/// Number of ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TicksKey)]
		[DescriptionLoc(LocalizedStrings.TickCountKey)]
		public int? TotalTicks
		{
			get => _totalTicks;
			set => _totalTicks = value;
		}

		private int? _upTicks;

		/// <summary>
		/// Number of up trending ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickUpKey)]
		[DescriptionLoc(LocalizedStrings.TickUpCountKey)]
		public int? UpTicks
		{
			get => _upTicks;
			set => _upTicks = value;
		}

		private int? _downTicks;

		/// <summary>
		/// Number of down trending ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickDownKey)]
		[DescriptionLoc(LocalizedStrings.TickDownCountKey)]
		public int? DownTicks
		{
			get => _downTicks;
			set => _downTicks = value;
		}

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
		/// Open interest.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OIKey)]
		[DescriptionLoc(LocalizedStrings.OpenInterestKey)]
		public decimal? OpenInterest { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
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
			destination.PriceLevels = PriceLevels?.Select(l => l.Clone()).ToArray();

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

		/// <summary>
		/// Candle arg.
		/// </summary>
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
		/// <summary>
		/// Maximum tick count.
		/// </summary>
		[DataMember]
		public int MaxTradeCount { get; set; }

		/// <summary>
		/// Candle arg.
		/// </summary>
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
		/// <summary>
		/// Maximum volume.
		/// </summary>
		[DataMember]
		public decimal Volume { get; set; }

		/// <summary>
		/// Candle arg.
		/// </summary>
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
		/// <summary>
		/// Range of price.
		/// </summary>
		[DataMember]
		public Unit PriceRange { get; set; }

		/// <summary>
		/// Candle arg.
		/// </summary>
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
		/// <summary>
		/// Value of arguments.
		/// </summary>
		[DataMember]
		public PnFArg PnFArg { get; set; }

		///// <summary>
		///// Type of symbols.
		///// </summary>
		//[DataMember]
		//public PnFTypes Type { get; set; }

		/// <summary>
		/// Candle arg.
		/// </summary>
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
		/// <summary>
		/// Possible price change range.
		/// </summary>
		[DataMember]
		public Unit BoxSize { get; set; }

		/// <summary>
		/// Candle arg.
		/// </summary>
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
}
