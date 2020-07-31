namespace StockSharp.Algo
{
	using StockSharp.Algo.Storages.Remote.Messages;
	using StockSharp.Algo.Strategies.Messages;
	using StockSharp.Algo.Testing;
	using StockSharp.Messages;

	/// <summary>
	/// Extended <see cref="MessageTypes"/>.
	/// </summary>
	public static class ExtendedMessageTypes
	{
		///// <summary>
		///// The last message identifier.
		///// </summary>
		//public const MessageTypes Last = (MessageTypes)(-1);

		///// <summary>
		///// <see cref="ClearingMessage"/>.
		///// </summary>
		//public const MessageTypes Clearing = (MessageTypes)(-2);

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
		//internal const MessageTypes ProcessSuspended = (MessageTypes)(-10);
		internal const MessageTypes StrategyChangeState = (MessageTypes)(-11);
		internal const MessageTypes Reconnect = (MessageTypes)(-12);
		internal const MessageTypes ReconnectingFinished = (MessageTypes)(-13);
		
		/// <summary>
		/// <see cref="ChangeTimeIntervalMessage"/>.
		/// </summary>
		public const MessageTypes ChangeTimeInterval = (MessageTypes)(-14);

		/// <summary>
		/// <see cref="StrategyLookupMessage"/>.
		/// </summary>
		public const MessageTypes StrategyLookup = (MessageTypes)(-15);

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

		internal const MessageTypes ReconnectingStarted = (MessageTypes)(-20);

		internal const MessageTypes PartialDownload = (MessageTypes)(-21);

		/// <summary>
		/// <see cref="RemoteFileMessage"/>.
		/// </summary>
		public const MessageTypes RemoteFile = (MessageTypes)(-22);

		/// <summary>
		/// <see cref="RemoteFileCommandMessage"/>.
		/// </summary>
		public const MessageTypes RemoteFileCommand = (MessageTypes)(-23);

		/// <summary>
		/// <see cref="StrategySubscriptionInfoMessage"/>.
		/// </summary>
		public const MessageTypes StrategySubscriptionInfo = (MessageTypes)(-24);

		/// <summary>
		/// <see cref="StrategyBacktestResultMessage"/>.
		/// </summary>
		public const MessageTypes StrategyBacktestResult = (MessageTypes)(-25);

		/// <summary>
		/// <see cref="SubscriptionSecurityAllMessage"/>.
		/// </summary>
		public const MessageTypes SubscriptionSecurityAll = (MessageTypes)(-26);

		/// <summary>
		/// <see cref="AvailableDataRequestMessage"/>.
		/// </summary>
		public const MessageTypes AvailableDataRequest = (MessageTypes)(-27);

		/// <summary>
		/// <see cref="AvailableDataInfoMessage"/>.
		/// </summary>
		public const MessageTypes AvailableDataInfo = (MessageTypes)(-28);
	}
}