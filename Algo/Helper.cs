#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: Helper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	static class Helper
	{
		public static void CheckOption(this Security option)
		{
			if (option == null)
				throw new ArgumentNullException(nameof(option));

			if (option.Type != SecurityTypes.Option)
				throw new ArgumentException(LocalizedStrings.Str900Params.Put(option.Type), nameof(option));

			if (option.OptionType == null)
				throw new ArgumentException(LocalizedStrings.Str703Params.Put(option), nameof(option));

			if (option.ExpiryDate == null)
				throw new ArgumentException(LocalizedStrings.Str901Params.Put(option), nameof(option));

			if (option.UnderlyingSecurityId == null)
				throw new ArgumentException(LocalizedStrings.Str902Params.Put(option), nameof(option));
		}

		public static ExchangeBoard CheckExchangeBoard(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (security.Board == null)
				throw new ArgumentException(LocalizedStrings.Str903Params.Put(security), nameof(security));

			return security.Board;
		}

		public static Security CheckPriceStep(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (security.PriceStep == null)
				throw new ArgumentException(LocalizedStrings.Str905Params.Put(security.Id));

			return security;
		}

		public static long GetTradeId(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var tradeId = message.TradeId;

			if (tradeId == null)
				throw new ArgumentOutOfRangeException(nameof(message), null, LocalizedStrings.Str1020);

			return tradeId.Value;
		}

		public static decimal GetTradePrice(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var price = message.TradePrice;

			if (price == null)
				throw new ArgumentOutOfRangeException(nameof(message), null, LocalizedStrings.Str1021Params.Put(message.TradeId));

			return price.Value;
		}

		public static decimal GetBalance(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var balance = message.Balance;

			if (balance != null)
				return balance.Value;

			throw new ArgumentOutOfRangeException(nameof(message));
		}

		public static void ReplaceSecurityId(this Message message, SecurityId securityId)
		{
			switch (message.Type)
			{
				//case MessageTypes.Position:
				//{
				//	var positionMsg = (PositionMessage)message;
				//	positionMsg.SecurityId = securityId;
				//	break;
				//}

				case MessageTypes.PositionChange:
				{
					var positionMsg = (PositionChangeMessage)message;
					positionMsg.SecurityId = securityId;
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					execMsg.SecurityId = securityId;
					break;
				}

				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message;
					level1Msg.SecurityId = securityId;
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteChangeMsg = (QuoteChangeMessage)message;
					quoteChangeMsg.SecurityId = securityId;
					break;
				}

				case MessageTypes.News:
				{
					var newsMsg = (NewsMessage)message;
					newsMsg.SecurityId = securityId;
					break;
				}

				case MessageTypes.OrderRegister:
				{
					var msg = (OrderRegisterMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				case MessageTypes.OrderReplace:
				{
					var msg = (OrderReplaceMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				case MessageTypes.OrderCancel:
				{
					var msg = (OrderCancelMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				case MessageTypes.MarketData:
				{
					var msg = (MarketDataMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandleRange:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				{
					var msg = (CandleMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.Str2770);
			}
		}

		public class SubscriptionKey : Tuple<MarketDataTypes, SecurityId, object, int?, Tuple<DateTimeOffset?, DateTimeOffset?, long?>>
		{
			public SubscriptionKey(MarketDataTypes item1, SecurityId item2, object item3, int? item4, Tuple<DateTimeOffset?, DateTimeOffset?, long?> item5)
				: base(item1, item2, item3, item4, item5)
			{
			}
		}

		public static SubscriptionKey CreateKey(this MarketDataMessage message, SecurityId? securityId = null)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var isRealTime = message.To == null;
			var range = isRealTime ? null : Tuple.Create(message.From, message.To, message.Count);

			return new SubscriptionKey(message.DataType, securityId ?? message.SecurityId, message.Arg, message.MaxDepth, range);
		}

		public static bool NotRequiredSecurityId(this SecurityMessage secMsg)
		{
			if (secMsg == null)
				throw new ArgumentNullException(nameof(secMsg));

			if (secMsg.Type == MessageTypes.MarketData && ((MarketDataMessage)secMsg).DataType == MarketDataTypes.News)
				return secMsg.SecurityId.IsDefault();
			else if (secMsg.Type == MessageTypes.OrderGroupCancel)
				return secMsg.SecurityId.IsDefault();

			return false;
		}
	}
}