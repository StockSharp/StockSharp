namespace StockSharp.Algo.Import
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Storages;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Importing fields registry.
	/// </summary>
	public static class FieldMappingRegistry
	{
		/// <summary>
		/// Generate importing fields for the specified type.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <returns>Importing fields.</returns>
		public static IEnumerable<FieldMapping> CreateFields(DataType dataType)
		{
			var secCodeDescr = LocalizedStrings.Str2850;
			var boardCodeDescr = LocalizedStrings.Str2851;

			var dateDescr = LocalizedStrings.Str2852;
			var timeDescr = LocalizedStrings.Str2853;

			var fields = new List<FieldMapping>();
			var msgType = dataType.MessageType;

			if (msgType == typeof(SecurityMessage))
			{
				fields.Add(new FieldMapping<SecurityMessage, string>(GetSecurityCodeField(nameof(SecurityMessage.SecurityId)), LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
				fields.Add(new FieldMapping<SecurityMessage, string>(GetBoardCodeField(nameof(SecurityMessage.SecurityId)), LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

				fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.Name), LocalizedStrings.Name, LocalizedStrings.Str362, (i, v) => i.Name = v));
				fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.PriceStep), LocalizedStrings.PriceStep, LocalizedStrings.MinPriceStep, (i, v) => i.PriceStep = v));
				fields.Add(new FieldMapping<SecurityMessage, int>(nameof(SecurityMessage.Decimals), LocalizedStrings.Decimals, LocalizedStrings.Str548, (i, v) => i.Decimals = v));
				fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.VolumeStep), LocalizedStrings.VolumeStep, LocalizedStrings.Str366, (i, v) => i.VolumeStep = v));
				fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.MinVolume), LocalizedStrings.MinVolume, LocalizedStrings.MinVolumeDesc, (i, v) => i.MinVolume = v));
				fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.MaxVolume), LocalizedStrings.MaxVolume, LocalizedStrings.MaxVolumeDesc, (i, v) => i.MaxVolume = v));
				fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.Multiplier), LocalizedStrings.Str330, LocalizedStrings.LotVolume, (i, v) => i.Multiplier = v));
				fields.Add(new FieldMapping<SecurityMessage, SecurityTypes>(nameof(SecurityMessage.SecurityType), LocalizedStrings.Type, LocalizedStrings.Str360, (i, v) => i.SecurityType = v));
				fields.Add(new FieldMapping<SecurityMessage, CurrencyTypes>(nameof(SecurityMessage.Currency), LocalizedStrings.Currency, LocalizedStrings.Str382, (i, v) => i.Currency = v));
				fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.UnderlyingSecurityCode), LocalizedStrings.UnderlyingAsset, LocalizedStrings.UnderlyingAssetCode, (i, v) => i.UnderlyingSecurityCode = v));
				fields.Add(new FieldMapping<SecurityMessage, SecurityTypes>(nameof(SecurityMessage.UnderlyingSecurityType), LocalizedStrings.UnderlyingSecurityType, LocalizedStrings.UnderlyingSecurityType, (i, v) => i.UnderlyingSecurityType = v));
				fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.UnderlyingSecurityMinVolume), LocalizedStrings.UnderlyingMinVolume, LocalizedStrings.UnderlyingMinVolumeDesc, (i, v) => i.UnderlyingSecurityMinVolume = v));
				fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.Strike), LocalizedStrings.Strike, LocalizedStrings.OptionStrikePrice, (i, v) => i.Strike = v));
				fields.Add(new FieldMapping<SecurityMessage, OptionTypes>(nameof(SecurityMessage.OptionType), LocalizedStrings.OptionsContract, LocalizedStrings.OptionContractType, (i, v) => i.OptionType = v));
				fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.BinaryOptionType), LocalizedStrings.BinaryOption, LocalizedStrings.TypeBinaryOption, (i, v) => i.BinaryOptionType = v));
				fields.Add(new FieldMapping<SecurityMessage, DateTimeOffset>(nameof(SecurityMessage.ExpiryDate), LocalizedStrings.ExpiryDate, LocalizedStrings.Str371, (i, v) => i.ExpiryDate = v));
				fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.Class), LocalizedStrings.Class, LocalizedStrings.SecurityClass, (i, v) => i.Class = v));
				fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.IssueSize), LocalizedStrings.IssueSize, LocalizedStrings.IssueSize, (i, v) => i.IssueSize = v));
				fields.Add(new FieldMapping<SecurityMessage, DateTimeOffset>(nameof(SecurityMessage.IssueDate), LocalizedStrings.IssueDate, LocalizedStrings.IssueDate, (i, v) => i.IssueDate = v));
				fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.CfiCode), LocalizedStrings.CfiCode, LocalizedStrings.CfiCodeDesc, (i, v) => i.CfiCode = v));
				fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.ShortName), LocalizedStrings.Str363, LocalizedStrings.Str364, (i, v) => i.ShortName = v));
				fields.Add(new FieldMapping<SecurityMessage, bool>(nameof(SecurityMessage.Shortable), LocalizedStrings.Shortable, LocalizedStrings.ShortableDesc, (i, v) => i.Shortable = v));
				fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.BasketCode), LocalizedStrings.Basket, LocalizedStrings.BasketCode, (i, v) => i.BasketCode = v));
				fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.BasketExpression), LocalizedStrings.Expression, LocalizedStrings.ExpressionDesc, (i, v) => i.BasketExpression = v));
				fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.FaceValue), LocalizedStrings.FaceValue, LocalizedStrings.FaceValueDesc, (i, v) => i.FaceValue = v));

				//fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.SecurityId.Native), LocalizedStrings.NativeId, LocalizedStrings.NativeIdDesc, (i, v) => { }));
				fields.Add(new FieldMapping<SecurityIdMapping, string>(GetSecurityCodeField(LocalizedStrings.Adapter), LocalizedStrings.AdapterCode, secCodeDescr, (i, v) =>
				{
					var adapterId = i.AdapterId;
					adapterId.SecurityCode = v;
					i.AdapterId = adapterId;
				}) { IsAdapter = true });
				fields.Add(new FieldMapping<SecurityIdMapping, string>(GetBoardCodeField(LocalizedStrings.Adapter), LocalizedStrings.AdapterBoard, boardCodeDescr, (i, v) =>
				{
					var adapterId = i.AdapterId;
					adapterId.BoardCode = v;
					i.AdapterId = adapterId;
				}) { IsAdapter = true });
			}
			else if (msgType == typeof(ExecutionMessage))
			{
				switch ((ExecutionTypes)dataType.Arg)
				{
					case ExecutionTypes.Tick:
					{
						fields.Add(new FieldMapping<ExecutionMessage, string>(GetSecurityCodeField(nameof(ExecutionMessage.SecurityId)), LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, string>(GetBoardCodeField(nameof(ExecutionMessage.SecurityId)), LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

						fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.TradeId), LocalizedStrings.Id, string.Empty, (i, v) => i.TradeId = v));
						fields.Add(new FieldMapping<ExecutionMessage, string>(nameof(ExecutionMessage.TradeStringId), LocalizedStrings.Str2856, string.Empty, (i, v) => i.TradeStringId = v));
						fields.Add(new FieldMapping<ExecutionMessage, DateTimeOffset>(GetDateField(nameof(ExecutionMessage.ServerTime)), LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, TimeSpan>(GetTimeOfDayField(nameof(ExecutionMessage.ServerTime)), LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.TradePrice), LocalizedStrings.Price, string.Empty, (i, v) => i.TradePrice = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.TradeVolume), LocalizedStrings.Volume, string.Empty, (i, v) => i.TradeVolume = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, Sides>(nameof(ExecutionMessage.OriginSide), LocalizedStrings.Str329, LocalizedStrings.Str149, (i, v) => i.OriginSide = v));
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OpenInterest), LocalizedStrings.Str150, LocalizedStrings.Str151, (i, v) => i.OpenInterest = v));
						fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsSystem), LocalizedStrings.Str342, LocalizedStrings.Str140, (i, v) => i.IsSystem = v));
						fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsUpTick), LocalizedStrings.Str157, LocalizedStrings.Str158, (i, v) => i.IsUpTick = v));
						fields.Add(new FieldMapping<ExecutionMessage, CurrencyTypes>(nameof(ExecutionMessage.Currency), LocalizedStrings.Currency, LocalizedStrings.Str382, (i, v) => i.Currency = v));
						fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.OrderBuyId), LocalizedStrings.Buy, LocalizedStrings.OrderBuyId, (i, v) => i.OrderBuyId = v));
						fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.OrderSellId), LocalizedStrings.Sell, LocalizedStrings.OrderSellId, (i, v) => i.OrderSellId = v));

						break;
					}
					case ExecutionTypes.OrderLog:
					{
						fields.Add(new FieldMapping<ExecutionMessage, string>(GetSecurityCodeField(nameof(ExecutionMessage.SecurityId)), LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, string>(GetBoardCodeField(nameof(ExecutionMessage.SecurityId)), LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

						fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.OrderId), LocalizedStrings.Id, LocalizedStrings.OrderId, (i, v) => i.OrderId = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, DateTimeOffset>(GetDateField(nameof(ExecutionMessage.ServerTime)), LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, TimeSpan>(GetTimeOfDayField(nameof(ExecutionMessage.ServerTime)), LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OrderPrice), LocalizedStrings.Price, LocalizedStrings.OrderPrice, (i, v) => i.OrderPrice = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OrderVolume), LocalizedStrings.Volume, LocalizedStrings.OrderVolume, (i, v) => i.OrderVolume = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, Sides>(nameof(ExecutionMessage.Side), LocalizedStrings.Str128, LocalizedStrings.Str129, (i, v) => i.Side = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsSystem), LocalizedStrings.Str342, LocalizedStrings.Str140, (i, v) => i.IsSystem = v));
						fields.Add(new FieldMapping<ExecutionMessage, OrderStates>(nameof(ExecutionMessage.OrderState), LocalizedStrings.Str722, LocalizedStrings.Str134, (i, v) => i.OrderState = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, TimeInForce>(nameof(ExecutionMessage.TimeInForce), LocalizedStrings.TimeInForce, LocalizedStrings.Str144, (i, v) => i.TimeInForce = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.TradeId), LocalizedStrings.Str723, LocalizedStrings.Str145, (i, v) => i.TradeId = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.TradePrice), LocalizedStrings.Str724, LocalizedStrings.Str147, (i, v) => i.TradePrice = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OpenInterest), LocalizedStrings.Str150, LocalizedStrings.Str151, (i, v) => i.OpenInterest = v));

						break;
					}
					case ExecutionTypes.Transaction:
					{
						fields.Add(new FieldMapping<ExecutionMessage, string>(GetSecurityCodeField(nameof(ExecutionMessage.SecurityId)), LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, string>(GetBoardCodeField(nameof(ExecutionMessage.SecurityId)), LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, DateTimeOffset>(GetDateField(nameof(ExecutionMessage.ServerTime)), LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, TimeSpan>(GetTimeOfDayField(nameof(ExecutionMessage.ServerTime)), LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
						fields.Add(new FieldMapping<ExecutionMessage, string>(nameof(ExecutionMessage.PortfolioName), LocalizedStrings.Portfolio, LocalizedStrings.PortfolioName, (i, v) => i.PortfolioName = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.TransactionId), LocalizedStrings.TransactionId, LocalizedStrings.TransactionId, (i, v) => i.TransactionId = v));
						fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.OrderId), LocalizedStrings.Id, LocalizedStrings.OrderId, (i, v) => i.OrderId = v));
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OrderPrice), LocalizedStrings.Price, LocalizedStrings.OrderPrice, (i, v) => { i.OrderPrice = v; i.HasOrderInfo = true; }) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OrderVolume), LocalizedStrings.Volume, LocalizedStrings.OrderVolume, (i, v) => i.OrderVolume = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.Balance), LocalizedStrings.Str1325, LocalizedStrings.Str131, (i, v) => i.Balance = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, Sides>(nameof(ExecutionMessage.Side), LocalizedStrings.Str329, LocalizedStrings.Str129, (i, v) => i.Side = v));
						fields.Add(new FieldMapping<ExecutionMessage, OrderTypes>(nameof(ExecutionMessage.OrderType), LocalizedStrings.Str132, LocalizedStrings.Str133, (i, v) => i.OrderType = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, OrderStates>(nameof(ExecutionMessage.OrderState), LocalizedStrings.State, LocalizedStrings.Str134, (i, v) => i.OrderState = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, TimeInForce>(nameof(ExecutionMessage.TimeInForce), LocalizedStrings.TimeInForce, LocalizedStrings.Str144, (i, v) => i.TimeInForce = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.TradeId), LocalizedStrings.Str723, LocalizedStrings.Str145, (i, v) => i.TradeId = v) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.TradePrice), LocalizedStrings.Str724, LocalizedStrings.Str147, (i, v) => { i.TradePrice = v; i.HasTradeInfo = true; }) { IsRequired = true });
						fields.Add(new FieldMapping<ExecutionMessage, string>(nameof(ExecutionMessage.UserOrderId), LocalizedStrings.Str165, LocalizedStrings.Str166, (i, v) => i.UserOrderId = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, string>(nameof(ExecutionMessage.StrategyId), LocalizedStrings.Strategy, LocalizedStrings.Strategy, (i, v) => i.StrategyId = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, CurrencyTypes>(nameof(ExecutionMessage.Currency), LocalizedStrings.Currency, LocalizedStrings.Str382, (i, v) => i.Currency = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsMarketMaker), LocalizedStrings.MarketMaker, LocalizedStrings.MarketMakerOrder, (i, v) => i.IsMarketMaker = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsMargin), LocalizedStrings.Margin, LocalizedStrings.IsMargin, (i, v) => i.IsMargin = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsManual), LocalizedStrings.Manual, LocalizedStrings.IsOrderManual, (i, v) => i.IsManual = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.MinVolume), LocalizedStrings.MinVolume, LocalizedStrings.MinVolumeDesc, (i, v) => i.MinVolume = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, OrderPositionEffects>(nameof(ExecutionMessage.PositionEffect), LocalizedStrings.PositionEffect, LocalizedStrings.PositionEffectDesc, (i, v) => i.PositionEffect = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.PostOnly), LocalizedStrings.PostOnly, LocalizedStrings.PostOnlyOrder, (i, v) => i.PostOnly = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.Initiator), LocalizedStrings.Initiator, LocalizedStrings.InitiatorTrade, (i, v) => i.Initiator = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.SeqNum), LocalizedStrings.Str1916, LocalizedStrings.SequenceNumber, (i, v) => i.SeqNum = v) { IsRequired = false });
						fields.Add(new FieldMapping<ExecutionMessage, int>(nameof(ExecutionMessage.Leverage), LocalizedStrings.Leverage, LocalizedStrings.Str261, (i, v) => i.Leverage = v) { IsRequired = false });

						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else if (msgType == typeof(CandleMessage) || msgType.IsCandleMessage())
			{
				fields.Add(new FieldMapping<CandleMessage, string>(GetSecurityCodeField(nameof(CandleMessage.SecurityId)), LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
				fields.Add(new FieldMapping<CandleMessage, string>(GetBoardCodeField(nameof(CandleMessage.SecurityId)), LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

				fields.Add(new FieldMapping<CandleMessage, DateTimeOffset>(GetDateField(nameof(CandleMessage.OpenTime)), LocalizedStrings.Date, dateDescr, (i, v) =>
				{
					i.OpenTime = v + i.OpenTime.TimeOfDay;

					if (!i.CloseTime.IsDefault())
						i.CloseTime = v + i.CloseTime.TimeOfDay;
				}) { IsRequired = true });
				fields.Add(new FieldMapping<CandleMessage, TimeSpan>(GetTimeOfDayField(nameof(CandleMessage.OpenTime)), LocalizedStrings.Str2860, LocalizedStrings.CandleOpenTime, (i, v) => i.OpenTime += v));
				fields.Add(new FieldMapping<CandleMessage, TimeSpan>(nameof(CandleMessage.CloseTime), LocalizedStrings.Str2861, LocalizedStrings.CandleCloseTime, (i, v) =>
				{
					if (i.CloseTime.IsDefault())
						i.CloseTime = i.OpenTime - i.OpenTime.TimeOfDay + v;
					else
						i.CloseTime += v;
				}));
				fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.OpenInterest), LocalizedStrings.Str150, string.Empty, (i, v) => i.OpenInterest = v));
				fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.OpenPrice), "O", LocalizedStrings.Str80, (i, v) => i.OpenPrice = v) { IsRequired = true });
				fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.HighPrice), "H", LocalizedStrings.Str82, (i, v) => i.HighPrice = v) { IsRequired = true });
				fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.LowPrice), "L", LocalizedStrings.Str84, (i, v) => i.LowPrice = v) { IsRequired = true });
				fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.ClosePrice), "C", LocalizedStrings.Str86, (i, v) => i.ClosePrice = v) { IsRequired = true });
				fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.TotalVolume), "V", LocalizedStrings.TotalCandleVolume, (i, v) => i.TotalVolume = v) { IsRequired = true });
				//_allFields.Add(new FieldMapping<CandleMessage>("Arg", "Параметр", string.Empty, typeof(object), (i, v) => i.Arg = v) { IsRequired = true });
				fields.Add(new FieldMapping<CandleMessage, int>(nameof(CandleMessage.UpTicks), LocalizedStrings.TickUp, LocalizedStrings.TickUpCount, (i, v) => i.UpTicks = v) { IsRequired = false });
				fields.Add(new FieldMapping<CandleMessage, int>(nameof(CandleMessage.DownTicks), LocalizedStrings.TickDown, LocalizedStrings.TickDownCount, (i, v) => i.DownTicks = v) { IsRequired = false });
				fields.Add(new FieldMapping<CandleMessage, int>(nameof(CandleMessage.TotalTicks), LocalizedStrings.Ticks, LocalizedStrings.TickCount, (i, v) => i.TotalTicks = v) { IsRequired = false });
			}
			else if (msgType == typeof(QuoteChangeMessage))
			{
				fields.Add(new FieldMapping<TimeQuoteChange, string>(GetSecurityCodeField(nameof(TimeQuoteChange.SecurityId)), LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
				fields.Add(new FieldMapping<TimeQuoteChange, string>(GetBoardCodeField(nameof(TimeQuoteChange.SecurityId)), LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

				fields.Add(new FieldMapping<TimeQuoteChange, DateTimeOffset>(GetDateField(nameof(TimeQuoteChange.ServerTime)), LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
				fields.Add(new FieldMapping<TimeQuoteChange, TimeSpan>(GetTimeOfDayField(nameof(TimeQuoteChange.ServerTime)), LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
				fields.Add(new FieldMapping<TimeQuoteChange, decimal>(nameof(QuoteChange.Price), LocalizedStrings.Price, LocalizedStrings.Str275, (i, v) =>
				{
					var q = i.Quote;
					q.Price = v;
					i.Quote = q;
				}) { IsRequired = true });
				fields.Add(new FieldMapping<TimeQuoteChange, decimal>(nameof(QuoteChange.Volume), LocalizedStrings.Volume, LocalizedStrings.Str276, (i, v) =>
				{
					var q = i.Quote;
					q.Volume = v;
					i.Quote = q;
				}) { IsRequired = true });
				fields.Add(new FieldMapping<TimeQuoteChange, Sides>(nameof(TimeQuoteChange.Side), LocalizedStrings.Str128, LocalizedStrings.Str277, (i, v) => i.Side = v) { IsRequired = true });
				fields.Add(new FieldMapping<TimeQuoteChange, int>(nameof(QuoteChange.OrdersCount), LocalizedStrings.Str668, LocalizedStrings.XamlStr248, (i, v) =>
				{
					var q = i.Quote;
					q.OrdersCount = v;
					i.Quote = q;
				}));
				fields.Add(new FieldMapping<TimeQuoteChange, QuoteConditions>(nameof(QuoteChange.Condition), LocalizedStrings.Str154, LocalizedStrings.QuoteCondition, (i, v) =>
				{
					var q = i.Quote;
					q.Condition = v;
					i.Quote = q;
				}));
				fields.Add(new FieldMapping<TimeQuoteChange, int>(nameof(QuoteChange.StartPosition), LocalizedStrings.Str2421, LocalizedStrings.Str2421, (i, v) =>
				{
					var q = i.Quote;
					q.StartPosition = v;
					i.Quote = q;
				}));
				fields.Add(new FieldMapping<TimeQuoteChange, int>(nameof(QuoteChange.EndPosition), LocalizedStrings.End, LocalizedStrings.End, (i, v) =>
				{
					var q = i.Quote;
					q.EndPosition = v;
					i.Quote = q;
				}));
				fields.Add(new FieldMapping<TimeQuoteChange, QuoteChangeActions>(nameof(QuoteChange.Action), LocalizedStrings.Str722, LocalizedStrings.Str722, (i, v) =>
				{
					var q = i.Quote;
					q.Action = v;
					i.Quote = q;
				}));
			}
			else if (msgType == typeof(Level1ChangeMessage))
			{
				fields.Add(new FieldMapping<Level1ChangeMessage, string>(GetSecurityCodeField(nameof(Level1ChangeMessage.SecurityId)), LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
				fields.Add(new FieldMapping<Level1ChangeMessage, string>(GetBoardCodeField(nameof(Level1ChangeMessage.SecurityId)), LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

				fields.Add(new FieldMapping<Level1ChangeMessage, DateTimeOffset>(GetDateField(nameof(Level1ChangeMessage.ServerTime)), LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
				fields.Add(new FieldMapping<Level1ChangeMessage, TimeSpan>(GetTimeOfDayField(nameof(Level1ChangeMessage.ServerTime)), LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));

				foreach (var f in Enumerator.GetValues<Level1Fields>().ExcludeObsolete())
				{
					var field = f;

					fields.Add(new FieldMapping<Level1ChangeMessage, object>(GetChangesField(field), field.GetDisplayName(), field.GetFieldDescription(), field.ToType(), (i, v) =>
					{
						if (v is decimal d && d == 0m)
							return;

						i.Changes.Add(field, v);
					}));
				}
			}
			else if (msgType == typeof(PositionChangeMessage))
			{
				fields.Add(new FieldMapping<PositionChangeMessage, string>(GetSecurityCodeField(nameof(PositionChangeMessage.SecurityId)), LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
				fields.Add(new FieldMapping<PositionChangeMessage, string>(GetBoardCodeField(nameof(PositionChangeMessage.SecurityId)), LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

				fields.Add(new FieldMapping<PositionChangeMessage, DateTimeOffset>(GetDateField(nameof(PositionChangeMessage.ServerTime)), LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
				fields.Add(new FieldMapping<PositionChangeMessage, TimeSpan>(GetTimeOfDayField(nameof(PositionChangeMessage.ServerTime)), LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));

				fields.Add(new FieldMapping<PositionChangeMessage, string>(nameof(PositionChangeMessage.StrategyId), LocalizedStrings.Strategy, LocalizedStrings.Strategy, (i, v) => i.StrategyId = v) { IsRequired = false });
				fields.Add(new FieldMapping<PositionChangeMessage, Sides>(nameof(PositionChangeMessage.Side), LocalizedStrings.Side, LocalizedStrings.Side, (i, v) => i.Side = v) { IsRequired = false });

				foreach (var f in Enumerator.GetValues<PositionChangeTypes>().Where(l1 => !l1.IsObsolete()))
				{
					var field = f;

					fields.Add(new FieldMapping<PositionChangeMessage, object>(GetChangesField(field), field.GetDisplayName(), field.GetFieldDescription(), field.ToType(), (i, v) =>
					{
						if (v is decimal d && d == 0m)
							return;

						i.Changes.Add(field, v);
					}));
				}
			}
			else if (msgType == typeof(NewsMessage))
			{
				fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Id), LocalizedStrings.Id, string.Empty, (i, v) => i.Id = v) { IsRequired = true });
				fields.Add(new FieldMapping<NewsMessage, string>(GetSecurityCodeField(nameof(NewsMessage.SecurityId)), LocalizedStrings.Security, secCodeDescr, (i, v) => { i.SecurityId = new SecurityId { SecurityCode = v }; }));
				fields.Add(new FieldMapping<NewsMessage, string>(GetBoardCodeField(nameof(NewsMessage.SecurityId)), LocalizedStrings.Board, boardCodeDescr, (i, v) => i.BoardCode = v));
				fields.Add(new FieldMapping<NewsMessage, DateTimeOffset>(GetDateField(nameof(NewsMessage.ServerTime)), LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
				fields.Add(new FieldMapping<NewsMessage, TimeSpan>(GetTimeOfDayField(nameof(NewsMessage.ServerTime)), LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
				fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Headline), LocalizedStrings.Str215, LocalizedStrings.Str215, (i, v) => i.Headline = v));
				fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Story), LocalizedStrings.Str217, LocalizedStrings.Str218, (i, v) => i.Story = v));
				fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Source), LocalizedStrings.Str213, LocalizedStrings.Str214, (i, v) => i.Source = v));
				fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Url), LocalizedStrings.Str221, LocalizedStrings.Str222, (i, v) => i.Url = v));
				fields.Add(new FieldMapping<NewsMessage, NewsPriorities>(nameof(NewsMessage.Priority), LocalizedStrings.Priority, LocalizedStrings.NewsPriority, (i, v) => i.Priority = v));
				fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Language), LocalizedStrings.Str3429, LocalizedStrings.Str3429, (i, v) => i.Language = v));
			}
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1655);

			return fields;
		}

		/// <summary>
		/// Generate extended fields for the specified storage.
		/// </summary>
		/// <param name="storage">Extended info <see cref="Message.ExtensionInfo"/> storage.</param>
		/// <returns>Extended fields.</returns>
		public static FieldMapping[] CreateExtendedFields(IExtendedInfoStorageItem storage)
		{
			return storage
				.Fields
				.Select(t => (FieldMapping)new FieldMapping<SecurityMessage, object>($"{nameof(IExtendableEntity.ExtensionInfo)}[{t.Item1}]", t.Item1, string.Empty, (s, v) => s.ExtensionInfo[t.Item1] = v) { IsExtended = true })
				.ToArray();
		}

		private static void SetSecCode(dynamic message, string code)
		{
			SecurityId securityId = message.SecurityId;
			securityId.SecurityCode = code;
			message.SecurityId = securityId;
		}

		private static void SetBoardCode(dynamic message, string code)
		{
			SecurityId securityId = message.SecurityId;
			securityId.BoardCode = code;
			message.SecurityId = securityId;
		}

		private static string GetSecurityCodeField(string prefix)
		{
			return prefix + "." + nameof(SecurityId.SecurityCode);
		}

		private static string GetBoardCodeField(string prefix)
		{
			return prefix + "." + nameof(SecurityId.BoardCode);
		}

		private static string GetDateField(string prefix)
		{
			return prefix + "." + nameof(DateTimeOffset.Date);
		}

		private static string GetTimeOfDayField(string prefix)
		{
			return prefix + "." + nameof(DateTimeOffset.TimeOfDay);
		}

		private static string GetChangesField(Level1Fields field)
		{
			return $"{nameof(Level1ChangeMessage.Changes)}[{field}]";
		}

		private static string GetChangesField(PositionChangeTypes type)
		{
			return $"{nameof(PositionChangeMessage.Changes)}[{type}]";
		}
	}
}