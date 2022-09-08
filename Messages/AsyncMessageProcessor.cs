using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecng.Collections;
using Ecng.Common;
using Nito.AsyncEx;
using StockSharp.Localization;
using StockSharp.Logging;

namespace StockSharp.Messages;

/// <summary>
/// Async message processor helper.
/// </summary>
public class AsyncMessageProcessor : BaseLogReceiver
{
	private class MessageQueueItem
	{
		public MessageQueueItem(Message msg, CancellationTokenSource cts)
		{
			Message = msg;

			IsControl = Message.Type
				is MessageTypes.Reset
				or MessageTypes.Connect
				or MessageTypes.Disconnect;

			IsTransaction = Message.Type
				is MessageTypes.OrderRegister
				or MessageTypes.OrderReplace
				or MessageTypes.OrderPairReplace
				or MessageTypes.OrderCancel
				or MessageTypes.OrderGroupCancel;

			Cts = cts;
		}

		public Message Message { get; }
		public CancellationTokenSource Cts { get; }
		public Task Task { get; set; }

		public bool IsStartedProcessing => Task != null;
		public bool IsProcessing => Task?.IsCompleted == false;
		public bool IsDone => Task?.IsCompleted == true;

		public bool IsCanceled => Cts.Token.IsCancellationRequested;

		public bool IsControl { get; }
		public bool IsTransaction { get; }
	}

	private readonly SynchronizedList<MessageQueueItem> _messages = new();

	private readonly SynchronizedList<(string name, Task task)> _childTasks = new();
	private readonly SynchronizedDictionary<long, CancellationTokenSource> _childTokens = new();

	private readonly AsyncManualResetEvent _processMessageEvt = new(false);
	private CancellationTokenSource _globalCts = new();

	private bool _isConnectionStarted, _isDisconnecting;

	private IAsyncMessageAdapter _adapter => (IAsyncMessageAdapter)Parent;

	/// <summary>
	/// Initialize <see cref="AsyncMessageProcessor"/>.
	/// </summary>
	public AsyncMessageProcessor(IAsyncMessageAdapter adapter)
	{
		Parent = adapter;
		Task.Run(ProcessMessagesAsync);
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		base.DisposeManaged();
		_processMessageEvt.Set();
	}

	/// <summary>
	/// </summary>
	public bool EnqueueMessage(Message msg)
	{
		this.AddVerboseLog("enqueue: {0}", msg.Type);

		lock (_messages.SyncRoot)
		{
			if (msg is ResetMessage)
				CancelAndReplaceGlobalCts();

			_messages.Add(new MessageQueueItem(msg, _globalCts));
		}

		_processMessageEvt.Set();

		return true;
	}

	/// <summary>
	/// </summary>
	public void TryAddChildTask(string name, Task task)
	{
		if(!task.IsCompleted)
			_childTasks.Add((name, task));
	}

	/// <summary>
	/// </summary>
	public void TryAddChildTask(string name, ValueTask task)
	{
		if(!task.IsCompleted)
			_childTasks.Add((name, task.AsTask()));
	}

	private MessageQueueItem SelectNextMessage()
	{
		lock (_messages.SyncRoot)
		{
			var isControlProcessing = false;
			var isTransactionProcessing = false;

			foreach (var msg in _messages.Where(m => m.IsProcessing))
			{
				isControlProcessing |= msg.IsControl;
				isTransactionProcessing |= msg.IsTransaction;
			}

			// cant process anything in parallel while connect/disconnect/reset is processing
			if(isControlProcessing)
				return null;

			// if transaction is processing currently, we can process other non-exclusive messages in parallel (marketdata request for example)
			if(isTransactionProcessing)
				return _messages.FirstOrDefault(m => !m.IsStartedProcessing && !(m.IsControl || m.IsTransaction));

			return _messages.FirstOrDefault(m => !m.IsStartedProcessing);
		}
	}

	private void BeginProcessMessage(MessageQueueItem msg, Func<ValueTask> process)
	{
		if(msg.IsStartedProcessing)
			throw new ArgumentException($"processing is already started for {msg.Message}", nameof(msg));

		ValueTask wrapper()
		{
			try
			{
				if(msg.IsCanceled)
					throw new OperationCanceledException("canceled");

				var vt = process();

				if (vt.IsCompleted)
				{
					msg.Task = Task.CompletedTask;
					return vt;
				}

				msg.Task = vt.AsTask();
				msg.Task.ContinueWith(t =>
				{
					if(!t.IsCompletedSuccessfully)
						_adapter.HandleMessageException(msg.Message, t.IsFaulted ? t.Exception : new OperationCanceledException("canceled"));

					_processMessageEvt.Set(); // check next message
				});

				return vt;
			}
			catch (Exception e)
			{
				var tcs = AsyncHelper.CreateTaskCompletionSource(false);
				tcs.TrySetFrom(e);
				msg.Task = tcs.Task;

				_ = tcs.Task.Exception; // observe

				throw;
			}
		}

#pragma warning disable CA2012

		AsyncHelper.CatchHandle(
			wrapper,
			handleError:    e => _adapter.HandleMessageException(msg.Message, e),
			handleCancel:   e => _adapter.HandleMessageException(msg.Message, e),
			rethrowCancel:  false,
			rethrowErr:     false
		);

#pragma warning restore CA2012
	}

