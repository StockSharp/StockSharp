namespace StockSharp.Algo.Candles.Compression
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Candle holder adapter.
	/// </summary>
	public class CandleHolderMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<long, CandleMessage> _infos = new SynchronizedDictionary<long, CandleMessage>();

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleHolderMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public CandleHolderMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
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

					switch (mdMsg.DataType)
					{
						case MarketDataTypes.CandleTimeFrame:
						case MarketDataTypes.CandleTick:
						case MarketDataTypes.CandleVolume:
						case MarketDataTypes.CandleRange:
						case MarketDataTypes.CandlePnF:
						case MarketDataTypes.CandleRenko:
						{
							var info = _infos.SafeAdd(mdMsg.TransactionId, k => mdMsg.DataType.ToCandleMessage().CreateInstance<CandleMessage>());
							info.SecurityId = mdMsg.SecurityId;
							info.Arg = mdMsg.Arg;
							break;
						}
					}

					break;
				}
			}

			base.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				base.OnInnerAdapterNewOutMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				{
					ProcessCandle((CandleMessage)message);
					break;
				}

				case MessageTypes.MarketDataFinished:
				{
					_infos.Remove(((MarketDataFinishedMessage)message).OriginalTransactionId);
					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void ProcessCandle(CandleMessage message)
		{
			var info = _infos.TryGetValue(message.OriginalTransactionId);

			if (info == null)
				return;

			if (info.SecurityId == default(SecurityId))
				info.SecurityId = message.SecurityId;
			else
				message.SecurityId = info.SecurityId;

			if (info.Arg == null)
				info.Arg = message.Arg;
			else
				message.Arg = info.Arg;

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

			if (currentValue == default(DateTimeOffset) && fromValue != default(DateTimeOffset))
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
			return new CandleHolderMessageAdapter(InnerAdapter);
		}
	}
}