namespace StockSharp.Messages;

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;

/// <summary>
/// Async extensions for <see cref="IMessageAdapter"/>.
/// </summary>
public static class IMessageAdapterAsyncExtensions
{
	/// <summary>
	/// Async connect for <see cref="IMessageAdapter"/> via <see cref="ConnectMessage"/>.
	/// Completes when an outgoing <see cref="ConnectMessage"/> without error is received.
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public static async ValueTask ConnectAsync(this IMessageAdapter adapter, CancellationToken cancellationToken)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var ctr = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

		ValueTask OnOut(Message msg, CancellationToken ct)
		{
			if (msg is ConnectMessage cm)
			{
				if (cm.Error != null)
					tcs.TrySetException(cm.Error);
				else
					tcs.TrySetResult(true);
			}

			return default;
		}

		adapter.NewOutMessageAsync += OnOut;
		try
		{
			await adapter.SendInMessageAsync(new ConnectMessage(), cancellationToken);
			await tcs.Task;
		}
		finally
		{
			adapter.NewOutMessageAsync -= OnOut;
		}
	}

	/// <summary>
	/// Async disconnect for <see cref="IMessageAdapter"/> via <see cref="DisconnectMessage"/>.
	/// Completes when an outgoing <see cref="DisconnectMessage"/> without error is received.
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public static async ValueTask DisconnectAsync(this IMessageAdapter adapter, CancellationToken cancellationToken)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var ctr = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

		ValueTask OnOut(Message msg, CancellationToken ct)
		{
			if (msg is DisconnectMessage dm)
			{
				if (dm.Error != null)
					tcs.TrySetException(dm.Error);
				else
					tcs.TrySetResult(true);
			}

			return default;
		}

		adapter.NewOutMessageAsync += OnOut;
		try
		{
			await adapter.SendInMessageAsync(new DisconnectMessage(), cancellationToken);
			await tcs.Task;
		}
		finally
		{
			adapter.NewOutMessageAsync -= OnOut;
		}
	}

	/// <summary>
	/// Subscribe and get an async stream of outgoing data messages of type <typeparamref name="T"/> associated with the given <paramref name="subscription"/>.
	/// Use <c>.WithCancellation(token)</c> to pass cancellation token.
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <param name="subscription"><see cref="ISubscriptionMessage"/></param>
	/// <returns>Async stream of messages.</returns>
	public static IAsyncEnumerable<T> SubscribeAsync<T>(
		this IMessageAdapter adapter,
		ISubscriptionMessage subscription)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		return SubscribeAsyncImpl<T>(adapter, subscription);
	}

	private static async IAsyncEnumerable<T> SubscribeAsyncImpl<T>(
		IMessageAdapter adapter,
		ISubscriptionMessage subscription,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			yield break;

		if (subscription.TransactionId == 0)
			subscription.TransactionId = adapter.TransactionIdGenerator.GetNextId();
		subscription.IsSubscribe = true;

		var subId = subscription.TransactionId;

		var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
			AllowSynchronousContinuations = true,
		});

		ValueTask OnOut(Message msg, CancellationToken ct)
		{
			if (msg is SubscriptionResponseMessage resp && resp.OriginalTransactionId == subId && resp.Error != null)
				channel.Writer.TryComplete(resp.Error);
			else if (msg is SubscriptionFinishedMessage fin && fin.OriginalTransactionId == subId)
			{
				// Write the finished message so callers can process Body (e.g., archive data)
				if (msg is T t)
					channel.Writer.TryWrite(t);
				channel.Writer.TryComplete();
			}
			else if (msg is ISubscriptionIdMessage sid)
			{
				var ids = sid.GetSubscriptionIds();

				// An adapter driven directly tags what it sends out with the original transaction id and
				// leaves the subscription identifiers alone, because those are filled in later by
				// BasketMessageAdapter. Nothing fills them here, so matching on them alone discards every
				// message and leaves the caller with a subscription that finishes empty.
				var matched = ids.Contains(subId) ||
					(ids.Length == 0 && msg is IOriginalTransactionIdMessage orig && orig.OriginalTransactionId == subId);

				if (matched && msg is T t)
				{
					channel.Writer.TryWrite(t);
				}
			}

			return default;
		}

		adapter.NewOutMessageAsync += OnOut;

		using var ctr = cancellationToken.Register(() => channel.Writer.TryComplete());

		try
		{
			var isCancelled = false;

			try
			{
				await adapter.SendInMessageAsync((Message)subscription, cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				isCancelled = true;
			}

			if (isCancelled)
				yield break;

			await using var enumerator = channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

			while (true)
			{
				bool hasNext;

				try
				{
					hasNext = await enumerator.MoveNextAsync();
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					break;
				}

				if (!hasNext)
					break;

				yield return enumerator.Current;
			}
		}
		finally
		{
			adapter.NewOutMessageAsync -= OnOut;

			// Send the unsubscribe on cancellation so it reaches the adapter. The cancellation callback can
			// miss it because the enumerator may unwind on the token before the registered callback runs, so
			// issue it from the finally that always runs.
			if (cancellationToken.IsCancellationRequested)
			{
				try
				{
					var unsub = subscription.TypedClone();
					unsub.IsSubscribe = false;
					unsub.OriginalTransactionId = subId;
					unsub.TransactionId = adapter.TransactionIdGenerator.GetNextId();
					_ = adapter.SendInMessageAsync((Message)unsub, CancellationToken.None);
				}
				catch { /* ignore */ }
			}
		}
	}

	/// <summary>
	/// Subscribe, wait for start/finish, and keep it active until <paramref name="cancellationToken"/> is canceled.
	/// For historical subscriptions completes when finished, for live completes after cancellation and unsubscribe processed.
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <param name="subscription"><see cref="ISubscriptionMessage"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public static async ValueTask SubscribeAsync(
		this IMessageAdapter adapter,
		ISubscriptionMessage subscription,
		CancellationToken cancellationToken)
	{
		if (adapter is null)			throw new ArgumentNullException(nameof(adapter));
		if (subscription is null)		throw new ArgumentNullException(nameof(subscription));

		cancellationToken.ThrowIfCancellationRequested();

		if (subscription.TransactionId == 0)
			subscription.TransactionId = adapter.TransactionIdGenerator.GetNextId();

		subscription.IsSubscribe = true;

		var subId = subscription.TransactionId;

		var startedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var finishedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var failedTcs = new TaskCompletionSource<ExceptionDispatchInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
		var unsubTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var cancelledTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		long unsubTransId = 0;

		ValueTask OnOut(Message msg, CancellationToken ct)
		{
			if (msg is SubscriptionResponseMessage resp)
			{
				if (resp.OriginalTransactionId == subId)
				{
					if (resp.Error != null)
						failedTcs.TrySetResult(ExceptionDispatchInfo.Capture(resp.Error));
					else
						startedTcs.TrySetResult(true);
				}
				else if (resp.OriginalTransactionId == Interlocked.Read(ref unsubTransId))
				{
					unsubTcs.TrySetResult(true);
				}
			}

			if (msg is SubscriptionOnlineMessage on && on.OriginalTransactionId == subId)
				startedTcs.TrySetResult(true);

			if (msg is SubscriptionFinishedMessage fin && fin.OriginalTransactionId == subId)
				finishedTcs.TrySetResult(true);

			return default;
		}

		adapter.NewOutMessageAsync += OnOut;

		using var ctr = cancellationToken.Register(() => cancelledTcs.TrySetResult(true));

		static void ObserveFault(Task task)
		{
			if (task.IsCompleted)
			{
				_ = task.Exception;
				return;
			}

			_ = task.ContinueWith(
				static completed => _ = completed.Exception,
				CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
		}

		ISubscriptionMessage CreateUnsubscribe()
		{
			var unsub = subscription.TypedClone();

			unsub.IsSubscribe = false;
			unsub.OriginalTransactionId = subId;
			unsub.TransactionId = adapter.TransactionIdGenerator.GetNextId();
			return unsub;
		}

		async ValueTask SendUnsubscribeAsync(ISubscriptionMessage unsub, bool waitForResponse)
		{
			try
			{
				Interlocked.Exchange(ref unsubTransId, unsub.TransactionId);

				using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
				// The caller token is already cancelled and the one-second limit only bounds
				// our wait. Do not cancel the actual cleanup send: a slow but valid adapter
				// must still be allowed to remove the subscription after we return.
				var sendTask = adapter.SendInMessageAsync((Message)unsub, CancellationToken.None).AsTask();

				try
				{
					await sendTask.WithCancellation(timeoutCts.Token);

					if (waitForResponse)
						await unsubTcs.Task.WithCancellation(timeoutCts.Token);
				}
				catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
				{
					ObserveFault(sendTask);

					// Cancellation cleanup is best effort and bounded so a broken adapter cannot
					// hold the caller (and a test host) indefinitely.
				}
				catch
				{
					// The subscription is already being cancelled. Do not replace the original
					// cancellation with an unsubscribe failure.
				}
			}
			catch
			{
				// Cancellation cleanup is best effort.
			}
		}

		async ValueTask UnsubscribeAsync()
		{
			try
			{
				await SendUnsubscribeAsync(CreateUnsubscribe(), true);
			}
			catch
			{
				// Cloning or assigning an unsubscribe id can fail for a custom message.
				// Cancellation still has to complete promptly.
			}
		}

		void UnsubscribeAfterSubscribe(Task subscribeTask)
		{
			ISubscriptionMessage unsub;

			try
			{
				// Capture the request before returning control to the caller, who may reuse
				// or mutate the original subscription after cancellation completes.
				unsub = CreateUnsubscribe();
			}
			catch
			{
				ObserveFault(subscribeTask);
				return;
			}

			async Task CleanupAsync()
			{
				try
				{
					await subscribeTask.NoWait();
				}
				catch
				{
					// A faulted or cancelled send may still have partially applied the
					// subscription, so preserve ordering and issue cleanup after it settles.
				}

				// The public operation has already completed and detached OnOut, therefore
				// only bound the transport send; do not wait for an acknowledgement here.
				await SendUnsubscribeAsync(unsub, false);
			}

			_ = CleanupAsync();
		}

		try
		{
			Task subscribeTask = null;

			try
			{
				subscribeTask = adapter.SendInMessageAsync((Message)subscription, cancellationToken).AsTask();
				await subscribeTask.WithCancellation(cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				if (subscribeTask is { IsCompleted: false })
					UnsubscribeAfterSubscribe(subscribeTask);
				else
				{
					if (subscribeTask != null)
						ObserveFault(subscribeTask);

					await UnsubscribeAsync();
				}

				if (subscription.To == null)
					return;

				throw;
			}

			var first = await Task.WhenAny(startedTcs.Task, failedTcs.Task, cancelledTcs.Task).NoWait();

			if (first == failedTcs.Task)
				(await failedTcs.Task.NoWait()).Throw();
			else if (first == cancelledTcs.Task)
			{
				await UnsubscribeAsync();

				if (subscription.To == null)
					return;

				cancellationToken.ThrowIfCancellationRequested();
			}

			if (subscription.To == null)
			{
				await cancelledTcs.Task.NoWait();
				await UnsubscribeAsync();
			}
			else
			{
				var completed = await Task.WhenAny(finishedTcs.Task, failedTcs.Task, cancelledTcs.Task).NoWait();

				if (completed == failedTcs.Task)
					(await failedTcs.Task.NoWait()).Throw();
				else if (completed == cancelledTcs.Task)
				{
					await UnsubscribeAsync();
					cancellationToken.ThrowIfCancellationRequested();
				}
				else
					await finishedTcs.Task.NoWait();
			}
		}
		finally
		{
			adapter.NewOutMessageAsync -= OnOut;
		}
	}

	/// <summary>
	/// Connect, subscribe and get an async stream of messages, then disconnect on completion.
	/// This is a convenience method that handles the full lifecycle: connect -> subscribe -> disconnect.
	/// </summary>
	/// <typeparam name="T">Message type to receive.</typeparam>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <param name="subscription"><see cref="ISubscriptionMessage"/></param>
	/// <returns>Async stream of messages.</returns>
	public static IAsyncEnumerable<T> ConnectAndDownloadAsync<T>(
		this IMessageAdapter adapter,
		ISubscriptionMessage subscription)
		where T : Message
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		return ConnectAndDownloadAsyncImpl<T>(adapter, subscription);
	}

	private static async IAsyncEnumerable<T> ConnectAndDownloadAsyncImpl<T>(
		IMessageAdapter adapter,
		ISubscriptionMessage subscription,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
		where T : Message
	{
		if (cancellationToken.IsCancellationRequested)
			yield break;

		await adapter.ConnectAsync(cancellationToken);

		try
		{
			await foreach (var msg in adapter.SubscribeAsync<T>(subscription).WithCancellation(cancellationToken))
				yield return msg;
		}
		finally
		{
			await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken.None);
		}
	}

	/// <summary>
	/// Register order and get an async stream of <see cref="ExecutionMessage"/> (order state changes and own trades).
	/// When cancellation token (via <c>.WithCancellation(token)</c>) is canceled, the order is automatically canceled.
	/// Completes when the order reaches a final state (<see cref="OrderStates.Done"/> or <see cref="OrderStates.Failed"/>).
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <param name="order"><see cref="OrderRegisterMessage"/> to register.</param>
	/// <returns>Async stream of <see cref="ExecutionMessage"/> with order info and trades.</returns>
	public static IAsyncEnumerable<ExecutionMessage> RegisterOrderAndWaitAsync(
		this IMessageAdapter adapter,
		OrderRegisterMessage order)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return RegisterOrderAndWaitAsyncImpl(adapter, order);
	}

	private static async IAsyncEnumerable<ExecutionMessage> RegisterOrderAndWaitAsyncImpl(
		IMessageAdapter adapter,
		OrderRegisterMessage order,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			yield break;

		if (order.TransactionId == 0)
			order.TransactionId = adapter.TransactionIdGenerator.GetNextId();

		var transId = order.TransactionId;
		long? orderId = null;
		string orderStringId = null;

		var channel = Channel.CreateUnbounded<ExecutionMessage>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
			AllowSynchronousContinuations = true,
		});

		ValueTask OnOut(Message msg, CancellationToken ct)
		{
			if (msg is not ExecutionMessage exec || exec.DataType != DataType.Transactions)
				return default;

			// Match by OriginalTransactionId or by OrderId/OrderStringId
			var isMatch =
				exec.OriginalTransactionId == transId ||
				(orderId != null && exec.OrderId == orderId) ||
				(!orderStringId.IsEmpty() && exec.OrderStringId == orderStringId);

			if (!isMatch)
				return default;

			// Track order ID for subsequent matching
			if (exec.OrderId != null)
				orderId = exec.OrderId;
			if (!exec.OrderStringId.IsEmpty())
				orderStringId = exec.OrderStringId;

			// Check for error
			if (exec.Error != null)
			{
				channel.Writer.TryWrite(exec);
				channel.Writer.TryComplete(exec.Error);
				return default;
			}

			channel.Writer.TryWrite(exec);

			// Complete on final state
			if (exec.OrderState is OrderStates.Done or OrderStates.Failed)
				channel.Writer.TryComplete();

			return default;
		}

		adapter.NewOutMessageAsync += OnOut;

		using var ctr = cancellationToken.Register(() =>
		{
			// Send cancel message
			try
			{
				var cancel = new OrderCancelMessage
				{
					TransactionId = adapter.TransactionIdGenerator.GetNextId(),
					OrderId = orderId,
					OrderStringId = orderStringId,
					SecurityId = order.SecurityId,
					PortfolioName = order.PortfolioName,
					Side = order.Side,
				};

				_ = adapter.SendInMessageAsync(cancel, CancellationToken.None);
			}
			catch
			{
				// ignore cancel errors
			}
		});

		try
		{
			var isCancelled = false;

			try
			{
				await adapter.SendInMessageAsync(order, cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				isCancelled = true;
			}

			if (isCancelled)
				yield break;

			await using var enumerator = channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

			while (true)
			{
				bool hasNext;

				try
				{
					hasNext = await enumerator.MoveNextAsync();
				}
				catch (ChannelClosedException)
				{
					break;
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					break;
				}

				if (!hasNext)
					break;

				yield return enumerator.Current;
			}
		}
		finally
		{
			adapter.NewOutMessageAsync -= OnOut;
		}
	}
}
