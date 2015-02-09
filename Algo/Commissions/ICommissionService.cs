namespace StockSharp.Algo.Commissions
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// Интерфейс к серверу лицензий.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/commissionservice.svc")]
	public interface ICommissionService
	{
		/// <summary>
		/// Получить список названий комиссий.
		/// </summary>
		/// <param name="sessionId">Идентифиатор сессии.</param>
		/// <returns>Названия комиссий.</returns>
		[OperationContract]
		string[] GetNames(Guid sessionId);

		/// <summary>
		/// Получить правила комиссии по ее названию.
		/// </summary>
		/// <param name="sessionId">Идентифиатор сессии.</param>
		/// <param name="name">Название комиссии.</param>
		/// <returns>Правила комиссии.</returns>
		[OperationContract]
		CommissionRule[] GetRules(Guid sessionId, string name);
	}
}