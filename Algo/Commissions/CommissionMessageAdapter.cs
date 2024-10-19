namespace StockSharp.Algo.Commissions;

/// <summary>
/// The message adapter, automatically calculating commission.
/// </summary>
public class CommissionMessageAdapter : MessageAdapterWrapper
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CommissionMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	public CommissionMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
	}

	private ICommissionManager _commissionManager = new CommissionManager();

	/// <summary>
	/// The commission calculating manager.
	/// </summary>
	public ICommissionManager CommissionManager
	{
		get => _commissionManager;
		set => _commissionManager = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		CommissionManager.Process(message);
		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		if (message is ExecutionMessage execMsg && execMsg.DataType == DataType.Transactions && execMsg.Commission == null)
			execMsg.Commission = CommissionManager.Process(execMsg);

		base.OnInnerAdapterNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="CommissionMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new CommissionMessageAdapter(InnerAdapter.TypedClone());
	}
}