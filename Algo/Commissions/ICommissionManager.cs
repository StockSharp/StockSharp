namespace StockSharp.Algo.Commissions;

/// <summary>
/// The commission calculating manager interface.
/// </summary>
public interface ICommissionManager : IAsyncPersistable
{
	/// <summary>
	/// Create a copy of the manager.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Copy.</returns>
	ValueTask<ICommissionManager> CloneAsync(CancellationToken cancellationToken);

	/// <summary>
	/// The list of commission calculating rules.
	/// </summary>
	ISynchronizedCollection<ICommissionRule> Rules { get; }

	/// <summary>
	/// Total commission.
	/// </summary>
	decimal Commission { get; }

	/// <summary>
	/// To reset the state.
	/// </summary>
	void Reset();

	/// <summary>
	/// To calculate commission.
	/// </summary>
	/// <param name="message">The message containing the information about the order or own trade.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
	ValueTask<decimal?> ProcessAsync(Message message, CancellationToken cancellationToken);
}