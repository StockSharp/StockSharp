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

	/// <summary>
	/// The aggregator-storage, allowing to load data simultaneously market data for <see cref="ContinuousSecurity"/>.
	/// </summary>
	/// <typeparam name="T">Message type.</typeparam>
	public class ContinuousSecurityMarketDataStorage<T> : BasketMarketDataStorage<T>
		where T : Message
	{
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

			Security = _security = security;
			Arg = arg;
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
			securityId.EnsureHashCode();

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
}