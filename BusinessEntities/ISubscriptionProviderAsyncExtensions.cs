namespace StockSharp.BusinessEntities;

using System.Threading.Channels;

/// <summary>
/// Async extensions for <see cref="ISubscriptionProvider"/>.
/// </summary>
public static class ISubscriptionProviderAsyncExtensions
{
	/// <summary>
	/// Subscribe and get an async stream of incoming data of type <typeparamref name="T"/> for the specified <paramref name="subscription"/>.
	/// Enumeration can be canceled via <paramref name="cancellationToken"/>, which triggers <see cref="ISubscriptionProvider.UnSubscribe(Subscription)"/>.
	/// The stream completes when the subscription stops or fails.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="subscription">Target subscription.</param>
	/// <param name="cancellationToken">Cancellation token controlling the stream and lifetime of the subscription.</param>
	/// <returns>Async stream of incoming data objects for the given subscription filtered to <typeparamref name="T"/>.</returns>
	public static async IAsyncEnumerable<T> SubscribeAsync<T>(
		this ISubscriptionProvider provider,
		Subscription subscription,
		[EnumeratorCancellation]CancellationToken cancellationToken)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
			AllowSynchronousContinuations = true,
		});

		void OnReceived(Subscription s, object value)
		{
			if (ReferenceEquals(s, subscription) && value is T t)
				channel.Writer.TryWrite(t);
		}
		void OnFailed(Subscription s, Exception ex, bool _)
		{
			if (ReferenceEquals(s, subscription))
				channel.Writer.TryComplete(ex);
		}
		void OnStopped(Subscription s, Exception ex)
		{
			if (ReferenceEquals(s, subscription))
				channel.Writer.TryComplete(ex);
		}

		provider.SubscriptionReceived += OnReceived;
		provider.SubscriptionFailed += OnFailed;
		provider.SubscriptionStopped += OnStopped;

		using var ctr = cancellationToken.Register(() =>
		{
			try { provider.UnSubscribe(subscription); }
			catch { /* ignore */ }
			finally { channel.Writer.TryComplete(); }
		});

		try
		{
			provider.Subscribe(subscription);

			await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).WithEnforcedCancellation(cancellationToken))
				yield return item;
		}
		finally
		{
			provider.SubscriptionReceived -= OnReceived;
			provider.SubscriptionFailed -= OnFailed;
			provider.SubscriptionStopped -= OnStopped;
		}
	}

	/// <summary>
	/// Subscribe, wait for start, and keep it active until <paramref name="cancellationToken"/> is canceled.
	/// On cancellation the method will call <see cref="ISubscriptionProvider.UnSubscribe(Subscription)"/> and complete after the subscription is stopped.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="subscription">Subscription to manage.</param>
	/// <param name="cancellationToken">Cancellation token that triggers unsubscription.</param>
	/// <returns>A <see cref="ValueTask"/> that completes after the subscription is stopped (due to cancellation or failure).</returns>
	public static async ValueTask SubscribeAsync(
		this ISubscriptionProvider provider,
		Subscription subscription,
		CancellationToken cancellationToken)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		var startedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var stoppedTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
		var failedTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
		var cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		void OnStarted(Subscription s) { if (ReferenceEquals(s, subscription)) startedTcs.TrySetResult(true); }
		void OnStopped(Subscription s, Exception ex) { if (ReferenceEquals(s, subscription)) stoppedTcs.TrySetResult(ex); }
		void OnFailed(Subscription s, Exception ex, bool _) { if (ReferenceEquals(s, subscription)) failedTcs.TrySetException(ex); }

		provider.SubscriptionStarted += OnStarted;
		provider.SubscriptionStopped += OnStopped;
		provider.SubscriptionFailed += OnFailed;

		var ctr = cancellationToken.Register(() =>
		{
			try { provider.UnSubscribe(subscription); }
			finally { cancelTcs.TrySetResult(true); }
		});

		try
		{
			provider.Subscribe(subscription);

			var first = await Task.WhenAny(startedTcs.Task, failedTcs.Task, cancelTcs.Task).NoWait();
			
			if (first == failedTcs.Task)
				await failedTcs.Task.NoWait();

			if (first == cancelTcs.Task)
			{
				await stoppedTcs.Task.NoWait();
				return;
			}

			var next = await Task.WhenAny(failedTcs.Task, cancelTcs.Task).NoWait();

			if (next == failedTcs.Task)
				await failedTcs.Task.NoWait();

			await stoppedTcs.Task.NoWait();
		}
		catch
		{
			if (!cancellationToken.IsCancellationRequested)
				throw;
		}
		finally
		{
			ctr.Dispose();

			provider.SubscriptionStarted -= OnStarted;
			provider.SubscriptionStopped -= OnStopped;
			provider.SubscriptionFailed -= OnFailed;
		}
	}
}
