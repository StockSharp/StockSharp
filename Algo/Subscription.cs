namespace StockSharp.Algo
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using DataType = StockSharp.Messages.DataType;

	/// <summary>
	/// Subscription states.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum SubscriptionStates
	{
		/// <summary>
		/// Stopped.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str3178Key)]
		Stopped,

		/// <summary>
		/// Active.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str2229Key)]
		Active,

		/// <summary>
		/// Error.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str152Key)]
		Error,

		/// <summary>
		/// Finished.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FinishedKey)]
		Finished,

		/// <summary>
		/// Online.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OnlineKey)]
		Online,
	}

	/// <summary>
	/// Subscription.
	/// </summary>
	public class Subscription
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		public SecurityId? SecurityId => (SubscriptionMessage as ISecurityIdMessage)?.SecurityId;

		/// <summary>
		/// Data type info.
		/// </summary>
		public DataType DataType => SubscriptionMessage.DataType;

		/// <summary>
		/// Subscription message.
		/// </summary>
		public ISubscriptionMessage SubscriptionMessage { get; }

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
		/// Portfolio, describing the trading account and the size of its generated commission.
		/// </summary>
		public Portfolio Portfolio { get; }

		/// <summary>
		/// State.
		/// </summary>
		public SubscriptionStates State { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Subscription"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="security">Security.</param>
		public Subscription(DataType dataType, SecurityMessage security)
			: this(dataType.ToSubscriptionMessage(), security)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Subscription"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="security">Security.</param>
		public Subscription(DataType dataType, Security security)
			: this(dataType.ToSubscriptionMessage(), security)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Subscription"/>.
		/// </summary>
		/// <param name="candleSeries">Candles series.</param>
		public Subscription(CandleSeries candleSeries)
			: this(candleSeries.ToMarketDataMessage(true), candleSeries.Security)
		{
			CandleSeries = candleSeries;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Subscription"/>.
		/// </summary>
		/// <param name="portfolio">Portfolio, describing the trading account and the size of its generated commission.</param>
		public Subscription(Portfolio portfolio)
			: this(portfolio.ToMessage(), (SecurityMessage)null)
		{
			Portfolio = portfolio;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Subscription"/>.
		/// </summary>
		/// <param name="subscriptionMessage">Subscription message.</param>
		/// <param name="security">Security.</param>
		public Subscription(ISubscriptionMessage subscriptionMessage, Security security = null)
			: this(subscriptionMessage, security?.ToMessage(copyExtendedId: true))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Subscription"/>.
		/// </summary>
		/// <param name="subscriptionMessage">Subscription message.</param>
		/// <param name="security">Security.</param>
		public Subscription(ISubscriptionMessage subscriptionMessage, SecurityMessage security = null)
		{
			SubscriptionMessage = subscriptionMessage ?? throw new ArgumentNullException(nameof(subscriptionMessage));
			SubscriptionMessage.IsSubscribe = true;

			if (security == null)
				return;

			switch (subscriptionMessage)
			{
				case MarketDataMessage mdMsg:
					security.CopyTo(mdMsg, false);
					break;
				case ISecurityIdMessage secIdMsg:
					secIdMsg.SecurityId = security.SecurityId;
					break;
				case INullableSecurityIdMessage nullSecIdMsg:
					nullSecIdMsg.SecurityId = security.SecurityId;
					break;
			}
		}

		/// <inheritdoc />
		public override string ToString() => SubscriptionMessage.ToString();
	}
}