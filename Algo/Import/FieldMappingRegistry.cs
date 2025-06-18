namespace StockSharp.Algo.Import;

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
		string secCodeDescr() => LocalizedStrings.SecCodeDescription;
		string boardCodeDescr() => LocalizedStrings.BoardCodeDescription;

		string dateDescr() => LocalizedStrings.DateDescription;
		string timeDescr() => LocalizedStrings.TimeDescription;

		var fields = new List<FieldMapping>();
		var msgType = dataType.MessageType;

		if (msgType == typeof(SecurityMessage))
		{
			fields.Add(new FieldMapping<SecurityMessage, string>(GetSecurityCodeField(nameof(SecurityMessage.SecurityId)), () => LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
			fields.Add(new FieldMapping<SecurityMessage, string>(GetBoardCodeField(nameof(SecurityMessage.SecurityId)), () => LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

			fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.Name), () => LocalizedStrings.Name, () => LocalizedStrings.SecurityName, (i, v) => i.Name = v));
			fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.PriceStep), () => LocalizedStrings.PriceStep, () => LocalizedStrings.MinPriceStep, (i, v) => i.PriceStep = v));
			fields.Add(new FieldMapping<SecurityMessage, int>(nameof(SecurityMessage.Decimals), () => LocalizedStrings.Decimals, () => LocalizedStrings.DecimalsDesc, (i, v) => i.Decimals = v));
			fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.VolumeStep), () => LocalizedStrings.VolumeStep, () => LocalizedStrings.MinVolStep, (i, v) => i.VolumeStep = v));
			fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.MinVolume), () => LocalizedStrings.MinVolume, () => LocalizedStrings.MinVolumeDesc, (i, v) => i.MinVolume = v));
			fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.MaxVolume), () => LocalizedStrings.MaxVolume, () => LocalizedStrings.MaxVolumeDesc, (i, v) => i.MaxVolume = v));
			fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.Multiplier), () => LocalizedStrings.Lot, () => LocalizedStrings.LotVolume, (i, v) => i.Multiplier = v));
			fields.Add(new FieldMapping<SecurityMessage, SecurityTypes>(nameof(SecurityMessage.SecurityType), () => LocalizedStrings.Type, () => LocalizedStrings.SecurityTypeDesc, (i, v) => i.SecurityType = v));
			fields.Add(new FieldMapping<SecurityMessage, CurrencyTypes>(nameof(SecurityMessage.Currency), () => LocalizedStrings.Currency, () => LocalizedStrings.CurrencyDesc, (i, v) => i.Currency = v));
			fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.UnderlyingSecurityId), () => LocalizedStrings.UnderlyingAsset, () => LocalizedStrings.UnderlyingAssetCode, (i, v) => i.UnderlyingSecurityId = new() { SecurityCode = v }));
			fields.Add(new FieldMapping<SecurityMessage, SecurityTypes>(nameof(SecurityMessage.UnderlyingSecurityType), () => LocalizedStrings.UnderlyingSecurityType, () => LocalizedStrings.UnderlyingSecurityType, (i, v) => i.UnderlyingSecurityType = v));
			fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.UnderlyingSecurityMinVolume), () => LocalizedStrings.UnderlyingMinVolume, () => LocalizedStrings.UnderlyingMinVolumeDesc, (i, v) => i.UnderlyingSecurityMinVolume = v));
			fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.Strike), () => LocalizedStrings.Strike, () => LocalizedStrings.OptionStrikePrice, (i, v) => i.Strike = v));
			fields.Add(new FieldMapping<SecurityMessage, OptionTypes>(nameof(SecurityMessage.OptionType), () => LocalizedStrings.OptionsContract, () => LocalizedStrings.OptionContractType, (i, v) => i.OptionType = v));
			fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.BinaryOptionType), () => LocalizedStrings.BinaryOption, () => LocalizedStrings.TypeBinaryOption, (i, v) => i.BinaryOptionType = v));
			fields.Add(new FieldMapping<SecurityMessage, DateTimeOffset>(nameof(SecurityMessage.ExpiryDate), () => LocalizedStrings.ExpiryDate, () => LocalizedStrings.ExpiryDateDesc, (i, v) => i.ExpiryDate = v));
			fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.Class), () => LocalizedStrings.Class, () => LocalizedStrings.SecurityClass, (i, v) => i.Class = v));
			fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.IssueSize), () => LocalizedStrings.IssueSize, () => LocalizedStrings.IssueSize, (i, v) => i.IssueSize = v));
			fields.Add(new FieldMapping<SecurityMessage, DateTimeOffset>(nameof(SecurityMessage.IssueDate), () => LocalizedStrings.IssueDate, () => LocalizedStrings.IssueDate, (i, v) => i.IssueDate = v));
			fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.CfiCode), () => LocalizedStrings.CfiCode, () => LocalizedStrings.CfiCodeDesc, (i, v) => i.CfiCode = v));
			fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.ShortName), () => LocalizedStrings.ShortName, () => LocalizedStrings.ShortNameDesc, (i, v) => i.ShortName = v));
			fields.Add(new FieldMapping<SecurityMessage, bool>(nameof(SecurityMessage.Shortable), () => LocalizedStrings.Shortable, () => LocalizedStrings.ShortableDesc, (i, v) => i.Shortable = v));
			fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.BasketCode), () => LocalizedStrings.Basket, () => LocalizedStrings.BasketCode, (i, v) => i.BasketCode = v));
			fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.BasketExpression), () => LocalizedStrings.Expression, () => LocalizedStrings.ExpressionDesc, (i, v) => i.BasketExpression = v));
			fields.Add(new FieldMapping<SecurityMessage, decimal>(nameof(SecurityMessage.FaceValue), () => LocalizedStrings.FaceValue, () => LocalizedStrings.FaceValueDesc, (i, v) => i.FaceValue = v));
			fields.Add(new FieldMapping<SecurityMessage, OptionStyles>(nameof(SecurityMessage.OptionStyle), () => LocalizedStrings.OptionStyle, () => LocalizedStrings.OptionStyleDesc, (i, v) => i.OptionStyle = v));
			fields.Add(new FieldMapping<SecurityMessage, SettlementTypes>(nameof(SecurityMessage.SettlementType), () => LocalizedStrings.Settlement, () => LocalizedStrings.SettlementTypeDesc, (i, v) => i.SettlementType = v));

			//fields.Add(new FieldMapping<SecurityMessage, string>(nameof(SecurityMessage.SecurityId.Native), () => LocalizedStrings.NativeId, () => LocalizedStrings.NativeIdDesc, (i, v) => { }));
			fields.Add(new FieldMapping<SecurityIdMapping, string>(GetSecurityCodeField(LocalizedStrings.Adapter), () => LocalizedStrings.AdapterCode, secCodeDescr, (i, v) =>
			{
				var adapterId = i.AdapterId;
				adapterId.SecurityCode = v;
				i.AdapterId = adapterId;
			}) { IsAdapter = true });
			fields.Add(new FieldMapping<SecurityIdMapping, string>(GetBoardCodeField(LocalizedStrings.Adapter), () => LocalizedStrings.AdapterBoard, boardCodeDescr, (i, v) =>
			{
				var adapterId = i.AdapterId;
				adapterId.BoardCode = v;
				i.AdapterId = adapterId;
			}) { IsAdapter = true });
		}
		else if (msgType == typeof(ExecutionMessage))
		{
			switch (dataType.Arg.To<ExecutionTypes>())
			{
				case ExecutionTypes.Tick:
				{
					fields.Add(new FieldMapping<ExecutionMessage, string>(GetSecurityCodeField(nameof(ExecutionMessage.SecurityId)), () => LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, string>(GetBoardCodeField(nameof(ExecutionMessage.SecurityId)), () => LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

					fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.TradeId), () => LocalizedStrings.Id, () => LocalizedStrings.Id, (i, v) => i.TradeId = v));
					fields.Add(new FieldMapping<ExecutionMessage, string>(nameof(ExecutionMessage.TradeStringId), () => LocalizedStrings.StringId, () => LocalizedStrings.StringId, (i, v) => i.TradeStringId = v));
					fields.Add(new FieldMapping<ExecutionMessage, DateTimeOffset>(GetDateField(nameof(ExecutionMessage.ServerTime)), () => LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, TimeSpan>(GetTimeOfDayField(nameof(ExecutionMessage.ServerTime)), () => LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.TradePrice), () => LocalizedStrings.Price, () => LocalizedStrings.Price, (i, v) => i.TradePrice = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.TradeVolume), () => LocalizedStrings.Volume, () => LocalizedStrings.Volume, (i, v) => i.TradeVolume = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, Sides>(nameof(ExecutionMessage.OriginSide), () => LocalizedStrings.Initiator, () => LocalizedStrings.DirectionDesc, (i, v) => i.OriginSide = v));
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OpenInterest), () => LocalizedStrings.OpenInterest, () => LocalizedStrings.OpenInterestDesc, (i, v) => i.OpenInterest = v));
					fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsSystem), () => LocalizedStrings.System, () => LocalizedStrings.IsSystemTrade, (i, v) => i.IsSystem = v));
					fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsUpTick), () => LocalizedStrings.UpTrend, () => LocalizedStrings.UpTrendDesc, (i, v) => i.IsUpTick = v));
					fields.Add(new FieldMapping<ExecutionMessage, CurrencyTypes>(nameof(ExecutionMessage.Currency), () => LocalizedStrings.Currency, () => LocalizedStrings.CurrencyDesc, (i, v) => i.Currency = v));
					fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.OrderBuyId), () => LocalizedStrings.Buy, () => LocalizedStrings.OrderBuyId, (i, v) => i.OrderBuyId = v));
					fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.OrderSellId), () => LocalizedStrings.Sell, () => LocalizedStrings.OrderSellId, (i, v) => i.OrderSellId = v));

					break;
				}
				case ExecutionTypes.OrderLog:
				{
					fields.Add(new FieldMapping<ExecutionMessage, string>(GetSecurityCodeField(nameof(ExecutionMessage.SecurityId)), () => LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, string>(GetBoardCodeField(nameof(ExecutionMessage.SecurityId)), () => LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

					fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.OrderId), () => LocalizedStrings.Id, () => LocalizedStrings.OrderId, (i, v) => i.OrderId = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, DateTimeOffset>(GetDateField(nameof(ExecutionMessage.ServerTime)), () => LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, TimeSpan>(GetTimeOfDayField(nameof(ExecutionMessage.ServerTime)), () => LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OrderPrice), () => LocalizedStrings.Price, () => LocalizedStrings.OrderPrice, (i, v) => i.OrderPrice = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OrderVolume), () => LocalizedStrings.Volume, () => LocalizedStrings.OrderVolume, (i, v) => i.OrderVolume = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, Sides>(nameof(ExecutionMessage.Side), () => LocalizedStrings.Direction, () => LocalizedStrings.OrderSide, (i, v) => i.Side = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsSystem), () => LocalizedStrings.System, () => LocalizedStrings.IsSystemTrade, (i, v) => i.IsSystem = v));
					fields.Add(new FieldMapping<ExecutionMessage, OrderStates>(nameof(ExecutionMessage.OrderState), () => LocalizedStrings.Action, () => LocalizedStrings.OrderStateDesc, (i, v) => i.OrderState = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, TimeInForce>(nameof(ExecutionMessage.TimeInForce), () => LocalizedStrings.TimeInForce, () => LocalizedStrings.ExecutionConditionDesc, (i, v) => i.TimeInForce = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.TradeId), () => LocalizedStrings.IdTrade, () => LocalizedStrings.TradeId, (i, v) => i.TradeId = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.TradePrice), () => LocalizedStrings.TradePrice, () => LocalizedStrings.TradePrice, (i, v) => i.TradePrice = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OpenInterest), () => LocalizedStrings.OpenInterest, () => LocalizedStrings.OpenInterestDesc, (i, v) => i.OpenInterest = v));

					break;
				}
				case ExecutionTypes.Transaction:
				{
					fields.Add(new FieldMapping<ExecutionMessage, string>(GetSecurityCodeField(nameof(ExecutionMessage.SecurityId)), () => LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, string>(GetBoardCodeField(nameof(ExecutionMessage.SecurityId)), () => LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, DateTimeOffset>(GetDateField(nameof(ExecutionMessage.ServerTime)), () => LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, TimeSpan>(GetTimeOfDayField(nameof(ExecutionMessage.ServerTime)), () => LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
					fields.Add(new FieldMapping<ExecutionMessage, string>(nameof(ExecutionMessage.PortfolioName), () => LocalizedStrings.Portfolio, () => LocalizedStrings.PortfolioName, (i, v) => i.PortfolioName = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.TransactionId), () => LocalizedStrings.TransactionId, () => LocalizedStrings.TransactionId, (i, v) => i.TransactionId = v));
					fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.OrderId), () => LocalizedStrings.Id, () => LocalizedStrings.OrderId, (i, v) => i.OrderId = v));
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OrderPrice), () => LocalizedStrings.Price, () => LocalizedStrings.OrderPrice, (i, v) => { i.OrderPrice = v; i.HasOrderInfo = true; }) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.OrderVolume), () => LocalizedStrings.Volume, () => LocalizedStrings.OrderVolume, (i, v) => i.OrderVolume = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.Balance), () => LocalizedStrings.Balance, () => LocalizedStrings.OrderBalance, (i, v) => i.Balance = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, Sides>(nameof(ExecutionMessage.Side), () => LocalizedStrings.Initiator, () => LocalizedStrings.OrderSide, (i, v) => i.Side = v));
					fields.Add(new FieldMapping<ExecutionMessage, OrderTypes>(nameof(ExecutionMessage.OrderType), () => LocalizedStrings.OrderType, () => LocalizedStrings.OrderTypeDesc, (i, v) => i.OrderType = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, OrderStates>(nameof(ExecutionMessage.OrderState), () => LocalizedStrings.State, () => LocalizedStrings.OrderStateDesc, (i, v) => i.OrderState = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, TimeInForce>(nameof(ExecutionMessage.TimeInForce), () => LocalizedStrings.TimeInForce, () => LocalizedStrings.ExecutionConditionDesc, (i, v) => i.TimeInForce = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.TradeId), () => LocalizedStrings.IdTrade, () => LocalizedStrings.TradeId, (i, v) => i.TradeId = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, string>(nameof(ExecutionMessage.UserOrderId), () => LocalizedStrings.UserId, () => LocalizedStrings.UserOrderId, (i, v) => i.UserOrderId = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, string>(nameof(ExecutionMessage.StrategyId), () => LocalizedStrings.Strategy, () => LocalizedStrings.Strategy, (i, v) => i.StrategyId = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, CurrencyTypes>(nameof(ExecutionMessage.Currency), () => LocalizedStrings.Currency, () => LocalizedStrings.CurrencyDesc, (i, v) => i.Currency = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsMarketMaker), () => LocalizedStrings.MarketMaker, () => LocalizedStrings.MarketMakerOrder, (i, v) => i.IsMarketMaker = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, MarginModes>(nameof(ExecutionMessage.MarginMode), () => LocalizedStrings.Margin, () => LocalizedStrings.MarginMode, (i, v) => i.MarginMode = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.IsManual), () => LocalizedStrings.Manual, () => LocalizedStrings.IsOrderManual, (i, v) => i.IsManual = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.MinVolume), () => LocalizedStrings.MinVolume, () => LocalizedStrings.MinVolumeDesc, (i, v) => i.MinVolume = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, OrderPositionEffects>(nameof(ExecutionMessage.PositionEffect), () => LocalizedStrings.PositionEffect, () => LocalizedStrings.PositionEffectDesc, (i, v) => i.PositionEffect = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.PostOnly), () => LocalizedStrings.PostOnly, () => LocalizedStrings.PostOnlyOrder, (i, v) => i.PostOnly = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, bool>(nameof(ExecutionMessage.Initiator), () => LocalizedStrings.Initiator, () => LocalizedStrings.InitiatorTrade, (i, v) => i.Initiator = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, long>(nameof(ExecutionMessage.SeqNum), () => LocalizedStrings.SeqNum, () => LocalizedStrings.SequenceNumber, (i, v) => i.SeqNum = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, int>(nameof(ExecutionMessage.Leverage), () => LocalizedStrings.Leverage, () => LocalizedStrings.MarginLeverage, (i, v) => i.Leverage = v) { IsRequired = false });
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.TradePrice), () => LocalizedStrings.Price, () => LocalizedStrings.Price, (i, v) => i.TradePrice = v) { IsRequired = true });
					fields.Add(new FieldMapping<ExecutionMessage, decimal>(nameof(ExecutionMessage.TradeVolume), () => LocalizedStrings.Volume, () => LocalizedStrings.Volume, (i, v) => i.TradeVolume = v) { IsRequired = true });

					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(dataType), msgType, LocalizedStrings.InvalidValue);
			}
		}
		else if (msgType == typeof(CandleMessage) || msgType.IsCandleMessage())
		{
			fields.Add(new FieldMapping<CandleMessage, string>(GetSecurityCodeField(nameof(CandleMessage.SecurityId)), () => LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
			fields.Add(new FieldMapping<CandleMessage, string>(GetBoardCodeField(nameof(CandleMessage.SecurityId)), () => LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

			fields.Add(new FieldMapping<CandleMessage, DateTimeOffset>(GetDateField(nameof(CandleMessage.OpenTime)), () => LocalizedStrings.Date, dateDescr, (i, v) =>
			{
				i.OpenTime = v + i.OpenTime.TimeOfDay;

				if (i.CloseTime != default)
					i.CloseTime = v + i.CloseTime.TimeOfDay;
			}) { IsRequired = true });
			fields.Add(new FieldMapping<CandleMessage, TimeSpan>(GetTimeOfDayField(nameof(CandleMessage.OpenTime)), () => LocalizedStrings.CandleOpenTime, () => LocalizedStrings.CandleOpenTime, (i, v) => i.OpenTime += v));
			fields.Add(new FieldMapping<CandleMessage, TimeSpan>(nameof(CandleMessage.CloseTime), () => LocalizedStrings.CandleCloseTime, () => LocalizedStrings.CandleCloseTime, (i, v) =>
			{
				if (i.CloseTime == default)
					i.CloseTime = i.OpenTime - i.OpenTime.TimeOfDay + v;
				else
					i.CloseTime += v;
			}));
			fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.OpenInterest), () => LocalizedStrings.OpenInterest, () => LocalizedStrings.OpenInterest, (i, v) => i.OpenInterest = v));
			fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.OpenPrice), () => "O", () => LocalizedStrings.CandleOpenPrice, (i, v) => i.OpenPrice = v) { IsRequired = true });
			fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.HighPrice), () => "H", () => LocalizedStrings.HighPriceOfCandle, (i, v) => i.HighPrice = v) { IsRequired = true });
			fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.LowPrice), () => "L", () => LocalizedStrings.LowPriceOfCandle, (i, v) => i.LowPrice = v) { IsRequired = true });
			fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.ClosePrice), () => "C", () => LocalizedStrings.ClosePriceOfCandle, (i, v) => i.ClosePrice = v) { IsRequired = true });
			fields.Add(new FieldMapping<CandleMessage, decimal>(nameof(CandleMessage.TotalVolume), () => "V", () => LocalizedStrings.TotalCandleVolume, (i, v) => i.TotalVolume = v) { IsRequired = true });
			fields.Add(new FieldMapping<CandleMessage, int>(nameof(CandleMessage.UpTicks), () => LocalizedStrings.TickUp, () => LocalizedStrings.TickUpCount, (i, v) => i.UpTicks = v) { IsRequired = false });
			fields.Add(new FieldMapping<CandleMessage, int>(nameof(CandleMessage.DownTicks), () => LocalizedStrings.TickDown, () => LocalizedStrings.TickDownCount, (i, v) => i.DownTicks = v) { IsRequired = false });
			fields.Add(new FieldMapping<CandleMessage, int>(nameof(CandleMessage.TotalTicks), () => LocalizedStrings.Ticks, () => LocalizedStrings.TickCount, (i, v) => i.TotalTicks = v) { IsRequired = false });
		}
		else if (msgType == typeof(QuoteChangeMessage))
		{
			fields.Add(new FieldMapping<TimeQuoteChange, string>(GetSecurityCodeField(nameof(TimeQuoteChange.SecurityId)), () => LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
			fields.Add(new FieldMapping<TimeQuoteChange, string>(GetBoardCodeField(nameof(TimeQuoteChange.SecurityId)), () => LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

			fields.Add(new FieldMapping<TimeQuoteChange, DateTimeOffset>(GetDateField(nameof(TimeQuoteChange.ServerTime)), () => LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
			fields.Add(new FieldMapping<TimeQuoteChange, TimeSpan>(GetTimeOfDayField(nameof(TimeQuoteChange.ServerTime)), () => LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
			fields.Add(new FieldMapping<TimeQuoteChange, decimal>(nameof(QuoteChange.Price), () => LocalizedStrings.Price, () => LocalizedStrings.QuotePrice, (i, v) =>
			{
				var q = i.Quote;
				q.Price = v;
				i.Quote = q;
			}) { IsRequired = true });
			fields.Add(new FieldMapping<TimeQuoteChange, decimal>(nameof(QuoteChange.Volume), () => LocalizedStrings.Volume, () => LocalizedStrings.QuoteVolume, (i, v) =>
			{
				var q = i.Quote;
				q.Volume = v;
				i.Quote = q;
			}) { IsRequired = true });
			fields.Add(new FieldMapping<TimeQuoteChange, Sides>(nameof(TimeQuoteChange.Side), () => LocalizedStrings.Direction, () => LocalizedStrings.DirBuyOrSell, (i, v) => i.Side = v) { IsRequired = true });
			fields.Add(new FieldMapping<TimeQuoteChange, int>(nameof(QuoteChange.OrdersCount), () => LocalizedStrings.Orders, () => LocalizedStrings.OrdersCount, (i, v) =>
			{
				var q = i.Quote;
				q.OrdersCount = v;
				i.Quote = q;
			}));
			fields.Add(new FieldMapping<TimeQuoteChange, QuoteConditions>(nameof(QuoteChange.Condition), () => LocalizedStrings.Condition, () => LocalizedStrings.QuoteCondition, (i, v) =>
			{
				var q = i.Quote;
				q.Condition = v;
				i.Quote = q;
			}));
			fields.Add(new FieldMapping<TimeQuoteChange, int>(nameof(QuoteChange.StartPosition), () => LocalizedStrings.Start, () => LocalizedStrings.Start, (i, v) =>
			{
				var q = i.Quote;
				q.StartPosition = v;
				i.Quote = q;
			}));
			fields.Add(new FieldMapping<TimeQuoteChange, int>(nameof(QuoteChange.EndPosition), () => LocalizedStrings.End, () => LocalizedStrings.End, (i, v) =>
			{
				var q = i.Quote;
				q.EndPosition = v;
				i.Quote = q;
			}));
			fields.Add(new FieldMapping<TimeQuoteChange, QuoteChangeActions>(nameof(QuoteChange.Action), () => LocalizedStrings.Action, () => LocalizedStrings.Action, (i, v) =>
			{
				var q = i.Quote;
				q.Action = v;
				i.Quote = q;
			}));
		}
		else if (msgType == typeof(Level1ChangeMessage))
		{
			fields.Add(new FieldMapping<Level1ChangeMessage, string>(GetSecurityCodeField(nameof(Level1ChangeMessage.SecurityId)), () => LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
			fields.Add(new FieldMapping<Level1ChangeMessage, string>(GetBoardCodeField(nameof(Level1ChangeMessage.SecurityId)), () => LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

			fields.Add(new FieldMapping<Level1ChangeMessage, DateTimeOffset>(GetDateField(nameof(Level1ChangeMessage.ServerTime)), () => LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
			fields.Add(new FieldMapping<Level1ChangeMessage, TimeSpan>(GetTimeOfDayField(nameof(Level1ChangeMessage.ServerTime)), () => LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));

			foreach (var f in Enumerator.GetValues<Level1Fields>().ExcludeObsolete())
			{
				var field = f;

				fields.Add(new FieldMapping<Level1ChangeMessage, object>(GetChangesField(field), () => field.GetDisplayName(), () => field.GetFieldDescription(), field.ToType(), (i, v) =>
				{
					if (v is decimal d && d == 0m)
						return;

					i.Changes.Add(field, v);
				}));
			}
		}
		else if (msgType == typeof(PositionChangeMessage))
		{
			fields.Add(new FieldMapping<PositionChangeMessage, string>(GetSecurityCodeField(nameof(PositionChangeMessage.SecurityId)), () => LocalizedStrings.Security, secCodeDescr, SetSecCode) { IsRequired = true });
			fields.Add(new FieldMapping<PositionChangeMessage, string>(GetBoardCodeField(nameof(PositionChangeMessage.SecurityId)), () => LocalizedStrings.Board, boardCodeDescr, SetBoardCode) { IsRequired = true });

			fields.Add(new FieldMapping<PositionChangeMessage, DateTimeOffset>(GetDateField(nameof(PositionChangeMessage.ServerTime)), () => LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
			fields.Add(new FieldMapping<PositionChangeMessage, TimeSpan>(GetTimeOfDayField(nameof(PositionChangeMessage.ServerTime)), () => LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));

			fields.Add(new FieldMapping<PositionChangeMessage, string>(nameof(PositionChangeMessage.StrategyId), () => LocalizedStrings.Strategy, () => LocalizedStrings.Strategy, (i, v) => i.StrategyId = v) { IsRequired = false });
			fields.Add(new FieldMapping<PositionChangeMessage, Sides>(nameof(PositionChangeMessage.Side), () => LocalizedStrings.Side, () => LocalizedStrings.Side, (i, v) => i.Side = v) { IsRequired = false });

			foreach (var f in Enumerator.GetValues<PositionChangeTypes>().Where(l1 => !l1.IsObsolete()))
			{
				var field = f;

				fields.Add(new FieldMapping<PositionChangeMessage, object>(GetChangesField(field), () => field.GetDisplayName(), () => field.GetFieldDescription(), field.ToType(), (i, v) =>
				{
					if (v is decimal d && d == 0m)
						return;

					i.Changes.Add(field, v);
				}));
			}
		}
		else if (msgType == typeof(NewsMessage))
		{
			fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Id), () => LocalizedStrings.Id, () => LocalizedStrings.Id, (i, v) => i.Id = v) { IsRequired = true });
			fields.Add(new FieldMapping<NewsMessage, string>(GetSecurityCodeField(nameof(NewsMessage.SecurityId)), () => LocalizedStrings.Security, secCodeDescr, (i, v) => { i.SecurityId = new SecurityId { SecurityCode = v }; }));
			fields.Add(new FieldMapping<NewsMessage, string>(GetBoardCodeField(nameof(NewsMessage.SecurityId)), () => LocalizedStrings.Board, boardCodeDescr, (i, v) => i.BoardCode = v));
			fields.Add(new FieldMapping<NewsMessage, DateTimeOffset>(GetDateField(nameof(NewsMessage.ServerTime)), () => LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
			fields.Add(new FieldMapping<NewsMessage, TimeSpan>(GetTimeOfDayField(nameof(NewsMessage.ServerTime)), () => LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
			fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Headline), () => LocalizedStrings.Header, () => LocalizedStrings.Header, (i, v) => i.Headline = v));
			fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Story), () => LocalizedStrings.Text, () => LocalizedStrings.NewsText, (i, v) => i.Story = v));
			fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Source), () => LocalizedStrings.Source, () => LocalizedStrings.NewsSource, (i, v) => i.Source = v));
			fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Url), () => LocalizedStrings.Link, () => LocalizedStrings.NewsLink, (i, v) => i.Url = v));
			fields.Add(new FieldMapping<NewsMessage, NewsPriorities>(nameof(NewsMessage.Priority), () => LocalizedStrings.Priority, () => LocalizedStrings.NewsPriority, (i, v) => i.Priority = v));
			fields.Add(new FieldMapping<NewsMessage, string>(nameof(NewsMessage.Language), () => LocalizedStrings.Language, () => LocalizedStrings.Language, (i, v) => i.Language = v));
		}
		else if (msgType == typeof(BoardMessage))
		{
			fields.Add(new FieldMapping<BoardMessage, string>(nameof(BoardMessage.Code), () => LocalizedStrings.Board, () => LocalizedStrings.BoardCode, (i, v) => i.Code = v) { IsRequired = true });
			fields.Add(new FieldMapping<BoardMessage, string>(nameof(BoardMessage.ExchangeCode), () => LocalizedStrings.Exchange, () => LocalizedStrings.ExchangeBoardDesc, (i, v) => i.ExchangeCode = v) { IsRequired = true });
		}
		else if (msgType == typeof(BoardStateMessage))
		{
			fields.Add(new FieldMapping<BoardStateMessage, DateTimeOffset>(GetDateField(nameof(BoardStateMessage.ServerTime)), () => LocalizedStrings.Date, dateDescr, (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
			fields.Add(new FieldMapping<BoardStateMessage, TimeSpan>(GetTimeOfDayField(nameof(BoardStateMessage.ServerTime)), () => LocalizedStrings.Time, timeDescr, (i, v) => i.ServerTime += v));
			fields.Add(new FieldMapping<BoardStateMessage, string>(nameof(BoardStateMessage.BoardCode), () => LocalizedStrings.Board, () => LocalizedStrings.BoardCode, (i, v) => i.BoardCode = v));
			fields.Add(new FieldMapping<BoardStateMessage, SessionStates>(nameof(BoardStateMessage.State), () => LocalizedStrings.State, () => LocalizedStrings.State, (i, v) => i.State = v));
		}
		else
			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);

		return fields;
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