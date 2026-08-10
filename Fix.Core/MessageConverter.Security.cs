namespace StockSharp.Fix;

/// <summary>
/// Security message converters: SecurityListRequest, SecurityStatusRequest, SecurityLegsRequest, SecurityMapping.
/// </summary>
partial class MessageConverter
{
	/// <inheritdoc />
	public virtual SecurityLookupMessage ToMessage(FixSecurityListRequest fix)
	{
		var msg = new SecurityLookupMessage
		{
			TransactionId = fix.SecurityReqId.ToLong(),
			Name = fix.SecurityDesc,
			Strike = fix.Strike,
			OptionType = fix.PutOrCall != null ? ((PutOrCall)fix.PutOrCall.Value).FromFixOptionType() : null,
			Skip = fix.Skip,
			Count = fix.Count,
			OnlySecurityId = fix.OnlySecurityId,
			SecurityType = GetSecurityType(fix.SecurityType),
			// Preserve the per-type filter and the archive policy. Dropping them widened
			// "these types, no archive" into a heavier, differently-scoped result.
			SecurityTypes = fix.SecurityTypes?
				.Select(t => t.FromFixType())
				.Where(t => t is not null)
				.Select(t => t.Value)
				.ToArray(),
			DisableArchive = fix.DisableArchive,
		};

		if (!fix.Currency.IsEmpty())
			msg.Currency = fix.Currency.FromMicexCurrencyName(ex => { });

		if (!fix.Symbol.IsEmpty())
		{
			var secId = msg.SecurityId;
			secId.SecurityCode = fix.Symbol;
			msg.SecurityId = secId;
		}

		if (!fix.SecurityExchange.IsEmpty())
		{
			var secId = msg.SecurityId;
			secId.BoardCode = fix.SecurityExchange;
			msg.SecurityId = secId;
		}

		if (!fix.CfiCode.IsEmpty())
			msg.CfiCode = fix.CfiCode;

		if (fix.RedemptionDate != null)
			msg.ExpiryDate = fix.RedemptionDate;

		if (fix.SecurityIds != null && fix.SecurityIds.Length > 0)
			msg.SecurityIds = fix.SecurityIds;

		return msg;
	}

	/// <inheritdoc />
	public virtual FixSecurityListRequest ToFixSecurityListRequest(SecurityLookupMessage message)
	{
		return new FixSecurityListRequest(
			SecurityReqId: FixId.FromLong(message.TransactionId),
			Currency: message.Currency?.ToFix(),
			SecurityDesc: message.Name,
			Strike: message.Strike,
			SecurityType: ToFixSecurityType(message.SecurityType),
			SecurityTypes: message.SecurityTypes?.Select(t => ToFixSecurityType(t)).Where(s => !s.IsEmpty()).ToArray(),
			CfiCode: message.CfiCode,
			Symbol: message.SecurityId.SecurityCode,
			SecurityExchange: message.SecurityId.BoardCode,
			Text: null,
			RedemptionDate: message.ExpiryDate,
			PutOrCall: message.OptionType?.ToFix(),
			Skip: message.Skip,
			Count: message.Count,
			OnlySecurityId: message.OnlySecurityId,
			SecurityIds: message.SecurityIds,
			DisableArchive: message.DisableArchive);
	}

	/// <inheritdoc />
	public virtual SecurityMessage ToMessage(FixSecurityStatusRequest fix)
	{
		var msg = new SecurityMessage
		{
			OriginalTransactionId = fix.SecurityStatusReqId.ToLong(),
			SecurityType = GetSecurityType(fix.SecurityType),
			SecurityId = new SecurityId
			{
				SecurityCode = fix.Symbol,
				BoardCode = fix.SecurityExchange,
			},
		};

		return msg;
	}

	/// <inheritdoc />
	public virtual FixSecurityStatusRequest ToFixSecurityStatusRequest(SecurityMessage message)
	{
		return new FixSecurityStatusRequest(
			SecurityStatusReqId: FixId.FromLong(message.OriginalTransactionId),
			SubscriptionRequestType: null,
			Symbol: message.SecurityId.SecurityCode,
			SecurityExchange: message.SecurityId.BoardCode,
			SecurityType: ToFixSecurityType(message.SecurityType));
	}

	/// <inheritdoc />
	public virtual SecurityLegsRequestMessage ToMessage(FixSecurityLegsRequest fix)
	{
		var msg = new SecurityLegsRequestMessage
		{
			TransactionId = fix.MdReqId.ToLong(),
			Like = fix.Symbol,
		};

		return msg;
	}

	/// <inheritdoc />
	public virtual FixSecurityLegsRequest ToFixSecurityLegsRequest(SecurityLegsRequestMessage message)
	{
		return new FixSecurityLegsRequest(
			MdReqId: FixId.FromLong(message.TransactionId),
			Symbol: message.Like);
	}

	/// <inheritdoc />
	public virtual SecurityMappingMessage ToMessage(FixSecurityMapping fix)
	{
		var msg = new SecurityMappingMessage
		{
			TransactionId = fix.MdReqId.ToLong(),
			StorageName = fix.IdSource,
			IsDelete = fix.Action == MDUpdateAction.Delete,
			Mapping = new SecurityIdMapping
			{
				StockSharpId = new SecurityId
				{
					SecurityCode = fix.Symbol,
					BoardCode = fix.SecExchange,
				},
				AdapterId = new SecurityId
				{
					SecurityCode = fix.AltId,
					BoardCode = fix.AltIdSource,
				},
			},
		};

		return msg;
	}

	/// <inheritdoc />
	public virtual FixSecurityMapping ToFixSecurityMapping(SecurityMappingMessage message)
	{
		return new FixSecurityMapping(
			// Preserve the storage name and the add/delete intent — dropping them made a
			// forwarded mapping lose its source and a delete be misread as an add on decode.
			IdSource: message.StorageName,
			Symbol: message.Mapping.StockSharpId.SecurityCode,
			SecExchange: message.Mapping.StockSharpId.BoardCode,
			AltId: message.Mapping.AdapterId.SecurityCode,
			AltIdSource: message.Mapping.AdapterId.BoardCode,
			Action: message.IsDelete ? MDUpdateAction.Delete : MDUpdateAction.New,
			MdReqId: FixId.FromLong(message.TransactionId));
	}
}
