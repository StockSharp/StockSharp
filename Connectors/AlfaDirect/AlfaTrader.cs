#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.AlfaDirect.AlfaDirect
File: AlfaTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.AlfaDirect
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Messages;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/>, предоставляющая подключение к брокеру AlfaDirect.
	/// </summary>
	[Icon("AlfaDirect_logo.png")]
	public sealed class AlfaTrader : Connector, IExternalCandleSource
	{
		private Timer _candlesTimer;
		
		private readonly SynchronizedPairSet<long, CandleSeries> _series = new SynchronizedPairSet<long, CandleSeries>();
		private readonly CachedSynchronizedSet<CandleSeries> _realTimeSeries = new CachedSynchronizedSet<CandleSeries>();

		/// <summary>
		/// Создать <see cref="AlfaTrader"/>.
		/// </summary>
		public AlfaTrader()
		{
			Adapter.InnerAdapters.Add(new AlfaDirectMessageAdapter(TransactionIdGenerator));
		}

		private AlfaDirectMessageAdapter NativeAdapter => Adapter.InnerAdapters.OfType<AlfaDirectMessageAdapter>().First();

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через метод <see cref="IConnector.ReRegisterOrder(Order,Order)"/>
		/// в виде одной транзакции. По-умолчанию включено.
		/// </summary>
		public override bool IsSupportAtomicReRegister => false;

		/// <summary>
		/// Имя пользователя в терминале Альфа-Директ.
		/// </summary>
		public string Login
		{
			get { return NativeAdapter.Login; }
			set { NativeAdapter.Login = value; }
		}

		/// <summary>
		/// Пароль для входа в терминал.
		/// </summary>
		public string Password
		{
			get { return NativeAdapter.Password.To<string>(); }
			set { NativeAdapter.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// Временной отступ для нового запроса получение новой свечи. По-умолчанию равен 5 секундам.
		/// </summary>
		/// <remarks>Необходим для того, чтобы сервер успел сформировать данные в своем хранилище свечек.</remarks>
		public TimeSpan RealTimeCandleOffset { get; set; } = TimeSpan.FromSeconds(5);

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
					if (((ConnectMessage)message).Error == null)
					{
						_candlesTimer = this.StartRealTime(_realTimeSeries, RealTimeCandleOffset,
							(series, range) => RequestCandles(series.Security, (TimeSpan)series.Arg, range.Min, range.Max, _series.TryGetKey(series)), TimeSpan.FromSeconds(3));
					}
						
					break;

				case MessageTypes.Disconnect:
					if (((DisconnectMessage)message).Error == null && _candlesTimer != null)
					{
						_candlesTimer.Dispose();
						_candlesTimer = null;
					}

					break;

				default:
				{
					var candleMsg = message as CandleMessage;

					if (candleMsg == null)
						break;

					var series = _series.TryGetValue(candleMsg.OriginalTransactionId);

					if (series == null)
						return;

					var candle = candleMsg.ToCandle(series);

					NewCandles.SafeInvoke(series, new[] { candle });

					if (candleMsg.IsFinished)
						Stopped.SafeInvoke(series);

					return; // base class throws exception
				}
			}

			base.OnProcessMessage(message);
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источника для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (series.CandleType == typeof(TimeFrameCandle) &&
				series.Arg is TimeSpan && AlfaTimeFrames.CanConvert((TimeSpan)series.Arg))
			{
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
			}
		}

		/// <summary>
		/// Подписаться на получение свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		public void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var timeFrame = series.Arg.To<TimeSpan>();
			var transactionId = TransactionIdGenerator.GetNextId();

			_series[transactionId] = series;

			RequestCandles(series.Security, timeFrame, from, to, transactionId);

			if (to == DateTimeOffset.MaxValue)
				_realTimeSeries.Add(series);
		}

		private void RequestCandles(Security security, TimeSpan timeFrame, DateTimeOffset from, DateTimeOffset to, long transactionId)
		{
			SendInMessage(new MarketDataMessage
			{
				From = from,
				To = to,
				//SecurityId = GetSecurityId(security),
				DataType = MarketDataTypes.CandleTimeFrame,
				Arg = timeFrame,
				TransactionId = transactionId,
				IsSubscribe = true,
			}.FillSecurityInfo(this, security));
		}

		/// <summary>
		/// Остановить подписку получения свечек, ранее созданную через <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
			_series.RemoveByValue(series);
			_realTimeSeries.Remove(series);
		}

		/// <summary>
		/// Событие появления новых свечек, полученных после подписки через <see cref="SubscribeCandles"/>.
		/// </summary>
		public event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// Событие окончания обработки серии.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			RealTimeCandleOffset = storage.GetValue<TimeSpan>(nameof(RealTimeCandleOffset));
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(RealTimeCandleOffset), RealTimeCandleOffset);
		}
	}
}