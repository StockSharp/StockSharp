namespace StockSharp.OpenECry
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to the OEC broker.
	/// </summary>
	[Icon("OpenECry_logo.png")]
	public sealed class OECTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedPairSet<long, CandleSeries> _series = new SynchronizedPairSet<long, CandleSeries>();
		private readonly OpenECryMessageAdapter _adapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="OECTrader"/>.
		/// </summary>
		public OECTrader()
		{
			_adapter = new OpenECryMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter);
		}

		/// <summary>
		/// Unique software ID.
		/// </summary>
		public string Uuid
		{
			get { return _adapter.Uuid; }
			set { _adapter.Uuid = value; }
		}

		/// <summary>
		/// OpenECry login.
		/// </summary>
		public string Login
		{
			get { return _adapter.Login; }
			set { _adapter.Login = value; }
		}

		/// <summary>
		/// OpenECry password.
		/// </summary>
		public string Password
		{
			get { return _adapter.Password.To<string>(); }
			set { _adapter.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// The required mode of connection to the terminal. The default is <see cref="OpenECryRemoting.None"/>.
		/// </summary>
		public OpenECryRemoting RemotingRequired
		{
			get { return _adapter.Remoting; }
			set { _adapter.Remoting = value; }
		}

		/// <summary>
		/// To use the 'native' reconnection process. Enabled by default.
		/// </summary>
		public bool UseNativeReconnect
		{
			get { return _adapter.UseNativeReconnect; }
			set { _adapter.UseNativeReconnect = value; }
		}

		/// <summary>
		/// The OpenECry server API address. The default is <see cref="OpenECryAddresses.Api"/>.
		/// </summary>
		public EndPoint Address
		{
			get { return _adapter.Address; }
			set { _adapter.Address = value; }
		}

		/// <summary>
		/// To use the OEC library logging.
		/// </summary>
		public bool EnableOECLogging
		{
			get { return _adapter.EnableOECLogging; }
			set { _adapter.EnableOECLogging = value; }
		}

		///// <summary>
		///// Проверить, установлено ли еще соединение. Проверяется только в том случае, если был вызван метод <see cref="IConnector.Connect"/>.
		///// </summary>
		///// <returns><see langword="true"/>, если соединение еще установлено, false, если торговая система разорвала подключение.</returns>
		//protected override bool IsConnectionAlive()
		//{
		//	return SessionHolder.Session != null && !SessionHolder.Session.CompleteConnected;
		//}

		/// <summary>
		/// To send a message to another user.
		/// </summary>
		/// <param name="userName">The recipient name.</param>
		/// <param name="text">Message text.</param>
		public void SendMessage(string userName, string text)
		{
			SendInMessage(new NewsMessage
			{
				Source = userName,
				Headline = text,
				ServerTime = CurrentTime
			});
		}

		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType != typeof(TimeFrameCandle) || !(series.Arg is TimeSpan))
				yield break;

			var tf = (TimeSpan)series.Arg;

			if (OpenECryMessageAdapter.TimeFrames.Contains(tf))
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
			if (series == null)
				throw new ArgumentNullException("series");

			if (series.CandleType != typeof(TimeFrameCandle))
				throw new ArgumentException(LocalizedStrings.NotSupportCandle.Put("OpenECry", series.CandleType), "series");

			if (!(series.Arg is TimeSpan))
				throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), "series");

			var transactionId = TransactionIdGenerator.GetNextId();

			_series.Add(transactionId, series);

			SendInMessage(new MarketDataMessage
			{
				TransactionId = transactionId,
				DataType = MarketDataTypes.CandleTimeFrame,
				//SecurityId = GetSecurityId(series.Security),
				Arg = series.Arg,
				IsSubscribe = true,
				From = from,
				To = to,
			}.FillSecurityInfo(this, series.Security));
		}

		/// <summary>
		/// To stop the candles receiving subscription, previously created by <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
			var originalTransactionId = _series.TryGetKey(series);

			if (originalTransactionId == 0)
				return;

			SendInMessage(new MarketDataMessage
			{
				OriginalTransactionId = originalTransactionId,
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.CandleTimeFrame,
				//SecurityId = GetSecurityId(series.Security),
				Arg = series.Arg,
				IsSubscribe = false,
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

			var series = _series.TryGetValue(candleMsg.OriginalTransactionId);

			if (series == null)
				return;

			var candle = candleMsg.ToCandle(series);
			NewCandles.SafeInvoke(series, new[] { candle });

			if (candleMsg.IsFinished)
				Stopped.SafeInvoke(series);
		}
	}
}