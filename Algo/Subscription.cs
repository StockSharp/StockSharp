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

		private ISubscriptionMessage SubscriptionMessage { get; }

		/// <summary>
		/// Request identifier.
		/// </summary>
		public long TransactionId
		{
			get => SubscriptionMessage.TransactionId;
			set => SubscriptionMessage.TransactionId = value;
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
		/// The message contains information about portfolio.
		/// </summary>
		public PortfolioMessage PortfolioMessage { get; }

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
				SubscriptionMessage = MarketDataMessage = new MarketDataMessage
				{
					DataType = dataType.ToMarketDataType().Value,
					Arg = dataType.Arg,
					IsSubscribe = true
				};

				if (Security != null)
					MarketDataMessage.FillSecurityInfo(Security);
			}
			else if (dataType == DataType.Transactions)
				SubscriptionMessage = OrderStatusMessage = new OrderStatusMessage { IsSubscribe = true };
			else if (dataType == DataType.PositionChanges)
				SubscriptionMessage = PortfolioLookupMessage = new PortfolioLookupMessage { IsSubscribe = true };
			else if (dataType.IsPortfolio)
				SubscriptionMessage = PortfolioMessage = new PortfolioMessage { IsSubscribe = true };
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

		/// <summary>
		/// Initializes a new instance of the <see cref="Subscription"/>.
		/// </summary>
		/// <param name="portfolio">Portfolio, describing the trading account and the size of its generated commission.</param>
		public Subscription(Portfolio portfolio)
			: this(DataType.Portfolio(portfolio.Name), null)
		{
			PortfolioMessage = portfolio.ToMessage();
			PortfolioMessage.IsSubscribe = true;
			Portfolio = portfolio;
		}

		/// <summary>
		/// Portfolio, describing the trading account and the size of its generated commission.
		/// </summary>
		public Portfolio Portfolio { get; }
	}
}