namespace StockSharp.Algo.Commissions
{
	using Ecng.Collections;

	/// <summary>
	/// The commission calculating manager interface.
	/// </summary>
	public interface ICommissionManager : ICommissionRule
	{
		/// <summary>
		/// The list of commission calculating rules.
		/// </summary>
		ISynchronizedCollection<ICommissionRule> Rules { get; }

		///// <summary>
		///// Ñóììàðíîå çíà÷åíèå êîìèññèè, ñãðóïïèðîâàííîå ïî òèïàì.
		///// </summary>
		//IDictionary<CommissionTypes, decimal> CommissionPerTypes { get; }
	}
}