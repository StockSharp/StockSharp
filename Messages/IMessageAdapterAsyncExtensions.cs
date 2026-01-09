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
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <param name="subscription"><see cref="ISubscriptionMessage"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public static async IAsyncEnumerable<T> SubscribeAsync<T>(
		this IMessageAdapter adapter,
		ISubscriptionMessage subscription,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (adapter is null)			throw new ArgumentNullException(nameof(adapter));
		if (subscription is null)		throw new ArgumentNullException(nameof(subscription));

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
				var ids = sid.SubscriptionIds ?? (sid.SubscriptionId != 0 ? [sid.SubscriptionId] : Array.Empty<long>());
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

		void OnOut(Message msg)
		{
			if (msg is SubscriptionResponseMessage resp && resp.OriginalTransactionId == subId)
			{
				if (resp.Error != null)
					failedTcs.TrySetException(resp.Error);
				else
					startedTcs.TrySetResult(true);
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

				_ = adapter.SendInMessageAsync((Message)unsub, CancellationToken.None);
			}
			finally
			{
				// rely on finishedTcs after unsubscription processed by adapter
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
					await finishedTcs.Task.NoWait();
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
}
