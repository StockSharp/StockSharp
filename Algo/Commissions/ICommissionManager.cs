namespace StockSharp.Algo.Commissions;

/// <summary>
/// The commission calculating manager interface.
/// </summary>
public interface ICommissionManager : IPersistable
{
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
	/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
	decimal? Process(Message message);
}