namespace StockSharp.Algo.Storages
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

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Security identifier mappings storage.
	/// </summary>
	public interface ISecurityMappingStorage
	{
		/// <summary>
		/// The new native security identifier added to storage.
		/// </summary>
		event Action<string, SecurityIdMapping> Changed;

		/// <summary>
		/// Initialize the storage.
		/// </summary>
		/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
		IDictionary<string, Exception> Init();

		/// <summary>
		/// Get storage names.
		/// </summary>
		/// <returns>Storage names.</returns>
		IEnumerable<string> GetStorageNames();

		/// <summary>
		/// Get security identifier mappings for storage. 
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <returns>Security identifiers mapping.</returns>
		IEnumerable<SecurityIdMapping> Get(string storageName);

		/// <summary>
		/// Add security identifier mapping.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="mapping">Security identifier mapping.</param>
		/// <returns><see langword="true"/> if security mapping was added. If was changed, <see langword="false" />.</returns>
		bool Add(string storageName, SecurityIdMapping mapping);

		/// <summary>
		/// Remove security mapping.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="stockSharpId">StockSharp format.</param>
		/// <returns><see langword="true"/> if mapping was added. Otherwise, <see langword="false" />.</returns>
		bool Remove(string storageName, SecurityId stockSharpId);
	}

	/// <summary>
	/// In memory security identifier mappings storage.
	/// </summary>
	public class InMemorySecurityMappingStorage : ISecurityMappingStorage
	{
		private readonly SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>> _mappings = new SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>>(StringComparer.InvariantCultureIgnoreCase);

		private event Action<string, SecurityIdMapping> _changed;

		event Action<string, SecurityIdMapping> ISecurityMappingStorage.Changed
		{
			add => _changed += value;
			remove => _changed -= value;
		}

		IDictionary<string, Exception> ISecurityMappingStorage.Init()
		{
			return new Dictionary<string, Exception>();
		}

		IEnumerable<string> ISecurityMappingStorage.GetStorageNames()
		{
			lock (_mappings.SyncRoot)
				return _mappings.Keys.ToArray();
		}

		IEnumerable<SecurityIdMapping> ISecurityMappingStorage.Get(string storageName)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			lock (_mappings.SyncRoot)
				return _mappings.TryGetValue(storageName)?.Select(p => (SecurityIdMapping)p).ToArray() ?? Enumerable.Empty<SecurityIdMapping>();
		}

		bool ISecurityMappingStorage.Add(string storageName, SecurityIdMapping mapping)
		{
			return Add(storageName, mapping, out _);
		}

		internal bool Add(string storageName, SecurityIdMapping mapping, out IEnumerable<SecurityIdMapping> all)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (mapping.IsDefault())
				throw new ArgumentNullException(nameof(mapping));

			var added = false;

			lock (_mappings.SyncRoot)
			{
				var mappings = _mappings.SafeAdd(storageName);

				var stockSharpId = mapping.StockSharpId;

				if (mappings.ContainsKey(stockSharpId))
				{
					mappings.Remove(stockSharpId);
				}
				else
					added = true;

				mappings.Add(stockSharpId, mapping.AdapterId);

				all = added ? null : mappings.Select(p => (SecurityIdMapping)p).ToArray();
			}

			_changed?.Invoke(storageName, mapping);

			return added;
		}

		bool ISecurityMappingStorage.Remove(string storageName, SecurityId stockSharpId)
		{
			return Remove(storageName, stockSharpId, out _);
		}

		internal bool Remove(string storageName, SecurityId stockSharpId, out IEnumerable<SecurityIdMapping> all)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (stockSharpId.IsDefault())
				throw new ArgumentNullException(nameof(storageName));

			all = null;

			lock (_mappings.SyncRoot)
			{
				var mappings = _mappings.TryGetValue(storageName);

				if (mappings == null)
					return false;

				var removed = mappings.Remove(stockSharpId);

				if (!removed)
					return false;

				all = mappings.Select(p => (SecurityIdMapping)p).ToArray();
			}

			_changed?.Invoke(storageName, new SecurityIdMapping { StockSharpId = stockSharpId });

			return true;
		}

		internal void Load(string storageName, List<Tuple<SecurityId, SecurityId>> pairs)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (pairs == null)
				throw new ArgumentNullException(nameof(pairs));

			lock (_mappings.SyncRoot)
			{
				var mappings = _mappings.SafeAdd(storageName);

				foreach (var tuple in pairs)
					mappings.Add(tuple.Item1, tuple.Item2);
			}
		}
	}

	/// <summary>
	/// CSV security identifier mappings storage.
	/// </summary>
	public sealed class CsvSecurityMappingStorage : ISecurityMappingStorage
	{
		private readonly ISecurityMappingStorage _inMemory = new InMemorySecurityMappingStorage();

		private readonly string _path;

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
		public event Action<string, SecurityIdMapping> Changed;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvSecurityMappingStorage"/>.
		/// </summary>
		/// <param name="path">Path to storage.</param>
		public CsvSecurityMappingStorage(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_path = path.ToFullPath();
			_delayAction = new DelayAction(ex => ex.LogError());
		}

		/// <inheritdoc />
		public IDictionary<string, Exception> Init()
		{
			if (!Directory.Exists(_path))
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
		public IEnumerable<string> GetStorageNames() => _inMemory.GetStorageNames();

		/// <inheritdoc />
		public IEnumerable<SecurityIdMapping> Get(string storageName) => _inMemory.Get(storageName);

		/// <inheritdoc />
		public bool Add(string storageName, SecurityIdMapping mapping)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (mapping.IsDefault())
				throw new ArgumentNullException(nameof(mapping));

			var added = ((InMemorySecurityMappingStorage)_inMemory).Add(storageName, mapping, out var all);

			if (added)
				Save(storageName, false, new[] { mapping });
			else
				Save(storageName, true, all);

			Changed?.Invoke(storageName, mapping);

			return added;
		}

		/// <inheritdoc />
		public bool Remove(string storageName, SecurityId stockSharpId)
		{
			if (!((InMemorySecurityMappingStorage)_inMemory).Remove(storageName, stockSharpId, out var all))
				return false;

			Save(storageName, true, all);

			Changed?.Invoke(storageName, new SecurityIdMapping { StockSharpId = stockSharpId });

			return true;
		}

		private void LoadFile(string fileName)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				if (!File.Exists(fileName))
					return;

				var pairs = new List<Tuple<SecurityId, SecurityId>>();

				using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					var reader = new FastCsvReader(stream, Encoding.UTF8);

					reader.NextLine();

					while (reader.NextLine())
					{
						var securityId = new SecurityId
						{
							SecurityCode = reader.ReadString(),
							BoardCode = reader.ReadString()
						};
						var adapterId = new SecurityId
						{
							SecurityCode = reader.ReadString(),
							BoardCode = reader.ReadString()
						};

						pairs.Add(Tuple.Create(securityId, adapterId));
					}
				}

				((InMemorySecurityMappingStorage)_inMemory).Load(Path.GetFileNameWithoutExtension(fileName), pairs);
			});
		}

		private void Save(string name, bool overwrite, IEnumerable<SecurityIdMapping> mappings)
		{
			DelayAction.DefaultGroup.Add(() =>
			{
				var fileName = Path.Combine(_path, name + ".csv");

				var appendHeader = overwrite || !File.Exists(fileName);
				var mode = overwrite ? FileMode.Create : FileMode.Append;

				using (var writer = new CsvFileWriter(new TransactionFileStream(fileName, mode)))
				{
					if (appendHeader)
						writer.WriteRow(new[] { "SecurityCode", "BoardCode", "AdapterCode", "AdapterBoard" });

					foreach (var mapping in mappings)
					{
						writer.WriteRow(new[]
						{
							mapping.StockSharpId.SecurityCode,
							mapping.StockSharpId.BoardCode,
							mapping.AdapterId.SecurityCode,
							mapping.AdapterId.BoardCode
						});
					}
				}
			});
		}
	}
}