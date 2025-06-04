namespace StockSharp.Messages;

/// <summary>
/// Portfolio states.
/// </summary>
[DataContract]
[Serializable]
public enum PortfolioStates
{
	/// <summary>
	/// Active.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ActiveKey)]
	Active,
	
	/// <summary>
	/// Blocked.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BlockedKey)]
	Blocked,
}

/// <summary>
/// The message contains information about portfolio.
/// </summary>
[DataContract]
[Serializable]
public class PortfolioMessage : BaseSubscriptionIdMessage<PortfolioMessage>, IPortfolioNameMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.PortfolioNameKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string PortfolioName { get; set; }

	/// <summary>
	/// Portfolio currency.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrencyKey,
		Description = LocalizedStrings.PortfolioCurrencyKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public CurrencyTypes? Currency { get; set; }

	/// <summary>
	/// Electronic board code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BoardKey,
		Description = LocalizedStrings.BoardCodeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string BoardCode { get; set; }

	/// <summary>
	/// Client code assigned by the broker.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClientCodeKey,
		Description = LocalizedStrings.ClientCodeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string ClientCode { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PortfolioMessage"/>.
	/// </summary>
	public PortfolioMessage()
		: base(MessageTypes.Portfolio)
	{
	}

	/// <summary>
	/// Initialize <see cref="PortfolioMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected PortfolioMessage(MessageTypes type)
		: base(type)
	{
	}

	/// <inheritdoc />
	public override DataType DataType => PortfolioName.Portfolio();

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",Name={PortfolioName}";

		if (Currency != default)
			str += $",Curr={Currency}";

		if (!BoardCode.IsEmpty())
			str += $",Board={BoardCode}";

		return str;
	}

	/// <inheritdoc />
	public override void CopyTo(PortfolioMessage destination)
	{
		base.CopyTo(destination);

		destination.PortfolioName = PortfolioName;
		destination.Currency = Currency;
		destination.BoardCode = BoardCode;
		destination.ClientCode = ClientCode;
	}
}