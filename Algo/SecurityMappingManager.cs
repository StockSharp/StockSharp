namespace StockSharp.Algo;

using StockSharp.Algo.Storages;

/// <summary>
/// Security mapping message processing logic.
/// </summary>
public interface ISecurityMappingManager
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <returns>Processing result with modified message (or null if not modified) and whether to forward.</returns>
	(Message message, bool forward) ProcessInMessage(Message message);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <returns>Processing result with modified message (or null if should be skipped) and whether to forward.</returns>
	(Message message, bool forward) ProcessOutMessage(Message message);
}

/// <summary>
/// Security mapping message processing implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SecurityMappingManager"/>.
/// </remarks>
/// <param name="provider">Security identifier mappings storage provider.</param>
/// <param name="storageName">Storage name for lookups.</param>
/// <param name="logInfo">Logger for info messages.</param>
public sealed class SecurityMappingManager(
	ISecurityMappingStorageProvider provider,
	Func<string> storageName,
	Action<string, object, object, object> logInfo) : ISecurityMappingManager
{
	private readonly ISecurityMappingStorageProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
	private readonly Func<string> _storageName = storageName ?? throw new ArgumentNullException(nameof(storageName));
	private readonly Action<string, object, object, object> _logInfo = logInfo;

	private string StorageName => _storageName();

	private ISecurityMappingStorage GetStorage() => _provider.GetStorage(StorageName);

	private ISecurityMappingStorage GetStorage(string name) => _provider.GetStorage(name);

	/// <inheritdoc />
	public (Message message, bool forward) ProcessInMessage(Message message)
	{
		switch (message)
		{
			case SecurityLookupMessage:
				// SecurityLookupMessage passes through without modification
				return (message, true);

			case SecurityMessage secMsg:
				ReplaceSecurityId(secMsg);
				return (message, true);

			default:
				return (message, true);
		}
	}

	/// <inheritdoc />
	public (Message message, bool forward) ProcessOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Security:
			{
				var secMsg = (SecurityMessage)message;

				var adapterId = secMsg.SecurityId.SetNativeId(null);

				if (adapterId == default)
					throw new InvalidOperationException(secMsg.ToString());

				var stockSharpId = GetStorage().TryGetStockSharpId(adapterId);

				if (stockSharpId != null)
					secMsg.SecurityId = stockSharpId.Value;

				return (message, true);
			}

			case MessageTypes.News:
			{
				var newsMsg = (NewsMessage)message;

				if (newsMsg.SecurityId != null)
				{
					var adapterId = newsMsg.SecurityId.Value;

					if (!adapterId.IsSpecial)
						adapterId = adapterId.SetNativeId(null);

					if (adapterId != default)
					{
						var stockSharpId = GetStorage().TryGetStockSharpId(adapterId);

						if (stockSharpId != null)
							newsMsg.SecurityId = stockSharpId;
					}
				}

				return (message, true);
			}

			case MessageTypes.SecurityMapping:
			{
				var mappingMsg = (SecurityMappingMessage)message;

				if (mappingMsg.IsDelete)
					GetStorage(mappingMsg.StorageName).Remove(mappingMsg.Mapping.StockSharpId);
				else
					GetStorage(mappingMsg.StorageName).Save(mappingMsg.Mapping);

				// SecurityMappingMessage is consumed, not forwarded
				return (null, false);
			}

			default:
			{
				if (message is ISecurityIdMessage secIdMsg && !secIdMsg.SecurityId.IsSpecial)
				{
					ProcessSecurityIdMessage(secIdMsg.SecurityId, message);
				}

				return (message, true);
			}
		}
	}

	private void ReplaceSecurityId(SecurityMessage secMsg)
	{
		if (secMsg.SecurityId == default)
			return;

		var stockSharpId = secMsg.SecurityId.SetNativeId(null);
		var adapterId = GetStorage().TryGetAdapterId(stockSharpId);

		if (adapterId != null)
		{
			_logInfo?.Invoke("{0}->{1}, {2}", stockSharpId, adapterId.Value, secMsg);
			secMsg.ReplaceSecurityId(adapterId.Value);
		}
	}

	private void ProcessSecurityIdMessage(SecurityId adapterId, Message message)
	{
		if (!adapterId.IsSpecial)
			adapterId = adapterId.SetNativeId(null);

		if (adapterId != default)
		{
			var stockSharpId = GetStorage().TryGetStockSharpId(adapterId);

			if (stockSharpId != null)
				message.ReplaceSecurityId(stockSharpId.Value);
		}
	}
}
