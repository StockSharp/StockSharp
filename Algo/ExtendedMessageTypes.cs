namespace StockSharp.Algo;

/// <summary>
/// Extended <see cref="MessageTypes"/>.
/// </summary>
public static class ExtendedMessageTypes
{
	/// <summary>
	/// <see cref="Testing.Generation.GeneratorMessage"/>.
	/// </summary>
	public const MessageTypes Generator = (MessageTypes)(-6);

	/// <summary>
	/// <see cref="Testing.CommissionRuleMessage"/>.
	/// </summary>
	public const MessageTypes CommissionRule = (MessageTypes)(-7);
	
	internal const MessageTypes RemoveSecurity = (MessageTypes)(-9);
	//internal const MessageTypes ProcessSuspended = (MessageTypes)(-10);
	internal const MessageTypes StrategyChangeState = (MessageTypes)(-11);
	internal const MessageTypes Reconnect = (MessageTypes)(-12);

	internal const MessageTypes PartialDownload = (MessageTypes)(-21);

	/// <summary>
	/// <see cref="SubscriptionSecurityAllMessage"/>.
	/// </summary>
	public const MessageTypes SubscriptionSecurityAll = (MessageTypes)(-26);
}