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
	protected override bool OnSendInMessage(Message message)
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
						base.OnSendInMessage(new ResetMessage());

					_wasConnected = true;
					base.OnSendInMessage(new ConnectMessage());
				}

				break;
			}
		}

		return base.OnSendInMessage(message);
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