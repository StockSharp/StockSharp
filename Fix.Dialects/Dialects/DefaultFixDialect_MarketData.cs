namespace StockSharp.Fix.Dialects;

partial class DefaultFixDialect
{
	/// <summary>
	/// Register new candle type.
	/// </summary>
	/// <param name="code"><see cref="FixTags.MDEntryType"/> value.</param>
	/// <param name="messageType">Message type.</param>
	public static void RegisterCandleType(char code, Type messageType) => Native.Extensions.RegisterCandleType(code, messageType);

	private static async IAsyncEnumerable<Message> ReadSubscriptionResponseAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string mdReqId = null;
		string text = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.MDReqID:
					mdReqId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Text:
					text = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		yield return new SubscriptionResponseMessage
		{
			OriginalTransactionId = mdReqId.To<long>(),
			Error = text.IsEmpty() ? null : new InvalidOperationException(text),
		};
	}

	private static async IAsyncEnumerable<Message> ReadSubscriptionFinishedAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string mdReqId = null;

		var result = reader.ReadFileInfoAsync<SubscriptionFinishedMessage>(async (tag, reader, ct) =>
		{
			switch (tag)
			{
				case FixTags.MDReqID:
					mdReqId = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		await foreach (var msg in result.WithEnforcedCancellation(cancellationToken))
		{
			if (!mdReqId.IsEmpty())
				msg.OriginalTransactionId = mdReqId.To<long>();

			yield return msg;
		}
	}

	private static async IAsyncEnumerable<Message> ReadSubscriptionOnlineAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string mdReqId = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.MDReqID:
					mdReqId = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		yield return new SubscriptionOnlineMessage
		{
			OriginalTransactionId = mdReqId.To<long>(),
		};
	}

	private async ValueTask<string> WriteSecurityListRequestAsync(IFixWriter writer, SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.SecurityReqID, cancellationToken);
		await writer.WriteAsync(lookupMsg.TransactionId.To<string>(), cancellationToken);

		if (lookupMsg.OriginalTransactionId != 0)
		{
			await writer.WriteAsync(FixTags.SecurityResponseID, cancellationToken);
			await writer.WriteAsync(lookupMsg.OriginalTransactionId.To<string>(), cancellationToken);
		}

		await writer.WriteAsync(FixTags.SecurityListRequestType, cancellationToken);
		await writer.WriteAsync((int)SecurityListRequestType.AllSecurities, cancellationToken);

		if (!lookupMsg.SecurityId.SecurityCode.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Symbol, cancellationToken);
			await writer.WriteAsync(lookupMsg.SecurityId.SecurityCode, cancellationToken);
		}

		if (!lookupMsg.SecurityId.BoardCode.IsEmpty())
		{
			await writer.WriteAsync(FixTags.SecurityExchange, cancellationToken);
			await writer.WriteAsync(lookupMsg.SecurityId.BoardCode, cancellationToken);
		}

		var secTypes = lookupMsg.GetSecurityTypes();

		if (secTypes.Count > 0)
		{
			await writer.WriteAsync(FixTags.NoSecurityTypes, cancellationToken);
			await writer.WriteAsync(secTypes.Count, cancellationToken);

			foreach (var secType in secTypes)
			{
				await writer.WriteAsync(FixTags.SecurityType, cancellationToken);
				await writer.WriteAsync(secType.ToFix(), cancellationToken);
			}
		}

		if (lookupMsg.Currency != null)
		{
			await writer.WriteAsync(FixTags.Currency, cancellationToken);
			await writer.WriteAsync(lookupMsg.Currency.Value.ToFix(), cancellationToken);
		}

		if (lookupMsg.OptionType != null)
		{
			await writer.WriteAsync(FixTags.PutOrCall, cancellationToken);
			await writer.WriteAsync(lookupMsg.OptionType.Value.ToFix(), cancellationToken);
		}

		if (lookupMsg.Strike != null)
		{
			await writer.WriteAsync(FixTags.StrikePrice, cancellationToken);
			await writer.WriteAsync(lookupMsg.Strike.Value, cancellationToken);
		}

		if (lookupMsg.ExpiryDate != null)
		{
			await writer.WriteAsync(FixTags.RedemptionDate, cancellationToken);
			await writer.WriteUtcAsync(lookupMsg.ExpiryDate.Value, TimeStampParser, cancellationToken);
		}

		if (!lookupMsg.Name.IsEmpty())
		{
			await writer.WriteAsync(FixTags.SecurityDesc, cancellationToken);
			await writer.WriteAsync(lookupMsg.Name, cancellationToken);
		}

		if (!lookupMsg.GetUnderlyingCode().IsEmpty())
		{
			await writer.WriteAsync(FixTags.Text, cancellationToken);
			await writer.WriteAsync(lookupMsg.GetUnderlyingCode(), cancellationToken);
		}

		if (lookupMsg.Skip != default)
		{
			await writer.WriteAsync(FixTags.MarketDataSkip, cancellationToken);
			await writer.WriteAsync(lookupMsg.Skip.Value, cancellationToken);
		}

		if (lookupMsg.Count != default)
		{
			await writer.WriteAsync(FixTags.MarketDataCount, cancellationToken);
			await writer.WriteAsync(lookupMsg.Count.Value, cancellationToken);
		}

		if (lookupMsg.OnlySecurityId)
		{
			await writer.WriteAsync(FixTags.OnlyId, cancellationToken);
			await writer.WriteAsync(true, cancellationToken);
		}

		if (lookupMsg.SecurityIds.Length > 0)
		{
			await writer.WriteAsync(FixTags.NoUnderlyings, cancellationToken);
			await writer.WriteAsync(lookupMsg.SecurityIds.Length, cancellationToken);

			foreach (var id in lookupMsg.SecurityIds)
			{
				if (!id.SecurityCode.IsEmpty())
				{
					await writer.WriteAsync(FixTags.UnderlyingSymbol, cancellationToken);
					await writer.WriteAsync(id.SecurityCode, cancellationToken);
				}

				if (!id.BoardCode.IsEmpty())
				{
					await writer.WriteAsync(FixTags.UnderlyingSecurityExchange, cancellationToken);
					await writer.WriteAsync(id.BoardCode, cancellationToken);
				}
			}
		}

		if (lookupMsg.DisableArchive)
		{
			await writer.WriteAsync(FixTags.DisableArchive, cancellationToken);
			await writer.WriteAsync(lookupMsg.DisableArchive, cancellationToken);
		}

		return FixMessages.SecurityListRequest;
	}

	private async ValueTask<string> WriteSecurityUploadAsync(IFixWriter writer, SecurityMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteSecurityListAsync(TimeStampParser, false, null, null, [message], true, cancellationToken);
		return FixExtendedMessages.SecurityListUpload;
	}

	private static async ValueTask<string> WriteTradingSessionStatusRequestAsync(IFixWriter writer, MarketDataMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.TradSesReqID, cancellationToken);
		await writer.WriteAsync(message.TransactionId.To<long>(), cancellationToken);

		if (message.OriginalTransactionId != 0)
		{
			await writer.WriteAsync(FixTags.MDResponseID, cancellationToken);
			await writer.WriteAsync(message.OriginalTransactionId.To<long>(), cancellationToken);
		}

		await writer.WriteAsync(FixTags.TradingSessionID, cancellationToken);
		await writer.WriteAsync(message.BoardCode, cancellationToken);

		await writer.WriteAsync(FixTags.SubscriptionRequestType, cancellationToken);
		await writer.WriteAsync(message.GetSubscriptionType(), cancellationToken);

		return FixMessages.TradingSessionStatusRequest;
	}

	private async ValueTask<string> WriteBoardLookupAsync(IFixWriter writer, BoardLookupMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteSubscriptionRequestAsync(message, TimeStampParser, FixTags.MDReqID, cancellationToken);

		if (!message.Like.IsEmpty())
		{
			await writer.WriteAsync(FixTags.TradingSessionID, cancellationToken);
			await writer.WriteAsync(message.Like, cancellationToken);
		}

		if (message.DisableArchive)
		{
			await writer.WriteAsync(FixTags.DisableArchive, cancellationToken);
			await writer.WriteAsync(message.DisableArchive, cancellationToken);
		}

		return FixExtendedMessages.BoardLookup;
	}

	private async IAsyncEnumerable<Message> ReadBoardAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string requestId = null;
		string sessionId = null;
		string subSessionId = null;
		TimeSpan? expiryTime = null;
		string sessionPeriods = null;
		string sessionSpecialDays = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.TradSesReqID:
					requestId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.TradingSessionID:
					sessionId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.TradingSessionSubID:
					subSessionId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.ExpireTime:
					expiryTime = await reader.ReadTimeSpanAsync(TimeParser, ct);
					return true;
				case FixTags.SessionPeriods:
					sessionPeriods = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SessionSpecialDays:
					sessionSpecialDays = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (sessionId.IsEmpty())
		{
			LogWarning("Empty board for the '{0}' exchange.", subSessionId);
			yield break;
		}

		var board = new BoardMessage
		{
			OriginalTransactionId = requestId.To<long?>() ?? 0,
			Code = sessionId,
			ExchangeCode = subSessionId,
			ExpiryTime = expiryTime ?? default,
		};

		if (!sessionPeriods.IsEmpty())
			board.WorkingTime.Periods.AddRange(sessionPeriods.DecodeToPeriods());

		if (!sessionSpecialDays.IsEmpty())
			board.WorkingTime.SpecialDays.AddRange(sessionSpecialDays.DecodeToSpecialDays());

		yield return board;
	}

	private static async IAsyncEnumerable<Message> ReadBoardLookupResultAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string requestId = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.TradSesReqID:
					requestId = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		yield return new SubscriptionFinishedMessage
		{
			OriginalTransactionId = requestId.To<long?>() ?? 0
		};
	}

	private async IAsyncEnumerable<Message> ReadDataTypeInfo(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string requestId = null;
		string symbol = null;
		string exchange = null;
		char? mdEntryType = null;
		string mdEntryArg = null;
		int? format = null;
		var mdEntryDates = Array.Empty<DateTime>();
		var mdEntryDatesIdx = -1;
		var hasObsolete = false;
		string timeFramesStr = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.MDReqID:
					requestId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Symbol:
					symbol = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityExchange:
					exchange = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.MDEntryType:
					mdEntryType = await reader.ReadCharAsync(ct);
					return true;
				case FixTags.MDEntryArg:
					mdEntryArg = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Format:
					format = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.NoMDEntries:
					mdEntryDates = new DateTime[await reader.ReadIntAsync(ct)];
					mdEntryDatesIdx = 0;
					return true;
				case FixTags.MDEntryDate:
					mdEntryDates[mdEntryDatesIdx] = (await reader.ReadDateTimeAsync(DateParser, ct)).UtcKind();
					mdEntryDatesIdx++;
					return true;
				case FixTags.Obsolete:
					// TODO 2025-03-18 Remove few years later
					// backward compatibility
					hasObsolete = true;
					timeFramesStr = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		var originTransId = requestId.To<long?>() ?? 0;

		if (!hasObsolete)
		{
			var dt = mdEntryType?.ToDataType(mdEntryArg);

			yield return new DataTypeInfoMessage
			{
				OriginalTransactionId = originTransId,
				SecurityId = new SecurityId
				{
					SecurityCode = symbol,
					BoardCode = exchange,
				},
				FileDataType = dt,
				Format = format ?? default,
				Dates = mdEntryDates,
			};
		}
		else
		{
			// 2025-03-18 Remove few years later

			var timeFrames = timeFramesStr.SplitByComma().Select(s => s.To<long>().To<TimeSpan>()).ToArray();

			foreach (var tf in timeFrames)
			{
				yield return new DataTypeInfoMessage
				{
					OriginalTransactionId = originTransId,
					FileDataType = tf.TimeFrame(),
				};
			}

			if (originTransId != 0)
				yield return new SubscriptionFinishedMessage { OriginalTransactionId = originTransId };
		}
	}

	private static async ValueTask<string> WriteSecurityLegsRequestAsync(IFixWriter writer, SecurityLegsRequestMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.MassStatusReqID, cancellationToken);
		await writer.WriteAsync(message.TransactionId.To<string>(), cancellationToken);

		if (!message.Like.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Symbol, cancellationToken);
			await writer.WriteAsync(message.Like, cancellationToken);
		}

		return FixExtendedMessages.SecurityLegsRequest;
	}

	private static async IAsyncEnumerable<Message> ReadSecurityLegsInfo(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string requestId = null;
		string symbol = null;
		string securityExchange = null;
		var legsDict = new Dictionary<SecurityId, IEnumerable<SecurityId>>();
		SecurityId[] legs = null;
		var legsIdx = -1;

		void Flush()
		{
			legsDict.Add(new SecurityId { SecurityCode = symbol, BoardCode = securityExchange }, legs);
			symbol = null;
			securityExchange = null;
			legs = null;
			legsIdx = -1;
		}

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.MassStatusReqID:
					requestId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.NoRelatedSym:
					await reader.ReadIntAsync(ct);
					return true;
				case FixTags.NoLegs:
					legs = new SecurityId[await reader.ReadIntAsync(ct)];
					legsIdx = 0;
					return true;
				case FixTags.Symbol:
				{
					if (!symbol.IsEmpty())
						Flush();

					symbol = await reader.ReadStringAsync(ct);
					return true;
				}
				case FixTags.SecurityExchange:
				{
					if (!securityExchange.IsEmpty())
						Flush();

					securityExchange = await reader.ReadStringAsync(ct);
					return true;
				}
				case FixTags.LegSymbol:
				{
					if (!legs[legsIdx].SecurityCode.IsEmpty())
						legsIdx++;

					var id = legs[legsIdx];
					id.SecurityCode = await reader.ReadStringAsync(ct);
					legs[legsIdx] = id;
					return true;
				}
				case FixTags.LegSecurityExchange:
				{
					if (!legs[legsIdx].BoardCode.IsEmpty())
						legsIdx++;

					var id = legs[legsIdx];
					id.BoardCode = await reader.ReadStringAsync(ct);
					legs[legsIdx] = id;
					return true;
				}
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (!symbol.IsEmpty())
			Flush();

		yield return new SecurityLegsInfoMessage
		{
			OriginalTransactionId = requestId.To<long?>() ?? 0,
			Legs = legsDict,
		};
	}

	private async ValueTask<string> WriteSubscriptionListRequestAsync(IFixWriter writer, SubscriptionListRequestMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteSubscriptionRequestAsync(message, TimeStampParser, FixTags.MDReqID, cancellationToken);

		return FixExtendedMessages.SubscriptionListRequest;
	}

	private async IAsyncEnumerable<Message> ReadSubscriptionAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		var result = reader.ReadMarketDataMessagesAsync(TimeStampParser, cancellationToken);

		await foreach (var (msg, mdReqId, mdResponseId) in result.WithEnforcedCancellation(cancellationToken))
		{
			var transId = mdReqId.To<long>();
			var originTransId = mdResponseId.To<long>();

			var subscription = (MarketDataMessage)msg;

			subscription.TransactionId = transId;
			subscription.OriginalTransactionId = originTransId;

			yield return msg;
		}
	}

	private static async ValueTask<string> WriteSecurityMappingAsync(IFixWriter writer, SecurityMappingMessage message, CancellationToken cancellationToken)
	{
		if (message.TransactionId != 0)
		{
			await writer.WriteAsync(FixTags.MassStatusReqID, cancellationToken);
			await writer.WriteAsync(message.TransactionId.To<string>(), cancellationToken);
		}

		await writer.WriteAsync(FixTags.IDSource, cancellationToken);
		await writer.WriteAsync(message.StorageName, cancellationToken);

		var mapping = message.Mapping;

		await writer.WriteAsync(FixTags.MDUpdateAction, cancellationToken);
		await writer.WriteAsync(message.IsDelete ? MDUpdateAction.Delete : MDUpdateAction.Change, cancellationToken);

		await writer.WriteAsync(FixTags.Symbol, cancellationToken);
		await writer.WriteAsync(mapping.StockSharpId.SecurityCode, cancellationToken);

		await writer.WriteAsync(FixTags.SecurityExchange, cancellationToken);
		await writer.WriteAsync(mapping.StockSharpId.BoardCode, cancellationToken);

		await writer.WriteAsync(FixTags.SecurityAltID, cancellationToken);
		await writer.WriteAsync(mapping.AdapterId.SecurityCode, cancellationToken);

		await writer.WriteAsync(FixTags.SecurityAltIDSource, cancellationToken);
		await writer.WriteAsync(mapping.AdapterId.BoardCode, cancellationToken);

		return FixExtendedMessages.SecurityMapping;
	}

	private static async ValueTask<string> WriteRemoveAsync(IFixWriter writer, RemoveMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.MDReqID, cancellationToken);
		await writer.WriteAsync(message.TransactionId.To<string>(), cancellationToken);

		await writer.WriteAsync(FixTags.MDEntryId, cancellationToken);
		await writer.WriteAsync(message.RemoveId, cancellationToken);

		await writer.WriteAsync(FixTags.MDEntryType, cancellationToken);
		await writer.WriteAsync(message.RemoveType.To<string>(), cancellationToken);

		return FixExtendedMessages.Remove;
	}

	private async ValueTask<string> WriteRemoteFileCommandAsync(IFixWriter writer, RemoteFileCommandMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteCommandAsync(message, TimeStampParser, async (w, m, ct) =>
		{
			if (m.SecurityId != default)
			{
				await writer.WriteAsync(FixTags.Symbol, ct);
				await writer.WriteAsync(m.SecurityId.SecurityCode, ct);

				await writer.WriteAsync(FixTags.SecurityExchange, ct);
				await writer.WriteAsync(m.SecurityId.BoardCode, ct);
			}

			await writer.WriteDataTypeAsync(m.FileDataType, ct);

			await writer.WriteAsync(FixTags.Format, ct);
			await writer.WriteAsync(m.Format, ct);

			if (m.Body.Length > 0)
			{
				await writer.WriteAsync(FixTags.RawDataLength, ct);
				await writer.WriteAsync(m.Body.Length, ct);

				await writer.WriteAsync(FixTags.RawData, ct);
				await writer.WriteBytesAsync(m.Body.AsMemory(), ct);
			}
		}, cancellationToken);

		return FixExtendedMessages.RemoteFileCommand;
	}

	private async IAsyncEnumerable<Message> ReadRemoteFileAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		long? requestId = null;
		long? responseId = null;
		string symbol = null;
		string exchange = null;
		char? mdEntryType = null;
		string mdEntryArg = null;
		int? format = null;
		DateTime? mdEntryDate = null;
		string fileName = null;
		string fileId = null;
		string groupId = null;
		bool isPublic = false;
		string url = null;
		string hash = null;

		var result = reader.ReadFileInfoAsync<RemoteFileMessage>(async (tag, r, ct) =>
		{
			switch (tag)
			{
				case FixTags.MDReqID:
					requestId = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.MDResponseID:
					responseId = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.Symbol:
					symbol = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SecurityExchange:
					exchange = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.MDEntryType:
					mdEntryType = await reader.ReadCharAsync(ct);
					return true;
				case FixTags.MDEntryArg:
					mdEntryArg = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Format:
					format = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.Name:
					fileName = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Id:
					fileId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.GroupId:
					groupId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Scope:
					isPublic = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.URLLink:
					url = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Hash:
					hash = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.MDEntryDate:
					mdEntryDate = await reader.ReadUtcAsync(DateParser, ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		await foreach (var msg in result.WithEnforcedCancellation(cancellationToken))
		{
			if (!symbol.IsEmpty())
				msg.SecurityId = new() { SecurityCode = symbol, BoardCode = exchange };

			if (mdEntryType != null)
				msg.FileDataType = mdEntryType.Value.ToDataType(mdEntryArg);

			if (format != null)
				msg.Format = format.Value;

			if (mdEntryDate != null)
				msg.Date = mdEntryDate.Value;

			if (requestId != null)
				msg.TransactionId = requestId.Value;

			if (responseId != null)
				msg.OriginalTransactionId = responseId.Value;

			yield return msg;
		}
	}

	private async ValueTask<string> WriteDataTypeLookupAsync(IFixWriter writer, DataTypeLookupMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.MDReqID, cancellationToken);
		await writer.WriteAsync(message.TransactionId.To<string>(), cancellationToken);

		if (message.SecurityId == default)
		{
			await writer.WriteAsync(FixTags.NoRelatedSym, cancellationToken);
			await writer.WriteAsync(0, cancellationToken);
		}
		else
		{
			await writer.WriteAsync(FixTags.NoRelatedSym, cancellationToken);
			await writer.WriteAsync(1, cancellationToken);

			await writer.WriteAsync(FixTags.Symbol, cancellationToken);
			await writer.WriteAsync(message.SecurityId.SecurityCode, cancellationToken);

			await writer.WriteAsync(FixTags.SecurityExchange, cancellationToken);
			await writer.WriteAsync(message.SecurityId.BoardCode, cancellationToken);
		}

		if (message.RequestDataType != null)
			await writer.WriteDataTypeAsync(message.RequestDataType, cancellationToken);

		if (message.Format != null)
		{
			await writer.WriteAsync(FixTags.Format, cancellationToken);
			await writer.WriteAsync(message.Format.Value, cancellationToken);
		}

		if (message.IncludeDates)
		{
			await writer.WriteAsync(FixTags.IncludeDates, cancellationToken);
			await writer.WriteAsync(true, cancellationToken);
		}

		return FixExtendedMessages.DataTypeLookup;
	}

	private async ValueTask<string> WriteNewsAsync(IFixWriter writer, NewsMessage message, CancellationToken cancellationToken)
	{
		if (message.TransactionId > 0)
		{
			await writer.WriteAsync(FixTags.MDReqID, cancellationToken);
			await writer.WriteAsync(message.TransactionId.To<string>(), cancellationToken);
		}

		await writer.WriteNewsAsync(message, TimeStampParser, cancellationToken);
		return FixMessages.News;
	}
}