namespace StockSharp.Algo.Positions
{
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically replied on <see cref="PortfolioLookupMessage"/>.
	/// </summary>
	public class PositionReplyMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PositionReplyMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public PositionReplyMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public override IEnumerable<MessageTypeInfo> PossibleSupportedMessages
			=> base.PossibleSupportedMessages.Concat(new[] { MessageTypes.PortfolioLookup.ToInfo() }).Distinct();

		/// <inheritdoc />
		public override IEnumerable<MessageTypes> SupportedResultMessages
			=> base.SupportedResultMessages.Concat(new[] { MessageTypes.PortfolioLookup }).Distinct();

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.PortfolioLookup:
				{
					var lookupMsg = (PortfolioLookupMessage)message;

					if (lookupMsg.IsSubscribe)
					{
						RaiseNewOutMessage(lookupMsg.CreateResult());
					}
					else
					{
						//RaiseNewOutMessage(lookupMsg.CreateResponse());
					}

					return true;
				}
			}
			
			return base.OnSendInMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="PositionReplyMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone() => new PositionReplyMessageAdapter(InnerAdapter.TypedClone());
	}
}