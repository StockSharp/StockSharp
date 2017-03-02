#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: ContinuousSecurityMarketDataStorage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	///// <summary>
	///// The interface, describing the storage of market-date of continuous instrument.
	///// </summary>
	//public interface IContinuousSecurityMarketDataStorage
	//{
	//	/// <summary>
	//	/// To get storage for the composite instrument.
	//	/// </summary>
	//	/// <param name="date">Date.</param>
	//	/// <returns>The storage of the composite instrument.</returns>
	//	IMarketDataStorage GetStorage(DateTime date);
	//}

	/// <summary>
	/// The aggregator-storage, allowing to load data simultaneously market data for <see cref="ContinuousSecurity"/>.
	/// </summary>
	/// <typeparam name="T">Message type.</typeparam>
	public class ContinuousSecurityMarketDataStorage<T> : BasketMarketDataStorage<T>
		where T : Message
	{
		//private readonly Func<Security, IMarketDataDrive, IMarketDataStorage<T>> _getStorage;
		//private readonly Func<T, Security> _getSecurity;
		//private readonly Func<T, DateTimeOffset> _getTime;

		private readonly ContinuousSecurity _security;

		/// <summary>
		/// Initializes a new instance of the <see cref="ContinuousSecurityMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="security">Continuous security (generally, a futures contract), containing expirable securities.</param>
		/// <param name="arg">The additional argument, associated with data. For example, <see cref="CandleMessage.Arg"/>.</param>
		public ContinuousSecurityMarketDataStorage(ContinuousSecurity security, object arg)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			//if (getTime == null)
			//	throw new ArgumentNullException(nameof(getTime));

			//if (getSecurity == null)
			//	throw new ArgumentNullException(nameof(getSecurity));

			//if (getStorage == null)
			//	throw new ArgumentNullException(nameof(getStorage));

			//if (drive == null)
			//	throw new ArgumentNullException(nameof(drive));

			//_getStorage = getStorage;
			Security = _security = security;
			//_getTime = getTime;
			//_getSecurity = getSecurity;
			
			Arg = arg;
			//Drive = drive;
		}

		/// <summary>
		/// The type of market-data, operated by given storage.
		/// </summary>
		public override Type DataType => typeof(T);

		/// <summary>
		/// The instrument, operated by the external storage.
		/// </summary>
		public override Security Security { get; }

		/// <summary>
		/// The additional argument, associated with data. For example, <see cref="CandleMessage.Arg"/>.
		/// </summary>
		public override object Arg { get; }

		/// <summary>
		/// To load messages from embedded storages for specified date.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>The messages.</returns>
		protected override IEnumerable<T> OnLoad(DateTime date)
		{
			var securityId = _security.ToSecurityId();
			var currentSecurityId = _security.GetSecurity(date);

			foreach (Message msg in base.OnLoad(date))
			{
				switch (msg.Type)
				{
					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandleVolume:
					{
						var candleMsg = (CandleMessage)msg;

						if (candleMsg.SecurityId == currentSecurityId)
							yield return ReplaceSecurityId(msg, securityId);

						break;
					}

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)msg;

						if (execMsg.SecurityId == currentSecurityId)
							yield return ReplaceSecurityId(msg, securityId);

						break;
					}

					case MessageTypes.QuoteChange:
					{
						var quoteMsg = (QuoteChangeMessage)msg;

						if (quoteMsg.SecurityId == currentSecurityId)
							yield return ReplaceSecurityId(msg, securityId);

						break;
					}

					case MessageTypes.Level1Change:
					{
						var l1Msg = (Level1ChangeMessage)msg;

						if (l1Msg.SecurityId == currentSecurityId)
							yield return ReplaceSecurityId(msg, securityId);

						break;
					}

					default:
						yield return (T)msg;
						break;
				}
			}
		}

		private static T ReplaceSecurityId(Message msg, SecurityId securityId)
		{
			var clone = msg.Clone();
			clone.ReplaceSecurityId(securityId);
			return (T)clone;
		}
	}

	//class ConvertableContinuousSecurityMarketDataStorage<TMessage, TEntity> : ContinuousSecurityMarketDataStorage<TMessage>, IMarketDataStorage<TEntity>, IMarketDataStorageInfo<TEntity>
	//	where TMessage : Message
	//{
	//	private readonly ContinuousSecurity _security;
	//	private readonly Func<TEntity, TMessage> _toMessage;
	//	private readonly Func<TEntity, DateTimeOffset> _getEntityTime;

	//	public ConvertableContinuousSecurityMarketDataStorage(ContinuousSecurity security, 
	//		object arg,
	//		Func<TMessage, DateTimeOffset> getTime, 
	//		Func<TMessage, Security> getSecurity,
	//		Func<TEntity, TMessage> toMessage,
	//		Func<TEntity, DateTimeOffset> getEntityTime,
	//		Func<Security, IMarketDataDrive, IMarketDataStorage<TMessage>> getStorage, 
	//		IMarketDataStorageDrive drive)
	//		: base(security, arg, getTime, getSecurity, getStorage, drive)
	//	{
	//		if (toMessage == null)
	//			throw new ArgumentNullException(nameof(toMessage));

	//		if (getEntityTime == null)
	//			throw new ArgumentNullException(nameof(getEntityTime));

	//		_security = security;
	//		_toMessage = toMessage;
	//		_getEntityTime = getEntityTime;
	//	}

	//	int IMarketDataStorage<TEntity>.Save(IEnumerable<TEntity> data)
	//	{
	//		return Save(data.Select(_toMessage));
	//	}

	//	void IMarketDataStorage<TEntity>.Delete(IEnumerable<TEntity> data)
	//	{
	//		Delete(data.Select(_toMessage));
	//	}

	//	IEnumerable<TEntity> IMarketDataStorage<TEntity>.Load(DateTime date)
	//	{
	//		var security = _security.GetSecurity(date.ApplyTimeZone(TimeZoneInfo.Utc));

	//		if (typeof(TEntity) == typeof(Candle))
	//		{
	//			var messages = Load(date);

	//			return messages
	//				.Cast<CandleMessage>()
	//				.ToCandles<TEntity>(security);
	//		}
	//		else
	//			return Load(date).ToEntities<TMessage, TEntity>(security);
	//	}

	//	IMarketDataSerializer<TEntity> IMarketDataStorage<TEntity>.Serializer
	//	{
	//		get { throw new NotSupportedException(); }
	//	}

	//	DateTimeOffset IMarketDataStorageInfo<TEntity>.GetTime(TEntity data)
	//	{
	//		return _getEntityTime(data);
	//	}
	//}

	//class CandleContinuousSecurityMarketDataStorage<TCandle> : ContinuousSecurityMarketDataStorage<CandleMessage>, IMarketDataStorage<Candle>, IMarketDataStorageInfo<Candle>
	//{
	//	private readonly ContinuousSecurity _security;
	//	private readonly object _arg;

	//	public CandleContinuousSecurityMarketDataStorage(ContinuousSecurity security,
	//		object arg,
	//		Func<CandleMessage, DateTimeOffset> getTime,
	//		Func<CandleMessage, Security> getSecurity,
	//		Func<Security, IMarketDataDrive, IMarketDataStorage<CandleMessage>> getStorage,
	//		IMarketDataStorageDrive drive)
	//		: base(security, arg, getTime, getSecurity, getStorage, drive)
	//	{
	//		_security = security;
	//		_arg = arg;
	//	}

	//	int IMarketDataStorage<Candle>.Save(IEnumerable<Candle> data)
	//	{
	//		return Save(data.Select(c => c.ToMessage()));
	//	}

	//	void IMarketDataStorage<Candle>.Delete(IEnumerable<Candle> data)
	//	{
	//		Delete(data.Select(c => c.ToMessage()));
	//	}

	//	IEnumerable<Candle> IMarketDataStorage<Candle>.Load(DateTime date)
	//	{
	//		var messages = Load(date);

	//		return messages
	//			.ToCandles<TCandle>(_security.GetSecurity(date.ApplyTimeZone(TimeZoneInfo.Utc)))
	//			.Cast<Candle>();
	//	}

	//	IMarketDataSerializer<Candle> IMarketDataStorage<Candle>.Serializer
	//	{
	//		get { throw new NotSupportedException(); }
	//	}

	//	DateTimeOffset IMarketDataStorageInfo<Candle>.GetTime(Candle data)
	//	{
	//		return data.OpenTime;
	//	}
	//}
}