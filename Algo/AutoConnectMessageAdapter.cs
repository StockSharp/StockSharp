namespace StockSharp.Algo;

/// <summary>
/// The messages adapter makes auto connection.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AutoConnectMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Underlying adapter.</param>
public class AutoConnectMessageAdapter(IMessageAdapter innerAdapter) : OfflineMessageAdapter(innerAdapter)
{
	private bool _isConnect;
	private bool _wasConnected;

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				_isConnect = false;
				_wasConnected = default;
				break;
			case MessageTypes.Connect:
			case MessageTypes.Disconnect:
			case ExtendedMessageTypes.Reconnect:
			case MessageTypes.ProcessSuspended:
				break;

			default:
			{
				if (!_isConnect)
				{
					if (_wasConnected)
						await base.OnSendInMessageAsync(new ResetMessage(), cancellationToken);

					_wasConnected = true;
					await base.OnSendInMessageAsync(new ConnectMessage(), cancellationToken);
				}

				break;
			}
		}

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Connect:
			{
				var connectMsg = (ConnectMessage)message;

				_isConnect = connectMsg.IsOk();
				break;
			}
			case MessageTypes.Disconnect:
				_isConnect = false;
				break;
		}

		base.OnInnerAdapterNewOutMessage(message);
	}
}