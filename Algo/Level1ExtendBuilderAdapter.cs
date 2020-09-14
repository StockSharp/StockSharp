namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Level1 extend builder adapter.
	/// </summary>
	public class Level1ExtendBuilderAdapter : MessageAdapterWrapper
	{
		private class Level1Info
		{
			//public readonly HashSet<DataType> AllowedSources = new HashSet<DataType>
			//{
			//	DataType.Ticks,
			//	DataType.MarketDepth,
			//	DataType.CandleTimeFrame,
			//};

			public Level1Info(MarketDataMessage origin)
				=> Origin = origin ?? throw new ArgumentNullException(nameof(origin));

			public MarketDataMessage Origin { get; }
			public SubscriptionStates State { get; set; }
		}

		private readonly SyncObject _syncObject = new SyncObject();
		private readonly Dictionary<long, Level1Info> _level1Subscriptions = new Dictionary<long, Level1Info>();

		/// <summary>
		/// Initializes a new instance of the <see cref="Level1ExtendBuilderAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public Level1ExtendBuilderAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public override bool SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_syncObject)
						_level1Subscriptions.Clear();
					
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.SecurityId == default)
							break;

						if (mdMsg.BuildMode == MarketDataBuildModes.Load)
							break;

						if (mdMsg.DataType2 != DataType.Level1 || mdMsg.To != null)
							break;

						var transId = mdMsg.TransactionId;

						mdMsg = mdMsg.TypedClone();
						mdMsg.DataType2 = mdMsg.BuildFrom ?? DataType.MarketDepth;
						mdMsg.BuildFrom = null;

						lock (_syncObject)
							_level1Subscriptions.Add(transId, new Level1Info(mdMsg.TypedClone()));
						
						message = mdMsg;
						break;
					}
					else
					{
						var id = mdMsg.OriginalTransactionId;

						lock (_syncObject)
						{
							if (_level1Subscriptions.TryGetAndRemove(id, out var info))
								info.State = info.State.ChangeSubscriptionState(SubscriptionStates.Stopped, id, this);
						}

						break;
					}
				}
			}

			return base.SendInMessage(message);
		}

		private TMessage TryConvert<TMessage>(TMessage subscrMsg, DataType dataType, Func<TMessage, Level1ChangeMessage> convert)
			where TMessage : ISubscriptionIdMessage
		{
			if (subscrMsg is null)
				throw new ArgumentNullException(nameof(subscrMsg));

			List<long> subscriptions = null;
			List<long> leftSubscriptions = null;

			lock (_syncObject)
			{
				if (_level1Subscriptions.Count == 0)
					return subscrMsg;

				var ids = subscrMsg.GetSubscriptionIds();

				foreach (var id in ids)
				{
					if (!_level1Subscriptions.TryGetValue(id, out var info) || info.Origin.DataType2 != dataType)
					{
						leftSubscriptions ??= new List<long>();
						leftSubscriptions.Add(id);
						continue;
					}

					subscriptions ??= new List<long>();
					subscriptions.Add(id);
				}
			}

			if (subscriptions == null)
				return subscrMsg;

			var level1 = convert(subscrMsg);
			level1.SetSubscriptionIds(subscriptions.ToArray());
			base.OnInnerAdapterNewOutMessage(level1);

			if (leftSubscriptions == null)
				return default;

			subscrMsg.SetSubscriptionIds(leftSubscriptions.ToArray());
			return subscrMsg;
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				{
					var responseMsg = (SubscriptionResponseMessage)message;
					var id = responseMsg.OriginalTransactionId;

					lock (_syncObject)
					{
						if (!_level1Subscriptions.TryGetValue(id, out var info))
							break;

						if (responseMsg.IsOk())
							info.State = info.State.ChangeSubscriptionState(SubscriptionStates.Active, id, this);
						else
						{
							_level1Subscriptions.Remove(id);
							info.State = info.State.ChangeSubscriptionState(SubscriptionStates.Error, id, this);
						}
					}

					break;
				}

				case MessageTypes.SubscriptionFinished:
				{
					var id = ((SubscriptionFinishedMessage)message).OriginalTransactionId;

					lock (_syncObject)
					{
						if (_level1Subscriptions.TryGetAndRemove(id, out var info))
							info.State = info.State.ChangeSubscriptionState(SubscriptionStates.Finished, id, this);
					}

					break;
				}

				case MessageTypes.SubscriptionOnline:
				{
					var id = ((SubscriptionOnlineMessage)message).OriginalTransactionId;

					lock (_syncObject)
					{
						if (_level1Subscriptions.TryGetValue(id, out var info))
							info.State = info.State.ChangeSubscriptionState(SubscriptionStates.Online, id, this);
					}
					
					break;
				}

				//case MessageTypes.Level1Change:
				//{
				//	var level1Msg = (Level1ChangeMessage)message;

				//	lock (_syncObject)
				//	{
				//		if (_level1Subscriptions.Count == 0)
				//			break;

				//		bool? hasBestQuote = null;
				//		bool? hasLastTrade = null;
				//		bool? hasCandle = null;

				//		foreach (var id in level1Msg.GetSubscriptionIds())
				//		{
				//			if (!_level1Subscriptions.TryGetValue(id, out var info))
				//				continue;

				//			var sources = info.AllowedSources;

				//			if (sources.Contains(DataType.MarketDepth))
				//			{
				//				hasBestQuote ??= level1Msg.IsContainsQuotes();

				//				if (hasBestQuote == true)
				//					sources.Remove(DataType.MarketDepth);
				//			}
							
				//			if (sources.Contains(DataType.Ticks))
				//			{
				//				hasLastTrade ??= level1Msg.IsContainsTick();

				//				if (hasLastTrade == true)
				//					sources.Remove(DataType.Ticks);
				//			}

				//			if (sources.Contains(DataType.CandleTimeFrame))
				//			{
				//				hasCandle ??= level1Msg.IsContainsCandle();

				//				if (hasCandle == true)
				//					sources.Remove(DataType.CandleTimeFrame);
				//			}
				//		}
				//	}

				//	break;
				//}

				case MessageTypes.QuoteChange:
				{
					var quotesMsg = (QuoteChangeMessage)message;

					if (quotesMsg.State != null)
						break;

					message = TryConvert(quotesMsg, DataType.MarketDepth, Extensions.ToLevel1);
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType != ExecutionTypes.Tick)
						break;

					message = TryConvert(execMsg, DataType.Ticks, Extensions.ToLevel1);
					break;
				}

				case MessageTypes.CandleTimeFrame:
				{
					var candleMsg = (TimeFrameCandleMessage)message;

					message = TryConvert(candleMsg, DataType.CandleTimeFrame, Extensions.ToLevel1);
					break;
				}
			}

			if (message != null)
				base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="Level1ExtendBuilderAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone() => new Level1ExtendBuilderAdapter(InnerAdapter.TypedClone());
	}
}