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
			if (provider == null)
				throw new ArgumentNullException("provider");

			_provider = provider;
			_ownProvider = ownProvider;

			//ExcludeFilter = excludeFilter;

			_provider.Added += AddSecurity;
			_provider.Removed += RemoveSecurity;
			_provider.Cleared += ClearSecurities;

			_provider.LookupAll().ForEach(AddSecurity);
		}

		/// <summary>
		/// Gets the number of instruments contained in the <see cref="ISecurityProvider"/>.
		/// </summary>
		public int Count
		{
			get { return _trie.Count; }
		}

		/// <summary>
		/// New instrument created.
		/// </summary>
		public event Action<Security> Added;

		/// <summary>
		/// Instrument deleted.
		/// </summary>
		public event Action<Security> Removed;

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
				throw new ArgumentNullException("criteria");

			var filter = criteria.Id.IsEmpty()
				? (criteria.IsLookupAll() ? string.Empty : criteria.Code.ToLowerInvariant())
				: criteria.Id.ToLowerInvariant();

			var securities = _trie.Retrieve(filter);

			if (!criteria.Id.IsEmpty())
				securities = securities.Where(s => s.Id.CompareIgnoreCase(criteria.Id));

			return securities;
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}

		private void AddSecurity(Security security)
		{
			_trie.Add(security);
			Added.SafeInvoke(security);
		}

		private void RemoveSecurity(Security security)
		{
			_trie.Remove(security);
			Removed.SafeInvoke(security);
		}

		private void ClearSecurities()
		{
			_trie.Clear();
			Cleared.SafeInvoke();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_provider.Added -= AddSecurity;
			_provider.Removed -= RemoveSecurity;
			_provider.Cleared -= ClearSecurities;

			if (_ownProvider)
				_provider.Dispose();

			base.DisposeManaged();
		}
	}
}
