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
					var mappings = Storage.Get(InnerAdapter.StorageName);

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
			var stockSharpId = secMsg.SecurityId;

			if ((secMsg as MarketDataMessage)?.DataType == MarketDataTypes.News && stockSharpId.IsDefault())
				return;

			SecurityId? adapterId;

			lock (_syncRoot)
				adapterId = _securityIds.TryGetKey2(stockSharpId);

			if (adapterId != null)
			{
				message.ReplaceSecurityId(adapterId.Value);
			}
		}

		private void ProcessMessage<TMessage>(SecurityId adapterId, TMessage message)
			where TMessage : Message
		{
			if (!adapterId.IsDefault())
			{
				SecurityId? securityId;

				lock (_syncRoot)
					securityId = _securityIds.TryGetValue2(adapterId);

				if (securityId != null)
				{
					message.ReplaceSecurityId(securityId.Value);
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void OnStorageMappingChanged(string storageName, SecurityIdMapping mapping)
		{
			if (!InnerAdapter.StorageName.CompareIgnoreCase(storageName))
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