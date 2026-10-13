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
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		await _commissionManager.ProcessAsync(message, cancellationToken);
		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message is ExecutionMessage execMsg && execMsg.DataType == DataType.Transactions && execMsg.Commission == null)
			execMsg.Commission = await _commissionManager.ProcessAsync(execMsg, cancellationToken);

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="CommissionMessageAdapter"/>.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Copy.</returns>
	public override async ValueTask<IMessageAdapter> CloneAsync(CancellationToken cancellationToken)
	{
		return new CommissionMessageAdapter(await InnerAdapter.CloneAsync(cancellationToken), await _commissionManager.CloneAsync(cancellationToken));
	}
}