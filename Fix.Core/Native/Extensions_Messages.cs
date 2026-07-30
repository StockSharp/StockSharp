namespace StockSharp.Fix.Native;

static partial class Extensions
{
	/// <summary>
	/// Write <see cref="SecurityMessage"/> list.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="dateParser">Time parser.</param>
	/// <param name="convertToLatin">Convert texts to latin.</param>
	/// <param name="requestId"><see cref="FixTags.MDReqID"/> value.</param>
	/// <param name="responseId"><see cref="FixTags.MDResponseID"/> value.</param>
	/// <param name="securityMessages">Securities.</param>
	/// <param name="lastFragment">Last message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteSecurityListAsync(this IFixWriter writer, FastDateTimeParser dateParser, bool convertToLatin, string requestId, string responseId, ICollection<SecurityMessage> securityMessages, bool lastFragment, CancellationToken cancellationToken)
	{
		if (!requestId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.SecurityReqID, cancellationToken);
			await writer.WriteAsync(requestId, cancellationToken);
		}

		if (!responseId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.SecurityResponseID, cancellationToken);
			await writer.WriteAsync(responseId, cancellationToken);
		}

		await writer.WriteAsync(FixTags.SecurityRequestResult, cancellationToken);
		await writer.WriteAsync((int)SecurityRequestResult.ValidRequest, cancellationToken);

		await writer.WriteAsync(FixTags.LastFragment, cancellationToken);
		await writer.WriteAsync(lastFragment, cancellationToken);

		if (securityMessages.Count == 0)
			return;

		await writer.WriteAsync(FixTags.NoRelatedSym, cancellationToken);
		await writer.WriteAsync(securityMessages.Count, cancellationToken);

		foreach (var securityMessage in securityMessages)
		{
			var secId = securityMessage.SecurityId;

			await writer.WriteAsync(FixTags.Symbol, cancellationToken);
			await writer.WriteAsync(secId.SecurityCode, cancellationToken);

			await writer.WriteAsync(FixTags.SecurityExchange, cancellationToken);
			await writer.WriteAsync(secId.BoardCode, cancellationToken);

			if (!secId.Isin.IsEmpty())
			{
				await writer.WriteAsync(FixTags.SecurityID, cancellationToken);
				await writer.WriteAsync(secId.Isin, cancellationToken);

				await writer.WriteAsync(FixTags.IDSource, cancellationToken);
				await writer.WriteAsync(SecurityIDSource.IsinNumber, cancellationToken);
			}

			if (!securityMessage.Name.IsEmpty())
			{
				await writer.WriteAsync(FixTags.SecurityDesc, cancellationToken);
				await writer.WriteAsync(convertToLatin ? securityMessage.Name.ToLatin() : securityMessage.Name, cancellationToken);
			}

			if (!securityMessage.Class.IsEmpty())
			{
				await writer.WriteAsync(FixTags.Product, cancellationToken);
				await writer.WriteAsync(securityMessage.Class, cancellationToken);
			}

			if (!securityMessage.CfiCode.IsEmpty())
			{
				await writer.WriteAsync(FixTags.CFICode, cancellationToken);
				await writer.WriteAsync(securityMessage.CfiCode, cancellationToken);
			}

			if (securityMessage.VolumeStep != null)
			{
				await writer.WriteAsync(FixTags.RoundLot, cancellationToken);
				await writer.WriteAsync(securityMessage.VolumeStep.Value, cancellationToken);
			}

			if (securityMessage.PriceStep != null)
			{
				await writer.WriteAsync(FixTags.MinPriceIncrement, cancellationToken);
				await writer.WriteAsync(securityMessage.PriceStep.Value, cancellationToken);
			}

			if (securityMessage.Multiplier != null)
			{
				await writer.WriteAsync(FixTags.ContractMultiplier, cancellationToken);
				await writer.WriteAsync(securityMessage.Multiplier.Value, cancellationToken);
			}

			if (securityMessage.Decimals != null)
			{
				await writer.WriteAsync(FixTags.NoInstrAttrib, cancellationToken);
				await writer.WriteAsync(1, cancellationToken);

				await writer.WriteAsync(FixTags.InstrAttribType, cancellationToken);
				await writer.WriteAsync(27, cancellationToken);

				await writer.WriteAsync(FixTags.InstrAttribValue, cancellationToken);
				await writer.WriteAsync(securityMessage.Decimals.To<string>(), cancellationToken);
			}

			if (securityMessage.SecurityType != null)
			{
				await writer.WriteAsync(FixTags.SecurityType, cancellationToken);
				await writer.WriteAsync(securityMessage.SecurityType.Value.ToFix(), cancellationToken);
			}

			if (securityMessage.Currency != null)
			{
				await writer.WriteAsync(FixTags.Currency, cancellationToken);
				await writer.WriteAsync(securityMessage.Currency.Value.ToFix(), cancellationToken);
			}

			if (securityMessage.ExpiryDate != null)
			{
				await writer.WriteAsync(FixTags.EndDate, cancellationToken);
				await writer.WriteUtcAsync(securityMessage.ExpiryDate.Value, dateParser, cancellationToken);
			}

			if (securityMessage.SettlementDate != null)
			{
				await writer.WriteAsync(FixTags.ContractSettlMonth, cancellationToken);
				await writer.WriteUtcAsync(securityMessage.SettlementDate.Value, dateParser, cancellationToken);
			}

			if (!securityMessage.GetUnderlyingCode().IsEmpty())
			{
				await writer.WriteAsync(FixTags.SymbolSfx, cancellationToken);
				await writer.WriteAsync(securityMessage.GetUnderlyingCode(), cancellationToken);
			}

			if (securityMessage.IssueDate != null)
			{
				await writer.WriteAsync(FixTags.IssueDate, cancellationToken);
				await writer.WriteUtcAsync(securityMessage.IssueDate.Value, dateParser, cancellationToken);
			}

			if (securityMessage.IssueSize != null)
			{
				await writer.WriteAsync(FixTags.IssueSize, cancellationToken);
				await writer.WriteAsync(securityMessage.IssueSize.Value, cancellationToken);
			}

			if (securityMessage.MinVolume != null)
			{
				await writer.WriteAsync(FixTags.MinQty, cancellationToken);
				await writer.WriteAsync(securityMessage.MinVolume.Value, cancellationToken);
			}

			if (securityMessage.MaxVolume != null)
			{
				await writer.WriteAsync(FixTags.MaxTradeVol, cancellationToken);
				await writer.WriteAsync(securityMessage.MaxVolume.Value, cancellationToken);
			}

			if (securityMessage.Shortable != null)
			{
				await writer.WriteAsync(FixTags.Shortable, cancellationToken);
				await writer.WriteAsync(securityMessage.Shortable.Value, cancellationToken);
			}

			if (securityMessage.SecurityType == SecurityTypes.Option)
			{
				if (securityMessage.Strike != null)
				{
					await writer.WriteAsync(FixTags.StrikePrice, cancellationToken);
					await writer.WriteAsync(securityMessage.Strike.Value, cancellationToken);
				}

				if (securityMessage.OptionType != null)
				{
					await writer.WriteAsync(FixTags.PutOrCall, cancellationToken);
					await writer.WriteAsync(securityMessage.OptionType.Value.ToFix(), cancellationToken);
				}
			}

			if (securityMessage.FaceValue != null)
			{
				await writer.WriteAsync(FixTags.FaceValue, cancellationToken);
				await writer.WriteAsync(securityMessage.FaceValue.Value, cancellationToken);
			}

			if (securityMessage.OptionStyle != null)
			{
				await writer.WriteAsync(FixTags.OptionStyle, cancellationToken);
				await writer.WriteAsync(securityMessage.OptionStyle.Value.ToFix(), cancellationToken);
			}

			if (securityMessage.SettlementType != null)
			{
				await writer.WriteAsync(FixTags.SettlType, cancellationToken);
				await writer.WriteAsync(securityMessage.SettlementType.Value.ToFix(), cancellationToken);
			}

			if (securityMessage.IsBasket())
			{
				await writer.WriteAsync(FixTags.Formula, cancellationToken);
				await writer.WriteAsync($"{securityMessage.BasketCode}_{securityMessage.BasketExpression}", cancellationToken);
			}

			if (securityMessage.PrimaryId != default)
			{
				await writer.WriteAsync(FixTags.PrimaryCode, cancellationToken);
				await writer.WriteAsync(securityMessage.PrimaryId.SecurityCode, cancellationToken);

				await writer.WriteAsync(FixTags.PrimaryBoard, cancellationToken);
				await writer.WriteAsync(securityMessage.PrimaryId.BoardCode, cancellationToken);
			}
		}
	}

	/// <summary>
	/// Read security message (async version).
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="dateParser">Date parser.</param>
	/// <param name="yearMonthParser">Year-month parser.</param>
	/// <param name="totalSecCountByRequestId">Total security count by request ID.</param>
	/// <param name="initSecId">Initialize security ID callback.</param>
	/// <param name="errorHandler">Error handler.</param>
	/// <param name="customTagHandler">Custom tag handler.</param>
	/// <param name="getSecurityType">Get security type callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Async enumerable of security messages with metadata.</returns>
	public static async IAsyncEnumerable<(SecurityMessage secMsg, bool? lastFragment, long? securityReqId, bool isError, string reason, string text)> ReadSecurityMessageAsync(
		this IFixReader reader,
		FastDateTimeParser dateParser,
		FastDateTimeParser yearMonthParser,
		IDictionary<long, RefPair<int, int>> totalSecCountByRequestId,
		Action<SecurityMessage, string, string, string, string> initSecId,
		Action<Exception> errorHandler,
		Func<FixTags, IFixReader, SecurityMessage, CancellationToken, ValueTask<bool>> customTagHandler,
		Func<string, SecurityTypes?> getSecurityType,
		[EnumeratorCancellation]CancellationToken cancellationToken)
	{
		if (reader is null)						throw new ArgumentNullException(nameof(reader));
		if (dateParser is null)					throw new ArgumentNullException(nameof(dateParser));
		if (yearMonthParser is null)			throw new ArgumentNullException(nameof(yearMonthParser));
		if (totalSecCountByRequestId is null)	throw new ArgumentNullException(nameof(totalSecCountByRequestId));
		if (initSecId is null)					throw new ArgumentNullException(nameof(initSecId));
		if (errorHandler is null)				throw new ArgumentNullException(nameof(errorHandler));
		if (customTagHandler is null)			throw new ArgumentNullException(nameof(customTagHandler));
		if (getSecurityType is null)			throw new ArgumentNullException(nameof(getSecurityType));

		var secMsg = new SecurityMessage();
		var messages = new List<SecurityMessage>();

		long? securityReqId = null;
		SecurityResponseType? securityResponseType = null;
		SecurityRequestResult? securityRequestResult = null;
		int? instrAttribType = null;
		string symbol = null;
		string securityExchange = null;
		string exDestination = null;
		string text = null;
		string idValue = null;
		string idSource = null;
		bool? lastFragment = null;
		int? totalNumSecurities = null;
		int? noRelatedSym = null;

		var count = 0;

		var tags = new HashSet<FixTags>();

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			if (!tags.Add(tag))
			{
				if (!symbol.IsEmpty())
				{
					initSecId(secMsg, symbol, securityExchange ?? exDestination, idSource, idValue);
					secMsg.OriginalTransactionId = securityReqId ?? 0;

					count++;
					messages.Add(secMsg);

					symbol = null;
					securityExchange = null;
					exDestination = null;
					idSource = null;
					idValue = null;
					instrAttribType = null;
					secMsg = new SecurityMessage();
				}

				tags.Clear();
				tags.Add(tag);
			}

			if (await customTagHandler(tag, reader, secMsg, cancellationToken))
			{
				if (symbol.IsEmpty())
					symbol = secMsg.SecurityId.SecurityCode;

				if (securityExchange.IsEmpty())
					securityExchange = secMsg.SecurityId.BoardCode;

				return true;
			}

			switch (tag)
			{
				case FixTags.LastFragment:
					lastFragment = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.TotNoRelatedSym:
					totalNumSecurities = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.NoRelatedSym:
					noRelatedSym = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.SecurityResponseType:
					securityResponseType = (SecurityResponseType)await reader.ReadIntAsync(ct);
					return true;
				case FixTags.Text:
					text = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityRequestResult:
					securityRequestResult = (SecurityRequestResult)await reader.ReadIntAsync(ct);
					return true;
				case FixTags.Symbol:
					symbol = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityExchange:
					securityExchange = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.ExDestination:
					exDestination = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityType:
					secMsg.SecurityType = getSecurityType(await reader.ReadStringAsync(ct));
					return true;
				case FixTags.SecurityDesc:
					secMsg.Name = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityID:
					idValue = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.IDSource:
					idSource = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.RoundLot:
					secMsg.VolumeStep = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.MinTradeVol:
				case FixTags.MinPriceIncrement:
					secMsg.PriceStep = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.SettlDate:
					secMsg.SettlementDate = await reader.ReadUtcAsync(dateParser, ct);
					return true;
				case FixTags.RatioQty:
				case FixTags.ContractMultiplier:
				case FixTags.Factor:
					secMsg.Multiplier = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.SecurityReqID:
					securityReqId = (await reader.ReadStringAsync(ct)).To<long>();
					return true;
				case FixTags.Currency:
					secMsg.Currency = (await reader.ReadStringAsync(ct)).FromMicexCurrencyName(errorHandler);
					return true;
				case FixTags.SettlCurrency:
					var curr = await reader.ReadStringAsync(ct);
					secMsg.Currency ??= curr.FromMicexCurrencyName(errorHandler);
					return true;
				case FixTags.EndDate:
					secMsg.ExpiryDate = await reader.ReadUtcAsync(dateParser, ct);
					return true;
				case FixTags.ContractSettlMonth:
					secMsg.SettlementDate = await reader.ReadUtcAsync(dateParser, ct);
					return true;
				case FixTags.SymbolSfx:
				case FixTags.UnderlyingSymbol:
				case FixTags.UnderlyingSecurityID:
					secMsg.TryFillUnderlyingId(await reader.ReadStringAsync(ct));
					return true;
				case FixTags.UnderlyingSecurityType:
					secMsg.UnderlyingSecurityType = (await reader.ReadStringAsync(ct)).FromFixType();
					return true;
				case FixTags.StrikePrice:
					secMsg.Strike = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.PutOrCall:
					secMsg.OptionType = ((PutOrCall)await reader.ReadIntAsync(ct)).FromFixOptionType();
					return true;
				case FixTags.CFICode:
					secMsg.CfiCode = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.MinQty:
					secMsg.MinVolume = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.MaxTradeVol:
					secMsg.MaxVolume = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.Shortable:
					secMsg.Shortable = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.MaturityMonthYear:
					secMsg.IssueDate = await reader.ReadUtcAsync(yearMonthParser, ct);
					return true;
				case FixTags.IssueDate:
					secMsg.IssueDate = await reader.ReadUtcAsync(dateParser, ct);
					return true;
				case FixTags.IssueSize:
					secMsg.IssueSize = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.FaceValue:
					secMsg.FaceValue = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.SettlType:
					secMsg.SettlementType = (await reader.ReadIntAsync(ct)).FromFixSettlType();
					return true;
				case FixTags.OptionStyle:
					secMsg.OptionStyle = (await reader.ReadIntAsync(ct)).FromFixOptionStyle();
					return true;
				case FixTags.PrimaryCode:
				{
					var primaryId = secMsg.PrimaryId;
					primaryId.SecurityCode = await reader.ReadStringAsync(ct);
					secMsg.PrimaryId = primaryId;
					return true;
				}
				case FixTags.PrimaryBoard:
				{
					var primaryId = secMsg.PrimaryId;
					primaryId.BoardCode = await reader.ReadStringAsync(ct);
					secMsg.PrimaryId = primaryId;
					return true;
				}
				case FixTags.InstrAttribType:
					instrAttribType = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.InstrAttribValue:
				{
					var value = await reader.ReadStringAsync(ct);

					switch (instrAttribType)
					{
						case 27:
							secMsg.Decimals = value.To<int>();
							break;
					}

					instrAttribType = null;
					return true;
				}
				case FixTags.Formula:
					var formula = await reader.ReadStringAsync(ct);
					secMsg.BasketCode = formula.Substring(0, 2);
					secMsg.BasketExpression = formula.Substring(3);
					return true;

				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		var isError = securityResponseType == SecurityResponseType.RejectSecurityProposal
			|| (securityRequestResult != null && securityRequestResult != SecurityRequestResult.ValidRequest &&
				securityRequestResult != SecurityRequestResult.NoInstrumentsFoundThatMatchSelectionCriteria);

		if (!isError)
		{
			//if (!legSymbol.IsEmpty())
			//{
			//	initSecId(leg, legSymbol, legSecurityExchange, null, null);
			//	legs.Add(leg);
			//}

			if (!symbol.IsEmpty())
			{
				secMsg.OriginalTransactionId = securityReqId ?? 0;

				initSecId(secMsg, symbol, securityExchange ?? exDestination, idSource, idValue);

				count++;
				messages.Add(secMsg);
			}

			if (securityReqId != null && totalSecCountByRequestId != null && totalNumSecurities != null && lastFragment == null)
			{
				var tuple = totalSecCountByRequestId.SafeAdd(securityReqId.Value, key => RefTuple.Create(totalNumSecurities.Value, 0));
				tuple.Second += noRelatedSym ?? count;

				if (tuple.First == tuple.Second)
				{
					totalSecCountByRequestId.Remove(securityReqId.Value);
					lastFragment = true;
				}
			}
		}

		var reason = securityRequestResult.To<string>() ?? securityResponseType.To<string>();

		if (messages.Count > 0)
		{
			foreach (var msg in messages)
				yield return (msg, lastFragment, securityReqId, isError, reason, text);
		}
		else if (lastFragment == true || isError)
		{
			// Yield with null message to signal lastFragment/error even when no securities returned
			yield return (null, lastFragment, securityReqId, isError, reason, text);
		}
	}

	/// <summary>
	/// Write parameters.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="parameters">Parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteParametersAsync(this IFixWriter writer, IDictionary<string, string> parameters, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.NoStrategyParameters, cancellationToken);
		await writer.WriteAsync(parameters.Count, cancellationToken);

		if (parameters.Count == 0)
			return;

		foreach (var param in parameters)
		{
			await writer.WriteAsync(FixTags.StrategyParameterName, cancellationToken);
			await writer.WriteAsync(param.Key, cancellationToken);

			if (param.Value.IsEmpty())
				continue;

			await writer.WriteAsync(FixTags.StrategyParameterValue, cancellationToken);
			await writer.WriteAsync(param.Value, cancellationToken);
		}
	}

	/// <summary>
	/// Write <see cref="MarketDataMessage"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="mdMsg">Message.</param>
	/// <param name="requestId"><see cref="FixTags.MDReqID"/> value.</param>
	/// <param name="responseId"><see cref="FixTags.MDResponseID"/> value.</param>
	/// <param name="dataBoundDateParser">Time parser.</param>
	/// <param name="writeSecurityId">Write security id.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteMarketDataMessageAsync(this IFixWriter writer, MarketDataMessage mdMsg, string requestId, string responseId, FastDateTimeParser dataBoundDateParser, Func<IFixWriter, MarketDataMessage, CancellationToken, ValueTask> writeSecurityId, CancellationToken cancellationToken)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));

		if (mdMsg == null)
			throw new ArgumentNullException(nameof(mdMsg));

		if (dataBoundDateParser == null)
			throw new ArgumentNullException(nameof(dataBoundDateParser));

		if (writeSecurityId == null)
			throw new ArgumentNullException(nameof(writeSecurityId));

		await writer.WriteAsync(FixTags.MDReqID, cancellationToken);
		await writer.WriteAsync(requestId, cancellationToken);

		if (!responseId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.MDResponseID, cancellationToken);
			await writer.WriteAsync(responseId, cancellationToken);
		}

		await writer.WriteAsync(FixTags.SubscriptionRequestType, cancellationToken);
		await writer.WriteAsync(mdMsg.GetSubscriptionType(), cancellationToken);

		if (mdMsg.IsSubscribe)
		{
			await writer.WriteAsync(FixTags.MDUpdateType, cancellationToken);
			await writer.WriteAsync((int)MDUpdateType.IncrementalRefresh, cancellationToken);
		}

		char[] types;
		string arg = null;

		if (mdMsg.DataType2 == DataType.MarketDepth)
		{
			await writer.WriteAsync(FixTags.MarketDepth, cancellationToken);
			/* 0 - full depth */
			await writer.WriteAsync(mdMsg.MaxDepth ?? 0, cancellationToken);

			types = [MDEntryType.Bid, MDEntryType.Offer];
		}
		else
			types = [mdMsg.DataType2.ToFixMDType(out arg)];

		await writer.WriteAsync(FixTags.NoMDEntryTypes, cancellationToken);
		await writer.WriteAsync(types.Length, cancellationToken);

		foreach (var entryType in types)
		{
			await writer.WriteAsync(FixTags.MDEntryType, cancellationToken);
			await writer.WriteAsync(entryType, cancellationToken);

			if (!arg.IsEmpty())
			{
				await writer.WriteAsync(FixTags.MDEntryArg, cancellationToken);
				await writer.WriteAsync(arg, cancellationToken);
			}
		}

		if (mdMsg.DataType2.IsCandles)
		{
			//await writer.WriteAsync(FixTags.MDEntryArg, cancellationToken);
			//await writer.WriteAsync(mdMsg.DataType2.CandleArgToFolderName(), cancellationToken);

			if (mdMsg.AllowBuildFromSmallerTimeFrame)
			{
				await writer.WriteAsync(FixTags.AllowBuildFromSmallerTimeFrame, cancellationToken);
				await writer.WriteAsync(mdMsg.AllowBuildFromSmallerTimeFrame, cancellationToken);
			}

			if (mdMsg.IsCalcVolumeProfile)
			{
				await writer.WriteAsync(FixTags.CalcVolumeProfile, cancellationToken);
				await writer.WriteAsync(mdMsg.IsCalcVolumeProfile, cancellationToken);
			}

			if (mdMsg.IsFinishedOnly)
			{
				await writer.WriteAsync(FixTags.FinishedCandles, cancellationToken);
				await writer.WriteAsync(mdMsg.IsFinishedOnly, cancellationToken);
			}
		}

		if (mdMsg.DataType2 != DataType.News || mdMsg.SecurityId != default)
		{
			await writer.WriteAsync(FixTags.NoRelatedSym, cancellationToken);
			await writer.WriteAsync(1, cancellationToken);

			await writeSecurityId(writer, mdMsg, cancellationToken);
		}

		if (mdMsg.From != null)
		{
			await writer.WriteAsync(FixTags.StartDate, cancellationToken);
			await writer.WriteUtcAsync(mdMsg.From.Value, dataBoundDateParser, cancellationToken);
		}

		if (mdMsg.To != null)
		{
			await writer.WriteAsync(FixTags.EndDate, cancellationToken);
			await writer.WriteUtcAsync(mdMsg.To.Value, dataBoundDateParser, cancellationToken);
		}

		if (mdMsg.Skip != null)
		{
			await writer.WriteAsync(FixTags.MarketDataSkip, cancellationToken);
			await writer.WriteAsync(mdMsg.Skip.Value, cancellationToken);
		}

		if (mdMsg.Count != null)
		{
			await writer.WriteAsync(FixTags.MarketDataCount, cancellationToken);
			await writer.WriteAsync(mdMsg.Count.Value, cancellationToken);
		}

		if (mdMsg.BuildMode != MarketDataBuildModes.LoadAndBuild)
		{
			await writer.WriteAsync(FixTags.MarketDataBuildMode, cancellationToken);
			await writer.WriteAsync((int)mdMsg.BuildMode, cancellationToken);
		}

		if (mdMsg.BuildFrom != null)
		{
			await writer.WriteAsync(FixTags.MarketDataBuildFrom, cancellationToken);
			await writer.WriteAsync(mdMsg.BuildFrom.ToFixMDType(out _), cancellationToken);
		}

		if (mdMsg.BuildField != null)
		{
			await writer.WriteAsync(FixTags.MarketDataBuildField, cancellationToken);
			await writer.WriteAsync(mdMsg.BuildField.Value.ToFix(), cancellationToken);
		}

		if (mdMsg.IsRegularTradingHours is bool rth)
		{
			await writer.WriteAsync(FixTags.RegularTradingHours, cancellationToken);
			await writer.WriteAsync(rth, cancellationToken);
		}

		if (mdMsg.Fields is not null)
		{
			var fields = mdMsg.Fields.ToArray();

			await writer.WriteAsync(FixTags.NoMDFields, cancellationToken);
			await writer.WriteAsync(fields.Length, cancellationToken);

			foreach (var field in fields)
			{
				await writer.WriteAsync(FixTags.MDField, cancellationToken);
				await writer.WriteAsync((int)field, cancellationToken);
			}
		}
	}

	/// <summary>
	/// Read market data request from FIX message (async version).
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="dateBoundParser">Date parser.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Market data request or null if read failed.</returns>
	public static async ValueTask<FixMarketDataRequest?> ReadMarketDataRequestAsync(this IFixReader reader, FastDateTimeParser dateBoundParser, CancellationToken cancellationToken)
	{
		if (reader == null)
			throw new ArgumentNullException(nameof(reader));

		if (dateBoundParser == null)
			throw new ArgumentNullException(nameof(dateBoundParser));

		string mdReqId = null;
		string mdResponseId = null;
		char? subscriptionRequestType = null;
		char[] mdEntryTypes = null;
		string[] mdEntryArgs = null;
		var mdEntryTypesIndex = 0;
		var mdEntryArgsIndex = 0;
		string symbol = null;
		string securityExchange = null;
		string cfiCode = null;
		int? marketDepth = null;
		string fromDateStr = null;
		string toDateStr = null;
		bool? allowBuildSmallerTf = null;
		bool? isCalcVolProfile = null;
		bool? isFinishedCandles = null;
		bool? isRth = null;
		int? buildMode = null;
		char? buildFrom = null;
		char? buildField = null;
		long? skip = null;
		long? count = null;
		var fieldsIndex = 0;
		Level1Fields[] fields = null;

		SecurityId[] securityIds = null;
		var securityIdsIndex = 0;

		string[] securityTypes = null;
		var securityTypesIdx = -1;

		void InitSecId()
		{
			securityIds[securityIdsIndex++] = new SecurityId
			{
				SecurityCode = symbol,
				BoardCode = securityExchange,
			};

			symbol = null;
			securityExchange = null;
			cfiCode = null;
		}

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.MDReqID:
					mdReqId = await reader.ReadStringAsync(ct);
					return true;

				case FixTags.MDResponseID:
					mdResponseId = await reader.ReadStringAsync(ct);
					return true;

				case FixTags.SubscriptionRequestType:
					subscriptionRequestType = await reader.ReadCharAsync(ct);
					return true;

				case FixTags.MarketDepth:
					marketDepth = await reader.ReadIntAsync(ct);

					/* 0 - full depth */
					if (marketDepth == 0)
						marketDepth = null;

					return true;

				case FixTags.NoMDEntryTypes:
				{
					var len = await reader.ReadIntAsync(ct);

					mdEntryTypes = new char[len];
					mdEntryArgs = new string[len];

					return true;
				}

				case FixTags.StartDate:
					fromDateStr = await reader.ReadStringAsync(ct);
					return true;

				case FixTags.EndDate:
					toDateStr = await reader.ReadStringAsync(ct);
					return true;

				case FixTags.MDEntryType:
					mdEntryTypes[mdEntryTypesIndex++] = await reader.ReadCharAsync(ct);
					return true;

				case FixTags.MDEntryArg:
					mdEntryArgs[mdEntryArgsIndex++] = await reader.ReadStringAsync(ct);
					return true;

				case FixTags.NoRelatedSym:
				{
					var len = await reader.ReadIntAsync(ct);

					securityIds = new SecurityId[len];
					securityTypes = new string[len];

					return true;
				}

				case FixTags.Symbol:
					if (symbol != null)
						InitSecId();

					symbol = await reader.ReadStringAsync(ct);
					return true;

				case FixTags.SecurityExchange:
					if (securityExchange != null)
						InitSecId();

					securityExchange = await reader.ReadStringAsync(ct);
					return true;

				case FixTags.SecurityType:
					securityTypes[++securityTypesIdx] = await reader.ReadStringAsync(ct);
					return true;

				case FixTags.CFICode:
					if (cfiCode != null)
						InitSecId();

					cfiCode = await reader.ReadStringAsync(ct);
					return true;

				case FixTags.AllowBuildFromSmallerTimeFrame:
					allowBuildSmallerTf = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.CalcVolumeProfile:
					isCalcVolProfile = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.FinishedCandles:
					isFinishedCandles = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.MarketDataBuildMode:
					buildMode = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.MarketDataBuildFrom:
					buildFrom = await reader.ReadCharAsync(ct);
					return true;
				case FixTags.MarketDataBuildField:
					buildField = await reader.ReadCharAsync(ct);
					return true;
				case FixTags.RegularTradingHours:
					isRth = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.MarketDataSkip:
					skip = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.MarketDataCount:
					count = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.NoMDFields:
					fields = new Level1Fields[await reader.ReadIntAsync(ct)];
					return true;
				case FixTags.MDField:
					fields[fieldsIndex++] = (Level1Fields)await reader.ReadIntAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			return null;

		if (symbol != null)
			InitSecId();

		var fromDate = fromDateStr == null ? (DateTime?)null : dateBoundParser.Parse(fromDateStr).UtcKind();
		var toDate = toDateStr == null ? (DateTime?)null : dateBoundParser.Parse(toDateStr).UtcKind();

		return new FixMarketDataRequest(
			mdReqId,
			mdResponseId,
			subscriptionRequestType,
			mdEntryTypes,
			mdEntryArgs,
			securityIds,
			securityTypes,
			marketDepth,
			fromDate,
			toDate,
			allowBuildSmallerTf,
			isCalcVolProfile,
			isFinishedCandles,
			isRth,
			buildMode,
			buildFrom,
			buildField,
			skip,
			count,
			fields);
	}

	/// <summary>
	/// Read market data messages (async version).
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="dataBoundDateParser">Date parser.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Async enumerable of market data messages with request/response IDs.</returns>
	public static async IAsyncEnumerable<(MarketDataMessage mdMsg, string reqId, string respId)> ReadMarketDataMessagesAsync(this IFixReader reader, FastDateTimeParser dataBoundDateParser, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		var fixReq = await reader.ReadMarketDataRequestAsync(dataBoundDateParser, cancellationToken);

		if (fixReq == null)
			yield break;

		var req = fixReq.Value;

		foreach (var msg in req.ToMessages())
			yield return (msg, req.MdReqId, req.MdResponseId);
	}

	/// <summary>
	/// Convert <see cref="FixMarketDataRequest"/> to <see cref="MarketDataMessage"/> enumerable.
	/// </summary>
	/// <param name="fix">FIX market data request.</param>
	/// <returns>Market data messages.</returns>
	public static IEnumerable<MarketDataMessage> ToMessages(this FixMarketDataRequest fix)
	{
		var isSubscribe = fix.SubscriptionRequestType.IsSubscribe();

		var hasDepth = false;

		foreach (var dataType in fix.MdEntryTypes.Select((t, i) => t.ToDataType(fix.MdEntryArgs?[i])))
		{
			var isDepth = dataType == DataType.MarketDepth;

			if (hasDepth)
			{
				if (isDepth)
					continue;
			}
			else
				hasDepth = isDepth;

			if (dataType == DataType.News)
			{
				var msg = new MarketDataMessage
				{
					TransactionId = fix.MdReqId.ToLongN() ?? 0,
					OriginalTransactionId = fix.MdResponseId.ToLongN() ?? 0,
					SecurityType = fix.SecurityTypes?.FirstOrDefault()?.FromFixType(),
					DataType2 = dataType,
					IsSubscribe = isSubscribe,
					From = fix.FromDate,
					To = fix.ToDate,
					Skip = fix.Skip,
					Count = fix.Count,
				}.ValidateBounds();

				yield return msg;
			}
			else
			{
				var idx = 0;

				foreach (var securityId in fix.SecurityIds ?? [])
				{
					var msg = new MarketDataMessage
					{
						TransactionId = fix.MdReqId.ToLongN() ?? 0,
						OriginalTransactionId = fix.MdResponseId.ToLongN() ?? 0,
						SecurityId = securityId,
						SecurityType = fix.SecurityTypes?[idx]?.FromFixType(),
						DataType2 = dataType,
						IsSubscribe = isSubscribe,
						MaxDepth = fix.MarketDepth,
						From = fix.FromDate,
						To = fix.ToDate,
						Skip = fix.Skip,
						Count = fix.Count,
						Fields = fix.Fields,
					}.ValidateBounds();

					if (fix.AllowBuildFromSmallerTimeFrame != null)
						msg.AllowBuildFromSmallerTimeFrame = fix.AllowBuildFromSmallerTimeFrame.Value;

					if (fix.IsRegularTradingHours != null)
						msg.IsRegularTradingHours = fix.IsRegularTradingHours.Value;

					if (fix.IsCalcVolumeProfile != null)
						msg.IsCalcVolumeProfile = fix.IsCalcVolumeProfile.Value;

					if (fix.IsFinishedOnly != null)
						msg.IsFinishedOnly = fix.IsFinishedOnly.Value;

					if (fix.BuildMode != null)
						msg.BuildMode = (MarketDataBuildModes)fix.BuildMode.Value;

					if (fix.BuildFrom != null)
						msg.BuildFrom = fix.BuildFrom.Value.ToDataType(null);

					if (fix.BuildField != null)
						msg.BuildField = fix.BuildField.Value.ToLevel1();

					yield return msg;

					idx++;
				}
			}
		}
	}

	/// <summary>
	/// Write <see cref="UserInfoMessage"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="message">Message.</param>
	/// <param name="dateParser">Time parser.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteUserInfoMessageAsync(this IFixWriter writer, UserInfoMessage message, FastDateTimeParser dateParser, CancellationToken cancellationToken)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (!message.Login.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Username, cancellationToken);
			await writer.WriteAsync(message.Login, cancellationToken);
		}

		if (!message.Password.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Password, cancellationToken);
			await writer.WriteAsync(message.Password.UnSecure(), cancellationToken);
		}

		if (message.IsBlocked)
		{
			await writer.WriteAsync(FixTags.UserStatus, cancellationToken);
			await writer.WriteAsync((int)UserStatus.SessionShutdownWarning, cancellationToken);
		}

		var addresses = message.IpRestrictions.ToArray();

		await writer.WriteAsync(FixTags.NoIpRestrictions, cancellationToken);
		await writer.WriteAsync(addresses.Length, cancellationToken);

		foreach (var address in addresses)
		{
			await writer.WriteAsync(FixTags.IpRestrictions, cancellationToken);
			await writer.WriteAsync(address.To<string>(), cancellationToken);
		}

		var permissions = message.Permissions.ToArray();

		await writer.WriteAsync(FixTags.NoPermissions, cancellationToken);
		await writer.WriteAsync(permissions.Length, cancellationToken);

		foreach (var permission in permissions)
		{
			await writer.WriteAsync(FixTags.Permissions, cancellationToken);
			await writer.WriteAsync((int)permission.Key, cancellationToken);

			// TODO
			await writer.WriteAsync(FixTags.NoPermissionsValues, cancellationToken);
			await writer.WriteAsync(0, cancellationToken);
		}

		if (message.Id != null)
		{
			await writer.WriteAsync(FixTags.Id, cancellationToken);
			await writer.WriteAsync(message.Id.Value, cancellationToken);
		}

		if (!message.DisplayName.IsEmpty())
		{
			await writer.WriteAsync(FixTags.DisplayName, cancellationToken);
			await writer.WriteAsync(message.DisplayName, cancellationToken);
		}

		if (!message.Phone.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Phone, cancellationToken);
			await writer.WriteAsync(message.Phone, cancellationToken);
		}

		if (!message.Homepage.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Homepage, cancellationToken);
			await writer.WriteAsync(message.Homepage, cancellationToken);
		}

		if (!message.Skype.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Skype, cancellationToken);
			await writer.WriteAsync(message.Skype, cancellationToken);
		}

		if (!message.City.IsEmpty())
		{
			await writer.WriteAsync(FixTags.City, cancellationToken);
			await writer.WriteAsync(message.City, cancellationToken);
		}

		if (message.Gender != null)
		{
			await writer.WriteAsync(FixTags.Gender, cancellationToken);
			await writer.WriteAsync(message.Gender.Value, cancellationToken);
		}

		if (message.IsSubscription != null)
		{
			await writer.WriteAsync(FixTags.Subscription, cancellationToken);
			await writer.WriteAsync(message.IsSubscription.Value, cancellationToken);
		}

		if (!message.Language.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Language, cancellationToken);
			await writer.WriteAsync(message.Language, cancellationToken);
		}

		if (message.Balance != null)
		{
			await writer.WriteAsync(FixTags.TradeVolume, cancellationToken);
			await writer.WriteAsync(message.Balance.Value, cancellationToken);
		}

		if (message.Avatar != null)
		{
			await writer.WriteAsync(FixTags.Picture, cancellationToken);
			await writer.WriteAsync(message.Avatar.Value, cancellationToken);
		}

		if (message.CreationDate != null)
		{
			await writer.WriteAsync(FixTags.IssueDate, cancellationToken);
			await writer.WriteUtcAsync(message.CreationDate.Value, dateParser, cancellationToken);
		}

		if (!message.AuthToken.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Token, cancellationToken);
			await writer.WriteAsync(message.AuthToken, cancellationToken);
		}

		await writer.WriteAsync(FixTags.PublishTrdIndicator, cancellationToken);
		await writer.WriteAsync(message.CanPublish, cancellationToken);

		if (message.IsAgreementAccepted != null)
		{
			await writer.WriteAsync(FixTags.AgreementID, cancellationToken);
			await writer.WriteAsync(message.IsAgreementAccepted.Value, cancellationToken);
		}

		if (message.UploadLimit != default)
		{
			await writer.WriteAsync(FixTags.MaxFloor, cancellationToken);
			await writer.WriteAsync(message.UploadLimit, cancellationToken);
		}

		if (message.Features.Length > 0)
		{
			await writer.WriteAsync(FixTags.NoPartyIDs, cancellationToken);
			await writer.WriteAsync(message.Features.Length, cancellationToken);

			foreach (var feature in message.Features)
			{
				await writer.WriteAsync(FixTags.PartyID, cancellationToken);
				await writer.WriteAsync(feature, cancellationToken);
			}
		}

		if (message.IsTrialVerified)
		{
			await writer.WriteAsync(FixTags.TrialAllow, cancellationToken);
			await writer.WriteAsync(message.IsTrialVerified, cancellationToken);
		}
	}

	/// <summary>
	/// Read user info message.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="dateParser">Date parser.</param>
	/// <param name="handler">Custom tag handler.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Async enumerable of user info messages.</returns>
	public static async IAsyncEnumerable<Message> ReadUserInfoMessageAsync(this IFixReader reader, FastDateTimeParser dateParser, Func<FixTags, CancellationToken, ValueTask<bool>> handler, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		if (reader is null)				throw new ArgumentNullException(nameof(reader));
		if (dateParser is null)			throw new ArgumentNullException(nameof(dateParser));
		if (handler is null)			throw new ArgumentNullException(nameof(handler));

		var msg = new UserInfoMessage();

		IPAddress[] ipRestrictions = null;
		var ipRestrictionsIdx = -1;
		var featureIdx = -1;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.Username:
					msg.Login = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Password:
					msg.Password = (await reader.ReadStringAsync(ct)).Secure();
					return true;
				case FixTags.UserStatus:
					msg.IsBlocked = (UserStatus)await reader.ReadIntAsync(ct) == UserStatus.SessionShutdownWarning;
					return true;
				case FixTags.NoIpRestrictions:
					ipRestrictions = new IPAddress[await reader.ReadIntAsync(ct)];
					ipRestrictionsIdx = 0;
					return true;
				case FixTags.IpRestrictions:
					ipRestrictions[ipRestrictionsIdx++] = (await reader.ReadStringAsync(ct)).To<IPAddress>();
					return true;
				case FixTags.NoPermissions:
					await reader.ReadIntAsync(ct);
					return true;
				case FixTags.Permissions:
					msg.Permissions.Add((UserPermissions)await reader.ReadIntAsync(ct), new Dictionary<(string, string, string, DateTime?), bool>());
					return true;
				case FixTags.Id:
					msg.Id = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.DisplayName:
					msg.DisplayName = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Phone:
					msg.Phone = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Homepage:
					msg.Homepage = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Skype:
					msg.Skype = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.City:
					msg.City = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Language:
					msg.Language = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Gender:
					msg.Gender = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.Subscription:
					msg.IsSubscription = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.TradeVolume:
					msg.Balance = await reader.ReadDecimalAsync(ct);
					return true;
				case FixTags.Picture:
					msg.Avatar = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.IssueDate:
					msg.CreationDate = await reader.ReadUtcAsync(dateParser, ct);
					return true;
				case FixTags.Token:
					msg.AuthToken = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.PublishTrdIndicator:
					msg.CanPublish = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.AgreementID:
					msg.IsAgreementAccepted = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.MaxFloor:
					msg.UploadLimit = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.NoPartyIDs:
					msg.Features = new string[await reader.ReadIntAsync(ct)];
					featureIdx = 0;
					return true;
				case FixTags.PartyID:
					msg.Features[featureIdx++] = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.TrialAllow:
					msg.IsTrialVerified = await reader.ReadBoolAsync(ct);
					return true;
				default:
					return await handler(tag, ct);
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		msg.IpRestrictions = ipRestrictions ?? Enumerable.Empty<IPAddress>();
		msg.Features ??= [];

		yield return msg;
	}

	/// <summary>
	/// Write <see cref="ISubscriptionMessage"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="subscription">Message.</param>
	/// <param name="parser">Time parser.</param>
	/// <param name="requestIdTag">Request id tag.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteSubscriptionRequestAsync(this IFixWriter writer, ISubscriptionMessage subscription, FastDateTimeParser parser, FixTags requestIdTag, CancellationToken cancellationToken)
	{
		if (subscription.TransactionId != 0)
		{
			await writer.WriteAsync(requestIdTag, cancellationToken);
			await writer.WriteAsync(subscription.TransactionId.To<string>(), cancellationToken);
		}

		if (subscription.OriginalTransactionId != 0)
		{
			await writer.WriteAsync(FixTags.MDResponseID, cancellationToken);
			await writer.WriteAsync(subscription.OriginalTransactionId.To<string>(), cancellationToken);
		}

		await writer.WriteAsync(FixTags.SubscriptionRequestType, cancellationToken);
		await writer.WriteAsync(subscription.GetSubscriptionType(), cancellationToken);

		if (subscription.From != null)
		{
			await writer.WriteAsync(FixTags.StartDate, cancellationToken);
			await writer.WriteUtcAsync(subscription.From.Value, parser, cancellationToken);
		}

		if (subscription.To != null)
		{
			await writer.WriteAsync(FixTags.EndDate, cancellationToken);
			await writer.WriteUtcAsync(subscription.To.Value, parser, cancellationToken);
		}

		if (subscription.Skip != default)
		{
			await writer.WriteAsync(FixTags.MarketDataSkip, cancellationToken);
			await writer.WriteAsync(subscription.Skip.Value, cancellationToken);
		}

		if (subscription.Count != default)
		{
			await writer.WriteAsync(FixTags.MarketDataCount, cancellationToken);
			await writer.WriteAsync(subscription.Count.Value, cancellationToken);
		}
	}

	/// <summary>
	/// Write <see cref="ISubscriptionMessage"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="msg">Message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteSubscriptionAsync(this IFixWriter writer, ISubscriptionMessage msg, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.MDReqID, cancellationToken);
		await writer.WriteAsync(msg.GetRequestId(), cancellationToken);

		await writer.WriteAsync(FixTags.SubscriptionRequestType, cancellationToken);
		await writer.WriteAsync(msg.GetSubscriptionType(), cancellationToken);
	}

	/// <summary>
	/// Write <see cref="CommandMessage"/>.
	/// </summary>
	/// <typeparam name="TCommandMessage">Message type.</typeparam>
	/// <param name="writer">Writer.</param>
	/// <param name="message">Message.</param>
	/// <param name="parser">Time parser.</param>
	/// <param name="writeTags">Handler.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteCommandAsync<TCommandMessage>(this IFixWriter writer, TCommandMessage message, FastDateTimeParser parser, Func<IFixWriter, TCommandMessage, CancellationToken, ValueTask> writeTags, CancellationToken cancellationToken)
		where TCommandMessage : CommandMessage
	{
		await writer.WriteSubscriptionRequestAsync(message, parser, FixTags.MDReqID, cancellationToken);

		await writer.WriteAsync(FixTags.Command, cancellationToken);
		await writer.WriteAsync((int)message.Command, cancellationToken);

		await writer.WriteAsync(FixTags.Scope, cancellationToken);
		await writer.WriteAsync((int)message.Scope, cancellationToken);

		if (!message.ObjectId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Id, cancellationToken);
			await writer.WriteAsync(message.ObjectId, cancellationToken);
		}

		await writer.WriteParametersAsync(message.Parameters, cancellationToken);

		await writeTags(writer, message, cancellationToken);
	}

	/// <summary>
	/// Write <see cref="RemoteFileMessage"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteFileAsync<TMessage>(this IFixWriter writer, TMessage message, CancellationToken cancellationToken)
		where TMessage : IFileMessage
	{
		if (message.Body.Length == 0)
			return;

		await writer.WriteAsync(FixTags.RawDataLength, cancellationToken);
		await writer.WriteAsync(message.Body.Length, cancellationToken);

		await writer.WriteAsync(FixTags.RawData, cancellationToken);
		await writer.WriteBytesAsync(new ReadOnlyMemory<byte>(message.Body, 0, message.Body.Length), cancellationToken);
	}

	/// <summary>
	/// Read file info.
	/// </summary>
	/// <typeparam name="TMessage">Message type.</typeparam>
	/// <param name="reader">Reader.</param>
	/// <param name="readTag">Custom tag handler.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Async enumerable of file messages.</returns>
	public static async IAsyncEnumerable<TMessage> ReadFileInfoAsync<TMessage>(this IFixReader reader, Func<FixTags, IFixReader, CancellationToken, ValueTask<bool>> readTag, [EnumeratorCancellation]CancellationToken cancellationToken)
		where TMessage : IFileMessage, new()
	{
		if (reader is null)
			throw new ArgumentNullException(nameof(reader));

		int? rawDataLength = null;
		byte[] rawData = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.RawDataLength:
					rawDataLength = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.RawData:
					rawData = new byte[rawDataLength.Value];
					await reader.ReadBytesAsync(new Memory<byte>(rawData), ct);
					return true;
				default:
					return await readTag(tag, reader, ct);
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		yield return new()
		{
			Body = rawData ?? [],
		};
	}

	/// <summary>
	/// Write <see cref="DataType"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteDataTypeAsync(this IFixWriter writer, DataType dataType, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		await writer.WriteAsync(FixTags.MDEntryType, cancellationToken);
		await writer.WriteAsync(dataType.ToFixMDType(out var arg), cancellationToken);

		if (!arg.IsEmpty())
		{
			await writer.WriteAsync(FixTags.MDEntryArg, cancellationToken);
			await writer.WriteAsync(arg, cancellationToken);
		}
	}

	/// <summary>
	/// Write <see cref="IGeneratedMessage.BuildFrom"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="buildFrom"><see cref="IGeneratedMessage.BuildFrom"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteBuildFromAsync(this IFixWriter writer, DataType buildFrom, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		if (buildFrom is null)
			throw new ArgumentNullException(nameof(buildFrom));

		await writer.WriteAsync(FixTags.BuildFromType, cancellationToken);
		await writer.WriteAsync(buildFrom.ToFixMDType(out var arg), cancellationToken);

		if (!arg.IsEmpty())
		{
			await writer.WriteAsync(FixTags.BuildFromArg, cancellationToken);
			await writer.WriteAsync(arg, cancellationToken);
		}
	}

	/// <summary>
	/// Read news.
	/// </summary>
	/// <param name="reader">FIX reader.</param>
	/// <param name="parser">Time parser.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>News messages.</returns>
	public static async IAsyncEnumerable<Message> ReadNewsAsync(this IFixReader reader, FastDateTimeParser parser, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string requestId = null;
		string responseId = null;
		string id = null;
		string symbol = null;
		string securityExchange = null;
		string urlLink = null;
		string headline = null;
		DateTime? sendingTime = null;
		DateTime? origTime = null;
		DateTime? expiryDate = null;
		char? urgency = null;
		string language = null;
		string story = null;
		string source = null;
		long[] attachments = null;
		var attachmentsIdx = -1;
		long productId = 0;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.MDReqID:
					requestId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.MDResponseID:
					responseId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.MDEntryId:
					id = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SendingTime:
					sendingTime = await reader.ReadUtcAsync(parser, ct);
					return true;
				case FixTags.NoRelatedSym:
					await reader.ReadIntAsync(ct);
					return true;
				case FixTags.Urgency:
					urgency = await reader.ReadCharAsync(ct);
					return true;
				case FixTags.Symbol:
					symbol = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityExchange:
					securityExchange = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.URLLink:
					urlLink = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Headline:
					headline = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Language:
					language = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Text:
					story = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.IDSource:
					source = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.ExpireTime:
					expiryDate = await reader.ReadUtcAsync(parser, ct);
					return true;
				case FixTags.OrigTime:
					origTime = await reader.ReadUtcAsync(parser, ct);
					return true;
				case FixTags.NoUnderlyings:
					attachments = new long[await reader.ReadIntAsync(ct)];
					attachmentsIdx = 0;
					return true;
				case FixTags.UnderlyingSymbol:
					attachments[attachmentsIdx++] = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.Product:
					productId = await reader.ReadLongAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		var securityId = default(SecurityId);

		if (symbol != null)
			securityId = new SecurityId { SecurityCode = symbol, BoardCode = securityExchange };

		yield return new NewsMessage
		{
			TransactionId = requestId.IsEmpty() ? default : requestId.To<long>(),
			OriginalTransactionId = responseId.IsEmpty() ? default : responseId.To<long>(),
			Id = id,
			Url = urlLink,
			Headline = headline,
			ServerTime = origTime ?? sendingTime ?? DateTime.UtcNow,
			SecurityId = symbol != null ? securityId : null,
			BoardCode = securityExchange,
			Priority = urgency.ToNewsPriority(),
			ExpiryDate = expiryDate,
			Language = language,
			Source = source,
			Story = story,
			Attachments = attachments ?? [],
			ProductId = productId,
		};
	}

	/// <summary>
	/// Write <see cref="NewsMessage"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="newsMsg">Message.</param>
	/// <param name="parser">Time parser.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteNewsAsync(this IFixWriter writer, NewsMessage newsMsg, FastDateTimeParser parser, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.Headline, cancellationToken);
		await writer.WriteAsync(newsMsg.Headline, cancellationToken);

		await writer.WriteAsync(FixTags.OrigTime, cancellationToken);
		await writer.WriteUtcAsync(newsMsg.ServerTime, parser, cancellationToken);

		if (!newsMsg.Url.IsEmpty())
		{
			await writer.WriteAsync(FixTags.URLLink, cancellationToken);
			await writer.WriteAsync(newsMsg.Url, cancellationToken);
		}

		if (!newsMsg.Id.IsEmpty())
		{
			await writer.WriteAsync(FixTags.MDEntryId, cancellationToken);
			await writer.WriteAsync(newsMsg.Id, cancellationToken);
		}

		if (newsMsg.ExpiryDate != null)
		{
			await writer.WriteAsync(FixTags.ExpireTime, cancellationToken);
			await writer.WriteUtcAsync(newsMsg.ExpiryDate.Value, parser, cancellationToken);
		}

		if (!newsMsg.Language.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Language, cancellationToken);
			await writer.WriteAsync(newsMsg.Language, cancellationToken);
		}

		if (!newsMsg.Story.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Text, cancellationToken);
			await writer.WriteAsync(newsMsg.Story, cancellationToken);
		}

		if (!newsMsg.Source.IsEmpty())
		{
			await writer.WriteAsync(FixTags.IDSource, cancellationToken);
			await writer.WriteAsync(newsMsg.Source, cancellationToken);
		}

		if (newsMsg.SecurityId != null)
		{
			var securityId = newsMsg.SecurityId.Value;

			if (securityId != default)
			{
				await writer.WriteAsync(FixTags.NoRelatedSym, cancellationToken);
				await writer.WriteAsync(1, cancellationToken);

				await writer.WriteAsync(FixTags.Symbol, cancellationToken);
				await writer.WriteAsync(securityId.SecurityCode, cancellationToken);

				if (!securityId.BoardCode.IsEmpty())
				{
					await writer.WriteAsync(FixTags.SecurityExchange, cancellationToken);
					await writer.WriteAsync(securityId.BoardCode, cancellationToken);
				}
			}
		}

		if (newsMsg.Attachments.Length > 0)
		{
			await writer.WriteAsync(FixTags.NoUnderlyings, cancellationToken);
			await writer.WriteAsync(newsMsg.Attachments.Length, cancellationToken);

			foreach (var id in newsMsg.Attachments)
			{
				await writer.WriteAsync(FixTags.UnderlyingSymbol, cancellationToken);
				await writer.WriteAsync(id, cancellationToken);
			}
		}

		if (newsMsg.ProductId != 0)
		{
			await writer.WriteAsync(FixTags.Product, cancellationToken);
			await writer.WriteAsync(newsMsg.ProductId, cancellationToken);
		}
	}
}