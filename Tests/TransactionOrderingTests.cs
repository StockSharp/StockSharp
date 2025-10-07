namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Messages;

[TestClass]
public class TransactionOrderingTests
{
	/// <summary>
	/// Mock adapter that behaves correctly - responds to messages properly.
	/// </summary>
	private class MockMessageAdapter : MessageAdapter
	{
		private readonly Queue<Message> _outMessages = [];
		private readonly bool _supportTransactionLog;

		public MockMessageAdapter(IdGenerator transactionIdGenerator, bool supportTransactionLog = false)
			: base(transactionIdGenerator)
		{
			_supportTransactionLog = supportTransactionLog;
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
		}

		public override bool IsSupportTransactionLog => _supportTransactionLog;

		public void EnqueueResponse(Message message)
		{
			_outMessages.Enqueue(message);
		}

		protected override bool OnSendInMessage(Message message)
		{
			// Simulate processing and send queued responses
			while (_outMessages.Count > 0)
			{
				var msg = _outMessages.Dequeue();
				SendOutMessage(msg);
			}

			// Auto-respond to subscriptions
			if (message is ISubscriptionMessage subMsg && subMsg.IsSubscribe)
			{
				SendOutMessage(new SubscriptionResponseMessage
				{
					OriginalTransactionId = subMsg.TransactionId,
				});
			}

			return true;
		}

		public override IMessageChannel Clone() => throw new NotSupportedException();
	}

	private static readonly IncrementalIdGenerator _idGenerator = new();

	[TestMethod]
	public void OrderRegister_RemoveTrailingZeros()
	{
		var mock = new MockMessageAdapter(_idGenerator);
		var adapter = new TransactionOrderingMessageAdapter(mock);

		var messages = new List<Message>();
		adapter.NewOutMessage += messages.Add;

		var secId = Helper.CreateSecurityId();

		// Send order with trailing zeros
		var regMsg = new OrderRegisterMessage
		{
			TransactionId = _idGenerator.GetNextId(),
			SecurityId = secId,
			Price = 100.0000m,
			Volume = 10.00m,
			VisibleVolume = 5.000m,
			Side = Sides.Buy,
		};

		adapter.SendInMessage(regMsg);

		// Verify trailing zeros were removed
		regMsg.Price.AssertEqual(100m);
		regMsg.Volume.AssertEqual(10m);
		regMsg.VisibleVolume.AssertEqual(5m);
	}

	[TestMethod]
	public void OrderReplace_InheritsSecurityId()
	{
		var mock = new MockMessageAdapter(_idGenerator);
		var adapter = new TransactionOrderingMessageAdapter(mock);

		var messages = new List<Message>();
		adapter.NewOutMessage += messages.Add;

		var secId = Helper.CreateSecurityId();

		var transId1 = _idGenerator.GetNextId();
		var transId2 = _idGenerator.GetNextId();

		// Register order
		adapter.SendInMessage(new OrderRegisterMessage
		{
			TransactionId = transId1,
			SecurityId = secId,
			Price = 100m,
			Volume = 10m,
			Side = Sides.Buy,
		});

		// Replace order
		adapter.SendInMessage(new OrderReplaceMessage
		{
			TransactionId = transId2,
			OriginalTransactionId = transId1,
			Price = 101m,
			Volume = 15m,
		});

		// Send execution for replace - SecurityId should be inherited
		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId2,
			DataTypeEx = DataType.Transactions,
			OrderState = OrderStates.Active,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		var exec = messages.OfType<ExecutionMessage>().FirstOrDefault();
		exec.AssertNotNull();
		exec.SecurityId.AssertEqual(secId);
	}

