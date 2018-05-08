namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Message adapter categories.
	/// </summary>
	[Flags]
	[DataContract]
	public enum MessageAdapterCategories
	{
		/// <summary>
		/// Russia.
		/// </summary>
		[EnumMember]
		Russia = 1,

		/// <summary>
		/// US.
		/// </summary>
		[EnumMember]
		US = Russia << 1,

		/// <summary>
		/// Europe.
		/// </summary>
		[EnumMember]
		Europe = US << 1,

		/// <summary>
		/// Asia.
		/// </summary>
		[EnumMember]
		Asia = Europe << 1,

		/// <summary>
		/// Stock.
		/// </summary>
		[EnumMember]
		Stock = Asia << 1,

		/// <summary>
		/// FX.
		/// </summary>
		[EnumMember]
		FX = Stock << 1,

		/// <summary>
		/// Cryptocurrencies.
		/// </summary>
		[EnumMember]
		Crypto = FX << 1,

		/// <summary>
		/// Historical data.
		/// </summary>
		[EnumMember]
		History = Crypto << 1,

		/// <summary>
		/// Real-time.
		/// </summary>
		[EnumMember]
		RealTime = History << 1,

		/// <summary>
		/// Free.
		/// </summary>
		[EnumMember]
		Free = RealTime << 1,

		/// <summary>
		/// Paid.
		/// </summary>
		[EnumMember]
		Paid = Free << 1,

		/// <summary>
		/// Tick data.
		/// </summary>
		[EnumMember]
		Ticks = Paid << 1,

		/// <summary>
		/// Candles data.
		/// </summary>
		[EnumMember]
		Candles = Ticks << 1,

		/// <summary>
		/// Order book data.
		/// </summary>
		[EnumMember]
		MarketDepth = Candles << 1,

		/// <summary>
		/// Level1 data.
		/// </summary>
		[EnumMember]
		Level1 = MarketDepth << 1,

		/// <summary>
		/// Order log data.
		/// </summary>
		[EnumMember]
		OrderLog = Level1 << 1,

		/// <summary>
		/// News data.
		/// </summary>
		[EnumMember]
		News = OrderLog << 1,

		/// <summary>
		/// Transactional adapter.
		/// </summary>
		[EnumMember]
		Transactions = News << 1,

		/// <summary>
		/// Tool.
		/// </summary>
		[EnumMember]
		Tool = Transactions << 1,

		/// <summary>
		/// Futures.
		/// </summary>
		[EnumMember]
		Futures = Tool << 1,

		/// <summary>
		/// Options.
		/// </summary>
		[EnumMember]
		Options = Futures << 1,

		/// <summary>
		/// Commodities.
		/// </summary>
		[EnumMember]
		Commodities = Options << 1,
	}

	/// <summary>
	/// Specifies a categories for message adapter.
	/// </summary>
	public class MessageAdapterCategoryAttribute : Attribute
	{
		/// <summary>
		/// Categories.
		/// </summary>
		public MessageAdapterCategories Categories { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageAdapterCategoryAttribute"/>.
		/// </summary>
		/// <param name="categories">Categories.</param>
		public MessageAdapterCategoryAttribute(MessageAdapterCategories categories)
		{
			Categories = categories;
		}
	}
}