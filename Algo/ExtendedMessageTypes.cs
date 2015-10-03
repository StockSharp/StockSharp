namespace StockSharp.Algo
{
	using System.Reflection;

	using StockSharp.Messages;

	[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	static class ExtendedMessageTypes
	{
		public const MessageTypes Last = (MessageTypes)(-1);
		public const MessageTypes Clearing = (MessageTypes)(-2);
		public const MessageTypes EmulationState = (MessageTypes)(-5);
		public const MessageTypes Generator = (MessageTypes)(-6);
		public const MessageTypes CommissionRule = (MessageTypes)(-7);
		public const MessageTypes HistorySource = (MessageTypes)(-8);
	}
}