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
	/// The interface for presentation in the form of list of trade objects, received from the external storage.
	/// </summary>
	public interface ICsvEntityList
	{
		/// <summary>
		/// Initialize the storage.
		/// </summary>
		/// <param name="errors">Possible errors.</param>
		void Init(IList<Exception> errors);
	}

	/// <summary>
	/// List of trade objects, received from the CSV storage.
	/// </summary>
	/// <typeparam name="T">Entity type.</typeparam>
	public abstract class CsvEntityList<T> : SynchronizedList<T>, IStorageEntityList<T>, ICsvEntityList
		where T : class
	{
		private readonly Dictionary<object, T> _items = new Dictionary<object, T>();

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
			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));

			Registry = registry ?? throw new ArgumentNullException(nameof(registry));

			FileName = Path.Combine(Registry.Path, fileName);
		}

		/// <summary>
		/// CSV file name.
		/// </summary>
		public string FileName { get; }

		#region IStorageEntityList<T>

		private DelayAction.IGroup<CsvFileWriter> _delayActionGroup;
		private DelayAction _delayAction;

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public DelayAction DelayAction
		{
			get => _delayAction;
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
					_delayActionGroup = _delayAction.CreateGroup(() =>
					{
						var stream = new TransactionFileStream(FileName, FileMode.OpenOrCreate);
						stream.Seek(0, SeekOrigin.End);
						return new CsvFileWriter(stream, Registry.Encoding);
					});
				}
			}
		}

		T IStorageEntityList<T>.ReadById(object id)
		{
			lock (SyncRoot)
				return _items.TryGetValue(NormalizedKey(id));
		}

		IEnumerable<T> IStorageEntityList<T>.ReadLasts(int count)
		{
			lock (SyncRoot)
				return _items.Values.Skip(Count - count).Take(count).ToArray();
		}

		private object GetNormalizedKey(T entity)
		{
			return NormalizedKey(GetKey(entity));
		}

		private static object NormalizedKey(object key)
		{
			if (key is string str)
				return str.ToLowerInvariant();

			return key;
		}

		/// <inheritdoc />
		public void Save(T entity)
		{
			Save(entity, false);
		}

		/// <summary>
		/// Save object into storage.
		/// </summary>
		/// <param name="entity">Trade object.</param>
		/// <param name="forced">Forced update.</param>
		public virtual void Save(T entity, bool forced)
		{
			lock (SyncRoot)
			{
				var item = _items.TryGetValue(GetNormalizedKey(entity));

				if (item == null)
				{
					Add(entity);
					return;
				}
				else if (IsChanged(entity, forced))
					UpdateCache(entity);
				else
					return;

				WriteMany(_items.Values.ToArray());
			}
		}

		#endregion

		/// <summary>
		/// Is <paramref name="entity"/> changed.
		/// </summary>
		/// <param name="entity">Trade object.</param>
		/// <param name="forced">Forced update.</param>
		/// <returns>Is changed.</returns>
		protected virtual bool IsChanged(T entity, bool forced)
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
		/// <param name="item"></param>
		/// <returns></returns>
		public override bool Contains(T item)
		{
			lock (SyncRoot)
				return _items.ContainsKey(GetNormalizedKey(item));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item">Trade object.</param>
		/// <returns></returns>
		protected override bool OnAdding(T item)
		{
			lock (SyncRoot)
			{
				if (!_items.TryAdd(GetNormalizedKey(item), item))
					return false;

				AddCache(item);

				_delayActionGroup.Add(Write, item);
			}

			return base.OnAdding(item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item">Trade object.</param>
		protected override void OnRemoved(T item)
		{
			base.OnRemoved(item);

			lock (SyncRoot)
			{
				_items.Remove(GetNormalizedKey(item));
				RemoveCache(item);

				WriteMany(_items.Values.ToArray());
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void OnCleared()
		{
			base.OnCleared();

			lock (SyncRoot)
			{
				_items.Clear();
				ClearCache();

				_delayActionGroup.Add(writer => writer.Writer.Truncate());
			}
		}

		/// <summary>
		/// Write data into storage.
		/// </summary>
		/// <param name="values">Trading objects.</param>
		private void WriteMany(T[] values)
		{
			_delayActionGroup.Add((writer, state) =>
			{
				writer.Writer.Truncate();

				foreach (var item in state)
					Write(writer, item);
			}, values, compareStates: (v1, v2) =>
			{
				if (v1 == null)
					return v2 == null;

				if (v2 == null)
					return false;

				if (v1.Length != v2.Length)
					return false;

				return v1.SequenceEqual(v2);
			});
		}

		void ICsvEntityList.Init(IList<Exception> errors)
		{
			if (errors == null)
				throw new ArgumentNullException(nameof(errors));

			if (!File.Exists(FileName))
				return;

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				using (var stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
				{
					var reader = new FastCsvReader(stream, Registry.Encoding);

					var hasDuplicates = false;
					var currErrors = 0;

					while (reader.NextLine())
					{
						try
						{
							var item = Read(reader);
							var key = GetNormalizedKey(item);

							lock (SyncRoot)
							{
								if (_items.TryAdd(key, item))
								{
									InnerCollection.Add(item);
									AddCache(item);
								}
								else
									hasDuplicates = true;
							}

							currErrors = 0;
						}
						catch (Exception ex)
						{
							if (errors.Count < 100)
								errors.Add(ex);

							currErrors++;
							
							if (currErrors >= 1000)
								break;
						}
					}

					if (!hasDuplicates)
						return;

					try
					{
						lock (SyncRoot)
						{
							stream.SetLength(0);

							using (var writer = new CsvFileWriter(stream, Registry.Encoding))
							{
								foreach (var item in InnerCollection)
									Write(writer, item);
							}
						}
					}
					catch (Exception ex)
					{
						errors.Add(ex);
					}
				}
			});

			InnerCollection.ForEach(OnAdded);
		}

		/// <summary>
		/// Clear cache.
		/// </summary>
		protected virtual void ClearCache()
		{
		}

		/// <summary>
		/// Add item to cache.
		/// </summary>
		/// <param name="item">New item.</param>
		protected virtual void AddCache(T item)
		{
		}

		/// <summary>
		/// Update item in cache.
		/// </summary>
		/// <param name="item">Item.</param>
		protected virtual void UpdateCache(T item)
		{
		}

		/// <summary>
		/// Remove item from cache.
		/// </summary>
		/// <param name="item">Item.</param>
		protected virtual void RemoveCache(T item)
		{
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return FileName;
		}
	}
}