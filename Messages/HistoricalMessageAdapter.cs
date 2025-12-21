namespace StockSharp.Messages;

/// <summary>
/// Historical <see cref="MessageAdapter"/>.
/// </summary>
/// <remarks>
/// Initialize <see cref="HistoricalMessageAdapter"/>.
/// </remarks>
/// <param name="transactionIdGenerator">Transaction id generator.</param>
public abstract class HistoricalMessageAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
{
	/// <inheritdoc />
	protected override ValueTask MarketDataAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		if (!mdMsg.IsSubscribe)
		{
			return SendSubscriptionReplyAsync(mdMsg.TransactionId, cancellationToken);
		}

		var from = mdMsg.From;
		var to = mdMsg.To;

		if (to is null || from is null)
		{
			mdMsg = (MarketDataMessage)mdMsg.Clone();

			mdMsg.To ??= DateTime.UtcNow;

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
