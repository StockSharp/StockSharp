namespace StockSharp.BusinessEntities;

using System.Runtime.CompilerServices;
using System.Threading.Channels;

/// <summary>
/// Async extensions for <see cref="IConnector"/>.
/// </summary>
public static class IConnectorAsyncExtensions
{
	/// <summary>
	/// Async version <see cref="IConnector.Connect"/>.
	/// </summary>
	/// <param name="connector"><see cref="IConnector"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public static async ValueTask ConnectAsync(this IConnector connector, CancellationToken cancellationToken)
	{
		if (connector is null)
			throw new ArgumentNullException(nameof(connector));

		if (connector.ConnectionState != ConnectionStates.Disconnected)
			throw new ArgumentException($"State is {connector.ConnectionState}.", nameof(connector));

		var tcs = AsyncHelper.CreateTaskCompletionSource<ConnectionStates>();

		using var _ = cancellationToken.Register(() => tcs.TrySetCanceled());

		void OnConnected() => tcs.TrySetResult(ConnectionStates.Connected);
		void OnConnectionError(Exception ex) => tcs.TrySetException(ex);

		connector.Connected += OnConnected;
		connector.ConnectionError += OnConnectionError;

		try
		{
			connector.Connect();

			await tcs.Task;
		}
		finally
		{
			connector.Connected -= OnConnected;
			connector.ConnectionError -= OnConnectionError;
		}
	}

	/// <summary>
	/// Async version <see cref="IConnector.Disconnect"/>.
	/// </summary>
	/// <param name="connector"><see cref="IConnector"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public static async ValueTask DisconnectAsync(this IConnector connector, CancellationToken cancellationToken)
	{
		if (connector is null)
			throw new ArgumentNullException(nameof(connector));

		if (connector.ConnectionState != ConnectionStates.Connected)
			throw new ArgumentException($"State is {connector.ConnectionState}.", nameof(connector));

		var tcs = AsyncHelper.CreateTaskCompletionSource<ConnectionStates>();

		using var _ = cancellationToken.Register(() => tcs.TrySetCanceled());

		void OnDisconnected() => tcs.TrySetResult(ConnectionStates.Disconnected);
		void OnConnectionError(Exception ex) => tcs.TrySetException(ex);

		connector.Disconnected += OnDisconnected;
		connector.ConnectionError += OnConnectionError;

		try
		{
			connector.Disconnect();

			await tcs.Task;
		}
		finally
		{
			connector.Disconnected -= OnDisconnected;
			connector.ConnectionError -= OnConnectionError;
		}
	}

	/// <summary>
	/// Register order and get an async stream of order state changes and own trades.
	/// When cancellation token (via <c>.WithCancellation(token)</c>) is canceled, the order is automatically canceled.
	/// Completes when the order reaches a final state (<see cref="OrderStates.Done"/> or <see cref="OrderStates.Failed"/>).
	/// </summary>
	/// <param name="connector"><see cref="IConnector"/></param>
	/// <param name="order"><see cref="Order"/> to register.</param>
	/// <returns>Async stream of tuples (Order, Trade) where Trade is null for state-only updates.</returns>
	public static IAsyncEnumerable<(Order order, MyTrade trade)> RegisterOrderAndWaitAsync(
		this IConnector connector,
		Order order)
	{
		if (connector is null)
			throw new ArgumentNullException(nameof(connector));
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return RegisterOrderAndWaitAsyncImpl(connector, order);
	}

	private static async IAsyncEnumerable<(Order order, MyTrade trade)> RegisterOrderAndWaitAsyncImpl(
		IConnector connector,
		Order order,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			yield break;

		var channel = Channel.CreateUnbounded<(Order, MyTrade)>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
			AllowSynchronousContinuations = true,
		});

		void OnOrderReceived(Subscription _, Order o)
		{
			if (o != order)
				return;

			channel.Writer.TryWrite((o, null));

			if (o.State is OrderStates.Done or OrderStates.Failed)
				channel.Writer.TryComplete();
		}

		void OnOwnTradeReceived(Subscription _, MyTrade trade)
		{
			if (trade.Order != order)
				return;

			channel.Writer.TryWrite((order, trade));
		}

		void OnOrderRegisterFailed(Subscription _, OrderFail fail)
		{
			if (fail.Order != order)
				return;

			channel.Writer.TryComplete(fail.Error ?? new InvalidOperationException(LocalizedStrings.ErrorRegOrder));
		}

		void OnOrderCancelFailed(Subscription _, OrderFail fail)
		{
			if (fail.Order != order)
				return;

			// Cancel failed - just log, don't complete the stream
			// The order might still be active
		}

		connector.OrderReceived += OnOrderReceived;
		connector.OwnTradeReceived += OnOwnTradeReceived;
		connector.OrderRegisterFailReceived += OnOrderRegisterFailed;
		connector.OrderCancelFailReceived += OnOrderCancelFailed;

		using var ctr = cancellationToken.Register(() =>
		{
			try
			{
				if (order.State is OrderStates.Active or OrderStates.Pending)
					connector.CancelOrder(order);
			}
			catch
			{
				// ignore cancel errors
			}
		});

		try
		{
			connector.RegisterOrder(order);

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
			connector.OrderReceived -= OnOrderReceived;
			connector.OwnTradeReceived -= OnOwnTradeReceived;
			connector.OrderRegisterFailReceived -= OnOrderRegisterFailed;
			connector.OrderCancelFailReceived -= OnOrderCancelFailed;
		}
	}
}