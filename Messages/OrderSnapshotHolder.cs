namespace StockSharp.Messages;

/// <summary>
/// <see cref="ExecutionMessage"/> order snapshots holder.
/// </summary>
/// <remarks>
/// Unlike other snapshot holders, this one returns the same reference for the same <see cref="ExecutionMessage.TransactionId"/>.
/// </remarks>
public class OrderSnapshotHolder : BaseLogReceiver
{
	private readonly SynchronizedDictionary<long, ExecutionMessage> _snapshots = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderSnapshotHolder"/>.
	/// </summary>
	public OrderSnapshotHolder()
	{
	}

	/// <summary>
	/// Throw exception on invalid order state transition.
	/// </summary>
	public bool ThrowOnInvalidStateTransition { get; set; }

	/// <summary>
	/// Try get snapshot for the specified transaction id.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="snapshot">Snapshot if exists, otherwise <see langword="null"/>.</param>
	/// <returns><c>true</c> if snapshot exists; otherwise <c>false</c>.</returns>
	public bool TryGetSnapshot(long transactionId, out ExecutionMessage snapshot)
	{
		return _snapshots.TryGetValue(transactionId, out snapshot);
	}

	/// <summary>
	/// Process <see cref="ExecutionMessage"/> change.
	/// </summary>
	/// <param name="execMsg"><see cref="ExecutionMessage"/> change.</param>
	/// <returns>
	/// Order snapshot. Returns the same reference for the same <see cref="ExecutionMessage.TransactionId"/>.
	/// Returns <see langword="null"/> if <see cref="ExecutionMessage.HasOrderInfo"/> is <see langword="false"/>.
	/// </returns>
	public ExecutionMessage Process(ExecutionMessage execMsg)
	{
		if (execMsg is null)
			throw new ArgumentNullException(nameof(execMsg));

		if (!execMsg.HasOrderInfo)
			return null;

		var transactionId = execMsg.TransactionId;

		if (transactionId == 0)
			throw new ArgumentException(LocalizedStrings.TransactionInvalid, nameof(execMsg));

		using (_snapshots.EnterScope())
		{
			if (_snapshots.TryGetValue(transactionId, out var snapshot))
			{
				ApplyChanges(snapshot, execMsg, transactionId);
				return snapshot;
			}
			else
			{
				snapshot = execMsg.TypedClone();
				_snapshots.Add(transactionId, snapshot);
				return snapshot;
			}
		}
	}

	private void ApplyChanges(ExecutionMessage snapshot, ExecutionMessage execMsg, long transactionId)
	{
		if (execMsg.Balance != null)
			snapshot.Balance = snapshot.Balance.ApplyNewBalance(execMsg.Balance.Value, transactionId, this);

		if (execMsg.OrderState != null)
		{
			snapshot.OrderState.VerifyOrderState(execMsg.OrderState.Value, transactionId, this, ThrowOnInvalidStateTransition);
			snapshot.OrderState = execMsg.OrderState.Value;
		}

		if (execMsg.OrderStatus != null)
			snapshot.OrderStatus = execMsg.OrderStatus;

		if (execMsg.OrderId != null)
			snapshot.OrderId = execMsg.OrderId;

		if (!execMsg.OrderStringId.IsEmpty())
			snapshot.OrderStringId = execMsg.OrderStringId;

		if (!execMsg.OrderBoardId.IsEmpty())
			snapshot.OrderBoardId = execMsg.OrderBoardId;

		if (execMsg.PnL != null)
			snapshot.PnL = execMsg.PnL;

		if (execMsg.Position != null)
			snapshot.Position = execMsg.Position;

		if (execMsg.Commission != null)
			snapshot.Commission = execMsg.Commission;

		if (!execMsg.CommissionCurrency.IsEmpty())
			snapshot.CommissionCurrency = execMsg.CommissionCurrency;

		if (execMsg.AveragePrice != null)
			snapshot.AveragePrice = execMsg.AveragePrice;

		if (execMsg.Latency != null)
			snapshot.Latency = execMsg.Latency;

		if (execMsg.ServerTime != default)
			snapshot.ServerTime = execMsg.ServerTime;

		if (execMsg.LocalTime != default)
			snapshot.LocalTime = execMsg.LocalTime;
	}

	/// <summary>
	/// Reset snapshot for the specified transaction id.
	/// </summary>
	/// <param name="transactionId">Transaction ID. Use 0 to clear all snapshots.</param>
	public void ResetSnapshot(long transactionId)
	{
		if (transactionId == 0)
			_snapshots.Clear();
		else
			_snapshots.Remove(transactionId);
	}
}
