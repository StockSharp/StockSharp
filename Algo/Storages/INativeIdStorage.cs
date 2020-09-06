namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Security native identifier storage.
	/// </summary>
	public interface INativeIdStorage
	{
		/// <summary>
		/// The new native security identifier added to storage.
		/// </summary>
		event Action<string, SecurityId, object> Added;

		/// <summary>
		/// Initialize the storage.
		/// </summary>
		/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
		IDictionary<string, Exception> Init();

		/// <summary>
		/// Get native security identifiers for storage. 
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <returns>Security identifiers.</returns>
		Tuple<SecurityId, object>[] Get(string storageName);

		/// <summary>
		/// Try add native security identifier to storage.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="nativeId">Native (internal) trading system security id.</param>
		/// <param name="isPersistable">Save the identifier as a permanent.</param>
		/// <returns><see langword="true"/> if native identifier was added. Otherwise, <see langword="false" />.</returns>
		bool TryAdd(string storageName, SecurityId securityId, object nativeId, bool isPersistable = true);

		/// <summary>
		/// Try get security identifier by native identifier.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="nativeId">Native (internal) trading system security id.</param>
		/// <returns>Security identifier.</returns>
		SecurityId? TryGetByNativeId(string storageName, object nativeId);

		/// <summary>
		/// Try get native security identifier by identifier.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <returns>Native (internal) trading system security id.</returns>
		object TryGetBySecurityId(string storageName, SecurityId securityId);

		/// <summary>
		/// Clear storage.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		void Clear(string storageName);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="storageName"></param>
		/// <param name="securityId"></param>
		/// <param name="isPersistable">Save the identifier as a permanent.</param>
		/// <returns></returns>
		bool RemoveBySecurityId(string storageName, SecurityId securityId, bool isPersistable = true);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="storageName"></param>
		/// <param name="nativeId">Native (internal) trading system security id.</param>
		/// <param name="isPersistable">Save the identifier as a permanent.</param>
		/// <returns></returns>
		bool RemoveByNativeId(string storageName, object nativeId, bool isPersistable = true);
	}

	/// <summary>
	/// CSV security native identifier storage.
	/// </summary>
	public sealed class CsvNativeIdStorage : INativeIdStorage
	{
		private readonly INativeIdStorage _inMemory = new InMemoryNativeIdStorage();
		private readonly SynchronizedDictionary<SecurityId, object> _buffer = new SynchronizedDictionary<SecurityId, object>();

		private readonly string _path;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvNativeIdStorage"/>.
		/// </summary>
		/// <param name="path">Path to storage.</param>
		public CsvNativeIdStorage(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_path = path.ToFullPath();
			_delayAction = new DelayAction(ex => ex.LogError());
		}

		private DelayAction _delayAction;

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public DelayAction DelayAction
		{
			get => _delayAction;
			set => _delayAction = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public event Action<string, SecurityId, object> Added;

		/// <inheritdoc />
		public IDictionary<string, Exception> Init()
		{
			Directory.CreateDirectory(_path);

			var errors = _inMemory.Init();

			var files = Directory.GetFiles(_path, "*.csv");

			foreach (var fileName in files)
			{
				try
				{
					LoadFile(fileName);
				}
				catch (Exception ex)
				{
					errors.Add(fileName, ex);
				}
			}

			return errors;
		}

		/// <inheritdoc />
		public Tuple<SecurityId, object>[] Get(string storageName)
		{
			return _inMemory.Get(storageName);
		}

		/// <inheritdoc />
		public bool TryAdd(string storageName, SecurityId securityId, object nativeId, bool isPersistable)
		{
			var added = _inMemory.TryAdd(storageName, securityId, nativeId, isPersistable);

			if (!added)
				return false;

			if (isPersistable)
				Save(storageName, securityId, nativeId);

			Added?.Invoke(storageName, securityId, nativeId);

			return true;
		}

		/// <inheritdoc />
		public void Clear(string storageName)
		{
			_inMemory.Clear(storageName);
			_buffer.Clear();
			DelayAction.DefaultGroup.Add(() => File.Delete(GetFileName(storageName)));
		}

		/// <inheritdoc />
		public bool RemoveBySecurityId(string storageName, SecurityId securityId, bool isPersistable)
		{
			var added = _inMemory.RemoveBySecurityId(storageName, securityId, isPersistable);

			if (!added)
				return false;

			if (isPersistable)
				SaveAll(storageName);

			return true;
		}

		/// <inheritdoc />
		public bool RemoveByNativeId(string storageName, object nativeId, bool isPersistable)
		{
			var added = _inMemory.RemoveByNativeId(storageName, nativeId, isPersistable);

			if (!added)
				return false;

			if (isPersistable)
				SaveAll(storageName);

			return true;
		}

		private void SaveAll(string storageName)
		{
			_buffer.Clear();

			DelayAction.DefaultGroup.Add(() =>
			{
				var fileName = GetFileName(storageName);

				File.Delete(fileName);

				var items = _inMemory.Get(storageName);

				if (items.Length == 0)
					return;

				using (var writer = new CsvFileWriter(new TransactionFileStream(fileName, FileMode.Append)))
				{
					WriteHeader(writer, items.FirstOrDefault().Item2);

					foreach (var item in items)
						WriteItem(writer, item.Item1, item.Item2);
				}
			});
		}

		/// <inheritdoc />
		public SecurityId? TryGetByNativeId(string storageName, object nativeId)
		{
			return _inMemory.TryGetByNativeId(storageName, nativeId);
		}

		/// <inheritdoc />
		public object TryGetBySecurityId(string storageName, SecurityId securityId)
		{
			return _inMemory.TryGetBySecurityId(storageName, securityId);
		}

		private void WriteHeader(CsvFileWriter writer, object nativeId)
		{
			if (nativeId is ITuple tuple)
			{
				var tupleValues = new List<string>();

				for (int i = 0; i < tuple.Length; i++)
					tupleValues.Add(GetTypeName(tuple[i].GetType()));

				writer.WriteRow(new[]
				{
					"Symbol",
					"Board",
				}.Concat(tupleValues));
			}
			else
			{
				writer.WriteRow(new[]
				{
					"Symbol",
					"Board",
					GetTypeName(nativeId.GetType()),
				});
			}
		}

		private void WriteItem(CsvFileWriter writer, SecurityId securityId, object nativeId)
		{
			if (nativeId is ITuple tuple)
			{
				writer.WriteRow(new[]
				{
					securityId.SecurityCode,
					securityId.BoardCode
				}.Concat(tuple.ToValues().Select(v => v.To<string>())));
			}
			else
			{
				writer.WriteRow(new[]
				{
					securityId.SecurityCode,
					securityId.BoardCode,
					nativeId.ToString()
				});
			}
		}

		private void Save(string storageName, SecurityId securityId, object nativeId)
		{
			_buffer[securityId] = nativeId;

			DelayAction.DefaultGroup.Add(() =>
			{
				var items = _buffer.SyncGet(c => c.CopyAndClear());

				if (items.Length == 0)
					return;

				var fileName = GetFileName(storageName);

				var appendHeader = !File.Exists(fileName) || new FileInfo(fileName).Length == 0;

				using (var writer = new CsvFileWriter(new TransactionFileStream(fileName, FileMode.Append)))
				{
					if (appendHeader)
						WriteHeader(writer, nativeId);

					foreach (var item in items)
						WriteItem(writer, item.Key, item.Value);
				}
			});
		}

		private string GetFileName(string storageName) => Path.Combine(_path, storageName + ".csv");

		private static string GetTypeName(Type nativeIdType)
		{
			return Converter.GetAlias(nativeIdType) ?? nativeIdType.GetTypeName(false);
		}

		private void LoadFile(string fileName)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				if (!File.Exists(fileName))
					return;

				var name = Path.GetFileNameWithoutExtension(fileName);

				var pairs = new List<Tuple<SecurityId, object>>();

				using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					var reader = new FastCsvReader(stream, Encoding.UTF8);

					reader.NextLine();
					reader.Skip(2);

					var types = new List<Type>();

					while ((reader.ColumnCurr + 1) < reader.ColumnCount)
						types.Add(reader.ReadString().To<Type>());

					var isTuple = types.Count > 1;

					while (reader.NextLine())
					{
						var securityId = new SecurityId
						{
							SecurityCode = reader.ReadString(),
							BoardCode = reader.ReadString()
						};

						object nativeId;

						if (isTuple)
						{
							var args = new List<object>();

							for (var i = 0; i < types.Count; i++)
								args.Add(reader.ReadString().To(types[i]));

							nativeId = args.ToTuple();
						}
						else
							nativeId = reader.ReadString().To(types[0]);
						
						pairs.Add(Tuple.Create(securityId, nativeId));
					}
				}

				((InMemoryNativeIdStorage)_inMemory).Add(name, pairs);
			});
        }
	}

	/// <summary>
	/// In memory security native identifier storage.
	/// </summary>
	public class InMemoryNativeIdStorage : INativeIdStorage
	{
		private readonly Dictionary<string, PairSet<SecurityId, object>> _nativeIds = new Dictionary<string, PairSet<SecurityId, object>>(StringComparer.InvariantCultureIgnoreCase);
		private readonly SyncObject _syncRoot = new SyncObject();

		private Action<string, SecurityId, object> _added;

		event Action<string, SecurityId, object> INativeIdStorage.Added
		{
			add => _added += value;
			remove => _added -= value;
		}

		IDictionary<string, Exception> INativeIdStorage.Init()
		{
			return new Dictionary<string, Exception>();
		}

		internal void Add(string storageName, IEnumerable<Tuple<SecurityId, object>> ids)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (ids == null)
				throw new ArgumentNullException(nameof(ids));

			lock (_syncRoot)
			{
				var dict = _nativeIds.SafeAdd(storageName);

				foreach (var tuple in ids)
				{
					var secId = tuple.Item1;
					var nativeId = tuple.Item2;

					// skip duplicates
					if (dict.ContainsKey(secId) || dict.ContainsValue(nativeId))
						continue;

					dict.Add(secId, nativeId);
				}
			}
		}

		bool INativeIdStorage.TryAdd(string storageName, SecurityId securityId, object nativeId, bool isPersistable)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (nativeId == null)
				throw new ArgumentNullException(nameof(nativeId));

			lock (_syncRoot)
			{
				var added = _nativeIds.SafeAdd(storageName).TryAdd(securityId, nativeId);

				if (!added)
					return false;
			}

			_added?.Invoke(storageName, securityId, nativeId);

			return true;
		}

		object INativeIdStorage.TryGetBySecurityId(string storageName, SecurityId securityId)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			lock (_syncRoot)
				return _nativeIds.TryGetValue(storageName)?.TryGetValue(securityId);
		}

		void INativeIdStorage.Clear(string storageName)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			lock (_syncRoot)
				_nativeIds.Remove(storageName);
		}

		SecurityId? INativeIdStorage.TryGetByNativeId(string storageName, object nativeId)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			var securityId = default(SecurityId);

			lock (_syncRoot)
			{
				if (_nativeIds.TryGetValue(storageName)?.TryGetKey(nativeId, out securityId) != true)
					return null;
			}

			return securityId;
		}

		Tuple<SecurityId, object>[] INativeIdStorage.Get(string storageName)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			lock (_syncRoot)
				return _nativeIds.TryGetValue(storageName)?.Select(p => Tuple.Create(p.Key, p.Value)).ToArray() ?? ArrayHelper.Empty<Tuple<SecurityId, object>>();
		}

		bool INativeIdStorage.RemoveBySecurityId(string storageName, SecurityId securityId, bool isPersistable)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			lock (_syncRoot)
			{
				var set = _nativeIds.TryGetValue(storageName);

				if (set == null)
					return false;

				return set.Remove(securityId);
			}
		}

		bool INativeIdStorage.RemoveByNativeId(string storageName, object nativeId, bool isPersistable)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			lock (_syncRoot)
			{
				var set = _nativeIds.TryGetValue(storageName);

				if (set == null)
					return false;

				return set.RemoveByValue(nativeId);
			}
		}
	}
}