	private bool BeginProcessNextMessage()
	{
		var msg = SelectNextMessage();

		if(msg == null)
			return false;

		var token = _globalCts.Token;

		ValueTask processMarketDataMessage(MarketDataMessage md)
		{
			if (md.IsSubscribe)
			{
				var childToken = CreateChildTokenByTransId(md.TransactionId, token);
				TryAddChildTask($"sub{md.TransactionId}", _adapter.RunSubscriptionAsync(md, childToken));
			}
			else
			{
				TryCancelChildTokenByTransId(md.OriginalTransactionId);
			}

			return default;
		}

		BeginProcessMessage(msg, () =>
		{
			this.AddVerboseLog("beginprocess: {0}", msg.Message.Type);

			if(msg.IsControl)
				return msg.Message switch
				{
					ConnectMessage m    => ConnectAsync(m, token),
					DisconnectMessage m => DisconnectAsync(m),
					ResetMessage m      => ResetAsync(m),
					_                   => throw new ArgumentOutOfRangeException(nameof(msg), $"unexpected message {msg.Message.Type}")
				};

			if(!_isConnectionStarted || _isDisconnecting)
				throw new InvalidOperationException($"unable to process {msg.Message.Type} in this state. connStarted={_isConnectionStarted}, disconnecting={_isDisconnecting}");

			return msg.Message switch
			{
				SecurityLookupMessage m    => _adapter.SecurityLookupAsync(m, token),
				PortfolioLookupMessage m   => _adapter.PortfolioLookupAsync(m, token),
				BoardLookupMessage m       => _adapter.BoardLookupAsync(m, token),

				OrderStatusMessage m       => _adapter.OrderStatusAsync(m, token),

				OrderReplaceMessage m      => _adapter.ReplaceOrderAsync(m, token),
				OrderPairReplaceMessage m  => _adapter.ReplaceOrderPairAsync(m, token),
				OrderRegisterMessage m     => _adapter.RegisterOrderAsync(m, token),
				OrderCancelMessage m       => _adapter.CancelOrderAsync(m, token),
				OrderGroupCancelMessage m  => _adapter.CancelOrderGroupAsync(m, token),

				MarketDataMessage m        => processMarketDataMessage(m),

				_                          => _adapter.ProcessMessageAsync(msg.Message, token)
			};
		});

		return true;
	}

	private async Task ProcessMessagesAsync()
	{
		while (true)
		{
			await _processMessageEvt.WaitAsync();
			if(IsDisposeStarted)
				break;

			_processMessageEvt.Reset();

			lock(_messages.SyncRoot)
				_messages.RemoveWhere(m => m.IsDone);

			try
			{
				while(BeginProcessNextMessage()) {}
			}
			catch (Exception e)
			{
				this.AddErrorLog("error processing message: {0}", e);
			}
		}
	}

	private ValueTask ConnectAsync(ConnectMessage msg, CancellationToken token)
	{
		if(_isConnectionStarted)
			throw new InvalidOperationException(LocalizedStrings.Str1619);

		_isConnectionStarted = true;

		return _adapter.ConnectAsync(msg, token);
	}

	private async ValueTask DisconnectAsync(DisconnectMessage msg)
	{
		if(!_isConnectionStarted)
			throw new InvalidOperationException("not connected");

		if(_isDisconnecting)
			throw new InvalidOperationException("already disconnecting");

		_isDisconnecting = true;

		CancelAndReplaceGlobalCts();

		if(!await WhenChildrenComplete(_adapter.DisconnectTimeout.CreateTimeoutToken()))
			throw new InvalidOperationException("unable to complete disconnect. some tasks are still running.");

		await _adapter.DisconnectAsync(msg);

		_isDisconnecting = _isConnectionStarted = false;
	}

	private async ValueTask ResetAsync(ResetMessage msg)
	{
		_isDisconnecting = true;

		// token is already canceled in EnqueueMessage
		await AsyncHelper.CatchHandle(() => WhenChildrenComplete(_adapter.DisconnectTimeout.CreateTimeoutToken()));

		await _adapter.ResetAsync(msg); // reset must not throw.

		_isDisconnecting = _isConnectionStarted = false;
	}

	private void CancelAndReplaceGlobalCts()
	{
		_globalCts.Cancel();
		_globalCts = new();
	}

	private CancellationToken CreateChildTokenByTransId(long transactionId, CancellationToken parentToken)
	{
		var (cts, childToken) = parentToken.CreateChildToken();
		_childTokens.Add(transactionId, cts);
		return childToken;
	}

	private void TryCancelChildTokenByTransId(long transactionId) => _childTokens.TryGetAndRemove(transactionId)?.Cancel();

	private async Task<bool> WhenChildrenComplete(CancellationToken token)
	{
		var tasks = _childTasks.ToArray();
		var allComplete = true;

		await Task.WhenAll(tasks.Select(t => t.task.WithCancellation(token))).CatchHandle(finalizer: () =>
		{
			var incomplete = tasks.Where(t => !t.task.IsCompleted).Select(t => t.name).ToArray();
			if(incomplete.Any())
			{
				allComplete = false;
				this.AddErrorLog("following tasks were not completed: " + incomplete.Join(", "));
			}
		});

		return allComplete;
	}
}
