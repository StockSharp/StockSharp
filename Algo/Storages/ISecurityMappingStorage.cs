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
		event Action<string, SecurityId, SecurityId> Changed;

		/// <summary>
		/// Get storae names.
		/// </summary>
		/// <returns>Storage names.</returns>
		IEnumerable<string> GetStorageNames();

		/// <summary>
		/// Get security identifier mappings for storage. 
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <returns>Security identifiers mapping.</returns>
		IEnumerable<Tuple<SecurityId, SecurityId>> Get(string name);

		/// <summary>
		/// Add security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="adapterId">Adapter security identifier.</param>
		/// <returns><see langword="true"/> if security mapping was added. If was changed, <see langword="false" />.</returns>
		bool Add(string storageName, SecurityId securityId, SecurityId adapterId);

		/// <summary>
		/// Remove security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <returns><see langword="true"/> if code mapping was added. Otherwise, <see langword="false" />.</returns>
		bool Remove(string storageName, SecurityId securityId);
	}

	/// <summary>
	/// In memory security identifier mappings storage.
	/// </summary>
	public class InMemorySecurityMappingStorage : ISecurityMappingStorage
	{
		private readonly SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>> _mappings = new SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>>(StringComparer.InvariantCultureIgnoreCase);

		private event Action<string, SecurityId, SecurityId> _changed;

		event Action<string, SecurityId, SecurityId> ISecurityMappingStorage.Changed
		{
			add => _changed += value;
			remove => _changed -= value;
		}

		IEnumerable<string> ISecurityMappingStorage.GetStorageNames()
		{
			lock (_mappings.SyncRoot)
				return _mappings.Keys.ToArray();
		}

		IEnumerable<Tuple<SecurityId, SecurityId>> ISecurityMappingStorage.Get(string name)
		{
			if (CollectionHelper.IsEmpty(name))
				throw new ArgumentNullException(nameof(name));

			lock (_mappings.SyncRoot)
				return _mappings.TryGetValue(name)?.Select(p => Tuple.Create(p.Key, p.Value)).ToArray() ?? ArrayHelper.Empty<Tuple<SecurityId, SecurityId>>();
		}

		bool ISecurityMappingStorage.Add(string storageName, SecurityId securityCode, SecurityId adapterCode)
		{
			if (CollectionHelper.IsEmpty(storageName))
				throw new ArgumentNullException(nameof(storageName));

			if (securityCode == null)
				throw new ArgumentNullException(nameof(securityCode));

			if (adapterCode == null)
				throw new ArgumentNullException(nameof(adapterCode));

			var added = false;

			lock (_mappings.SyncRoot)
			{
				var mappings = _mappings.SafeAdd(storageName);

				if (mappings.ContainsKey(securityCode))
				{
					mappings.Remove(securityCode);
				}
				else
					added = true;

				mappings.Add(securityCode, adapterCode);
			}

			_changed?.Invoke(storageName, securityCode, adapterCode);

			return added;
		}

		bool ISecurityMappingStorage.Remove(string storageName, SecurityId securityId)
		{
			if (CollectionHelper.IsEmpty(storageName))
				throw new ArgumentNullException(nameof(storageName));

			if (securityId == null)
				throw new ArgumentNullException(nameof(storageName));

			lock (_mappings.SyncRoot)
			{
				var mappings = _mappings.TryGetValue(storageName);

				if (mappings == null)
					return false;

				var removed = mappings.Remove(securityId);

				if (!removed)
					return false;
			}

			_changed?.Invoke(storageName, securityId, default(SecurityId));

			return true;
		}
	}

	/// <summary>
	/// CSV security identifier mappings storage.
	/// </summary>
	public sealed class CsvSecurityMappingStorage : ISecurityMappingStorage
	{
		private readonly SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>> _mappings = new SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>>(StringComparer.InvariantCultureIgnoreCase);

		private readonly string _path;

		private DelayAction _delayAction;

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public DelayAction DelayAction
		{
			get => _delayAction;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_delayAction = value;
			}
		}

		/// <inheritdoc />
		public event Action<string, SecurityId, SecurityId> Changed;

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
		public void Init()
		{
			if (!Directory.Exists(_path))
				Directory.CreateDirectory(_path);

			var files = Directory.GetFiles(_path, "*.csv");

			var errors = new List<Exception>();

			foreach (var fileName in files)
			{
				try
				{
					LoadFile(fileName);
				}
				catch (Exception ex)
				{
					errors.Add(ex);
				}
			}

			if (errors.Count > 0)
				throw new AggregateException(errors);
		}

		/// <inheritdoc />
		public IEnumerable<string> GetStorageNames()
		{
			lock (_mappings.SyncRoot)
				return _mappings.Keys.ToArray();
		}

		/// <inheritdoc />
		public IEnumerable<Tuple<SecurityId, SecurityId>> Get(string name)
		{
			if (CollectionHelper.IsEmpty(name))
				throw new ArgumentNullException(nameof(name));

			lock (_mappings.SyncRoot)
				return _mappings.TryGetValue(name)?.Select(p => Tuple.Create(p.Key, p.Value)).ToArray() ?? ArrayHelper.Empty<Tuple<SecurityId, SecurityId>>();
		}

		/// <inheritdoc />
		public bool Add(string storageName, SecurityId securityId, SecurityId adapterId)
		{
			if (CollectionHelper.IsEmpty(storageName))
				throw new ArgumentNullException(nameof(storageName));

			if (securityId == null)
				throw new ArgumentNullException(nameof(securityId));

			if (adapterId == null)
				throw new ArgumentNullException(nameof(adapterId));

			PairSet<SecurityId, SecurityId> mappings;
			var added = false;

			lock (_mappings.SyncRoot)
			{
				mappings = _mappings.SafeAdd(storageName);

				if (mappings.ContainsKey(securityId))
				{
					mappings.Remove(securityId);
				}
				else
					added = true;

				mappings.Add(securityId, adapterId);
			}

			if (!added)
			{
				KeyValuePair<SecurityId, SecurityId>[] items;

				lock (_mappings.SyncRoot)
					items = mappings.ToArray();

				Save(storageName, true, items);
			}
			else
				Save(storageName, false, new[] { new KeyValuePair<SecurityId, SecurityId>(securityId, adapterId) });

			Changed?.Invoke(storageName, securityId, adapterId);

			return added;
		}

		/// <inheritdoc />
		public bool Remove(string storageName, SecurityId securityId)
		{
			if (CollectionHelper.IsEmpty(storageName))
				throw new ArgumentNullException(nameof(storageName));

			if (securityId == null)
				throw new ArgumentNullException(nameof(securityId));

			PairSet<SecurityId, SecurityId> mappings;

			lock (_mappings.SyncRoot)
			{
				mappings = _mappings.TryGetValue(storageName);

				if (mappings == null)
					return false;

				var removed = mappings.Remove(securityId);

				if (!removed)
					return false;
			}

			KeyValuePair<SecurityId, SecurityId>[] items;

			lock (_mappings.SyncRoot)
				items = mappings.ToArray();

			Save(storageName, true, items);

			Changed?.Invoke(storageName, securityId, default(SecurityId));

			return true;
		}

		private void LoadFile(string fileName)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				if (!File.Exists(fileName))
					return;

				var name = Path.GetFileNameWithoutExtension(fileName);

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

				lock (_mappings.SyncRoot)
				{
					var mappings = _mappings.SafeAdd(name);

					foreach (var tuple in pairs)
						mappings.Add(tuple.Item1, tuple.Item2);
				}
			});
		}

		private void Save(string name, bool overwrite, IEnumerable<KeyValuePair<SecurityId, SecurityId>> items)
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

					foreach (var item in items)
						writer.WriteRow(new[] { item.Key.SecurityCode, item.Key.BoardCode, item.Value.SecurityCode, item.Value.BoardCode });
				}
			}, canBatch: false);
		}
	}
}