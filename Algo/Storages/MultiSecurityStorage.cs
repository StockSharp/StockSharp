namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Security storage for multiple inner storages (regular, index and continuous).
	/// </summary>
	public class MultiSecurityStorage : ISecurityStorage
	{
		private readonly ISecurityStorage _regular;
		private readonly ISecurityStorage _index;
		private readonly ISecurityStorage _continuous;

		/// <summary>
		/// Initializes a new instance of the <see cref="MultiSecurityStorage"/>.
		/// </summary>
		/// <param name="regular">Security storage of <see cref="Security"/>.</param>
		/// <param name="index">Security storage of <see cref="IndexSecurity"/>.</param>
		/// <param name="continuous">Security storage of <see cref="ContinuousSecurity"/>.</param>
		public MultiSecurityStorage(ISecurityStorage regular, ISecurityStorage index, ISecurityStorage continuous)
		{
			_regular = regular ?? throw new ArgumentNullException(nameof(regular));
			_index = index ?? throw new ArgumentNullException(nameof(index));
			_continuous = continuous ?? throw new ArgumentNullException(nameof(continuous));

			_regular.Added += OnSecuritiesAdded;
			_regular.Removed += OnSecuritiesRemoved;

			_index.Added += OnSecuritiesAdded;
			_index.Removed += OnSecuritiesRemoved;

			_continuous.Added += OnSecuritiesAdded;
			_continuous.Removed += OnSecuritiesRemoved;
		}

		private void OnSecuritiesAdded(IEnumerable<Security> securities)
		{
			_added?.Invoke(securities);
		}

		private void OnSecuritiesRemoved(IEnumerable<Security> securities)
		{
			_removed?.Invoke(securities);
		}

		void IDisposable.Dispose()
		{
			_regular.Dispose();
			_index.Dispose();
			_continuous.Dispose();
		}

		int ISecurityProvider.Count => _regular.Count + _index.Count + _continuous.Count;

		private Action<IEnumerable<Security>> _added;

		event Action<IEnumerable<Security>> ISecurityProvider.Added
		{
			add => _added += value;
			remove => _added -= value;
		}

		private Action<IEnumerable<Security>> _removed;

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add => _removed += value;
			remove => _removed -= value;
		}

		event Action ISecurityProvider.Cleared
		{
			add { }
			remove { }
		}

		IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
		{
			return _regular.Lookup(criteria)
			               .Concat(_index.Lookup(criteria))
			               .Concat(_continuous.Lookup(criteria));
		}

		void ISecurityStorage.Save(Security security, bool forced)
		{
			switch (security)
			{
				case IndexSecurity _:
					_index.Save(security, forced);
					break;
				case ContinuousSecurity _:
					_continuous.Save(security, forced);
					break;
				default:
					_regular.Save(security, forced);
					break;
			}
		}

		void ISecurityStorage.Delete(Security security)
		{
			switch (security)
			{
				case IndexSecurity _:
					_index.Delete(security);
					break;
				case ContinuousSecurity _:
					_continuous.Delete(security);
					break;
				default:
					_regular.Delete(security);
					break;
			}
		}

		void ISecurityStorage.DeleteBy(Security criteria)
		{
			_index.Delete(criteria);
			_continuous.Delete(criteria);
			_regular.Delete(criteria);
		}
	}
}