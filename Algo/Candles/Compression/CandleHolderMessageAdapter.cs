namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// Candle holder adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CandleHolderMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
public class CandleHolderMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private readonly SynchronizedDictionary<long, CandleMessage> _infos = [];

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				_infos.Clear();
				break;

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (!mdMsg.IsSubscribe)
				{
					// NOTE candles can be received during unsubscription process
					//_infos.Remove(mdMsg.OriginalTransactionId);
					break;
				}

				if (mdMsg.DataType2.IsCandles)
				{
					var info = _infos.SafeAdd(mdMsg.TransactionId, k => mdMsg.DataType2.MessageType.CreateInstance<CandleMessage>());
					info.SecurityId = mdMsg.SecurityId;
					info.DataType = mdMsg.DataType2;
				}

				break;
			}
		}

		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		switch (message)
		{
			case CandleMessage candleMsg:
				ProcessCandle(candleMsg);
				break;
			case SubscriptionFinishedMessage finishedMsg:
				_infos.Remove(finishedMsg.OriginalTransactionId);
				break;
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	private void ProcessCandle(CandleMessage message)
	{
		var info = _infos.TryGetValue(message.OriginalTransactionId);

		if (info == null)
			return;

		if (info.SecurityId == default)
			info.SecurityId = message.SecurityId;
		else
			message.SecurityId = info.SecurityId;

		if (info.DataType is null)
			info.DataType = message.DataType;
		else
			message.DataType = info.DataType;
	}

	/// <summary>
	/// Create a copy of <see cref="CandleHolderMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new CandleHolderMessageAdapter(InnerAdapter.TypedClone());
	}
}