	[TestMethod]
	public void SuspendedTrades_ProcessedAfterOrder()
	{
		var mock = new MockMessageAdapter(_idGenerator);
		var adapter = new TransactionOrderingMessageAdapter(mock);

		var messages = new List<Message>();
		adapter.NewOutMessage += messages.Add;

		var secId = Helper.CreateSecurityId();
		var transId = _idGenerator.GetNextId();

		// Register order
		adapter.SendInMessage(new OrderRegisterMessage
		{
			TransactionId = transId,
			SecurityId = secId,
			Price = 100m,
			Volume = 10m,
			Side = Sides.Buy,
		});

		// Trade arrives BEFORE order confirmation (should be suspended)
		mock.EnqueueResponse(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = 123,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 5m,
			ServerTime = DateTimeOffset.Now,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Trade should be suspended (not in output yet)
		messages.OfType<ExecutionMessage>().Count(m => m.TradeId != null).AssertEqual(0);

		messages.Clear();

		// Now order confirmation arrives
		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = transId,
			DataTypeEx = DataType.Transactions,
			OrderId = 123,
			OrderState = OrderStates.Active,
			Balance = 5m,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Now suspended trade should be processed
		var trades = messages.OfType<ExecutionMessage>().Where(m => m.TradeId != null).ToArray();
		trades.Length.AssertEqual(1);
		trades[0].TradeId.AssertEqual(1);
		trades[0].OrderId.AssertEqual(123);
	}

	[TestMethod]
	public void TransactionLog_BuildsSnapshot()
	{
		var mock = new MockMessageAdapter(_idGenerator, true);
		var adapter = new TransactionOrderingMessageAdapter(mock);

		var messages = new List<Message>();
		adapter.NewOutMessage += messages.Add;

		var secId = Helper.CreateSecurityId();
		var subId = _idGenerator.GetNextId();
		var transId = _idGenerator.GetNextId();

		// Subscribe to transaction log
		adapter.SendInMessage(new OrderStatusMessage
		{
			TransactionId = subId,
			IsSubscribe = true,
		});

		messages.Clear();

		// Register order
		adapter.SendInMessage(new OrderRegisterMessage
		{
			TransactionId = transId,
			SecurityId = secId,
			Price = 100m,
			Volume = 10m,
			Side = Sides.Buy,
		});

		// Send multiple updates for same order
		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = subId,
			DataTypeEx = DataType.Transactions,
			OrderState = OrderStates.Pending,
		});

		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = subId,
			DataTypeEx = DataType.Transactions,
			OrderState = OrderStates.Active,
			OrderId = 123,
		});

		// Trade
		var tradeMsg = new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = subId,
			DataTypeEx = DataType.Transactions,
			OrderId = 123,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 5m,
		};
		mock.EnqueueResponse(tradeMsg);

		// Final state
		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = subId,
			DataTypeEx = DataType.Transactions,
			OrderState = OrderStates.Done,
			OrderId = 123,
			Balance = 0m,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Should not receive individual updates yet (buffering)
		messages.OfType<ExecutionMessage>().Count().AssertEqual(0);

		// Send subscription finished
		mock.EnqueueResponse(new SubscriptionFinishedMessage
		{
			OriginalTransactionId = subId,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Now should receive snapshot
		var executions = messages.OfType<ExecutionMessage>().ToArray();
		(executions.Length > 0).AssertTrue();

		// Should have order snapshot
		var orderSnapshot = executions.FirstOrDefault(e => e.OrderState != null);
		orderSnapshot.AssertNotNull();
		orderSnapshot.OrderState.AssertEqual(OrderStates.Done);
		orderSnapshot.OrderId.AssertEqual(123);

		// Should have trade
		var trade = executions.FirstOrDefault(e => e.TradeId != null);
		trade.AssertNotNull();
		trade.TradeId.AssertEqual(1);
	}

	[TestMethod]
	public void OrderIdMapping_TradeFindsTransaction()
	{
		var mock = new MockMessageAdapter(_idGenerator);
		var adapter = new TransactionOrderingMessageAdapter(mock);

		var messages = new List<Message>();
		adapter.NewOutMessage += messages.Add;

		var secId = Helper.CreateSecurityId();
		var transId = _idGenerator.GetNextId();

		// Register order
		adapter.SendInMessage(new OrderRegisterMessage
		{
			TransactionId = transId,
			SecurityId = secId,
			Price = 100m,
			Volume = 10m,
			Side = Sides.Buy,
		});

		// Order confirmation with OrderId
		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = transId,
			DataTypeEx = DataType.Transactions,
			OrderId = 123,
			OrderState = OrderStates.Active,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		messages.Clear();

		// Trade comes with only OrderId (no OriginalTransactionId)
		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = 123,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 5m,
		};
		mock.EnqueueResponse(tradeMsg);

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Trade should be processed with correct OriginalTransactionId
		var trade = messages.OfType<ExecutionMessage>().FirstOrDefault(m => m.TradeId != null);
		trade.AssertNotNull();
		trade.OriginalTransactionId.AssertEqual(transId);
	}

	[TestMethod]
	public void OrderStringIdMapping_TradeFindsTransaction()
	{
		var mock = new MockMessageAdapter(_idGenerator);
		var adapter = new TransactionOrderingMessageAdapter(mock);

		var messages = new List<Message>();
		adapter.NewOutMessage += messages.Add;

		var secId = Helper.CreateSecurityId();
		var transId = _idGenerator.GetNextId();

		// Register order
		adapter.SendInMessage(new OrderRegisterMessage
		{
			TransactionId = transId,
			SecurityId = secId,
			Price = 100m,
			Volume = 10m,
			Side = Sides.Buy,
		});

		// Order confirmation with OrderStringId
		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = transId,
			DataTypeEx = DataType.Transactions,
			OrderStringId = "ABC123",
			OrderState = OrderStates.Active,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		messages.Clear();

		// Trade comes with only OrderStringId (no OriginalTransactionId)
		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderStringId = "ABC123",
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 5m,
		};
		mock.EnqueueResponse(tradeMsg);

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Trade should be processed with correct OriginalTransactionId
		var trade = messages.OfType<ExecutionMessage>().FirstOrDefault(m => m.TradeId != null);
		trade.AssertNotNull();
		trade.OriginalTransactionId.AssertEqual(transId);
	}

	[TestMethod]
	public void Reset_ClearsAllState()
	{
		var mock = new MockMessageAdapter(_idGenerator);
		var adapter = new TransactionOrderingMessageAdapter(mock);

		var messages = new List<Message>();
		adapter.NewOutMessage += messages.Add;

		var secId = Helper.CreateSecurityId();
		var transId = _idGenerator.GetNextId();

		// Register order
		adapter.SendInMessage(new OrderRegisterMessage
		{
			TransactionId = transId,
			SecurityId = secId,
			Price = 100m,
			Volume = 10m,
			Side = Sides.Buy,
		});

		// Order confirmation
		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = transId,
			DataTypeEx = DataType.Transactions,
			OrderId = 123,
			OrderState = OrderStates.Active,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Reset
		adapter.SendInMessage(new ResetMessage());

		messages.Clear();

		// Trade with previous OrderId should NOT find transaction anymore
		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = 123,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 5m,
		};
		mock.EnqueueResponse(tradeMsg);

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Trade should be suspended (mapping cleared)
		messages.OfType<ExecutionMessage>().Count(m => m.TradeId != null).AssertEqual(0);
	}

	[TestMethod]
	public void Cancellation_PassesThrough()
	{
		var mock = new MockMessageAdapter(_idGenerator);
		var adapter = new TransactionOrderingMessageAdapter(mock);

		var messages = new List<Message>();
		adapter.NewOutMessage += messages.Add;

		var transId = _idGenerator.GetNextId();

		// Send cancellation execution (should pass through immediately)
		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = transId,
			DataTypeEx = DataType.Transactions,
			IsCancellation = true,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Cancellation should pass through
		var exec = messages.OfType<ExecutionMessage>().FirstOrDefault();
		exec.AssertNotNull();
		exec.IsCancellation.AssertTrue();
	}

	[TestMethod]
	public void OrderStatus_WithoutTransactionLog_NoBuffering()
	{
		var mock = new MockMessageAdapter(_idGenerator, false);
		var adapter = new TransactionOrderingMessageAdapter(mock);

		var messages = new List<Message>();
		adapter.NewOutMessage += messages.Add;

		var secId = Helper.CreateSecurityId();
		var subId = _idGenerator.GetNextId();
		var transId = _idGenerator.GetNextId();

		// Subscribe to order status (without transaction log support)
		adapter.SendInMessage(new OrderStatusMessage
		{
			TransactionId = subId,
			IsSubscribe = true,
		});

		messages.Clear();

		// Register order
		adapter.SendInMessage(new OrderRegisterMessage
		{
			TransactionId = transId,
			SecurityId = secId,
			Price = 100m,
			Volume = 10m,
			Side = Sides.Buy,
		});

		// Send execution
		mock.EnqueueResponse(new ExecutionMessage
		{
			TransactionId = transId,
			OriginalTransactionId = subId,
			DataTypeEx = DataType.Transactions,
			OrderState = OrderStates.Active,
			OrderId = 123,
		});

		adapter.SendInMessage(new TimeMessage { LocalTime = DateTimeOffset.Now });

		// Should receive execution immediately (no buffering without transaction log)
		var exec = messages.OfType<ExecutionMessage>().FirstOrDefault();
		exec.AssertNotNull();
		exec.OrderState.AssertEqual(OrderStates.Active);
	}
}
