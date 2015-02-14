namespace StockSharp.Algo.Commissions
{
	using Ecng.Collections;

	/// <summary>
	/// Интерфейс менеджера расчета комиссии.
	/// </summary>
	public interface ICommissionManager : ICommissionRule
	{
		/// <summary>
		/// Список правил вычисления комиссии.
		/// </summary>
		ISynchronizedCollection<ICommissionRule> Rules { get; }

		///// <summary>
		///// Суммарное значение комиссии, сгруппированное по типам.
		///// </summary>
		//IDictionary<CommissionTypes, decimal> CommissionPerTypes { get; }
	}
}