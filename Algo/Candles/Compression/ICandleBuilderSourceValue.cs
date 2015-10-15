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
		decimal Volume { get; }

		/// <summary>
		/// Order side.
		/// </summary>
		Sides? OrderDirection { get; }
	}

	/// <summary>
	/// The <see cref="ICandleBuilderSource"/> source data is created on basis of <see cref="TradeCandleBuilderSourceValue.Trade"/>.
	/// </summary>
	[DebuggerDisplay("{Trade}")]
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
		public Trade Trade { get; private set; }

		Security ICandleBuilderSourceValue.Security
		{
			get { return Trade.Security; }
		}

		DateTimeOffset ICandleBuilderSourceValue.Time
		{
			get { return Trade.Time; }
		}

		decimal ICandleBuilderSourceValue.Price
		{
			get { return Trade.Price; }
		}

		decimal ICandleBuilderSourceValue.Volume
		{
			get { return Trade.Volume; }
		}

		Sides? ICandleBuilderSourceValue.OrderDirection
		{
			get { return Trade.OrderDirection; }
		}
	}

	/// <summary>
	/// The <see cref="ICandleBuilderSource"/> source data is created on basis of <see cref="Trade"/>.
	/// </summary>
	[DebuggerDisplay("{Tick}")]
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
		public ExecutionMessage Tick { get; private set; }

		private readonly Security _security;

		Security ICandleBuilderSourceValue.Security
		{
			get { return _security; }
		}

		DateTimeOffset ICandleBuilderSourceValue.Time
		{
			get { return Tick.ServerTime; }
		}

		decimal ICandleBuilderSourceValue.Price
		{
			get { return Tick.TradePrice ?? 0; }
		}

		decimal ICandleBuilderSourceValue.Volume
		{
			get { return Tick.Volume ?? 0; }
		}

		Sides? ICandleBuilderSourceValue.OrderDirection
		{
			get { return Tick.OriginSide; }
		}
	}

	/// <summary>
	/// The <see cref="ICandleBuilderSource"/> source data is created on basis of <see cref="MarketDepth"/>.
	/// </summary>
	[DebuggerDisplay("{Depth}")]
	public class DepthCandleBuilderSourceValue : ICandleBuilderSourceValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DepthCandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="depth">Market depth.</param>
		public DepthCandleBuilderSourceValue(MarketDepth depth)
		{
			Depth = depth;
		}

		/// <summary>
		/// Market depth.
		/// </summary>
		public MarketDepth Depth { get; private set; }

		Security ICandleBuilderSourceValue.Security
		{
			get { return Depth.Security; }
		}

		DateTimeOffset ICandleBuilderSourceValue.Time
		{
			get { return Depth.LastChangeTime; }
		}

		decimal ICandleBuilderSourceValue.Price
		{
			get
			{
				var pair = Depth.BestPair;
				return pair == null ? 0 : pair.MiddlePrice ?? 0;
			}
		}

		decimal ICandleBuilderSourceValue.Volume
		{
			get { return Depth.TotalBidsVolume - Depth.TotalAsksVolume; }
		}

		Sides? ICandleBuilderSourceValue.OrderDirection
		{
			get { return null; }
		}
	}
}