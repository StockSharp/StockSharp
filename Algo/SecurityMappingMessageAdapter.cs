namespace StockSharp.Algo
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
		/// <returns>security identifier mappings.</returns>
		IEnumerable<Tuple<SecurityId, SecurityId>> Get(string name);

		/// <summary>
		/// Add security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name</param>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="adapterId">Adapter security identifier.</param>
		/// <returns><see langword="true"/> if security mapping was added. If was changed, <see langword="false" />.</returns>
		bool Add(string storageName, SecurityId securityId, SecurityId adapterId);

		/// <summary>
		/// Remove security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name</param>
		/// <param name="securityCode">Security code.</param>
		/// <returns><see langword="true"/> if code mapping was added. Otherwise, <see langword="false" />.</returns>
		bool Remove(string storageName, SecurityId securityCode);
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
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_mappings.SyncRoot)
				return _mappings.TryGetValue(name)?.Select(p => Tuple.Create(p.Key, p.Value)).ToArray() ?? ArrayHelper.Empty<Tuple<SecurityId, SecurityId>>();
		}

		bool ISecurityMappingStorage.Add(string storageName, SecurityId securityCode, SecurityId adapterCode)
		{
			if (storageName.IsEmpty())
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
			if (storageName.IsEmpty())
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

		/// <summary>
		/// The security identifier added to storage.
		/// </summary>
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

		/// <summary>
		/// Initialize the storage.
		/// </summary>
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

		/// <summary>
		/// Get storae names.
		/// </summary>
		/// <returns>Storage names.</returns>
		public IEnumerable<string> GetStorageNames()
		{
			lock (_mappings.SyncRoot)
				return _mappings.Keys.ToArray();
		}

		/// <summary>
		/// Get security identifier mappings for storage. 
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <returns>Security identifier mappings.</returns>
		public IEnumerable<Tuple<SecurityId, SecurityId>> Get(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_mappings.SyncRoot)
				return _mappings.TryGetValue(name)?.Select(p => Tuple.Create(p.Key, p.Value)).ToArray() ?? ArrayHelper.Empty<Tuple<SecurityId, SecurityId>>();
		}

		/// <summary>
		/// Add security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name</param>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="adapterId">Adapter security identifier.</param>
		/// <returns><see langword="true"/> if code mapping was added. If was changed, <see langword="false" />.</returns>
		public bool Add(string storageName, SecurityId securityId, SecurityId adapterId)
		{
			if (storageName.IsEmpty())
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

		/// <summary>
		/// Remove security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name</param>
		/// <param name="securityId">Security identifier.</param>
		/// <returns><see langword="true"/> if code mapping was added. Otherwise, <see langword="false" />.</returns>
		public bool Remove(string storageName, SecurityId securityId)
		{
			if (storageName.IsEmpty())
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

	/// <summary>
	/// Security identifier mappings message adapter.
	/// </summary>
	public class SecurityMappingMessageAdapter : MessageAdapterWrapper
	{
		private readonly PairSet<SecurityId, SecurityId> _securityIds = new PairSet<SecurityId, SecurityId>();
		private readonly SyncObject _syncRoot = new SyncObject();

		/// <summary>
		/// Security identifier mappings storage.
		/// </summary>
		public ISecurityMappingStorage Storage { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityMappingMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="storage">Security identifier mappings storage.</param>
		public SecurityMappingMessageAdapter(IMessageAdapter innerAdapter, ISecurityMappingStorage storage)
			: base(innerAdapter)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			Storage = storage;
			Storage.Changed += OnStorageMappingChanged;
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			Storage.Changed -= OnStorageMappingChanged;
			base.Dispose();
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					var pairs = Storage.Get(InnerAdapter.StorageName);

					lock (_syncRoot)
					{
						foreach (var tuple in pairs)
						{
							var securityCode = tuple.Item1;
							var adapterCode = tuple.Item2;

							_securityIds.Add(adapterCode, securityCode);
						}
					}

					base.OnInnerAdapterNewOutMessage(message);
					break;
				}

				case MessageTypes.Reset:
				{
					lock (_syncRoot)
					{
						_securityIds.Clear();
					}

					base.OnInnerAdapterNewOutMessage(message);
					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;

					var securityCode = secMsg.SecurityId.SecurityCode;
					var boardCode = secMsg.SecurityId.BoardCode;

					if (securityCode.IsEmpty() || boardCode.IsEmpty())
						throw new InvalidOperationException();

					var adapterId = new SecurityId
					{
						SecurityCode = securityCode,
						BoardCode = boardCode
					};

					SecurityId securityId;

					lock (_syncRoot)
						securityId = _securityIds.TryGetValue(adapterId);

					if (!adapterId.IsDefault())
					{
						secMsg.SecurityId = securityId;
					}

					base.OnInnerAdapterNewOutMessage(message);
					break;
				}

				case MessageTypes.PositionChange:
				{
					var positionMsg = (PositionChangeMessage)message;

					ProcessMessage(positionMsg.SecurityId, positionMsg);
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					ProcessMessage(execMsg.SecurityId, execMsg);
					break;
				}

				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message;

					ProcessMessage(level1Msg.SecurityId, level1Msg);
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteChangeMsg = (QuoteChangeMessage)message;
					ProcessMessage(quoteChangeMsg.SecurityId, quoteChangeMsg);
					break;
				}

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandleRange:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				{
					var candleMsg = (CandleMessage)message;
					ProcessMessage(candleMsg.SecurityId, candleMsg);
					break;
				}

				case MessageTypes.News:
				{
					var newsMsg = (NewsMessage)message;

					if (newsMsg.SecurityId != null)
						ProcessMessage(newsMsg.SecurityId.Value, newsMsg);
					else
						base.OnInnerAdapterNewOutMessage(message);

					break;
				}

				default:
					base.OnInnerAdapterNewOutMessage(message);
					break;
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderGroupCancel:
				case MessageTypes.MarketData:
				{
					ReplaceSecurityId(message);
					break;
				}

				case MessageTypes.OrderPairReplace:
				{
					var pairMsg = (OrderPairReplaceMessage)message;
					ReplaceSecurityId(pairMsg.Message1);
					ReplaceSecurityId(pairMsg.Message2);
					break;
				}
			}

			base.SendInMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityMappingMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SecurityMappingMessageAdapter(InnerAdapter, Storage);
		}

		private void ReplaceSecurityId(Message message)
		{
			var secMsg = (SecurityMessage)message;
			var securityId = secMsg.SecurityId;

			if ((secMsg as MarketDataMessage)?.DataType == MarketDataTypes.News && securityId.IsDefault())
				return;

			SecurityId adapterId;

			lock (_syncRoot)
				adapterId = _securityIds.TryGetKey(securityId);

			if (!adapterId.IsDefault())
			{
				message.ReplaceSecurityId(adapterId);
			}
		}

		private void ProcessMessage<TMessage>(SecurityId adapterId, TMessage message)
			where TMessage : Message
		{
			if (!adapterId.IsDefault())
			{
				SecurityId securityId;

				lock (_syncRoot)
					securityId = _securityIds.TryGetValue(adapterId);

				if (!securityId.IsDefault())
				{
					message.ReplaceSecurityId(securityId);
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void OnStorageMappingChanged(string storageName, SecurityId securityId, SecurityId adapterId)
		{
			if (!InnerAdapter.StorageName.CompareIgnoreCase(storageName))
				return;

			// if adapter code is empty means mapping removed
			// also mapping can be changed (new adapter code for old security code)

			_securityIds.RemoveByValue(securityId);

			if (!adapterId.IsDefault())
				_securityIds.Add(securityId, adapterId);
		}
	}
}