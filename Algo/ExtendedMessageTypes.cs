namespace StockSharp.Algo
{
	using StockSharp.Algo.Testing;
	using StockSharp.Messages;

	/// <summary>
	/// Extended <see cref="MessageTypes"/>.
	/// </summary>
	public static class ExtendedMessageTypes
	{
		internal const MessageTypes Last = (MessageTypes)(-1);

		/// <summary>
		/// <see cref="ClearingMessage"/>.
		/// </summary>
		public const MessageTypes Clearing = (MessageTypes)(-2);

		/// <summary>
		/// <see cref="EmulationStateMessage"/>.
		/// </summary>
		public const MessageTypes EmulationState = (MessageTypes)(-5);

		/// <summary>
		/// <see cref="GeneratorMessage"/>.
		/// </summary>
		public const MessageTypes Generator = (MessageTypes)(-6);

		/// <summary>
		/// <see cref="CommissionRuleMessage"/>.
		/// </summary>
		public const MessageTypes CommissionRule = (MessageTypes)(-7);
		
		/// <summary>
		/// <see cref="HistorySourceMessage"/>.
		/// </summary>
		public const MessageTypes HistorySource = (MessageTypes)(-8);
		
		internal const MessageTypes RemoveSecurity = (MessageTypes)(-9);
		internal const MessageTypes ProcessSuspendedSecurityMessages = (MessageTypes)(-10);
		internal const MessageTypes StrategyChangeState = (MessageTypes)(-11);
		internal const MessageTypes Reconnect = (MessageTypes)(-12);
		internal const MessageTypes RestoringSubscription = (MessageTypes)(-13);
		
		/// <summary>
		/// <see cref="ChangeTimeIntervalMessage"/>.
		/// </summary>
		public const MessageTypes ChangeTimeInterval = (MessageTypes)(-14);
	}
}