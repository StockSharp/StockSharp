namespace StockSharp.Algo;

using StockSharp.Messages;

/// <summary>
/// The messages adapter makes auto connection.
/// </summary>
public class AutoConnectMessageAdapter : OfflineMessageAdapter
{
	private bool _isConnect;

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoConnectMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	public AutoConnectMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				_isConnect = false;
				break;
			case MessageTypes.Connect:
			case MessageTypes.Disconnect:
			case ExtendedMessageTypes.Reconnect:
			case MessageTypes.ProcessSuspended:
				break;

			default:
			{
				if (!_isConnect)
					base.OnSendInMessage(new ConnectMessage());

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

				_isConnect = connectMsg.Error is null;
				break;
			}
			case MessageTypes.Disconnect:
				_isConnect = false;
				break;
		}

		base.OnInnerAdapterNewOutMessage(message);
	}
}