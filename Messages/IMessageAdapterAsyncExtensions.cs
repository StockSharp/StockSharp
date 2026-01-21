namespace StockSharp.Messages;

using System.Runtime.CompilerServices;
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

		void OnOut(Message msg)
		{
			if (msg is ConnectMessage cm)
			{
				if (cm.Error != null)
					tcs.TrySetException(cm.Error);
				else
					tcs.TrySetResult(true);
			}
		}

		adapter.NewOutMessage += OnOut;
		try
		{
			await adapter.SendInMessageAsync(new ConnectMessage(), cancellationToken);
			await tcs.Task;
		}
		finally
		{
			adapter.NewOutMessage -= OnOut;
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

		void OnOut(Message msg)
		{
			if (msg is DisconnectMessage dm)
			{
				if (dm.Error != null)
					tcs.TrySetException(dm.Error);
				else
					tcs.TrySetResult(true);
			}
		}

		adapter.NewOutMessage += OnOut;
		try
		{
			await adapter.SendInMessageAsync(new DisconnectMessage(), cancellationToken);
			await tcs.Task;
		}
		finally
		{
			adapter.NewOutMessage -= OnOut;
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

		void OnOut(Message msg)
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
				if (ids.Contains(subId) && msg is T t)
				{
					channel.Writer.TryWrite(t);
				}
			}
		}

		adapter.NewOutMessage += OnOut;

		using var ctr = cancellationToken.Register(() =>
		{
			try
			{
				var unsub = (ISubscriptionMessage)((Message)subscription).Clone();
				unsub.IsSubscribe = false;
				unsub.OriginalTransactionId = subId;
				unsub.TransactionId = adapter.TransactionIdGenerator.GetNextId();
				_ = adapter.SendInMessageAsync((Message)unsub, CancellationToken.None);
			}
			catch { /* ignore */ }
			finally
			{
				channel.Writer.TryComplete();
			}
		});

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
			adapter.NewOutMessage -= OnOut;
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

		if (subscription.TransactionId == 0)
			subscription.TransactionId = adapter.TransactionIdGenerator.GetNextId();

		subscription.IsSubscribe = true;

		var subId = subscription.TransactionId;

		var startedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var finishedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var failedTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
		var unsubTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		long unsubTransId = 0;

		void OnOut(Message msg)
		{
			if (msg is SubscriptionResponseMessage resp)
			{
				if (resp.OriginalTransactionId == subId)
				{
					if (resp.Error != null)
						failedTcs.TrySetException(resp.Error);
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
		}

		adapter.NewOutMessage += OnOut;

		using var ctr = cancellationToken.Register(() =>
		{
			try
			{
				var unsub = subscription.TypedClone();

				unsub.IsSubscribe = false;
				unsub.OriginalTransactionId = subId;
				unsub.TransactionId = adapter.TransactionIdGenerator.GetNextId();

				Interlocked.Exchange(ref unsubTransId, unsub.TransactionId);

				_ = adapter.SendInMessageAsync((Message)unsub, CancellationToken.None);
			}
			catch
			{
				unsubTcs.TrySetResult(true);
			}
		});

		try
		{
			await adapter.SendInMessageAsync((Message)subscription, cancellationToken);

			var first = await Task.WhenAny(startedTcs.Task, failedTcs.Task).NoWait();

			if (first == failedTcs.Task)
				await failedTcs.Task.NoWait();

			if (subscription.To == null)
			{
				try
				{
					await Task.Delay(Timeout.Infinite, cancellationToken).NoWait();
				}
				catch (OperationCanceledException)
				{
					// Wait for unsubscribe response with short timeout
					// Don't wait too long - user cancelled, so exit promptly
					await Task.WhenAny(unsubTcs.Task, Task.Delay(TimeSpan.FromSeconds(1))).NoWait();
				}
			}
			else
			{
				await finishedTcs.Task.NoWait();
			}
		}
		finally
		{
			adapter.NewOutMessage -= OnOut;
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
	public static IAsyncEnumerable<ExecutionMessage> RegisterOrderAsync(
		this IMessageAdapter adapter,
		OrderRegisterMessage order)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return RegisterOrderAsyncImpl(adapter, order);
	}

	private static async IAsyncEnumerable<ExecutionMessage> RegisterOrderAsyncImpl(
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

		void OnOut(Message msg)
		{
			if (msg is not ExecutionMessage exec || exec.DataType != DataType.Transactions)
				return;

			// Match by OriginalTransactionId or by OrderId/OrderStringId
			var isMatch =
				exec.OriginalTransactionId == transId ||
				(orderId != null && exec.OrderId == orderId) ||
				(!orderStringId.IsEmpty() && exec.OrderStringId == orderStringId);

			if (!isMatch)
				return;

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
				return;
			}

			channel.Writer.TryWrite(exec);

			// Complete on final state
			if (exec.OrderState is OrderStates.Done or OrderStates.Failed)
				channel.Writer.TryComplete();
		}

		adapter.NewOutMessage += OnOut;

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

			await using var enumerator = channel.Reader.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();

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

				if (!hasNext)
					break;

				yield return enumerator.Current;
			}
		}
		finally
		{
			adapter.NewOutMessage -= OnOut;
		}
	}
}
