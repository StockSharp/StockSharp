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
		private readonly List<T> _addedItems = new List<T>();
		private readonly SyncObject _syncRoot = new SyncObject();

		private bool _isChanged;
		private bool _isFullChanged;

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

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public DelayAction DelayAction { get; set; }

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
		public void Save(T entity)
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
			lock (_syncRoot)
			{
				_isChanged = true;
				_isFullChanged = true;
			}

			Flush();
		}

		private void Write(T entity)
		{
			lock (_syncRoot)
			{
				_isChanged = true;
				_addedItems.Add(entity);
			}

			Flush();
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

		private void Flush()
		{
			DelayAction.Add(() =>
			{
				if (OnFlush())
					return;

				Flush();
			});
		}

		private bool OnFlush()
		{
			bool isChanged;
			bool isFullChanged;

			var addedItems = ArrayHelper.Empty<T>();

			lock (_syncRoot)
			{
				isChanged = _isChanged;
				isFullChanged = _isFullChanged;

				_isChanged = false;

				if (!isChanged)
				{
					_isFullChanged = false;
					addedItems = _addedItems.CopyAndClear();
				}
			}

			if (isChanged)
				return false;

			if (isFullChanged)
			{
				ClearCache();
				Write(_items.CachedValues, false);
			}
			else if (addedItems.Length > 0)
			{
				Write(addedItems, true);
			}

			return true;
		}

		/// <summary>
		/// Clear cache.
		/// </summary>
		protected virtual void ClearCache()
		{
			
		}

		private void Write(IEnumerable<T> items, bool append)
		{
			using (var stream = new FileStream(_fileName, FileMode.OpenOrCreate))
			{
				if (append)
					stream.Position = stream.Length;
				else
					stream.SetLength(0);

				using (var writer = new CsvFileWriter(stream, Registry.Encoding))
				{
					foreach (var item in items)
						Write(writer, item);
				}
			}
		}
	}
}