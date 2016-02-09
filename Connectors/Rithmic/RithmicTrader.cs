#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rithmic.Rithmic
File: RithmicTrader.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rithmic
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to the Rithmic.
	/// </summary>
	[Icon("Rithmic_logo.png")]
	public class RithmicTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, CandleSeries> _candleSeries = new SynchronizedDictionary<long, CandleSeries>();

		/// <summary>
		/// Initializes a new instance of the <see cref="RithmicTrader"/>.
		/// </summary>
		public RithmicTrader()
		{
			CreateAssociatedSecurity = true;
			Adapter.InnerAdapters.Add(new RithmicMessageAdapter(TransactionIdGenerator));
		}

		private RithmicMessageAdapter NativeAdapter => Adapter.InnerAdapters.OfType<RithmicMessageAdapter>().First();

		/// <summary>
		/// User name.
		/// </summary>
		public string UserName
		{
			get { return NativeAdapter.UserName; }
			set { NativeAdapter.UserName = value; }
		}

		/// <summary>
		/// Password.
		/// </summary>
		public string Password
		{
			get { return NativeAdapter.Password.To<string>(); }
			set { NativeAdapter.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// Path to certificate file, necessary yo connect to Rithmic system.
		/// </summary>
		public string CertFile
		{
			get { return NativeAdapter.CertFile; }
			set { NativeAdapter.CertFile = value; }
		}

		/// <summary>
		/// Server type.
		/// </summary>
		public RithmicServers Server
		{
			get { return NativeAdapter.Server; }
			set { NativeAdapter.Server = value; }
		}

		/// <summary>
		/// Path to lg file.
		/// </summary>
		public string LogFileName
		{
			get { return NativeAdapter.LogFileName; }
			set { NativeAdapter.LogFileName = value; }
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType != typeof(TimeFrameCandle) || !(series.Arg is TimeSpan))
				yield break;

			//var tf = (TimeSpan)series.Arg;

			//if (tf < TimeSpan.FromDays(1) || tf == TimeSpan.FromDays(1) || tf == TimeSpan.FromDays(7) || tf == TimeHelper.TicksPerMonth.To<TimeSpan>())
			yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
		}

		/// <summary>
		/// Event of new candles occurring, that are received after the subscription by <see cref="SubscribeCandles"/>.
		/// </summary>
		public event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// Subscribe to receive new candles.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		public void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			var mdMsg = new MarketDataMessage
			{
				//SecurityId = GetSecurityId(series.Security),
				DataType = MarketDataTypes.CandleTimeFrame,
				TransactionId = transactionId,
				From = from,
				To = to,
				Arg = series.Arg,
				IsSubscribe = true,
			}.FillSecurityInfo(this, series.Security);

			_candleSeries.Add(transactionId, series);

			SendInMessage(mdMsg);
		}

		/// <summary>
		/// To stop the candles receiving subscription, previously created by <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
			var transactionId = _candleSeries.GetKeys(series).FirstOrDefault();

			if (transactionId == 0)
				return;

			SendInMessage(new MarketDataMessage
			{
				OriginalTransactionId = transactionId
			}.FillSecurityInfo(this, series.Security));
		}

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnProcessMessage(Message message)
		{
			var candleMsg = message as CandleMessage;

			if (candleMsg == null)
			{
				base.OnProcessMessage(message);
				return;
			}

			var series = _candleSeries.TryGetValue(candleMsg.OriginalTransactionId);

			if (series == null)
				return;

			var candle = candleMsg.ToCandle(series);

			// сообщение с IsFinished = true не содержит данные по свече,
			// только флаг, что получение исторических данных завершено
			if (!candleMsg.IsFinished)
				NewCandles.SafeInvoke(series, new[] { candle });
			else
			{
				_candleSeries.Remove(candleMsg.OriginalTransactionId);

				if (candleMsg.IsFinished)
					Stopped.SafeInvoke(series);
			}
		}
	}
}