namespace StockSharp.Algo.History
{
	using System;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface of the loader of information about instruments.
	/// </summary>
	public interface ISecurityDownloader
	{
		/// <summary>
		/// Download new securities.
		/// </summary>
		/// <param name="securityStorage">Securities meta info storage.</param>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <param name="newSecurity">The handler through which a new instrument will be passed.</param>
		/// <param name="isCancelled">The handler which returns an attribute of search cancel.</param>
		void Refresh(ISecurityStorage securityStorage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled);
	}
}