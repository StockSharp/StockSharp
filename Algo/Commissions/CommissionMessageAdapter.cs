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
	protected override bool OnSendInMessage(Message message)
	{
		_commissionManager.Process(message);
		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		if (message is ExecutionMessage execMsg && execMsg.DataType == DataType.Transactions && execMsg.Commission == null)
			execMsg.Commission = _commissionManager.Process(execMsg);

		base.OnInnerAdapterNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="CommissionMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new CommissionMessageAdapter(InnerAdapter.TypedClone(), _commissionManager.Clone());
	}
}