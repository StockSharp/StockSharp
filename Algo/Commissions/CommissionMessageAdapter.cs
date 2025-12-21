namespace StockSharp.Algo.Commissions;

/// <summary>
/// The message adapter, automatically calculating commission.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CommissionMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
/// <param name="commissionManager">The commission calculating manager.</param>
public class CommissionMessageAdapter(IMessageAdapter innerAdapter, ICommissionManager commissionManager) : MessageAdapterWrapper(innerAdapter)
{
	private readonly ICommissionManager _commissionManager = commissionManager ?? throw new ArgumentNullException(nameof(commissionManager));

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		_commissionManager.Process(message);
		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message is ExecutionMessage execMsg && execMsg.DataType == DataType.Transactions && execMsg.Commission == null)
			execMsg.Commission = _commissionManager.Process(execMsg);

		return base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="CommissionMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new CommissionMessageAdapter(InnerAdapter.TypedClone(), _commissionManager.Clone());
	}
}