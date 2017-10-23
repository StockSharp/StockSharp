#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: NativeIdSecurityStorage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The storage of information on instruments using native id.
	/// </summary>
	/// <typeparam name="TNativeId">Native id type.</typeparam>
	public abstract class NativeIdSecurityStorage<TNativeId> : Disposable, ISecurityStorage
	{
		private readonly IStorageSecurityList _storageSecurityList;

		/// <summary>
		/// Initializes a new instance of the <see cref="NativeIdSecurityStorage{T}"/>.
		/// </summary>
		/// <param name="storageSecurityList">The storage of securities.</param>
		/// <param name="comparer"><typeparamref name="TNativeId"/> comparer.</param>
		protected NativeIdSecurityStorage(IStorageSecurityList storageSecurityList, IEqualityComparer<TNativeId> comparer)
		{
			if (storageSecurityList == null)
				throw new ArgumentNullException(nameof(storageSecurityList));

			_storageSecurityList = storageSecurityList;
			_cacheByNativeId = new SynchronizedDictionary<TNativeId, Security>(comparer);

			TryAddToCache(storageSecurityList);
		}

		private readonly SynchronizedDictionary<TNativeId, Security> _cacheByNativeId;

		/// <summary>
		/// All available securities.
		/// </summary>
		public IEnumerable<Security> Securities => _cacheByNativeId.Values;

		int ISecurityProvider.Count => _cacheByNativeId.Count;

		/// <summary>
		/// Get native id.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Native (internal) trading system security id.</returns>
		protected abstract TNativeId CreateNativeId(Security security);

		IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
		{
			var nativeId = criteria.ExtensionInfo == null
				? default(TNativeId)
				: CreateNativeId(criteria);

			if (nativeId.IsDefault())
				return _storageSecurityList.Lookup(criteria);

			var security = _cacheByNativeId.TryGetValue(nativeId);
			return security == null ? Enumerable.Empty<Security>() : new[] { security };
		}

		private void TryAddToCache(IEnumerable<Security> securities)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			var newSecurities = new List<Security>();

			foreach (var security in securities)
			{
				var nativeId = security.ExtensionInfo == null ? default(TNativeId) : CreateNativeId(security);

				if (nativeId == null)
					continue;

				bool isNew;
				_cacheByNativeId.SafeAdd(nativeId, key => security, out isNew);

				if (isNew)
					newSecurities.Add(security);
			}

			Added?.Invoke(newSecurities);
		}

		/// <summary>
		/// New instruments added.
		/// </summary>
		public event Action<IEnumerable<Security>> Added;

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add { }
			remove { }
		}

		event Action ISecurityProvider.Cleared
		{
			add { }
			remove { }
		}

		void ISecurityStorage.Save(Security security)
		{
			_storageSecurityList.Save(security);
			TryAddToCache(new[] { security });
		}

		IEnumerable<string> ISecurityStorage.GetSecurityIds()
		{
			return _storageSecurityList.GetSecurityIds();
		}

		void ISecurityStorage.Delete(Security security)
		{
			throw new NotSupportedException();
		}

		void ISecurityStorage.DeleteBy(Security criteria)
		{
			throw new NotSupportedException();
		}
	}
}