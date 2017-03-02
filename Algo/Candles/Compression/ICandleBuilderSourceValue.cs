#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: ICandleBuilderSourceValue.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Diagnostics;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The interface that describes data of the <see cref="ICandleBuilderSource"/> source.
	/// </summary>
	public interface ICandleBuilderSourceValue
	{
		/// <summary>
		/// The instrument by which data has been created.
		/// </summary>
		Security Security { get; }

		/// <summary>
		/// The time of new data occurrence.
		/// </summary>
		DateTimeOffset Time { get; }

		/// <summary>
		/// Price.
		/// </summary>
		decimal Price { get; }

		/// <summary>
		/// Volume.
		/// </summary>
		decimal? Volume { get; }

		/// <summary>
		/// Order side.
		/// </summary>
		Sides? OrderDirection { get; }
	}

	/// <summary>
	/// The <see cref="ICandleBuilderSource"/> source data is created on basis of <see cref="Trade"/>.
	/// </summary>
	[DebuggerDisplay("{" + nameof(Trade) + "}")]
	public class TradeCandleBuilderSourceValue : ICandleBuilderSourceValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TradeCandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="trade">Tick trade.</param>
		public TradeCandleBuilderSourceValue(Trade trade)
		{
			Trade = trade;
		}

		/// <summary>
		/// Tick trade.
		/// </summary>
		public Trade Trade { get; }

		Security ICandleBuilderSourceValue.Security => Trade.Security;

		DateTimeOffset ICandleBuilderSourceValue.Time => Trade.Time;

		decimal ICandleBuilderSourceValue.Price => Trade.Price;

		decimal? ICandleBuilderSourceValue.Volume => Trade.Volume == 0 ? (decimal?)null : Trade.Volume;

		Sides? ICandleBuilderSourceValue.OrderDirection => Trade.OrderDirection;
	}

	/// <summary>
	/// The <see cref="ICandleBuilderSource"/> source data is created on basis of <see cref="Trade"/>.
	/// </summary>
	[DebuggerDisplay("{" + nameof(Tick) + "}")]
	public class TickCandleBuilderSourceValue : ICandleBuilderSourceValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TickCandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="security">The instrument by which data has been created.</param>
		/// <param name="tick">Tick trade.</param>
		public TickCandleBuilderSourceValue(Security security, ExecutionMessage tick)
		{
			_security = security;
			Tick = tick;
		}

		/// <summary>
		/// Tick trade.
		/// </summary>
		public ExecutionMessage Tick { get; }

		private readonly Security _security;

		Security ICandleBuilderSourceValue.Security => _security;

		DateTimeOffset ICandleBuilderSourceValue.Time => Tick.ServerTime;

		decimal ICandleBuilderSourceValue.Price => Tick.TradePrice ?? 0;

		decimal? ICandleBuilderSourceValue.Volume => Tick.TradeVolume;

		Sides? ICandleBuilderSourceValue.OrderDirection => Tick.OriginSide;
	}

	/// <summary>
	/// Types of candle depth based data.
	/// </summary>
	public enum DepthCandleSourceTypes
	{
		/// <summary>
		/// Best bid.
		/// </summary>
		BestBid,

		/// <summary>
		/// Best ask.
		/// </summary>
		BestAsk,

		/// <summary>
		/// Spread middle.
		/// </summary>
		Middle,
	}

	/// <summary>
	/// The <see cref="ICandleBuilderSource"/> source data is created on basis of <see cref="MarketDepth"/>.
	/// </summary>
	[DebuggerDisplay("{" + nameof(Depth) + "}")]
	public class DepthCandleBuilderSourceValue : ICandleBuilderSourceValue
	{
		private readonly decimal _price;
		private readonly decimal? _volume;

		/// <summary>
		/// Initializes a new instance of the <see cref="DepthCandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="depth">Market depth.</param>
		/// <param name="type">Type of candle depth based data.</param>
		public DepthCandleBuilderSourceValue(MarketDepth depth, DepthCandleSourceTypes type)
		{
			Depth = depth;
			Type = type;

			var pair = Depth.BestPair;

			if (pair != null)
			{
				switch (Type)
				{
					case DepthCandleSourceTypes.BestBid:
						var bid = pair.Bid;

						if (bid != null)
						{
							_price = bid.Price;
							_volume = bid.Volume;
						}

						break;
					case DepthCandleSourceTypes.BestAsk:
						var ask = pair.Ask;

						if (ask != null)
						{
							_price = ask.Price;
							_volume = ask.Volume;
						}

						break;
					case DepthCandleSourceTypes.Middle:
						_price = pair.MiddlePrice ?? 0;
						//_volume = pair.Bid.Volume;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Market depth.
		/// </summary>
		public MarketDepth Depth { get; }

		/// <summary>
		/// Type of candle depth based data.
		/// </summary>
		public DepthCandleSourceTypes Type { get; }

		Security ICandleBuilderSourceValue.Security => Depth.Security;

		DateTimeOffset ICandleBuilderSourceValue.Time => Depth.LastChangeTime;

		decimal ICandleBuilderSourceValue.Price => _price;

		decimal? ICandleBuilderSourceValue.Volume => _volume;

		Sides? ICandleBuilderSourceValue.OrderDirection => null;
	}
}