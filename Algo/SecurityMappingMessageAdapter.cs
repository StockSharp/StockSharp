namespace StockSharp.Algo;

/// <summary>
/// Security identifier mappings message adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SecurityMappingMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
/// <param name="storage">Security identifier mappings storage.</param>
public class SecurityMappingMessageAdapter(IMessageAdapter innerAdapter, ISecurityMappingStorage storage) : MessageAdapterWrapper(innerAdapter)
{
	/// <summary>
	/// Security identifier mappings storage.
	/// </summary>
	public ISecurityMappingStorage Storage { get; } = storage ?? throw new ArgumentNullException(nameof(storage));

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Security:
			{
				var secMsg = (SecurityMessage)message;

				var adapterId = secMsg.SecurityId.SetNativeId(null);

				if (adapterId == default)
					throw new InvalidOperationException(secMsg.ToString());

				var stockSharpId = Storage.TryGetStockSharpId(StorageName, adapterId);

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

			case MessageTypes.SecurityMapping:
			{
				var mappingMsg = (SecurityMappingMessage)message;

				if (mappingMsg.IsDelete)
					Storage.Remove(mappingMsg.StorageName, mappingMsg.Mapping.StockSharpId);
				else
					Storage.Save(mappingMsg.StorageName, mappingMsg.Mapping);

				return;
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
	protected override bool OnSendInMessage(Message message)
	{
		switch (message)
		{
			case SecurityLookupMessage _:
				break;

			case SecurityMessage secMsg:
				ReplaceSecurityId(secMsg);
				break;
		}

		return base.OnSendInMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="SecurityMappingMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new SecurityMappingMessageAdapter(InnerAdapter.TypedClone(), Storage);
	}

	private void ReplaceSecurityId(SecurityMessage secMsg)
	{
		if (secMsg.SecurityId == default)
			return;

		var stockSharpId = secMsg.SecurityId.SetNativeId(null);
		var adapterId = Storage.TryGetAdapterId(StorageName, stockSharpId);

		if (adapterId != null)
		{
			LogInfo("{0}->{1}, {2}", stockSharpId, adapterId.Value, secMsg);
			secMsg.ReplaceSecurityId(adapterId.Value);
		}
	}

	private void ProcessMessage(SecurityId adapterId, Message message)
	{
		if (!adapterId.IsSpecial)
			adapterId.SetNativeId(null);

		if (adapterId != default)
		{
			var stockSharpId = Storage.TryGetStockSharpId(StorageName, adapterId);

			if (stockSharpId != null)
				message.ReplaceSecurityId(stockSharpId.Value);
		}

		base.OnInnerAdapterNewOutMessage(message);
	}
}