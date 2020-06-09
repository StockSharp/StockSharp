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
	using StockSharp.Messages;

	/// <summary>
	/// Provider of information about instruments supporting search using <see cref="SecurityTrie"/>.
	/// </summary>
	public class FilterableSecurityProvider : Disposable, ISecurityProvider
	{
		private readonly SecurityTrie _trie = new SecurityTrie();

		private readonly ISecurityProvider _provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="FilterableSecurityProvider"/>.
		/// </summary>
		/// <param name="provider">Security meta info provider.</param>
		public FilterableSecurityProvider(ISecurityProvider provider)
		{
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));

			_provider.Added += AddSecurities;
			_provider.Removed += RemoveSecurities;
			_provider.Cleared += ClearSecurities;

			AddSecurities(_provider.LookupAll());
		}

		/// <inheritdoc />
		public int Count => _trie.Count;

		/// <inheritdoc />
		public event Action<IEnumerable<Security>> Added;

		/// <inheritdoc />
		public event Action<IEnumerable<Security>> Removed;

		/// <inheritdoc />
		public event Action Cleared;

		/// <inheritdoc />
		public Security LookupById(SecurityId id) => _trie.GetById(id);

		/// <inheritdoc />
		public IEnumerable<Security> Lookup(SecurityLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var secId = criteria.SecurityId.ToStringId(nullIfEmpty: true);

			var filter = secId.IsEmpty()
				? (criteria.IsLookupAll() ? string.Empty : criteria.SecurityId.SecurityCode)
				: secId;

			var securities = _trie.Retrieve(filter);

			if (!secId.IsEmpty())
				securities = securities.Where(s => s.Id.CompareIgnoreCase(secId));

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

			base.DisposeManaged();
		}
	}
}
