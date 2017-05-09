namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// List of trade objects, received from the CSV storage.
	/// </summary>
	/// <typeparam name="T">Entity type.</typeparam>
	public abstract class CsvEntityList<T> : SynchronizedList<T>, IStorageEntityList<T>
		where T : class
	{
		private readonly string _fileName;

		private readonly CachedSynchronizedDictionary<object, T> _items = new CachedSynchronizedDictionary<object, T>();

		/// <summary>
		/// The CSV storage of trading objects.
		/// </summary>
		protected CsvEntityRegistry Registry { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvEntityList{T}"/>.
		/// </summary>
		/// <param name="registry">The CSV storage of trading objects.</param>
		/// <param name="fileName">CSV file name.</param>
		protected CsvEntityList(CsvEntityRegistry registry, string fileName)
		{
			if (registry == null)
				throw new ArgumentNullException(nameof(registry));

			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));

			Registry = registry;

			_fileName = Path.Combine(Registry.Path, fileName);
		}

		#region IStorageEntityList<T>

		private DelayAction.Group _delayActionGroup;
		private DelayAction _delayAction;

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public DelayAction DelayAction
		{
			get { return _delayAction; }
			set
			{
				if (_delayAction == value)
					return;

				if (_delayAction != null)
				{
					_delayAction.DeleteGroup(_delayActionGroup);
					_delayActionGroup = null;
				}

				_delayAction = value;

				if (_delayAction != null)
				{
					_delayActionGroup = _delayAction.CreateGroup(() => new CsvFileWriter(new FileStream(_fileName, FileMode.Append), Registry.Encoding));
				}
			}
		}

		T IStorageEntityList<T>.ReadById(object id)
		{
			return _items.TryGetValue(NormalizedKey(id));
		}

		IEnumerable<T> IStorageEntityList<T>.ReadLasts(int count)
		{
			return _items.CachedValues.Skip(Count - count).Take(count);
		}

		private object GetNormalizedKey(T entity)
		{
			return NormalizedKey(GetKey(entity));
		}

		private static object NormalizedKey(object key)
		{
			var str = key as string;

			if (str != null)
				return str.ToLowerInvariant();

			return key;
		}

		/// <summary>
		/// Save object into storage.
		/// </summary>
		/// <param name="entity">Trade object.</param>
		public virtual void Save(T entity)
		{
			var item = _items.TryGetValue(GetNormalizedKey(entity));

			if (item == null)
				Add(entity);
			else if (IsChanged(entity))
				Write();
		}

		#endregion

		/// <summary>
		/// Is <paramref name="entity"/> changed.
		/// </summary>
		/// <param name="entity">Trade object.</param>
		/// <returns>Is changed.</returns>
		protected virtual bool IsChanged(T entity)
		{
			return true;
		}

		/// <summary>
		/// Get key from trade object.
		/// </summary>
		/// <param name="item">Trade object.</param>
		/// <returns>The key.</returns>
		protected abstract object GetKey(T item);

		/// <summary>
		/// Write data into CSV.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Trade object.</param>
		protected abstract void Write(CsvFileWriter writer, T data);

		/// <summary>
		/// Read data from CSV.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <returns>Trade object.</returns>
		protected abstract T Read(FastCsvReader reader);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item">Trade object.</param>
		protected override void OnAdded(T item)
		{
			base.OnAdded(item);

			if (_items.TryAdd(GetNormalizedKey(item), item))
				Write(item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item">Trade object.</param>
		protected override void OnRemoved(T item)
		{
			base.OnRemoved(item);

			_items.Remove(GetNormalizedKey(item));
			Write();
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void OnCleared()
		{
			base.OnCleared();

			_items.Clear();
			Write();
		}

		private void Write()
		{
			_delayActionGroup.Add(() =>
			{
				ClearCache();

				using (var writer = new CsvFileWriter(new FileStream(_fileName, FileMode.Create), Registry.Encoding))
				{
					foreach (var item in _items.CachedValues)
						Write(writer, item);
				}
			}, canBatch: false);
		}

		private void Write(T entity)
		{
			_delayActionGroup.Add(s =>
			{
				Write((CsvFileWriter)s, entity);
			});
		}

		internal void ReadItems(List<Exception> errors)
		{
			if (errors == null)
				throw new ArgumentNullException(nameof(errors));

			if (!File.Exists(_fileName))
				return;

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				using (var stream = new FileStream(_fileName, FileMode.OpenOrCreate))
				{
					var reader = new FastCsvReader(stream, Registry.Encoding);

					while (reader.NextLine())
					{
						try
						{
							var item = Read(reader);
							var key = GetNormalizedKey(item);

							_items.Add(key, item);
							Add(item);
						}
						catch (Exception ex)
						{
							if (errors.Count < 10)
								errors.Add(ex);
							else
								break;
						}
					}
				}
			});
		}

		/// <summary>
		/// Clear cache.
		/// </summary>
		protected virtual void ClearCache()
		{
		}
	}
}