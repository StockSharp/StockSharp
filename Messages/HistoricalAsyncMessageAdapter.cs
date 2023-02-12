using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

namespace StockSharp.Messages;

/// <summary>
/// Historical <see cref="AsyncMessageAdapter"/>.
/// </summary>
public abstract class HistoricalAsyncMessageAdapter : AsyncMessageAdapter
{
	/// <summary>
	/// Initialize <see cref="HistoricalAsyncMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	protected HistoricalAsyncMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
	}

	/// <inheritdoc />
	protected override ValueTask OnConnectAsync(ConnectMessage msg, CancellationToken token)
	{
		SendOutMessage(new ConnectMessage());
		return default;
	}

	/// <inheritdoc />
	protected override ValueTask OnDisconnectAsync(DisconnectMessage msg, CancellationToken token)
	{
		SendOutMessage(new DisconnectMessage());
		return default;
	}

	/// <inheritdoc />
	protected override ValueTask OnResetAsync(ResetMessage msg, CancellationToken token)
	{
		SendOutMessage(new ResetMessage());
		return default;
	}

	/// <inheritdoc />
	protected override ValueTask OnRunSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		if (mdMsg.IsSubscribe)
		{
			var now = DateTimeOffset.UtcNow;

			var from = mdMsg.From;
			var to = mdMsg.To;

			if (from > now)
			{
				SendSubscriptionFinished(mdMsg.TransactionId);
				return default;
			}

			if (to is null || from is null)
			{
				mdMsg = (MarketDataMessage)mdMsg.Clone();

				mdMsg.To ??= now;

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
		}

		return base.OnRunSubscriptionAsync(mdMsg, cancellationToken);
	}
}