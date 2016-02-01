#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.OpenECry.OpenECry
File: OECTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

		/// <summary>
		/// Initializes a new instance of the <see cref="OECTrader"/>.
		/// </summary>
		public OECTrader()
		{
			Adapter.InnerAdapters.Add(new OpenECryMessageAdapter(TransactionIdGenerator));
		}

		private OpenECryMessageAdapter NativeAdapter => Adapter.InnerAdapters.OfType<OpenECryMessageAdapter>().First();

		/// <summary>
		/// Unique software ID.
		/// </summary>
		public string Uuid
		{
			get { return NativeAdapter.Uuid.To<string>(); }
			set { NativeAdapter.Uuid = value.To<SecureString>(); }
		}

		/// <summary>
		/// OpenECry login.
		/// </summary>
		public string Login
		{
			get { return NativeAdapter.Login; }
			set { NativeAdapter.Login = value; }
		}

		/// <summary>
		/// OpenECry password.
		/// </summary>
		public string Password
		{
			get { return NativeAdapter.Password.To<string>(); }
			set { NativeAdapter.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// The required mode of connection to the terminal. The default is <see cref="OpenECryRemoting.None"/>.
		/// </summary>
		public OpenECryRemoting RemotingRequired
		{
			get { return NativeAdapter.Remoting; }
			set { NativeAdapter.Remoting = value; }
		}

		/// <summary>
		/// To use the 'native' reconnection process. Enabled by default.
		/// </summary>
		public bool UseNativeReconnect
		{
			get { return NativeAdapter.UseNativeReconnect; }
			set { NativeAdapter.UseNativeReconnect = value; }
		}

		/// <summary>
		/// The OpenECry server API address. The default is <see cref="OpenECryAddresses.Api"/>.
		/// </summary>
		public EndPoint Address
		{
			get { return NativeAdapter.Address; }
			set { NativeAdapter.Address = value; }
		}

		/// <summary>
		/// To use the OEC library logging.
		/// </summary>
		public bool EnableOECLogging
		{
			get { return NativeAdapter.EnableOECLogging; }
			set { NativeAdapter.EnableOECLogging = value; }
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
				throw new ArgumentNullException(nameof(series));

			if (series.CandleType != typeof(TimeFrameCandle))
				throw new ArgumentException(LocalizedStrings.NotSupportCandle.Put("OpenECry", series.CandleType), nameof(series));

			if (!(series.Arg is TimeSpan))
				throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), nameof(series));

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