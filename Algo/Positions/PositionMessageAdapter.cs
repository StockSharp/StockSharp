#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Positions.Algo
File: PositionMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Positions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically calculating position.
	/// </summary>
	public class PositionMessageAdapter : MessageAdapterWrapper
	{
		private readonly SyncObject _sync = new SyncObject();
		private readonly IPositionManager _positionManager;

		private readonly CachedSynchronizedSet<long> _subscriptions = new CachedSynchronizedSet<long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="positionManager">The position calculation manager..</param>
		public PositionMessageAdapter(IMessageAdapter innerAdapter, IPositionManager positionManager)
			: base(innerAdapter)
		{
			_positionManager = positionManager ?? throw new ArgumentNullException(nameof(positionManager));

			if (_positionManager is ILogSource source && source.Parent == null)
				source.Parent = this;
		}

		private bool IsEmulate => InnerAdapter.IsPositionsEmulationRequired != null;

		/// <inheritdoc />
		public override IEnumerable<MessageTypeInfo> PossibleSupportedMessages
			=> InnerAdapter.PossibleSupportedMessages.Concat(IsEmulate ? new[] { MessageTypes.PortfolioLookup.ToInfo() } : Enumerable.Empty<MessageTypeInfo>()).Distinct();

		/// <inheritdoc />
		public override IEnumerable<MessageTypes> SupportedResultMessages
			=> InnerAdapter.SupportedResultMessages.Concat(IsEmulate ? new[] { MessageTypes.PortfolioLookup } : Enumerable.Empty<MessageTypes>()).Distinct();

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
						if (lookupMsg.To == null)
						{
							this.AddDebugLog("Subscription {0} added.", lookupMsg.TransactionId);
							_subscriptions.Add(lookupMsg.TransactionId);

							lock (_sync)
								_positionManager.ProcessMessage(message);
						}

						if (IsEmulate)
						{
							RaiseNewOutMessage(lookupMsg.CreateResult());
							return true;
						}
					}
					else
					{
						if (_subscriptions.Remove(lookupMsg.OriginalTransactionId))
						{
							this.AddDebugLog("Subscription {0} removed.", lookupMsg.OriginalTransactionId);

							lock (_sync)
								_positionManager.ProcessMessage(message);
						}

						if (IsEmulate)
						{
							//RaiseNewOutMessage(lookupMsg.CreateResponse());
							return true;
						}
					}

					break;
				}

				default:
				{
					lock (_sync)
						_positionManager.ProcessMessage(message);

					break;
				}
			}
			
			return base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			PositionChangeMessage change = null;

			if (message.Type != MessageTypes.Reset &&
				message.Type != MessageTypes.Connect &&
				message.Type != MessageTypes.Disconnect)
			{
				lock (_sync)
					change = _positionManager.ProcessMessage(message);
			}

			base.OnInnerAdapterNewOutMessage(message);

			if (change != null)
			{
				var subscriptions = _subscriptions.Cache;

				if (subscriptions.Length > 0)
					change.SetSubscriptionIds(subscriptions);

				base.OnInnerAdapterNewOutMessage(change);
			}
		}

		/// <summary>
		/// Create a copy of <see cref="PositionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone() => new PositionMessageAdapter(InnerAdapter.TypedClone(), _positionManager);
	}
}