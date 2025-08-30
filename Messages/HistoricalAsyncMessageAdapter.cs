namespace StockSharp.Messages;

/// <summary>
/// Historical <see cref="AsyncMessageAdapter"/>.
/// </summary>
/// <remarks>
/// Initialize <see cref="HistoricalAsyncMessageAdapter"/>.
/// </remarks>
/// <param name="transactionIdGenerator">Transaction id generator.</param>
public abstract class HistoricalAsyncMessageAdapter(IdGenerator transactionIdGenerator) : AsyncMessageAdapter(transactionIdGenerator)
{
	/// <inheritdoc />
	public override ValueTask MarketDataAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		if (!mdMsg.IsSubscribe)
		{
			SendSubscriptionReply(mdMsg.TransactionId);
			return default;
		}

		var from = mdMsg.From;
		var to = mdMsg.To;

		if (to is null || from is null)
		{
			mdMsg = (MarketDataMessage)mdMsg.Clone();

			mdMsg.To ??= DateTimeOffset.UtcNow;

			if (from is null)
			{
				var isTick =
					mdMsg.DataType2 == DataType.Level1 ||
					mdMsg.DataType2 == DataType.Ticks ||
					mdMsg.DataType2 == DataType.OrderLog ||
					mdMsg.DataType2 == DataType.MarketDepth;

				var isSmallTf = !isTick && mdMsg.DataType2.IsTFCandles && mdMsg.DataType2.GetTimeFrame().TotalMinutes < 1;
				var isBigTf = !isTick && mdMsg.DataType2.IsTFCandles && mdMsg.DataType2.GetTimeFrame().TotalDays > 1;

				mdMsg.From = mdMsg.To.Value.AddDays(isTick || isSmallTf ? -1 : (isBigTf ? -30 : -7));
			}
		}

		return base.MarketDataAsync(mdMsg, cancellationToken);
	}
}
