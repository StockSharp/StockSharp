namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	class IndexSecurityMarketDataStorage<T> : IMarketDataStorage<T>
	{
		private sealed class IndexMarketDataEnumerator : IEnumerator<T>
		{
			private readonly IEnumerable<IEnumerator<T>> _enumerators;
			private readonly List<IEnumerator<T>> _aliveEnumerators = new List<IEnumerator<T>>();
			private int _currentEnumerator;

			public IndexMarketDataEnumerator(IEnumerable<IEnumerator<T>> enumerators)
			{
				if (enumerators.IsEmpty())
					throw new ArgumentOutOfRangeException("enumerators");

				_enumerators = enumerators;
				_aliveEnumerators.AddRange(_enumerators);
			}

			void IDisposable.Dispose()
			{
				_enumerators.ForEach(e => e.Dispose());
			}

			bool IEnumerator.MoveNext()
			{
				var currentEnumerator = _aliveEnumerators[_currentEnumerator];

				if (currentEnumerator.MoveNext())
				{
					_currentEnumerator++;

					if (_currentEnumerator == _aliveEnumerators.Count)
						_currentEnumerator = 0;

					return true;
				}

				_aliveEnumerators.Remove(currentEnumerator);

				if (_currentEnumerator == _aliveEnumerators.Count)
					_currentEnumerator = 0;

				return _aliveEnumerators.Count > 0;
			}

			void IEnumerator.Reset()
			{
				_enumerators.ForEach(e => e.Reset());
				_aliveEnumerators.AddRange(_enumerators);
				_currentEnumerator = 0;
			}

			public T Current
			{
				get { return _aliveEnumerators[_currentEnumerator].Current; }
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}
		}

		private readonly Func<Security, IMarketDataDrive, IMarketDataStorage<T>> _getStorage;
		private readonly IndexSecurity _security;
		private readonly Func<T, Security> _getSecurity;

		public IndexSecurityMarketDataStorage(IndexSecurity security, object arg, Func<T, Security> getSecurity, Func<Security, IMarketDataDrive, IMarketDataStorage<T>> getStorage, IMarketDataStorageDrive drive)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (security.InnerSecurities.IsEmpty())
				throw new ArgumentOutOfRangeException("security");

			if (getSecurity == null)
				throw new ArgumentNullException("getSecurity");

			if (getStorage == null)
				throw new ArgumentNullException("getStorage");

			if (drive == null)
				throw new ArgumentNullException("drive");

			_security = security;
			_arg = arg;
			_getSecurity = getSecurity;
			_getStorage = getStorage;

			Drive = drive;
		}

		IEnumerable<DateTime> IMarketDataStorage.Dates
		{
			get { return _security.InnerSecurities.SelectMany(s => _getStorage(s, Drive.Drive).Dates).Distinct().OrderBy(); }
		}

		Type IMarketDataStorage.DataType
		{
			get { return typeof(T); }
		}

		Security IMarketDataStorage.Security
		{
			get { return _security; }
		}

		private readonly object _arg;

		object IMarketDataStorage.Arg
		{
			get { return _arg; }
		}

		public IMarketDataStorageDrive Drive { get; private set; }

		private bool _appendOnlyNew = true;

		bool IMarketDataStorage.AppendOnlyNew
		{
			get { return _appendOnlyNew; }
			set
			{
				_appendOnlyNew = value;
				_security.InnerSecurities.ForEach(s => _getStorage(s, Drive.Drive).AppendOnlyNew = value);
			}
		}

		IMarketDataSerializer IMarketDataStorage.Serializer
		{
			get { return ((IMarketDataStorage<T>)this).Serializer; }
		}

		IMarketDataSerializer<T> IMarketDataStorage<T>.Serializer
		{
			get { throw new NotSupportedException(); }
		}

		public void Save(IEnumerable<T> data)
		{
			foreach (var group in data.GroupBy(_getSecurity))
			{
				_getStorage(GetParent(group.Key), Drive.Drive).Save(group);
			}
		}

		public void Delete(IEnumerable<T> data)
		{
			foreach (var group in data.GroupBy(_getSecurity))
			{
				_getStorage(GetParent(group.Key), Drive.Drive).Delete(group);
			}
		}

		void IMarketDataStorage.Delete(IEnumerable data)
		{
			Delete((IEnumerable<T>)data);
		}

		void IMarketDataStorage.Delete(DateTime date)
		{
			_security.InnerSecurities.ForEach(s => _getStorage(s, Drive.Drive).Delete(date));
		}

		void IMarketDataStorage.Save(IEnumerable data)
		{
			Save((IEnumerable<T>)data);
		}

		IEnumerable IMarketDataStorage.Load(DateTime date)
		{
			return Load(date);
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			throw new NotSupportedException();
		}

		public IEnumerableEx<T> Load(DateTime date)
		{
			//return new IndexMarketDataEnumerator(_security.InnerSecurities.Select(s => _getStorage(s).Read(date).GetEnumerator()));
			throw new NotImplementedException();
		}

		public IDataStorageReader<T> GetReader(DateTime date)
		{
			throw new NotImplementedException();
		}

		private Security GetParent(Security security)
		{
			var parent = GetParent(_security, security);

			return parent == _security ? security : parent;
		}

		private static Security GetParent(BasketSecurity root, Security security)
		{
			if (!root.InnerSecurities.Contains(security))
				return root.InnerSecurities.OfType<BasketSecurity>().FirstOrDefault(basket => GetParent(basket, security) != null);
			else
				return root;
		}
	}
}
