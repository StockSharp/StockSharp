namespace StockSharp.BusinessEntities;

using System.Threading;
using System.Threading.Tasks;

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
}