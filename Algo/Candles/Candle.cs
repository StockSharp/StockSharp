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
	using System.ComponentModel;
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
			get { return _openTime; }
			set
			{
				ThrowIfFinished();
				_openTime = value;
			}
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
			get { return _closeTime; }
			set
			{
				ThrowIfFinished();
				_closeTime = value;
			}
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
			get { return _highTime; }
			set
			{
				ThrowIfFinished();
				_highTime = value;
			}
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
			get { return _lowTime; }
			set
			{
				ThrowIfFinished();
				_lowTime = value;
			}
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
			get { return _openPrice; }
			set
			{
				ThrowIfFinished();
				_openPrice = value;
			}
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
			get { return _closePrice; }
			set
			{
				ThrowIfFinished();
				_closePrice = value;
			}
		}

		private decimal _highPrice;

		/// <summary>
		/// Maximum price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str81Key)]
		[DescriptionLoc(LocalizedStrings.Str82Key)]
		public decimal HighPrice
		{
			get { return _highPrice; }
			set
			{
				ThrowIfFinished();
				_highPrice = value;
			}
		}

		private decimal _lowPrice;

		/// <summary>
		/// Minimum price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str83Key)]
		[DescriptionLoc(LocalizedStrings.Str84Key)]
		public decimal LowPrice
		{
			get { return _lowPrice; }
			set
			{
				ThrowIfFinished();
				_lowPrice = value;
			}
		}

		private decimal _totalPrice;

		/// <summary>
		/// Total trades volume.
		/// </summary>
		[DataMember]
		public decimal TotalPrice
		{
			get { return _totalPrice; }
			set
			{
				ThrowIfFinished();
				_totalPrice = value;
			}
		}

		private decimal? _openVolume;

		/// <summary>
		/// Volume at open.
		/// </summary>
		[DataMember]
		public decimal? OpenVolume
		{
			get { return _openVolume; }
			set
			{
				ThrowIfFinished();
				_openVolume = value;
			}
		}

		private decimal? _closeVolume;

		/// <summary>
		/// Volume at close.
		/// </summary>
		[DataMember]
		public decimal? CloseVolume
		{
			get { return _closeVolume; }
			set
			{
				ThrowIfFinished();
				_closeVolume = value;
			}
		}

		private decimal? _highVolume;

		/// <summary>
		/// Volume at high.
		/// </summary>
		[DataMember]
		public decimal? HighVolume
		{
			get { return _highVolume; }
			set
			{
				ThrowIfFinished();
				_highVolume = value;
			}
		}

		private decimal? _lowVolume;

		/// <summary>
		/// Minimum volume.
		/// </summary>
		[DataMember]
		public decimal? LowVolume
		{
			get { return _lowVolume; }
			set
			{
				ThrowIfFinished();
				_lowVolume = value;
			}
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
			get { return _totalVolume; }
			set
			{
				ThrowIfFinished();
				_totalVolume = value;
			}
		}

		private decimal? _relativeVolume;

		/// <summary>
		/// Relative colume.
		/// </summary>
		[DataMember]
		public decimal? RelativeVolume
		{
			get { return _relativeVolume; }
			set
			{
				ThrowIfFinished();
				_relativeVolume = value;
			}
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
			get { return _totalTicks; }
			set
			{
				ThrowIfFinished();
				_totalTicks = value;
			}
		}

		private int? _upTicks;

		/// <summary>
		/// Number of uptrending ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickUpKey)]
		[DescriptionLoc(LocalizedStrings.TickUpCountKey)]
		public int? UpTicks
		{
			get { return _upTicks; }
			set
			{
				ThrowIfFinished();
				_upTicks = value;
			}
		}

		private int? _downTicks;

		/// <summary>
		/// Number of downtrending ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickDownKey)]
		[DescriptionLoc(LocalizedStrings.TickDownCountKey)]
		public int? DownTicks
		{
			get { return _downTicks; }
			set
			{
				ThrowIfFinished();
				_downTicks = value;
			}
		}

		private CandleStates _state;

		/// <summary>
		/// State.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.CandleStateKey)]
		public CandleStates State
		{
			get { return _state; }
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
		/// <param name="destination">The object, which copied information.</param>
		/// <returns>The object, which copied information.</returns>
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
	[DisplayName("Time Frame")]
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
			get { return TimeFrame; }
			set { TimeFrame = (TimeSpan)value; }
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
	[DisplayName("Tick")]
	public class TickCandle : Candle
	{
		/// <summary>
		/// Maximum tick count.
		/// </summary>
		[DataMember]
		public int MaxTradeCount { get; set; }

		/// <summary>
		/// Current tick count.
		/// </summary>
		[DataMember]
		public int CurrentTradeCount { get; set; }

		/// <summary>
		/// Candle arg.
		/// </summary>
		public override object Arg
		{
			get { return MaxTradeCount; }
			set { MaxTradeCount = (int)value; }
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
	[DisplayName("Volume")]
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
			get { return Volume; }
			set { Volume = (decimal)value; }
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
	[DisplayName("Range")]
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
			get { return PriceRange; }
			set { PriceRange = (Unit)value; }
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
	[DisplayName("X&0")]
	public class PnFCandle : Candle
	{
		/// <summary>
		/// Value of arguments.
		/// </summary>
		[DataMember]
		public PnFArg PnFArg { get; set; }

		/// <summary>
		/// Type of symbols.
		/// </summary>
		[DataMember]
		public PnFTypes Type { get; set; }

		/// <summary>
		/// Candle arg.
		/// </summary>
		public override object Arg
		{
			get { return PnFArg; }
			set { PnFArg = (PnFArg)value; }
		}

		/// <summary>
		/// Create a copy of <see cref="PnFCandle"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Candle Clone()
		{
			return CopyTo(new PnFCandle { Type = Type });
		}
	}

	/// <summary>
	/// Renko candle.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayName("Renko")]
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
			get { return BoxSize; }
			set { BoxSize = (Unit)value; }
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
