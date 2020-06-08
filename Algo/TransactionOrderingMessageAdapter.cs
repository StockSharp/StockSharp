namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Logging;

	/// <summary>
	/// 
	/// </summary>
	public class TransactionOrderingMessageAdapter : MessageAdapterWrapper
	{
		private class SubscriptionInfo
		{
			public SubscriptionInfo(OrderStatusMessage original)
			{
				Original = original ?? throw new System.ArgumentNullException(nameof(original));
			}

			public SyncObject Sync { get; } = new SyncObject();

			public OrderStatusMessage Original { get; }
			public Dictionary<long, Tuple<ExecutionMessage, List<ExecutionMessage>>> Transactions { get; } = new Dictionary<long, Tuple<ExecutionMessage, List<ExecutionMessage>>>();
		}

		private readonly SynchronizedDictionary<long, SubscriptionInfo> _transactionLogSubscriptions = new SynchronizedDictionary<long, SubscriptionInfo>();
		private readonly SynchronizedDictionary<long, long> _orders = new SynchronizedDictionary<long, long>();
		private readonly SynchronizedDictionary<long, SecurityId> _secIds = new SynchronizedDictionary<long, SecurityId>();

		private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedByIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedByTransactionIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<string, List<ExecutionMessage>> _nonAssociatedByStringIdMyTrades = new Dictionary<string, List<ExecutionMessage>>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedOrderIds = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<string, List<ExecutionMessage>> _nonAssociatedStringOrderIds = new Dictionary<string, List<ExecutionMessage>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="TransactionOrderingMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public TransactionOrderingMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private void Reset()
		{
			_transactionLogSubscriptions.Clear();
			_orders.Clear();

			_secIds.Clear();

			_nonAssociatedByIdMyTrades.Clear();
			_nonAssociatedByStringIdMyTrades.Clear();
			_nonAssociatedByTransactionIdMyTrades.Clear();

			_nonAssociatedOrderIds.Clear();
			_nonAssociatedStringOrderIds.Clear();
		}

		/// <inheritdoc />
		public override bool SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					Reset();
					break;
				}
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					_secIds.TryAdd(regMsg.TransactionId, regMsg.SecurityId);
					break;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;

					if (_secIds.TryGetValue(replaceMsg.OriginalTransactionId, out var secId))
						_secIds.TryAdd(replaceMsg.TransactionId, secId);

					break;
				}
				case MessageTypes.OrderPairReplace:
				{
					var replaceMsg = (OrderPairReplaceMessage)message;

					if (_secIds.TryGetValue(replaceMsg.Message1.OriginalTransactionId, out var secId))
						_secIds.TryAdd(replaceMsg.Message1.TransactionId, secId);

					if (_secIds.TryGetValue(replaceMsg.Message2.OriginalTransactionId, out secId))
						_secIds.TryAdd(replaceMsg.Message2.TransactionId, secId);

					break;
				}
				case MessageTypes.OrderStatus:
				{
					var statusMsg = (OrderStatusMessage)message;

					if (statusMsg.IsSubscribe)
					{
						if (IsSupportTransactionLog)
							_transactionLogSubscriptions.Add(statusMsg.TransactionId, new SubscriptionInfo(statusMsg.TypedClone()));
					}

					break;
				}
			}

			return base.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				{
					var responseMsg = (SubscriptionResponseMessage)message;

					if (responseMsg.Error != null)
					{
						_transactionLogSubscriptions.Remove(responseMsg.OriginalTransactionId);
					}

					break;
				}
				case MessageTypes.SubscriptionFinished:
				case MessageTypes.SubscriptionOnline:
				{
					var originMsg = (IOriginalTransactionIdMessage)message;

					if (!_transactionLogSubscriptions.TryGetAndRemove(originMsg.OriginalTransactionId, out var subscription))
						break;

					foreach (var pair in subscription.Transactions)
					{
						base.OnInnerAdapterNewOutMessage(pair.Value.Item1);

						foreach (var trade in pair.Value.Item2)
							base.OnInnerAdapterNewOutMessage(trade);
					}

					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.IsMarketData())
						break;

					var transId = execMsg.TransactionId;

					if (transId != 0)
						_secIds.TryAdd(transId, execMsg.SecurityId);
					else
					{
						if (_secIds.TryGetValue(execMsg.OriginalTransactionId, out var secId))
							execMsg.SecurityId = secId;
					}

					if (_transactionLogSubscriptions.Count == 0)
						break;

					if (!_transactionLogSubscriptions.TryGetValue(execMsg.OriginalTransactionId, out var subscription))
					{
						if (!_orders.TryGetValue(execMsg.OriginalTransactionId, out var orderTransId))
							break;

						if (!_transactionLogSubscriptions.TryGetValue(orderTransId, out subscription))
							break;
					}

					if (transId == 0)
					{
						if (execMsg.HasTradeInfo)
							transId = execMsg.OriginalTransactionId;

						if (transId == 0)
						{
							this.AddWarningLog("Message {0} do not contains transaction id.", execMsg);
							break;
						}
					}

					lock (subscription.Sync)
					{
						if (subscription.Transactions.TryGetValue(transId, out var tuple))
						{
							var snapshot = tuple.Item1;

							if (execMsg.HasOrderInfo)
							{
								if (execMsg.Balance != null)
									snapshot.Balance = snapshot.Balance.ApplyNewBalance(execMsg.Balance.Value, transId, this);

								if (execMsg.OrderState != null)
									snapshot.OrderState = snapshot.OrderState.ApplyNewState(execMsg.OrderState.Value, transId, this);

								if (execMsg.OrderStatus != null)
									snapshot.OrderStatus = execMsg.OrderStatus;

								if (execMsg.OrderId != null)
									snapshot.OrderId = execMsg.OrderId;

								if (execMsg.OrderStringId != null)
									snapshot.OrderStringId = execMsg.OrderStringId;

								if (execMsg.OrderBoardId != null)
									snapshot.OrderBoardId = execMsg.OrderBoardId;

								if (execMsg.PnL != null)
									snapshot.PnL = execMsg.PnL;

								if (execMsg.Position != null)
									snapshot.Position = execMsg.Position;

								if (execMsg.Commission != null)
									snapshot.Commission = execMsg.Commission;

								if (execMsg.CommissionCurrency != null)
									snapshot.CommissionCurrency = execMsg.CommissionCurrency;

								if (execMsg.AveragePrice != null)
									snapshot.AveragePrice = execMsg.AveragePrice;

								if (execMsg.Latency != null)
									snapshot.Latency = execMsg.Latency;
							}
						
							if (execMsg.HasTradeInfo)
							{
								var clone = execMsg.TypedClone();
								// all order's info in snapshot
								clone.HasOrderInfo = false;
								tuple.Item2.Add(clone);
							}
						}
						else
						{
							_orders.Add(transId, execMsg.OriginalTransactionId);
							subscription.Transactions.Add(transId, Tuple.Create(execMsg.TypedClone(), new List<ExecutionMessage>()));
						}
					}

					return;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		//private void ProcessOrderMessage(ExecutionMessage message)
		//{
		//	if (notFound)
		//	{
		//		if (transactionId == 0 && !isStatusRequest)
		//		{
		//			if (message.OrderId != null)
		//			{
		//				this.AddInfoLog("{0} info suspended.", message.OrderId.Value);
		//				_nonAssociatedOrderIds.SafeAdd(message.OrderId.Value).Add(message.TypedClone());
		//			}
		//			else if (!message.OrderStringId.IsEmpty())
		//			{
		//				this.AddInfoLog("{0} info suspended.", message.OrderStringId);
		//				_nonAssociatedStringOrderIds.SafeAdd(message.OrderStringId).Add(message.TypedClone());
		//			}
		//		}
		//	}
		//	else
		//	{
		//		if (order.Id != null)
		//			ProcessMyTrades(order, order.Id.Value, _nonAssociatedByIdMyTrades);

		//		ProcessMyTrades(order, order.TransactionId, _nonAssociatedByTransactionIdMyTrades);

		//		if (!order.StringId.IsEmpty())
		//			ProcessMyTrades(order, order.StringId, _nonAssociatedByStringIdMyTrades);

		//		//ProcessConditionOrders(order);

		//		List<ExecutionMessage> suspended = null;

		//		if (order.Id != null)
		//			suspended = _nonAssociatedOrderIds.TryGetAndRemove(order.Id.Value);
		//		else if (!order.StringId.IsEmpty())
		//			suspended = _nonAssociatedStringOrderIds.TryGetAndRemove(order.StringId);

		//		if (suspended != null)
		//		{
		//			this.AddInfoLog("{0} resumed.", order.Id);

		//			foreach (var s in suspended)
		//			{
		//				ProcessOrderMessage(order, order.Security, s, transactionId, isStatusRequest);
		//			}
		//		}
		//	}
		//}

		//private void UnknownOwnTrade()
		//{
		//	if (tuple == null)
		//	{
		//		List<ExecutionMessage> nonOrderedMyTrades;

		//		if (message.OrderId != null)
		//			nonOrderedMyTrades = _nonAssociatedByIdMyTrades.SafeAdd(message.OrderId.Value);
		//		else if (message.OriginalTransactionId != 0)
		//			nonOrderedMyTrades = _nonAssociatedByTransactionIdMyTrades.SafeAdd(message.OriginalTransactionId);
		//		else
		//			nonOrderedMyTrades = _nonAssociatedByStringIdMyTrades.SafeAdd(message.OrderStringId);

		//		this.AddInfoLog("My trade delayed: {0}", message);

		//		nonOrderedMyTrades.Add(message.TypedClone());

		//		return;
		//	}
		//}

		/// <summary>
		/// Create a copy of <see cref="TransactionOrderingMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new TransactionOrderingMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}