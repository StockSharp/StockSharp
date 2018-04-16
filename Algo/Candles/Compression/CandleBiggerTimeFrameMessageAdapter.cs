namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Candle bigger time-frame builder adapter.
	/// </summary>
	public class CandleBiggerTimeFrameMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<long, BiggerTimeFrameCandleCompressor> _biggerTimeFrameCandleCompressors = new SynchronizedDictionary<long, BiggerTimeFrameCandleCompressor>();

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleBiggerTimeFrameMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public CandleBiggerTimeFrameMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					_biggerTimeFrameCandleCompressors.Clear();
					break;

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.DataType != MarketDataTypes.CandleTimeFrame)
						break;

					var originalTf = (TimeSpan)mdMsg.Arg;
					var timeFrames = InnerAdapter.GetTimeFrames(mdMsg.SecurityId).ToArray();

					if (timeFrames.Contains(originalTf))
						break;

					var smaller = timeFrames
						.FilterSmallerTimeFrames(originalTf)
						.OrderByDescending()
						.First();

					_biggerTimeFrameCandleCompressors.Add(mdMsg.TransactionId, new BiggerTimeFrameCandleCompressor((MarketDataMessage)mdMsg.Clone()));

					var clone = (MarketDataMessage)mdMsg.Clone();
					clone.Arg = smaller;
					base.SendInMessage(clone);
					return;
				}
			}

			base.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.CandleTimeFrame:
				{
					var smallCandle = (CandleMessage)message;
					var compressor = _biggerTimeFrameCandleCompressors.TryGetValue(smallCandle.OriginalTransactionId);
					
					if (compressor == null)
						break;

					var candles = compressor.Process(smallCandle).Where(c => c.State == CandleStates.Finished);

					foreach (var bigCandle in candles)
					{
						bigCandle.Adapter = smallCandle.Adapter;
						base.OnInnerAdapterNewOutMessage(bigCandle);
					}

					return;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="CandleHolderMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new CandleBiggerTimeFrameMessageAdapter(InnerAdapter);
		}
	}
}