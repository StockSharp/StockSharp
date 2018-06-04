#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: FilterableSecurityProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Provider of information about instruments supporting search using <see cref="SecurityTrie"/>.
	/// </summary>
	public class FilterableSecurityProvider : Disposable, ISecurityProvider
	{
		private readonly SecurityTrie _trie = new SecurityTrie();

		private readonly ISecurityProvider _provider;
		private readonly bool _ownProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="FilterableSecurityProvider"/>.
		/// </summary>
		/// <param name="provider">Security meta info provider.</param>
		/// <param name="ownProvider"><see langword="true"/> to leave the <paramref name="provider"/> open after the <see cref="FilterableSecurityProvider"/> object is disposed; otherwise, <see langword="false"/>.</param>
		///// <param name="excludeFilter">Filter for instruments exclusion.</param>
		public FilterableSecurityProvider(ISecurityProvider provider, bool ownProvider = false/*, Func<Security, bool> excludeFilter = null*/)
		{
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
			_ownProvider = ownProvider;

			//ExcludeFilter = excludeFilter;

			_provider.Added += AddSecurities;
			_provider.Removed += RemoveSecurities;
			_provider.Cleared += ClearSecurities;

			AddSecurities(_provider.LookupAll());
		}

		/// <summary>
		/// Gets the number of instruments contained in the <see cref="ISecurityProvider"/>.
		/// </summary>
		public int Count => _trie.Count;

		/// <summary>
		/// New instruments added.
		/// </summary>
		public event Action<IEnumerable<Security>> Added;

		/// <summary>
		/// Instruments removed.
		/// </summary>
		public event Action<IEnumerable<Security>> Removed;

		/// <summary>
		/// The storage was cleared.
		/// </summary>
		public event Action Cleared;

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var filter = criteria.Id.IsEmpty()
				? (criteria.IsLookupAll() ? string.Empty : criteria.Code)
				: criteria.Id;

			var securities = _trie.Retrieve(filter);

			if (!criteria.Id.IsEmpty())
				securities = securities.Where(s => s.Id.CompareIgnoreCase(criteria.Id));

			return securities.Filter(criteria);
		}

		private void AddSecurities(IEnumerable<Security> securities)
		{
			securities.ForEach(_trie.Add);
            Added?.Invoke(securities);
		}

		private void RemoveSecurities(IEnumerable<Security> securities)
		{
			_trie.RemoveRange(securities);
            Removed?.Invoke(securities);
		}

		private void ClearSecurities()
		{
			_trie.Clear();
			Cleared?.Invoke();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_provider.Added -= AddSecurities;
			_provider.Removed -= RemoveSecurities;
			_provider.Cleared -= ClearSecurities;

			if (_ownProvider)
				_provider.Dispose();

			base.DisposeManaged();
		}
	}
}
