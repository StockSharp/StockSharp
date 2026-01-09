namespace StockSharp.Algo;

/// <summary>
/// Extended <see cref="MessageTypes"/>.
/// </summary>
public static class ExtendedMessageTypes
{
	internal const MessageTypes RemoveSecurity = (MessageTypes)(-9);
	//internal const MessageTypes ProcessSuspended = (MessageTypes)(-10);
	internal const MessageTypes Reconnect = (MessageTypes)(-12);

	internal const MessageTypes PartialDownload = (MessageTypes)(-21);

	/// <summary>
	/// <see cref="SubscriptionSecurityAllMessage"/>.
	/// </summary>
	public const MessageTypes SubscriptionSecurityAll = (MessageTypes)(-26);
}