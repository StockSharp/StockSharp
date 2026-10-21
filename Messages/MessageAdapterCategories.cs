namespace StockSharp.Messages;

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
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RussiaKey)]
	Russia = 1,

	/// <summary>
	/// US.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UsKey)]
	US = Russia << 1,

	/// <summary>
	/// Europe.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EuropeKey)]
	Europe = US << 1,

	/// <summary>
	/// Asia.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AsiaKey)]
	Asia = Europe << 1,

	/// <summary>
	/// Stock.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StockKey)]
	Stock = Asia << 1,

	/// <summary>
	/// FX.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ForexKey)]
	FX = Stock << 1,

	/// <summary>
	/// Cryptocurrencies.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CryptoKey)]
	Crypto = FX << 1,

	/// <summary>
	/// History.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HistoryKey)]
	History = Crypto << 1,

	/// <summary>
	/// Real-time.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RealTimeKey)]
	RealTime = History << 1,

	/// <summary>
	/// Free.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FreeKey)]
	Free = RealTime << 1,

	/// <summary>
	/// Paid.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PaidKey)]
	Paid = Free << 1,

	/// <summary>
	/// Ticks.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TicksKey)]
	Ticks = Paid << 1,

	/// <summary>
	/// Candles.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CandlesKey)]
	Candles = Ticks << 1,

	/// <summary>
	/// Order book.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketDepthKey)]
	MarketDepth = Candles << 1,

	/// <summary>
	/// Level1.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Level1Key)]
	Level1 = MarketDepth << 1,

	/// <summary>
	/// Order log.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderLogKey)]
	OrderLog = Level1 << 1,

	/// <summary>
	/// News.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NewsKey)]
	News = OrderLog << 1,

	/// <summary>
	/// Transactions.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TransactionsKey)]
	Transactions = News << 1,

	/// <summary>
	/// Tool.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ToolsKey)]
	Tool = Transactions << 1,

	/// <summary>
	/// Futures.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FuturesKey)]
	Futures = Tool << 1,

	/// <summary>
	/// Options.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OptionsKey)]
	Options = Futures << 1,

	/// <summary>
	/// Commodities.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommoditiesKey)]
	Commodities = Options << 1,
}

/// <summary>
/// Specifies a categories for message adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MessageAdapterCategoryAttribute"/>.
/// </remarks>
/// <param name="categories">Categories.</param>
public class MessageAdapterCategoryAttribute(MessageAdapterCategories categories) : Attribute
{
	/// <summary>
	/// Categories.
	/// </summary>
	public MessageAdapterCategories Categories { get; } = categories;
}