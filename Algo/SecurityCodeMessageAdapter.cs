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
	/// Security code mappings storage.
	/// </summary>
	public interface ISecurityCodeMappingStorage
	{
		/// <summary>
		/// The new native security identifier added to storage.
		/// </summary>
		event Action<string, string, string> Changed;

		/// <summary>
		/// Get storae names.
		/// </summary>
		/// <returns>Storage names.</returns>
		IEnumerable<string> GetStorageNames();

		/// <summary>
		/// Get security code mappings for storage. 
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <returns>Security code mappings.</returns>
		IEnumerable<Tuple<string, string>> Get(string name);

		/// <summary>
		/// Add security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name</param>
		/// <param name="securityCode">Security code.</param>
		/// <param name="adapterCode">Adapter security code.</param>
		/// <returns><see langword="true"/> if code mapping was added. If was changed, <see langword="false" />.</returns>
		bool Add(string storageName, string securityCode, string adapterCode);

		/// <summary>
		/// Remove security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name</param>
		/// <param name="securityCode">Security code.</param>
		/// <returns><see langword="true"/> if code mapping was added. Otherwise, <see langword="false" />.</returns>
		bool Remove(string storageName, string securityCode);
	}

	/// <summary>
	/// In memory security code mapping storage.
	/// </summary>
	public class InMemorySecurityCodeMappingStorage : ISecurityCodeMappingStorage
	{
		private readonly SynchronizedDictionary<string, PairSet<string, string>> _mappings = new SynchronizedDictionary<string, PairSet<string, string>>(StringComparer.InvariantCultureIgnoreCase);

		private event Action<string, string, string> _changed;

		event Action<string, string, string> ISecurityCodeMappingStorage.Changed
		{
			add => _changed += value;
			remove => _changed -= value;
		}

		IEnumerable<string> ISecurityCodeMappingStorage.GetStorageNames()
		{
			lock (_mappings.SyncRoot)
				return _mappings.Keys.ToArray();
		}

		IEnumerable<Tuple<string, string>> ISecurityCodeMappingStorage.Get(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_mappings.SyncRoot)
				return _mappings.TryGetValue(name)?.Select(p => Tuple.Create(p.Key, p.Value)).ToArray() ?? ArrayHelper.Empty<Tuple<string, string>>();
		}

		bool ISecurityCodeMappingStorage.Add(string storageName, string securityCode, string adapterCode)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (securityCode.IsEmpty())
				throw new ArgumentNullException(nameof(securityCode));

			if (adapterCode.IsEmpty())
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

		bool ISecurityCodeMappingStorage.Remove(string storageName, string securityCode)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (securityCode.IsEmpty())
				throw new ArgumentNullException(nameof(securityCode));

			lock (_mappings.SyncRoot)
			{
				var mappings = _mappings.TryGetValue(storageName);

				if (mappings == null)
					return false;

				var removed = mappings.Remove(securityCode);

				if (!removed)
					return false;
			}

			_changed?.Invoke(storageName, securityCode, null);

			return true;
		}
	}

	/// <summary>
	/// CSV security native identifier storage.
	/// </summary>
	public sealed class CsvSecurityCodeMappingStorage : ISecurityCodeMappingStorage
	{
		private readonly SynchronizedDictionary<string, PairSet<string, string>> _mappings = new SynchronizedDictionary<string, PairSet<string, string>>(StringComparer.InvariantCultureIgnoreCase);

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
		/// The new native security identifier added to storage.
		/// </summary>
		public event Action<string, string, string> Changed;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvSecurityCodeMappingStorage"/>.
		/// </summary>
		/// <param name="path">Path to storage.</param>
		public CsvSecurityCodeMappingStorage(string path)
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
		/// Get security code mappings for storage. 
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <returns>Security code mappings.</returns>
		public IEnumerable<Tuple<string, string>> Get(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_mappings.SyncRoot)
				return _mappings.TryGetValue(name)?.Select(p => Tuple.Create(p.Key, p.Value)).ToArray() ?? ArrayHelper.Empty<Tuple<string, string>>();
		}

		/// <summary>
		/// Add security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name</param>
		/// <param name="securityCode">Security code.</param>
		/// <param name="adapterCode">Adapter security code.</param>
		/// <returns><see langword="true"/> if code mapping was added. If was changed, <see langword="false" />.</returns>
		public bool Add(string storageName, string securityCode, string adapterCode)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (securityCode.IsEmpty())
				throw new ArgumentNullException(nameof(securityCode));

			if (adapterCode.IsEmpty())
				throw new ArgumentNullException(nameof(adapterCode));

			PairSet<string, string> mappings;
			var added = false;

			lock (_mappings.SyncRoot)
			{
				mappings = _mappings.SafeAdd(storageName);

				if (mappings.ContainsKey(securityCode))
				{
					mappings.Remove(securityCode);
				}
				else
					added = true;

				mappings.Add(securityCode, adapterCode);
			}

			if (!added)
			{
				KeyValuePair<string, string>[] items;

				lock (_mappings.SyncRoot)
					items = mappings.ToArray();

				Save(storageName, true, items);
			}
			else
				Save(storageName, false, new[] { new KeyValuePair<string, string>(securityCode, adapterCode) });

			Changed?.Invoke(storageName, securityCode, adapterCode);

			return added;
		}

		/// <summary>
		/// Remove security code mapping.
		/// </summary>
		/// <param name="storageName">Storage name</param>
		/// <param name="securityCode">Security code.</param>
		/// <returns><see langword="true"/> if code mapping was added. Otherwise, <see langword="false" />.</returns>
		public bool Remove(string storageName, string securityCode)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (securityCode.IsEmpty())
				throw new ArgumentNullException(nameof(securityCode));

			PairSet<string, string> mappings;

			lock (_mappings.SyncRoot)
			{
				mappings = _mappings.TryGetValue(storageName);

				if (mappings == null)
					return false;

				var removed = mappings.Remove(securityCode);

				if (!removed)
					return false;
			}

			KeyValuePair<string, string>[] items;

			lock (_mappings.SyncRoot)
				items = mappings.ToArray();

			Save(storageName, true, items);

			Changed?.Invoke(storageName, securityCode, null);

			return true;
		}

		private void LoadFile(string fileName)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				if (!File.Exists(fileName))
					return;

				var name = Path.GetFileNameWithoutExtension(fileName);

				var pairs = new List<Tuple<string, string>>();

				using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					var reader = new FastCsvReader(stream, Encoding.UTF8);

					reader.NextLine();

					while (reader.NextLine())
					{
						var securityCode = reader.ReadString();
						var adapterCode = reader.ReadString();

						pairs.Add(Tuple.Create(securityCode, adapterCode));
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

		private void Save(string name, bool overwrite, IEnumerable<KeyValuePair<string, string>> items)
		{
			DelayAction.DefaultGroup.Add(() =>
			{
				var fileName = Path.Combine(_path, name + ".csv");

				var appendHeader = overwrite || !File.Exists(fileName);
				var mode = overwrite ? FileMode.Create : FileMode.Append;

				using (var writer = new CsvFileWriter(new TransactionFileStream(fileName, mode)))
				{
					if (appendHeader)
						writer.WriteRow(new[] { "SecurityCode", "AdapterCode" });

					foreach (var item in items)
						writer.WriteRow(new[] { item.Key, item.Value });
				}
			}, canBatch: false);
		}
	}

	/// <summary>
	/// Security native id message adapter.
	/// </summary>
	public class SecurityCodeMappingMessageAdapter : MessageAdapterWrapper
	{
		private readonly PairSet<string, string> _securityCodes = new PairSet<string, string>();
		private readonly SyncObject _syncRoot = new SyncObject();

		/// <summary>
		/// Security code mappings storage.
		/// </summary>
		public ISecurityCodeMappingStorage Storage { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityCodeMappingMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="storage">Security code mappings storage.</param>
		public SecurityCodeMappingMessageAdapter(IMessageAdapter innerAdapter, ISecurityCodeMappingStorage storage)
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

							_securityCodes.Add(adapterCode, securityCode);
						}
					}

					base.OnInnerAdapterNewOutMessage(message);
					break;
				}

				case MessageTypes.Reset:
				{
					lock (_syncRoot)
					{
						_securityCodes.Clear();
					}

					base.OnInnerAdapterNewOutMessage(message);
					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					var securityId = secMsg.SecurityId;

					var securityCode = securityId.SecurityCode;

					if (securityCode.IsEmpty())
						throw new InvalidOperationException();

					string code;

					lock (_syncRoot)
						code = _securityCodes.TryGetValue(securityCode);

					if (!code.IsEmpty())
					{
						securityId.SecurityCode = code;
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
				case MessageTypes.MarketData:
				{
					var secMsg = (SecurityMessage)message;
					var securityId = secMsg.SecurityId;

					if ((secMsg as MarketDataMessage)?.DataType == MarketDataTypes.News && securityId.IsDefault())
						break;

					string code;

					lock (_syncRoot)
						code = _securityCodes.TryGetKey(securityId.SecurityCode);

					if (!code.IsEmpty())
					{
						securityId.SecurityCode = code;
						message.ReplaceSecurityId(securityId);
					}

					break;
				}

				case MessageTypes.OrderPairReplace:
				{
					var pairMsg = (OrderPairReplaceMessage)message;
					// TODO
					break;
				}
			}

			base.SendInMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityCodeMappingMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SecurityCodeMappingMessageAdapter(InnerAdapter, Storage);
		}

		private void ProcessMessage<TMessage>(SecurityId securityId, TMessage message)
			where TMessage : Message
		{
			var securityCode = securityId.SecurityCode;

			if (!securityCode.IsEmpty())
			{
				string code;

				lock (_syncRoot)
					code = _securityCodes.TryGetValue(securityCode);

				if (code != null)
				{
					securityId.SecurityCode = code;
					message.ReplaceSecurityId(securityId);
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void OnStorageMappingChanged(string storageName, string securityCode, string adapterCode)
		{
			if (!InnerAdapter.StorageName.CompareIgnoreCase(storageName))
				return;

			// if adapter code is empty means mapping removed
			// also mapping can be changed (new adapter code for old security code)

			_securityCodes.RemoveByValue(securityCode);

			if (!adapterCode.IsEmpty())
				_securityCodes.Add(adapterCode, securityCode);
		}
	}
}