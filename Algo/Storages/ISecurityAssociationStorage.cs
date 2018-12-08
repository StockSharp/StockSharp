namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Globalization;
	using System.IO;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The interface describing the security associations storage.
	/// </summary>
	public interface ISecurityAssociationStorage
	{
		/// <summary>
		/// Initialize the storage.
		/// </summary>
		void Init();

		/// <summary>
		/// Save association.
		/// </summary>
		/// <param name="master">Master security id.</param>
		/// <param name="tradable">Tradable security ids.</param>
		void Save(SecurityId master, PairSet<string, SecurityId> tradable);

		/// <summary>
		/// Save association.
		/// </summary>
		/// <param name="master">Master security id.</param>
		/// <param name="adapterName">Adapter name.</param>
		/// <param name="tradable">Tradable security id.</param>
		void Save(SecurityId master, string adapterName, SecurityId tradable);

		/// <summary>
		/// Load all associations.
		/// </summary>
		/// <returns>All associations</returns>
		IDictionary<SecurityId, PairSet<string, SecurityId>> Load();

		/// <summary>
		/// Delete association.
		/// </summary>
		/// <param name="master">Master security id.</param>
		void Delete(SecurityId master);

		/// <summary>
		/// Delete association.
		/// </summary>
		/// <param name="master">Master security id.</param>
		/// <param name="adapterName">Adapter name.</param>
		void Delete(SecurityId master, string adapterName);

		/// <summary>
		/// Delete all associations.
		/// </summary>
		void DeleteAll();
	}

	/// <summary>
	/// CSV security associations storage.
	/// </summary>
	public class CsvSecurityAssociationStorage : ISecurityAssociationStorage
	{
		private readonly CachedSynchronizedDictionary<SecurityId, PairSet<string, SecurityId>> _associations = new CachedSynchronizedDictionary<SecurityId, PairSet<string, SecurityId>>();

		private const string _separator = "!!=!!";

		private readonly string _fileName;
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

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvSecurityAssociationStorage"/>.
		/// </summary>
		/// <param name="path">Path to storage.</param>
		public CsvSecurityAssociationStorage(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_path = path.ToFullPath();
			_fileName = Path.Combine(_path, "security_association.csv");
			_delayAction = new DelayAction(ex => ex.LogError());
		}

		/// <summary>
		/// Security id generator.
		/// </summary>
		public SecurityIdGenerator IdGenerator { get; set; }

		void ISecurityAssociationStorage.Init()
		{
			if (!Directory.Exists(_path))
				Directory.CreateDirectory(_path);

			if (!File.Exists(_fileName))
				return;

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				var associations = new Dictionary<SecurityId, PairSet<string, SecurityId>>();

				using (var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
				{
					var reader = new FastCsvReader(stream, Encoding.UTF8);

					reader.NextLine();

					while (reader.NextLine())
					{
						var master = reader.ReadString().ToSecurityId(IdGenerator);

						var tradables = new PairSet<string, SecurityId>(StringComparer.InvariantCultureIgnoreCase);

						while (reader.ColumnCurr < (reader.ColumnCount - 1))
						{
							var parts = reader.ReadString().Split(_separator);
							tradables.Add(parts[0], parts[1].ToSecurityId(IdGenerator));
						}

						associations.Add(master, tradables);
					}
				}

				lock (_associations.SyncRoot)
					_associations.AddRange(associations);
			});
		}

		void ISecurityAssociationStorage.Save(SecurityId master, PairSet<string, SecurityId> tradable)
		{
			if (tradable == null)
				throw new ArgumentNullException(nameof(tradable));

			_associations[master] = tradable.ToPairSet();
			Save(true);
		}

		void ISecurityAssociationStorage.Save(SecurityId master, string adapterName, SecurityId tradable)
		{
			lock (_associations.SyncRoot)
				_associations.SafeAdd(master, key => new PairSet<string, SecurityId>(StringComparer.InvariantCultureIgnoreCase))[adapterName] = tradable;

			Save(true);
		}

		IDictionary<SecurityId, PairSet<string, SecurityId>> ISecurityAssociationStorage.Load()
		{
			return _associations.CachedPairs.ToDictionary(p => p.Key, p => p.Value.ToPairSet());
		}

		void ISecurityAssociationStorage.Delete(SecurityId master)
		{
			if (_associations.Remove(master))
				Save(true);
		}

		void ISecurityAssociationStorage.Delete(SecurityId master, string adapterName)
		{
			lock (_associations.SyncRoot)
			{
				if (!_associations.TryGetValue(master, out var dict))
					return;

				dict.Remove(adapterName);

				if (dict.Count == 0)
					_associations.Remove(master);
			}
		}

		void ISecurityAssociationStorage.DeleteAll()
		{
			_associations.Clear();
			Save(true);
		}

		private void Save(bool overwrite)
		{
			DelayAction.DefaultGroup.Add(() =>
			{
				var appendHeader = overwrite || !File.Exists(_fileName);
				var mode = overwrite ? FileMode.Create : FileMode.Append;

				using (var writer = new CsvFileWriter(new TransactionFileStream(_fileName, mode)))
				{
					if (appendHeader)
						writer.WriteRow(new[] { "MasterId", "Tradables" });

					foreach (var pair in _associations.CachedPairs)
					{
						var row = new List<string>();

						row.AddRange(new[] { pair.Key.ToStringId(IdGenerator) });

						foreach (var tradable in pair.Value)
							row.Add($"{tradable.Key}{_separator}{tradable.Value.ToStringId()}");

						writer.WriteRow(row);
					}
				}
			});
		}
	}
}