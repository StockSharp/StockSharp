namespace StockSharp.Algo
{
	using StockSharp.Algo.Strategies.Messages;
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

		/// <summary>
		/// <see cref="StrategyLookupMessage"/>.
		/// </summary>
		public const MessageTypes StrategyLookup = (MessageTypes)(-15);

		/// <summary>
		/// <see cref="StrategyLookupResultMessage"/>.
		/// </summary>
		public const MessageTypes StrategyLookupResult = (MessageTypes)(-16);

		/// <summary>
		/// <see cref="StrategyInfoMessage"/>.
		/// </summary>
		public const MessageTypes StrategyInfo = (MessageTypes)(-17);

		/// <summary>
		/// <see cref="StrategyTypeMessage"/>.
		/// </summary>
		public const MessageTypes StrategyType = (MessageTypes)(-18);

		/// <summary>
		/// <see cref="StrategyStateMessage"/>.
		/// </summary>
		public const MessageTypes StrategyState = (MessageTypes)(-19);
	}
}