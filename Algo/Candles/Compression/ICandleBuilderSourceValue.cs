namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Diagnostics;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс, описывающий данные источника <see cref="ICandleBuilderSource"/>.
	/// </summary>
	public interface ICandleBuilderSourceValue
	{
		/// <summary>
		/// Уникальный идентификатор.
		/// </summary>
		long Id { get; }

		/// <summary>
		/// Инструмент, по которому были сформированы данные.
		/// </summary>
		Security Security { get; }

		/// <summary>
		/// Время появления новых данных.
		/// </summary>
		DateTimeOffset Time { get; }

		/// <summary>
		/// Цена.
		/// </summary>
		decimal Price { get; }

		/// <summary>
		/// Объем.
		/// </summary>
		decimal Volume { get; }

		/// <summary>
		/// Направление заявки.
		/// </summary>
		Sides? OrderDirection { get; }
	}

	/// <summary>
	/// Данные источника <see cref="ICandleBuilderSource"/>, созданные на основе <see cref="Trade"/>.
	/// </summary>
	[DebuggerDisplay("{Trade}")]
	public class TradeCandleBuilderSourceValue : ICandleBuilderSourceValue
	{
		/// <summary>
		/// Создать <see cref="TradeCandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="trade">Тиковая сделка.</param>
		public TradeCandleBuilderSourceValue(Trade trade)
		{
			Trade = trade;
		}

		/// <summary>
		/// Тиковая сделка.
		/// </summary>
		public Trade Trade { get; private set; }

		long ICandleBuilderSourceValue.Id
		{
			get { return Trade.Id; }
		}

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
	/// Данные источника <see cref="ICandleBuilderSource"/>, созданные на основе <see cref="MarketDepth"/>.
	/// </summary>
	[DebuggerDisplay("{Depth}")]
	public class DepthCandleBuilderSourceValue : ICandleBuilderSourceValue
	{
		private static readonly IdGenerator _idGenerator = new MillisecondIncrementalIdGenerator();

		/// <summary>
		/// Создать <see cref="DepthCandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="depth">Стакан.</param>
		public DepthCandleBuilderSourceValue(MarketDepth depth)
		{
			Depth = depth;
		}

		/// <summary>
		/// Стакан.
		/// </summary>
		public MarketDepth Depth { get; private set; }

		long ICandleBuilderSourceValue.Id
		{
			get { return _idGenerator.GetNextId(); }
		}

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