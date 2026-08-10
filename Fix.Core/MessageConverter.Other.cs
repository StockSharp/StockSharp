namespace StockSharp.Fix;

/// <summary>
/// Other message converters: BoardLookup, BoardUpdate, Remove, HistoryStart, DataTypeLookup.
/// </summary>
partial class MessageConverter
{
	/// <inheritdoc />
	public virtual BoardLookupMessage ToMessage(FixBoardLookup fix)
	{
		return new BoardLookupMessage
		{
			TransactionId = fix.MdReqId.ToLong(),
			Like = fix.Like,
		};
	}

	/// <inheritdoc />
	public virtual FixBoardLookup ToFixBoardLookup(BoardLookupMessage message)
	{
		return new FixBoardLookup(
			MdReqId: FixId.FromLong(message.TransactionId),
			Like: message.Like,
			DisableArchive: false);
	}

	/// <inheritdoc />
	public virtual BoardMessage ToMessage(FixBoardUpdate fix)
	{
		return new BoardMessage
		{
			Code = fix.Board,
			ExchangeCode = fix.Exchange,
		};
	}

	/// <inheritdoc />
	public virtual FixBoardUpdate ToFixBoardUpdate(BoardMessage message)
	{
		return new FixBoardUpdate(
			Board: message.Code,
			Exchange: message.ExchangeCode);
	}

	/// <inheritdoc />
	public virtual RemoveMessage ToMessage(FixRemove fix)
	{
		var msg = new RemoveMessage
		{
			TransactionId = fix.MdReqId.ToLong(),
			RemoveId = fix.Id,
		};

		if (!fix.Type.IsEmpty())
			msg.RemoveType = fix.Type.To<RemoveTypes>();

		return msg;
	}

	/// <inheritdoc />
	public virtual FixRemove ToFixRemove(RemoveMessage message)
	{
		return new FixRemove(
			MdReqId: FixId.FromLong(message.TransactionId),
			Id: message.RemoveId,
			Type: message.RemoveType.To<string>());
	}

	/// <inheritdoc />
	public virtual (string RequestId, DateTime? From) ToHistoryStart(FixHistoryStart fix)
	{
		return (null, fix.StartDate);
	}

	/// <inheritdoc />
	public virtual FixHistoryStart ToFixHistoryStart(string requestId, DateTime? from)
	{
		return new FixHistoryStart(
			StartDate: from,
			EndDate: null);
	}

	/// <inheritdoc />
	public virtual DataTypeLookupMessage ToMessage(FixDataTypeLookup fix)
	{
		var msg = new DataTypeLookupMessage
		{
			TransactionId = fix.MdReqId.ToLong(),
			IncludeDates = fix.IncludeDates,
		};

		if (!fix.Symbol.IsEmpty() || !fix.Exchange.IsEmpty())
		{
			msg.SecurityId = new SecurityId
			{
				SecurityCode = fix.Symbol,
				BoardCode = fix.Exchange,
			};
		}

		if (fix.MdEntryType != null)
			msg.RequestDataType = fix.MdEntryType.Value.ToDataType(fix.MdEntryArg);

		if (fix.Format != null)
			msg.Format = fix.Format.Value;

		return msg;
	}

	/// <inheritdoc />
	public virtual FixDataTypeLookup ToFixDataTypeLookup(DataTypeLookupMessage message)
	{
		string arg = null;
		var mdType = message.RequestDataType?.ToFixMDType(out arg);

		return new FixDataTypeLookup(
			MdReqId: FixId.FromLong(message.TransactionId),
			Symbol: message.SecurityId.SecurityCode,
			Exchange: message.SecurityId.BoardCode,
			MdEntryType: mdType,
			MdEntryArg: arg,
			Format: message.Format,
			IncludeDates: message.IncludeDates);
	}
}
