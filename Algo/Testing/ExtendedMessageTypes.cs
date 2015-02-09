namespace StockSharp.Algo.Testing
{
	using System.Reflection;

	using StockSharp.Messages;

	[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	static class ExtendedMessageTypes
	{
		public const MessageTypes Last = (MessageTypes)(-1);
		public const MessageTypes Clearing = (MessageTypes)(-2);
		public const MessageTypes Reset = (MessageTypes)(-3);
		//public const MessageTypes Action = (MessageTypes)(-4);
		public const MessageTypes EmulationState = (MessageTypes)(-5);
		public const MessageTypes Generator = (MessageTypes)(-6);
		public const MessageTypes CommissionRule = (MessageTypes)(-7);
	}
}