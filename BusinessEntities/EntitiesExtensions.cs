#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: UnitHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Extension class for <see cref="BusinessEntities"/>.
	/// </summary>
	public static class EntitiesExtensions
	{
		/// <summary>
		/// To create from <see cref="int"/> the pips values.
		/// </summary>
		/// <param name="value"><see cref="int"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment is taken.</param>
		/// <returns>Pips.</returns>
		public static Unit Pips(this int value, Security security)
		{
			return Pips((decimal)value, security);
		}

		/// <summary>
		/// To create from <see cref="double"/> the pips values.
		/// </summary>
		/// <param name="value"><see cref="double"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment is taken.</param>
		/// <returns>Pips.</returns>
		public static Unit Pips(this double value, Security security)
		{
			return Pips((decimal)value, security);
		}

		/// <summary>
		/// To create from <see cref="decimal"/> the pips values.
		/// </summary>
		/// <param name="value"><see cref="decimal"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment is taken.</param>
		/// <returns>Pips.</returns>
		public static Unit Pips(this decimal value, Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return new Unit(value, UnitTypes.Step, type => GetTypeValue(security, type));
		}

		/// <summary>
		/// To create from <see cref="int"/> the points values.
		/// </summary>
		/// <param name="value"><see cref="int"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment cost is taken.</param>
		/// <returns>Points.</returns>
		public static Unit Points(this int value, Security security)
		{
			return Points((decimal)value, security);
		}

		/// <summary>
		/// To create from <see cref="double"/> the points values.
		/// </summary>
		/// <param name="value"><see cref="double"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment cost is taken.</param>
		/// <returns>Points.</returns>
		public static Unit Points(this double value, Security security)
		{
			return Points((decimal)value, security);
		}

		/// <summary>
		/// To create from <see cref="decimal"/> the points values.
		/// </summary>
		/// <param name="value"><see cref="decimal"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment cost is taken.</param>
		/// <returns>Points.</returns>
		public static Unit Points(this decimal value, Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return new Unit(value, UnitTypes.Point, type => GetTypeValue(security, type));
		}

		/// <summary>
		/// Convert string to <see cref="Unit"/>.
		/// </summary>
		/// <param name="str">String value of <see cref="Unit"/>.</param>
		/// <param name="throwIfNull">Throw <see cref="ArgumentNullException"/> if the specified string is empty.</param>
		/// <param name="security">Information about the instrument. Required when using <see cref="UnitTypes.Point"/> и <see cref="UnitTypes.Step"/>.</param>
		/// <returns>Object <see cref="Unit"/>.</returns>
		public static Unit ToUnit2(this string str, bool throwIfNull = true, Security security = null)
		{
			return str.ToUnit(throwIfNull, t => GetTypeValue(security, t));
		}

		/// <summary>
		/// Cast the value to another type.
		/// </summary>
		/// <param name="unit">Source unit.</param>
		/// <param name="destinationType">Destination value type.</param>
		/// <param name="security">Information about the instrument. Required when using <see cref="UnitTypes.Point"/> и <see cref="UnitTypes.Step"/>.</param>
		/// <returns>Converted value.</returns>
		public static Unit Convert(this Unit unit, UnitTypes destinationType, Security security)
		{
			return unit.Convert(destinationType, type => GetTypeValue(security, type));
		}

		/// <summary>
		/// To cut the price, to make it multiple of minimal step, also to limit number of signs after the comma.
		/// </summary>
		/// <param name="security"><see cref="Security"/></param>
		/// <param name="price">The price to be made multiple.</param>
		/// <returns>The multiple price.</returns>
		public static decimal ShrinkPrice(this Security security, decimal price)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (security.PriceStep == null)
				throw new ArgumentException(LocalizedStrings.PriceStepNotSpecified, nameof(security));

			return price.Round(security.PriceStep ?? 1m, security.Decimals ?? 0);
		}

		/// <summary>
		/// To set the <see cref="Unit.GetTypeValue"/> property for the value.
		/// </summary>
		/// <param name="unit">Unit.</param>
		/// <param name="security">Security.</param>
		/// <returns>Unit.</returns>
		public static Unit SetSecurity(this Unit unit, Security security)
		{
			if (unit is null)
				throw new ArgumentNullException(nameof(unit));

			unit.GetTypeValue = type => GetTypeValue(security, type);

			return unit;
		}

		private static decimal? GetTypeValue(Security security, UnitTypes type)
		{
			switch (type)
			{
				case UnitTypes.Point:
					if (security == null)
						throw new ArgumentNullException(nameof(security));

					return security.StepPrice;
				case UnitTypes.Step:
					if (security == null)
						throw new ArgumentNullException(nameof(security));

					return security.PriceStep;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
			}
		}

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="provider">The transactional provider.</param>
		/// <param name="order">Order.</param>
		/// <param name="clone">Changes.</param>
		public static void ReRegisterOrderEx(this ITransactionProvider provider, Order order, Order clone)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			if (provider.IsOrderReplaceable(order) == true)
			{
				if (provider.IsOrderEditable(order) == true)
					provider.EditOrder(order, clone);
				else
					provider.ReRegisterOrder(order, clone);
			}
			else
			{
				provider.CancelOrder(order);
				provider.RegisterOrder(clone);
			}
		}

		/// <summary>
		/// To create copy of the order for re-registration.
		/// </summary>
		/// <param name="oldOrder">The original order.</param>
		/// <param name="newPrice">Price of the new order.</param>
		/// <param name="newVolume">Volume of the new order.</param>
		/// <returns>New order.</returns>
		public static Order ReRegisterClone(this Order oldOrder, decimal? newPrice = null, decimal? newVolume = null)
		{
			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			return new Order
			{
				Portfolio = oldOrder.Portfolio,
				Side = oldOrder.Side,
				TimeInForce = oldOrder.TimeInForce,
				Security = oldOrder.Security,
				Type = oldOrder.Type,
				Price = newPrice ?? oldOrder.Price,
				Volume = newVolume ?? oldOrder.Volume,
				ExpiryDate = oldOrder.ExpiryDate,
				VisibleVolume = oldOrder.VisibleVolume,
				BrokerCode = oldOrder.BrokerCode,
				ClientCode = oldOrder.ClientCode,
				Condition = oldOrder.Condition?.TypedClone(),
				IsManual = oldOrder.IsManual,
				IsMarketMaker = oldOrder.IsMarketMaker,
				MarginMode = oldOrder.MarginMode,
				MinVolume = oldOrder.MinVolume,
				PositionEffect = oldOrder.PositionEffect,
				PostOnly = oldOrder.PostOnly,
				StrategyId = oldOrder.StrategyId,
				Leverage = oldOrder.Leverage,
			};
		}

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="provider">The transactional provider.</param>
		/// <param name="oldOrder">Changing order.</param>
		/// <param name="price">Price of the new order.</param>
		/// <param name="volume">Volume of the new order.</param>
		/// <returns>New order.</returns>
		public static Order ReRegisterOrder(this ITransactionProvider provider, Order oldOrder, decimal price, decimal volume)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			var newOrder = oldOrder.ReRegisterClone(price, volume);
			provider.ReRegisterOrder(oldOrder, newOrder);
			return newOrder;
		}

		/// <summary>
		/// Get portfolio identifier.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Portfolio identifier.</returns>
		[Obsolete("Use Portfolio.Name property.")]
		public static string GetUniqueId(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return /*portfolio.InternalId?.To<string>() ?? */portfolio.Name;
		}

		/// <summary>
		/// To get the instrument by the identifier.
		/// </summary>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="id">Security ID.</param>
		/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
		public static Security LookupByStringId(this ISecurityProvider provider, string id)
		{
			return provider.LookupById(id.ToSecurityId());
		}

		private const BindingFlags _publicStatic = BindingFlags.Public | BindingFlags.Static;

		/// <summary>
		/// To get a list of exchanges.
		/// </summary>
		/// <returns>Exchanges.</returns>
		public static IEnumerable<Exchange> EnumerateExchanges()
			=> typeof(Exchange)
				.GetMembers<PropertyInfo>(_publicStatic, typeof(Exchange))
				.Select(prop => (Exchange)prop.GetValue(null, null));

		/// <summary>
		/// To get a list of boards.
		/// </summary>
		/// <returns>Boards.</returns>
		public static IEnumerable<ExchangeBoard> EnumerateExchangeBoards()
			=> typeof(ExchangeBoard)
				.GetMembers<PropertyInfo>(_publicStatic, typeof(ExchangeBoard))
				.Select(prop => (ExchangeBoard)prop.GetValue(null, null));

		/// <summary>
		/// To convert the message into tick trade.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Tick trade.</returns>
		[Obsolete("Use ITickTradeMessage.")]
		public static Trade ToTrade(this ExecutionMessage message, Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return message.ToTrade(new Trade { Security = security });
		}

		/// <summary>
		/// To convert the message into tick trade.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="trade">Tick trade.</param>
		/// <returns>Tick trade.</returns>
		[Obsolete("Use ITickTradeMessage.")]
		public static Trade ToTrade(this ExecutionMessage message, Trade trade)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			trade.Id = message.TradeId;
			trade.StringId = message.TradeStringId;
			trade.Price = message.TradePrice ?? 0;
			trade.Volume = message.TradeVolume ?? 0;
			trade.Status = message.TradeStatus;
			trade.IsSystem = message.IsSystem;
			trade.ServerTime = message.ServerTime;
			trade.LocalTime = message.LocalTime;
			trade.OpenInterest = message.OpenInterest;
			trade.OriginSide = message.OriginSide;
			trade.IsUpTick = message.IsUpTick;
			trade.Currency = message.Currency;
			trade.SeqNum = message.SeqNum;
			trade.BuildFrom = message.BuildFrom;
			trade.Yield = message.Yield;
			trade.OrderBuyId = message.OrderBuyId;
			trade.OrderSellId = message.OrderSellId;

			return trade;
		}
	}
}