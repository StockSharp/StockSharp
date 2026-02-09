namespace StockSharp.Algo.Testing;

/// <summary>
/// Adapter wrapping <see cref="IMarketEmulator"/> to provide <see cref="IMessageAdapter"/> interface.
/// </summary>
public class MarketEmulatorAdapter : MessageAdapter
{
	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	/// <param name="emulator">The market emulator.</param>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public MarketEmulatorAdapter(IMarketEmulator emulator, IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		Emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
		emulator.NewOutMessageAsync += OnEmulatorNewOutMessage;

		PossibleSupportedMessages = [.. emulator.PossibleSupportedMessages];
	}

	/// <summary>
	/// The market emulator.
	/// </summary>
	public IMarketEmulator Emulator { get; }

	/// <inheritdoc />
	public override bool? IsPositionsEmulationRequired => true;

	/// <inheritdoc />
	public override bool IsSupportTransactionLog => false;

	/// <inheritdoc />
	public override bool IsSupportExecutionsPnL => true;

	/// <inheritdoc />
	public override bool IsAutoReplyOnTransactonalUnsubscription => true;

	/// <inheritdoc />
	public override DateTime CurrentTime => Emulator.CurrentTime;

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		=> Emulator.SendInMessageAsync(message, cancellationToken);

	private ValueTask OnEmulatorNewOutMessage(Message message, CancellationToken cancellationToken)
		=> SendOutMessageAsync(message, cancellationToken);

	/// <inheritdoc />
	public override IMessageAdapter Clone()
		=> throw new NotSupportedException();

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Emulator.NewOutMessageAsync -= OnEmulatorNewOutMessage;
		base.DisposeManaged();
	}
}
