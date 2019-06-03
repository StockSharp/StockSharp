namespace StockSharp.Algo
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.Messages;

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
			Storage = storage ?? throw new ArgumentNullException(nameof(storage));
			Storage.Changed += OnStorageMappingChanged;
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			Storage.Changed -= OnStorageMappingChanged;
			base.Dispose();
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				base.OnInnerAdapterNewOutMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					var mappings = Storage.Get(StorageName);

					lock (_syncRoot)
					{
						foreach (var mapping in mappings)
						{
							_securityIds.Add(mapping.StockSharpId, mapping.AdapterId);
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

					SecurityId? stockSharpId;

					lock (_syncRoot)
						stockSharpId = _securityIds.TryGetValue2(adapterId);

					if (stockSharpId != null)
						secMsg.SecurityId = stockSharpId.Value;

					base.OnInnerAdapterNewOutMessage(message);
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
				{
					if (message is ISecurityIdMessage secIdMsg)
						ProcessMessage(secIdMsg.SecurityId, message);
					else
						base.OnInnerAdapterNewOutMessage(message);

					break;
				}
			}
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
		{
			switch (message)
			{
				case ISecurityIdMessage _:
					ReplaceSecurityId(message);
					break;
				case OrderPairReplaceMessage pairMsg:
					ReplaceSecurityId(pairMsg.Message1);
					ReplaceSecurityId(pairMsg.Message2);
					break;
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

			if (secMsg.NotRequiredSecurityId())
				return;

			SecurityId? adapterId;
			var stockSharpId = secMsg.SecurityId;

			lock (_syncRoot)
				adapterId = _securityIds.TryGetValue2(stockSharpId);

			if (adapterId != null)
				message.ReplaceSecurityId(adapterId.Value);
		}

		private void ProcessMessage(SecurityId adapterId, Message message)
		{
			if (!adapterId.IsDefault())
			{
				SecurityId? stockSharpId;

				lock (_syncRoot)
					stockSharpId = _securityIds.TryGetKey2(adapterId);

				if (stockSharpId != null)
					message.ReplaceSecurityId(stockSharpId.Value);
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void OnStorageMappingChanged(string storageName, SecurityIdMapping mapping)
		{
			if (!StorageName.CompareIgnoreCase(storageName))
				return;

			// if adapter code is empty means mapping removed
			// also mapping can be changed (new adapter code for old security code)

			lock (_syncRoot)
			{
				_securityIds.RemoveByValue(mapping.StockSharpId);

				if (!mapping.AdapterId.IsDefault())
					_securityIds.Add(mapping.StockSharpId, mapping.AdapterId);	
			}
		}
	}
}