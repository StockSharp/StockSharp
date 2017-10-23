#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: ISecurityProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The interface for access to provider of information about instruments.
	/// </summary>
	public interface ISecurityProvider : IDisposable
	{
		/// <summary>
		/// Gets the number of instruments contained in the <see cref="ISecurityProvider"/>.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// New instruments added.
		/// </summary>
		event Action<IEnumerable<Security>> Added;

		/// <summary>
		/// Instruments removed.
		/// </summary>
		event Action<IEnumerable<Security>> Removed;

		/// <summary>
		/// The storage was cleared.
		/// </summary>
		event Action Cleared;

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		IEnumerable<Security> Lookup(Security criteria);
	}
}