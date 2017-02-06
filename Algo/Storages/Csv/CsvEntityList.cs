namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;

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
		private readonly Encoding _encoding;

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
		/// <param name="encoding">CSV encoding.</param>
		protected CsvEntityList(CsvEntityRegistry registry, string fileName, Encoding encoding)
		{
			if (registry == null)
				throw new ArgumentNullException(nameof(registry));

			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));

			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));

			Registry = registry;

			_fileName = System.IO.Path.Combine(Registry.Path, fileName);
			_encoding = encoding;
		}

		#region IStorageEntityList<T>

		DelayAction IStorageEntityList<T>.DelayAction { get; set; }

		T IStorageEntityList<T>.ReadById(object id)
		{
			return _items.TryGetValue(id);
		}

		IEnumerable<T> IStorageEntityList<T>.ReadLasts(int count)
		{
			return _items.CachedValues.Skip(Count - count).Take(count);
		}

		/// <summary>
		/// Save object into storage.
		/// </summary>
		/// <param name="entity">Trade object.</param>
		public void Save(T entity)
		{
			var key = GetKey(entity);
			var item = _items.TryGetValue(key);

			if (item == null)
				Add(entity);
			else
				Write();
		}

		#endregion

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

			if (_items.TryAdd(GetKey(item), item))
				Write(item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item">Trade object.</param>
		protected override void OnRemoved(T item)
		{
			base.OnRemoved(item);

			_items.Remove(GetKey(item));
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

			Registry.TryCreateTimer();
		}

		private void Write(T entity)
		{
			lock (_syncRoot)
			{
				_isChanged = true;
				_addedItems.Add(entity);
			}

			Registry.TryCreateTimer();
		}

		internal void ReadItems(List<Exception> errors)
		{
			if (!File.Exists(_fileName))
				return;

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				using (var stream = new FileStream(_fileName, FileMode.OpenOrCreate))
				{
					var reader = new FastCsvReader(stream, _encoding);

					while (reader.NextLine())
					{
						try
						{
							var item = Read(reader);
							var key = GetKey(item);

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

		internal bool Flush()
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
				Write(_items.CachedValues, false, true);
			}
			else if (addedItems.Length > 0)
			{
				Write(addedItems, true, false);
			}

			return true;
		}

		private void Write(IEnumerable<T> items, bool append, bool clear)
		{
			using (var stream = new FileStream(_fileName, FileMode.OpenOrCreate))
			{
				if (clear)
					stream.SetLength(0);

				if (append)
					stream.Position = stream.Length;

				using (var writer = new CsvFileWriter(stream, _encoding))
				{
					foreach (var item in items)
						Write(writer, item);
				}
			}
		}
	}
}