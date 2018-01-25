#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: SecurityTrie.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using Gma.DataStructures.StringSearch;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Security trie collection.
	/// </summary>
	public class SecurityTrie : ICollection<Security>
	{
		private readonly SyncObject _sync = new SyncObject();

		private readonly HashSet<Security> _allSecurities = new HashSet<Security>();
		private readonly ITrie<Security> _trie = new SuffixTrie<Security>(1);

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityTrie"/>.
		/// </summary>
		public SecurityTrie()
		{
		}

		/// <summary>
		/// Gets the number of instruments contained in the <see cref="SecurityTrie"/>.
		/// </summary>
		public int Count
		{
			get
			{
				lock (_sync)
					return _allSecurities.Count;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, <see langword="false"/>.
		/// </returns>
		public bool IsReadOnly => false;

		/// <summary>
		/// Add new instrument.
		/// </summary>
		/// <param name="security">New instrument.</param>
		public void Add(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var externalId = security.ExternalId;

			lock (_sync)
			{
				AddSuffix(security.Id, security);
				AddSuffix(security.Code, security);
				//AddSuffix(security.Name, security);
				//AddSuffix(security.ShortName, security);
				AddSuffix(externalId.Bloomberg, security);
				AddSuffix(externalId.Cusip, security);
				AddSuffix(externalId.Isin, security);
				AddSuffix(externalId.Ric, security);
				AddSuffix(externalId.Sedol, security);

				_allSecurities.Add(security);
			}
		}

		private void AddSuffix(string text, Security security)
		{
			if (text.IsEmpty())
				return;

			_trie.Add(text.ToLowerInvariant(), security);
		}

		/// <summary>
		/// Find all instrument by filter.
		/// </summary>
		/// <param name="filter">Filter</param>
		/// <returns>Found instruments.</returns>
		public IEnumerable<Security> Retrieve(string filter)
		{
			lock (_sync)
				return (filter.IsEmpty() ? _allSecurities : _trie.Retrieve(filter.ToLowerInvariant())).ToArray();
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">Destination array.</param>
		/// <param name="arrayIndex">Start index.</param>
		public void CopyTo(Security[] array, int arrayIndex)
		{
			lock (_sync)
				_allSecurities.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Remove the instrument.
		/// </summary>
		/// <param name="security">The instrument.</param>
		/// <returns><see langword="true"/> if <paramref name="security"/> was successfully removed from the <see cref="SecurityTrie"/>; otherwise, <see langword="false"/>.</returns>
		public bool Remove(Security security)
		{
			lock (_sync)
			{
				_trie.Remove(security);
				return _allSecurities.Remove(security);
			}
		}

		/// <summary>
		/// Remove the instruments.
		/// </summary>
		/// <param name="securities">The instruments.</param>
		public void RemoveRange(IEnumerable<Security> securities)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			securities = securities.ToArray();

			lock (_sync)
			{
				if (securities.Count() > 1000 || (_allSecurities.Count > 1000 && securities.Count() > _allSecurities.Count * 0.1))
				{
					_allSecurities.RemoveRange(securities);

					securities = _allSecurities.ToArray();

					_allSecurities.Clear();
					_trie.Clear();

					securities.ForEach(Add);
				}
				else
				{
					_trie.RemoveRange(securities);
					_allSecurities.RemoveRange(securities);
				}
			}
		}

		/// <summary>
		/// Remove all instruments.
		/// </summary>
		public virtual void Clear()
		{
			lock (_sync)
			{
				_trie.Clear();
				_allSecurities.Clear();
			}
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		public bool Contains(Security item)
		{
			lock (_sync)
				return _allSecurities.Contains(item);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<Security> GetEnumerator()
		{
			lock (_sync)
				return _allSecurities.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}