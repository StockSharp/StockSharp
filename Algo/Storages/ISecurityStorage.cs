#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: ISecurityStorage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for access to the storage of information on instruments.
	/// </summary>
	public interface ISecurityStorage : ISecurityProvider
	{
		/// <summary>
		/// Save security.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="forced">Forced update.</param>
		void Save(Security security, bool forced);

		/// <summary>
		/// Delete security.
		/// </summary>
		/// <param name="security">Security.</param>
		void Delete(Security security);

		/// <summary>
		/// To delete instruments by the criterion.
		/// </summary>
		/// <param name="criteria">The criterion.</param>
		void DeleteBy(Security criteria);
	}
}