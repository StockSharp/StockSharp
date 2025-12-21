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
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
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

				await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
				break;
			}

			case MessageTypes.News:
			{
				var newsMsg = (NewsMessage)message;

				if (newsMsg.SecurityId != null)
					await ProcessMessageAsync(newsMsg.SecurityId.Value, newsMsg, cancellationToken);
				else
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

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
				if (message is ISecurityIdMessage secIdMsg && !secIdMsg.SecurityId.IsSpecial)
					await ProcessMessageAsync(secIdMsg.SecurityId, message, cancellationToken);
				else
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				break;
			}
		}
	}

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message)
		{
			case SecurityLookupMessage _:
				break;

			case SecurityMessage secMsg:
				ReplaceSecurityId(secMsg);
				break;
		}

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="SecurityMappingMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
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

	private ValueTask ProcessMessageAsync(SecurityId adapterId, Message message, CancellationToken cancellationToken)
	{
		if (!adapterId.IsSpecial)
			adapterId.SetNativeId(null);

		if (adapterId != default)
		{
			var stockSharpId = Storage.TryGetStockSharpId(StorageName, adapterId);

			if (stockSharpId != null)
				message.ReplaceSecurityId(stockSharpId.Value);
		}

		return base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}
}