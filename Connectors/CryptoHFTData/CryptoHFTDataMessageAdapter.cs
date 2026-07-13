namespace StockSharp.CryptoHFTData;

/// <summary>
/// Historical market-data adapter for CryptoHFTData.
/// </summary>
public partial class CryptoHFTDataMessageAdapter
{
	private CryptoHFTDataClient _client;

	/// <summary>
	/// Exchange identifiers published by the current CryptoHFTData SDK.
	/// </summary>
	public static IReadOnlyList<string> SupportedExchanges => CryptoHFTDataClient.Exchanges;

	/// <summary>
	/// Initializes a new instance of the <see cref="CryptoHFTDataMessageAdapter"/>.
	/// </summary>
	public CryptoHFTDataMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		this.AddMarketDataSupport();
		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.MarketDepth);
	}

	/// <inheritdoc />
	public override bool IsSupportOrderBookIncrements => true;

	/// <inheritdoc />
	public override string[] AssociatedBoards => [Exchange];

	/// <inheritdoc />
	protected override ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		if (_client is not null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_client = new(Address, Token) { Parent = this };
		return SendOutMessageAsync(new ConnectMessage(), cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		if (_client is null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_client.Dispose();
		_client = null;
		return SendOutMessageAsync(new DisconnectMessage(), cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		_client?.Dispose();
		_client = null;
		await SendOutMessageAsync(new ResetMessage(), cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
		=> default;
}
