namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// Candle holder adapter.
/// </summary>
public class CandleHolderMessageAdapter : MessageAdapterWrapper
{
	private readonly SynchronizedDictionary<long, CandleMessage> _infos = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="CandleHolderMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	public CandleHolderMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
	}

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

		TryUpdateValue(message, info, c => c.OpenPrice, (c, v) => c.OpenPrice = v);
		TryUpdateValue(message, info, c => c.HighPrice, (c, v) => c.HighPrice = v);
		TryUpdateValue(message, info, c => c.LowPrice, (c, v) => c.LowPrice = v);
		TryUpdateValue(message, info, c => c.ClosePrice, (c, v) => c.ClosePrice = v);
		TryUpdateValue(message, info, c => c.TotalPrice, (c, v) => c.TotalPrice = v);

		TryUpdateValue(message, info, c => c.OpenTime, (c, v) => c.OpenTime = v);
		TryUpdateValue(message, info, c => c.HighTime, (c, v) => c.HighTime = v);
		TryUpdateValue(message, info, c => c.LowTime, (c, v) => c.LowTime = v);
		TryUpdateValue(message, info, c => c.CloseTime, (c, v) => c.CloseTime = v);

		TryUpdateValue(message, info, c => c.OpenVolume, (c, v) => c.OpenVolume = v);
		TryUpdateValue(message, info, c => c.HighVolume, (c, v) => c.HighVolume = v);
		TryUpdateValue(message, info, c => c.LowVolume, (c, v) => c.LowVolume = v);
		TryUpdateValue(message, info, c => c.CloseVolume, (c, v) => c.CloseVolume = v);
		TryUpdateValue(message, info, c => c.RelativeVolume, (c, v) => c.RelativeVolume = v);
		TryUpdateValue(message, info, c => c.TotalVolume, (c, v) => c.TotalVolume = v);
		TryUpdateValue(message, info, c => c.BuyVolume, (c, v) => c.BuyVolume = v);
		TryUpdateValue(message, info, c => c.SellVolume, (c, v) => c.SellVolume = v);

		TryUpdateValue(message, info, c => c.UpTicks, (c, v) => c.UpTicks = v);
		TryUpdateValue(message, info, c => c.DownTicks, (c, v) => c.DownTicks = v);
		TryUpdateValue(message, info, c => c.TotalTicks, (c, v) => c.TotalTicks = v);
	}

	private static void TryUpdateValue<TCandle>(TCandle current, TCandle from, Func<TCandle, decimal> getValue, Action<TCandle, decimal> setValue)
		where TCandle : CandleMessage
	{
		var currentValue = getValue(current);
		var fromValue = getValue(from);

		if (currentValue == 0 && fromValue != 0)
			setValue(current, fromValue);
		else
			setValue(from, currentValue);
	}

	private static void TryUpdateValue<TCandle>(TCandle current, TCandle from, Func<TCandle, decimal?> getValue, Action<TCandle, decimal?> setValue)
		where TCandle : CandleMessage
	{
		var currentValue = getValue(current);
		var fromValue = getValue(from);

		if (currentValue == null && fromValue != null)
			setValue(current, fromValue);
		else
			setValue(from, currentValue);
	}

	private static void TryUpdateValue<TCandle>(TCandle current, TCandle from, Func<TCandle, int?> getValue, Action<TCandle, int?> setValue)
		where TCandle : CandleMessage
	{
		var currentValue = getValue(current);
		var fromValue = getValue(from);

		if (currentValue == null && fromValue != null)
			setValue(current, fromValue);
		else
			setValue(from, currentValue);
	}

	private static void TryUpdateValue<TCandle>(TCandle current, TCandle from, Func<TCandle, DateTimeOffset> getValue, Action<TCandle, DateTimeOffset> setValue)
		where TCandle : CandleMessage
	{
		var currentValue = getValue(current);
		var fromValue = getValue(from);

		if (currentValue == default && fromValue != default)
			setValue(current, fromValue);
		else
			setValue(from, currentValue);
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
