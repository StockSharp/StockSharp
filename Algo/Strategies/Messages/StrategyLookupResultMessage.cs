namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Strategies search result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyLookupResultMessage : BaseResultMessage<StrategyLookupResultMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyLookupResultMessage"/>.
		/// </summary>
		public StrategyLookupResultMessage()
			: base(ExtendedMessageTypes.StrategyLookupResult)
		{
		}
	}
}