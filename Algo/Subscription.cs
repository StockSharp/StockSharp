namespace StockSharp.Algo
{
	using System;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Subscription.
	/// </summary>
	public class Subscription
	{
		/// <summary>
		/// Security.
		/// </summary>
		public Security Security { get; }

		/// <summary>
		/// Data type info.
		/// </summary>
		public DataType DataType { get; }

		/// <summary>
		/// Request identifier.
		/// </summary>
		public long TransactionId
		{
			get
			{
				if (MarketDataMessage != null)
					return MarketDataMessage.TransactionId;
				else if (OrderStatusMessage != null)
					return OrderStatusMessage.TransactionId;
				else if (PortfolioLookupMessage != null)
					return PortfolioLookupMessage.TransactionId;
				else
					throw new ArgumentOutOfRangeException(nameof(DataType), DataType, LocalizedStrings.Str1219);
			}
			set
			{
				if (MarketDataMessage != null)
					MarketDataMessage.TransactionId = value;
				else if (OrderStatusMessage != null)
					OrderStatusMessage.TransactionId = value;
				else if (PortfolioLookupMessage != null)
					PortfolioLookupMessage.TransactionId = value;
				else
					throw new ArgumentOutOfRangeException(nameof(DataType), DataType, LocalizedStrings.Str1219);
			}
		}

		/// <summary>
		/// Candles series.
		/// </summary>
		public CandleSeries CandleSeries { get; }

		/// <summary>
		/// Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).
		/// </summary>
		public MarketDataMessage MarketDataMessage { get; }

		/// <summary>
		/// A message requesting current registered orders and trades.
		/// </summary>
		public OrderStatusMessage OrderStatusMessage { get; }

		/// <summary>
		/// Message portfolio lookup for specified criteria.
		/// </summary>
		public PortfolioLookupMessage PortfolioLookupMessage { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Subscription"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="security">Security.</param>
		public Subscription(DataType dataType, Security security)
		{
			DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
			Security = security;

			if (dataType.IsMarketData)
			{
				MarketDataMessage = new MarketDataMessage
				{
					DataType = dataType.ToMarketDataType().Value,
					Arg = dataType.Arg
				};

				if (Security != null)
					MarketDataMessage.FillSecurityInfo(Security);
			}
			else if (dataType == DataType.Transactions)
				OrderStatusMessage = new OrderStatusMessage { IsSubscribe = true };
			else if (dataType == DataType.PositionChanges)
				PortfolioLookupMessage = new PortfolioLookupMessage { IsSubscribe = true };
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1219);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Subscription"/>.
		/// </summary>
		/// <param name="candleSeries">Candles series.</param>
		public Subscription(CandleSeries candleSeries)
			: this(candleSeries.ToDataType(), candleSeries.Security)
		{
			CandleSeries = candleSeries;
		}
	}
}