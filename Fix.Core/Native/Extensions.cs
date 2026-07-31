namespace StockSharp.Fix.Native;

/// <summary>
/// FIX/FAST extension methods.
/// </summary>
public static partial class Extensions
{
	/// <summary>
	/// Default UTCDateOnly format.
	/// </summary>
	public const string DateFormat = "yyyyMMdd";

	/// <summary>
	/// Default MonthYear format.
	/// </summary>
	public const string YearMonthFormat = "yyyyMM";

	/// <summary>
	/// Default UTCTimeOnly format.
	/// </summary>
	public const string TimeFormat = "hh\\:mm\\:ss\\.fff";

	/// <summary>
	/// Default UTCTimestamp format.
	/// </summary>
	public const string TimeStampFormat = "yyyyMMdd-HH:mm:ss.fff";

	/// <summary>
	///
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static byte TryReplaceSoh(this byte value)
	{
		if (value == (int)AsciiSymbols.Soh)
			value = (byte)'|';

		return value;
	}

	/// <summary>
	/// Copy content.
	/// </summary>
	/// <param name="dest">Destination.</param>
	/// <param name="source">Source.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteStreamAsync(this IFixWriter dest, IFixWriter source, CancellationToken cancellationToken)
	{
		var stream = (MemoryStream)source.Stream;
		await dest.WriteBytesAsync(new ReadOnlyMemory<byte>(stream.GetBuffer(), 0, (int)stream.Position), cancellationToken);
		source.ClearState();
		stream.Position = 0;
	}

	/// <summary>
	/// Write FIX message.
	/// </summary>
	/// <param name="writer">Whole message writer.</param>
	/// <param name="bodyWriter">Body only writer.</param>
	/// <param name="version">Version.</param>
	/// <param name="msgType"><see cref="FixMessages"/> value.</param>
	/// <param name="senderCompId">Sender ID.</param>
	/// <param name="targetCompId">Target ID.</param>
	/// <param name="sendingTimeParser">Time parser.</param>
	/// <param name="seqNum">Sequence number.</param>
	/// <param name="handler">Handler.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteFixMessageAsync(this IFixWriter writer, IFixWriter bodyWriter, string version, string msgType, string senderCompId, string targetCompId, FastDateTimeParser sendingTimeParser, long seqNum, Action<IFixWriter> handler, CancellationToken cancellationToken)
	{
		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		await writer.WriteAsync(FixTags.BeginString, cancellationToken);
		await writer.WriteAsync(version, cancellationToken);

		await bodyWriter.WriteAsync(FixTags.MsgType, cancellationToken);
		await bodyWriter.WriteAsync(msgType, cancellationToken);

		if (!senderCompId.IsEmpty())
		{
			await bodyWriter.WriteAsync(FixTags.SenderCompID, cancellationToken);
			await bodyWriter.WriteAsync(senderCompId, cancellationToken);
		}

		if (!targetCompId.IsEmpty())
		{
			await bodyWriter.WriteAsync(FixTags.TargetCompID, cancellationToken);
			await bodyWriter.WriteAsync(targetCompId, cancellationToken);
		}

		await bodyWriter.WriteAsync(FixTags.SendingTime, cancellationToken);
		await bodyWriter.WriteAsync(DateTime.UtcNow, sendingTimeParser, cancellationToken);

		await bodyWriter.WriteAsync(FixTags.MsgSeqNum, cancellationToken);
		await bodyWriter.WriteAsync(seqNum, cancellationToken);

		handler(bodyWriter);

		await writer.WriteAsync(FixTags.BodyLength, cancellationToken);
		await writer.WriteAsync(bodyWriter.Stream.Position, cancellationToken);

		await writer.WriteStreamAsync(bodyWriter, cancellationToken);

		var checkSum = writer.CalcCheckSum();
		writer.CheckSum = 0;

		await writer.WriteAsync(FixTags.CheckSum, cancellationToken);
		await writer.WriteAsync($"{checkSum:000}", cancellationToken);
	}

	/// <summary>
	/// Write FIX message asynchronously with async handler.
	/// </summary>
	/// <param name="writer">Whole message writer.</param>
	/// <param name="bodyWriter">Body only writer.</param>
	/// <param name="version">Version.</param>
	/// <param name="msgType"><see cref="FixMessages"/> value.</param>
	/// <param name="senderCompId">Sender ID.</param>
	/// <param name="targetCompId">Target ID.</param>
	/// <param name="sendingTimeParser">Time parser.</param>
	/// <param name="seqNum">Sequence number.</param>
	/// <param name="handler">Async handler.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteFixMessageAsync(this IFixWriter writer, IFixWriter bodyWriter, string version, string msgType, string senderCompId, string targetCompId, FastDateTimeParser sendingTimeParser, long seqNum, Func<IFixWriter, CancellationToken, ValueTask> handler, CancellationToken cancellationToken)
	{
		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		try
		{
			await writer.WriteAsync(FixTags.BeginString, cancellationToken);
			await writer.WriteAsync(version, cancellationToken);

			await bodyWriter.WriteAsync(FixTags.MsgType, cancellationToken);
			await bodyWriter.WriteAsync(msgType, cancellationToken);

			if (!senderCompId.IsEmpty())
			{
				await bodyWriter.WriteAsync(FixTags.SenderCompID, cancellationToken);
				await bodyWriter.WriteAsync(senderCompId, cancellationToken);
			}

			if (!targetCompId.IsEmpty())
			{
				await bodyWriter.WriteAsync(FixTags.TargetCompID, cancellationToken);
				await bodyWriter.WriteAsync(targetCompId, cancellationToken);
			}

			await bodyWriter.WriteAsync(FixTags.SendingTime, cancellationToken);
			await bodyWriter.WriteAsync(DateTime.UtcNow, sendingTimeParser, cancellationToken);

			await bodyWriter.WriteAsync(FixTags.MsgSeqNum, cancellationToken);
			await bodyWriter.WriteAsync(seqNum, cancellationToken);

			await handler(bodyWriter, cancellationToken);

			await writer.WriteAsync(FixTags.BodyLength, cancellationToken);
			await writer.WriteAsync(bodyWriter.Stream.Position, cancellationToken);

			await writer.WriteStreamAsync(bodyWriter, cancellationToken);

			var checkSum = writer.CalcCheckSum();
			writer.CheckSum = 0;

			await writer.WriteAsync(FixTags.CheckSum, cancellationToken);
			await writer.WriteAsync($"{checkSum:000}", cancellationToken);
		}
		catch
		{
			// A failure mid-write (most reachably the body handler throwing) must not leave partial
			// bytes in the REUSED body writer. WriteStreamAsync above is the only place that resets it
			// on success, so without this the next message is appended AFTER the aborted body and ships
			// a single BodyLength- and checksum-consistent frame that silently swallows a real message.
			bodyWriter.ClearState();
			bodyWriter.Stream.SetLength(0);
			throw;
		}
	}

	/// <summary>
	/// To calculate the checksum.
	/// </summary>
	/// <param name="fix"><see cref="IFixBase"/></param>
	/// <returns>Checksum.</returns>
	public static int CalcCheckSum(this IFixBase fix)
	{
		if (fix is null)
			throw new ArgumentNullException(nameof(fix));

		return (int)(fix.CheckSum % 256);
	}

	/// <summary>
	/// Empty tag.
	/// </summary>
	public const FixTags EmptyTag = (FixTags)(-1);

	/// <summary>
	/// Read message.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="FixTags"/> values.</returns>
	public static async IAsyncEnumerable<FixTags> ReadMessageAsync(this IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		if (reader == null)
			throw new ArgumentNullException(nameof(reader));

		while (true)
		{
			yield return await reader.ReadTagAsync(cancellationToken);
		}
	}

	/// <summary>
	/// Read message.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="handler">Tag handler.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the message was successfully read, othewise, returns <see langword="false"/>.</returns>
	public static async ValueTask<bool> ReadMessageAsync(this IFixReader reader, Func<FixTags, CancellationToken, ValueTask<bool>> handler, CancellationToken cancellationToken)
	{
		await foreach (var tag in reader.ReadMessageAsync(cancellationToken).WithEnforcedCancellation(cancellationToken))
		{
			if (tag == EmptyTag)
				return false;

			if (tag == FixTags.CheckSum)
				return true;

			if (!await handler(tag, cancellationToken))
				await reader.SkipValueAsync(cancellationToken);
		}

		return true;
	}

	/// <summary>
	/// Read message.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="handler">Async tag handler.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the message was successfully read, othewise, returns <see langword="false"/>.</returns>
	public static async ValueTask<bool> ReadMessageAsync(this IFixReader reader, Func<FixTags, ValueTask<bool>> handler, CancellationToken cancellationToken)
	{
		if (reader == null)
			throw new ArgumentNullException(nameof(reader));

		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		while (true)
		{
			var tag = await reader.ReadTagAsync(cancellationToken);

			if (tag == EmptyTag)
				return false;

			if (tag == FixTags.CheckSum)
				return true;

			if (!await handler(tag))
				await reader.SkipValueAsync(cancellationToken);
		}
	}

	private const int _checkSumOffset = (byte)'1' + (byte)'0' + (byte)AsciiSymbols.Eq;

	/// <summary>
	/// Read FIX header.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="expectedVersion">Expected <see cref="FixVersions"/> value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="FixMessages"/> value.</returns>
	public static async ValueTask<string> ReadHeaderAsync(this IFixReader reader, string expectedVersion, CancellationToken cancellationToken)
	{
		reader.CheckSum = 0;
		reader.BytesCount = 2;

		if (!await reader.ReadTagAsync(FixTags.BeginString, cancellationToken))
			return null;

		var version = await reader.ReadStringAsync(cancellationToken);

		if (!version.EqualsIgnoreCase(expectedVersion))
		{
			// TODO
		}

		if (!await reader.ReadTagAsync(FixTags.BodyLength, cancellationToken))
			return null;

		/*var length = */
		await reader.ReadIntAsync(cancellationToken);

		if (!await reader.ReadTagAsync(FixTags.MsgType, cancellationToken))
			return null;

		var msgType = await reader.ReadStringAsync(cancellationToken);

		reader.BytesCount = 0;

		return msgType;
	}

	/// <summary>
	/// Read FIX trailer.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the message was successfully read, othewise, returns <see langword="false"/>.</returns>
	public static async ValueTask<bool> ReadTrailerAsync(this IFixReader reader, CancellationToken cancellationToken)
	{
		var calcCheckSum = (reader.CheckSum - _checkSumOffset) % 256;
		var receivedCheckSum = (await reader.ReadStringAsync(cancellationToken)).To<int>();

		if (!reader.CheckSumDisabled && calcCheckSum != receivedCheckSum)
			throw new InvalidOperationException("Check sum: calculated '{0}', received '{1}'.".Put(calcCheckSum, receivedCheckSum));

		return true;
	}

	/// <summary>
	/// Read tag and check if it matches expected tag.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="expectedTag">Expected tag.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the tag matches, <see langword="false"/> otherwise.</returns>
	public static async ValueTask<bool> ReadTagAsync(this IFixReader reader, FixTags expectedTag, CancellationToken cancellationToken)
	{
		var tag = await reader.ReadTagAsync(cancellationToken);
		return tag == expectedTag;
	}

	/// <summary>
	/// Skip reading message.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Possible error.</returns>
	public static async ValueTask<Exception> SkipMessageAsync(this IFixReader reader, CancellationToken cancellationToken)
	{
		try
		{
			// skip broken message
			while (reader.LastTag != EmptyTag)
			{
				if (!reader.IsValueRead)
					await reader.SkipValueAsync(cancellationToken);

				if (reader.LastTag == FixTags.CheckSum)
					break;

				await reader.ReadTagAsync(cancellationToken);
			}

			return null;
		}
		catch (Exception ex1)
		{
			return ex1;
		}
	}

	/// <summary>
	/// Convert <see cref="DataType"/> to <see cref="MDEntryType"/> value.
	/// </summary>
	/// <param name="type"><see cref="DataType"/> value.</param>
	/// <param name="mdEntryArg"><see cref="FixTags.MDEntryArg"/> value.</param>
	/// <returns><see cref="MDEntryType"/> value.</returns>
	public static char ToFixMDType(this DataType type, out string mdEntryArg)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		mdEntryArg = type.IsCandles && type.Arg != null ? type.DataTypeArgToString() : null;

		if (type == DataType.Level1)
			return Level1;
		else if (type == DataType.MarketDepth)
			return MDEntryType.Bid;
		else if (type == DataType.Ticks)
			return MDEntryType.Trade;
		else if (type == DataType.OrderLog)
			return OrderLog;
		else if (type == DataType.News)
			return News;
		else if (type == DataType.Transactions)
			return Transactions;
		else if (type.IsCandles)
		{
			if (_candleTypes.TryGetKey(type.MessageType, out var key))
				return key;

			return _candleTypes[typeof(TimeFrameCandleMessage)];
		}
		else
			throw new ArgumentOutOfRangeException(nameof(type), type, null);
	}

	/// <summary>
	/// Convert <see cref="MDEntryType"/> to <see cref="DataType"/> value.
	/// </summary>
	/// <param name="mdEntryType"><see cref="MDEntryType"/> value.</param>
	/// <param name="arg"><see cref="FixTags.MDEntryArg"/> value.</param>
	/// <returns><see cref="DataType"/> value.</returns>
	public static DataType ToDataType(this char mdEntryType, string arg)
	{
		switch (mdEntryType)
		{
			case Level1:
				return DataType.Level1;
			case OrderLog:
				return DataType.OrderLog;
			case MDEntryType.Trade:
				return DataType.Ticks;
			case MDEntryType.Bid:
			case MDEntryType.Offer:
				return DataType.MarketDepth;
			case News:
				return DataType.News;
			case Transactions:
				return DataType.Transactions;
			default:
			{
				if (_candleTypes.TryGetValue(mdEntryType, out var candleType))
					return DataType.Create(candleType, candleType.ToDataTypeArg(arg));

				throw new InvalidOperationException(LocalizedStrings.UnknownType.Put(mdEntryType));
			}
		}
	}

	/// <summary>
	/// Convert <see cref="SessionStates"/> to <see cref="TradSesStatus"/> value.
	/// </summary>
	/// <param name="state"><see cref="SessionStates"/> value.</param>
	/// <returns><see cref="TradSesStatus"/> value.</returns>
	public static TradSesStatus ToFix(this SessionStates state)
	{
		return state switch
		{
			SessionStates.Assigned => TradSesStatus.PreOpen,
			SessionStates.Active => TradSesStatus.Open,
			SessionStates.Paused => TradSesStatus.Halted,
			SessionStates.ForceStopped => TradSesStatus.Halted,
			SessionStates.Ended => TradSesStatus.Closed,
			_ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
		};
	}

	/// <summary>
	/// Convert <see cref="TradSesStatus"/> to <see cref="SessionStates"/> value.
	/// </summary>
	/// <param name="status"><see cref="TradSesStatus"/> value.</param>
	/// <returns><see cref="SessionStates"/> value.</returns>
	public static SessionStates FromFixStatus(this TradSesStatus status)
	{
		return status switch
		{
			TradSesStatus.Open or TradSesStatus.PreClose => SessionStates.Active,
			TradSesStatus.Closed => SessionStates.Ended,
			TradSesStatus.Halted => SessionStates.Paused,
			TradSesStatus.PreOpen => SessionStates.Assigned,
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="CurrencyTypes"/> to <see cref="string"/> value.
	/// </summary>
	/// <param name="type"><see cref="CurrencyTypes"/> value.</param>
	/// <returns><see cref="string"/> value.</returns>
	public static string ToFix(this CurrencyTypes type)
	{
		return type.To<string>();
	}

	/// <summary>
	/// Convert <see cref="OptionTypes"/> to <see cref="int"/> value.
	/// </summary>
	/// <param name="type"><see cref="OptionTypes"/> value.</param>
	/// <returns><see cref="int"/> value.</returns>
	public static int ToFix(this OptionTypes type)
	{
		return (int)(type == OptionTypes.Call ? PutOrCall.Call : PutOrCall.Put);
	}

	/// <summary>
	/// Convert <see cref="OptionStyles"/> to <see cref="int"/> value.
	/// </summary>
	/// <param name="style"><see cref="OptionStyles"/> value.</param>
	/// <returns><see cref="int"/> value.</returns>
	public static int ToFix(this OptionStyles style)
		=> style switch
		{
			OptionStyles.European => 0,
			OptionStyles.American => 1,
			OptionStyles.Exotic => 2,
			_ => throw new ArgumentOutOfRangeException(nameof(style), style, LocalizedStrings.InvalidValue),
		};

	/// <summary>
	/// Convert <see cref="int"/> to <see cref="OptionStyles"/> value.
	/// </summary>
	/// <param name="style"><see cref="int"/> value.</param>
	/// <returns><see cref="OptionStyles"/> value.</returns>
	public static OptionStyles FromFixOptionStyle(this int style)
		=> style switch
		{
			0 => OptionStyles.European,
			1 => OptionStyles.American,
			2 => OptionStyles.Exotic,
			_ => throw new ArgumentOutOfRangeException(nameof(style), style, LocalizedStrings.InvalidValue),
		};

	/// <summary>
	/// Convert <see cref="SettlementTypes"/> to <see cref="int"/> value.
	/// </summary>
	/// <param name="type"><see cref="SettlementTypes"/> value.</param>
	/// <returns><see cref="int"/> value.</returns>
	public static int ToFix(this SettlementTypes type)
		=> type switch
		{
			SettlementTypes.Delivery => 0,
			SettlementTypes.Cash => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};

	/// <summary>
	/// Convert <see cref="int"/> to <see cref="SettlementTypes"/> value.
	/// </summary>
	/// <param name="style"><see cref="int"/> value.</param>
	/// <returns><see cref="SettlementTypes"/> value.</returns>
	public static SettlementTypes FromFixSettlType(this int style)
		=> style switch
		{
			0 => SettlementTypes.Delivery,
			1 => SettlementTypes.Cash,
			_ => throw new ArgumentOutOfRangeException(nameof(style), style, LocalizedStrings.InvalidValue),
		};

	/// <summary>
	/// Convert <see cref="PutOrCall"/> to <see cref="OptionTypes"/> value.
	/// </summary>
	/// <param name="type"><see cref="PutOrCall"/> value.</param>
	/// <returns><see cref="OptionTypes"/> value.</returns>
	public static OptionTypes FromFixOptionType(this PutOrCall type)
	{
		return type switch
		{
			PutOrCall.Call => OptionTypes.Call,
			PutOrCall.Put => OptionTypes.Put,
			_ => throw new ArgumentOutOfRangeException(),
		};
	}

	/// <summary>
	/// Get <see cref="OrdType"/> value.
	/// </summary>
	/// <param name="message">A message containing info about the order.</param>
	/// <returns><see cref="OrdType"/> value.</returns>
	public static char GetFixType(this OrderMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var condition = message.Condition as FixOrderCondition;

		switch (condition?.Type)
		{
			case null:
			{
				switch (message.OrderType)
				{
					case null:
					case OrderTypes.Limit:
						return OrdType.Limit;
					case OrderTypes.Market:
						return OrdType.Market;
					case OrderTypes.Conditional:
					{
						return message.Condition switch
						{
							IStopLossOrderCondition slcond      => slcond.ActivationPrice == null ? OrdType.Stop : OrdType.StopLimit,
							ITakeProfitOrderCondition tpcond    => tpcond.ActivationPrice == null ? OrdType.Stop : OrdType.StopLimit,
							_                                   => OrdType.Stop
						};
					}
					default:
						throw new ArgumentOutOfRangeException(message.OrderType.ToString());
				}
			}
			case FixStopOrderTypes.StopLoss:
				return condition.StopLoss is null ? OrdType.Stop : OrdType.StopLimit;
			case FixStopOrderTypes.StopLossTrailing:
				return StopTrailing;
			case FixStopOrderTypes.TakeProfit:
				return TakeProfit;
			case FixStopOrderTypes.TakeProfitTrailing:
				return TakeProfitTrailing;
			case FixStopOrderTypes.MarketOnClose:
				return OrdType.MarketOnClose;
			case FixStopOrderTypes.MarketIfTouched:
				return OrdType.MarketIfTouched;
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(message.OrderType, message.TransactionId));
		}
	}

	/// <summary>
	/// Set <see cref="OrdType"/> value.
	/// </summary>
	/// <param name="message">A message containing info about the order.</param>
	/// <param name="ordType"><see cref="OrdType"/> value.</param>
	public static void SetOrderType(this OrderMessage message, char ordType)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		message.OrderType = ordType switch
		{
			OrdType.Limit => (OrderTypes?)OrderTypes.Limit,
			OrdType.Market => (OrderTypes?)OrderTypes.Market,
			OrdType.Stop or OrdType.StopLimit or OrdType.MarketIfTouched or OrdType.MarketOnClose or TakeProfit or TakeProfitTrailing or StopTrailing => (OrderTypes?)OrderTypes.Conditional,
			_ => throw new ArgumentOutOfRangeException(nameof(ordType), ordType, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="bool"/> to <see cref="TickDirection"/> value.
	/// </summary>
	/// <param name="dir"><see cref="bool"/> value.</param>
	/// <returns><see cref="TickDirection"/> value.</returns>
	public static char ToTickDir(this bool dir)
	{
		return dir ? TickDirection.PlusTick : TickDirection.MinusTick;
	}

	/// <summary>
	/// Convert <see cref="TickDirection"/> to <see cref="bool"/> value.
	/// </summary>
	/// <param name="dir"><see cref="TickDirection"/> value.</param>
	/// <returns><see cref="bool"/> value.</returns>
	public static bool FromTickDir(this char dir)
	{
		return dir switch
		{
			TickDirection.ZeroPlusTick or TickDirection.PlusTick => true,
			TickDirection.MinusTick or TickDirection.ZeroMinusTick => false,
			_ => throw new ArgumentOutOfRangeException(nameof(dir), dir, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="Sides"/> to <see cref="Side"/> value.
	/// </summary>
	/// <param name="side"><see cref="Sides"/> value.</param>
	/// <returns><see cref="Side"/> value.</returns>
	public static char ToFix(this Sides side)
	{
		return side == Sides.Buy ? Side.Buy : Side.Sell;
	}

	/// <summary>
	/// Convert <see cref="Side"/> to <see cref="Sides"/> value.
	/// </summary>
	/// <param name="side"><see cref="Side"/> value.</param>
	/// <param name="required"><paramref name="side"/> is required.</param>
	/// <returns><see cref="Sides"/> value.</returns>
	public static Sides FromFixSide(this char side, bool required = false)
	{
		switch (side)
		{
			case Side.Undisclosed:
			{
				if (required)
					throw new ArgumentNullException(nameof(side));

				return default;
			}
			case Side.Buy:
			case Side.BuyMinus:
				return Sides.Buy;
			case Side.Sell:
			case Side.SellShort:
			case Side.SellPlus:
			case Side.SellShortExempt:
			case Side.CrossShort:
			case Side.CrossShortExempt:
				return Sides.Sell;
			default:
				throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue);
		}
	}

	/// <summary>
	/// Convert <see cref="ExecutionMessage"/> to <see cref="OrdStatus"/> value.
	/// </summary>
	/// <param name="message"><see cref="ExecutionMessage"/> value.</param>
	/// <returns><see cref="OrdStatus"/> value.</returns>
	public static char? ToFixOrdStatus(this ExecutionMessage message)
	{
		switch (message.OrderState)
		{
			case OrderStates.Active:
				return OrdStatus.New;
			case OrderStates.Done:
				return message.Balance == null || message.Balance == 0 ? OrdStatus.Filled : OrdStatus.Canceled;
			case OrderStates.Failed:
				return OrdStatus.Rejected;
			case OrderStates.Pending:
				return OrdStatus.PendingNew;
		}

		if (!message.IsOk())
			return OrdStatus.Rejected;

		return null;
	}

	/// <summary>
	/// Convert <see cref="OrdStatus"/> to <see cref="OrderStates"/> value.
	/// </summary>
	/// <param name="status"><see cref="OrdStatus"/> value.</param>
	/// <returns><see cref="OrderStates"/> value or null.</returns>
	public static OrderStates? FromFixStatus2(this char status)
	{
		return status switch
		{
			OrdStatus.PendingNew or OrdStatus.PendingReplace => (OrderStates?)OrderStates.Pending,
			OrdStatus.New or OrdStatus.PartiallyFilled or OrdStatus.PendingCancel => (OrderStates?)OrderStates.Active,
			OrdStatus.Canceled or OrdStatus.Filled or OrdStatus.Expired or OrdStatus.Calculated => (OrderStates?)OrderStates.Done,
			OrdStatus.Replaced or OrdStatus.AcceptedForBidding => (OrderStates?)OrderStates.Active,
			OrdStatus.Rejected => (OrderStates?)OrderStates.Failed,
			_ => null,
		};
	}

	/// <summary>
	/// Convert <see cref="OrdStatus"/> to <see cref="OrderStates"/> value.
	/// </summary>
	/// <param name="status"><see cref="OrdStatus"/> value.</param>
	/// <returns><see cref="OrderStates"/> value.</returns>
	public static OrderStates FromFixStatus(this char status)
		=> status.FromFixStatus2() ?? throw new ArgumentOutOfRangeException(nameof(status), status, LocalizedStrings.InvalidValue);

	/// <summary>
	/// Encode the order's lifetime policy as a FIX <see cref="FixTimeInForce"/> value.
	/// </summary>
	/// <remarks>
	/// Always answers, because an absent tag 59 reads as Day, so order entry always states it.
	/// No till-date gives GoodTillCancel; a till-date equal to the
	/// <see cref="Messages.Extensions.Today"/> sentinel gives Day; any other date gives
	/// GoodTillDate, which is also what makes a writer emit the expiry tag alongside.
	/// </remarks>
	/// <param name="message">The message containing the information for the order registration.</param>
	/// <returns><see cref="FixTimeInForce"/> value.</returns>
	public static char GetFixTimeInForce(this OrderRegisterMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		return message.TimeInForce.ToFix(message.TillDate);
	}

	/// <summary>
	/// Encode a lifetime policy and its expiry as a FIX <see cref="FixTimeInForce"/> value.
	/// </summary>
	/// <remarks>
	/// The enum on its own cannot say how long an order lives: GoodTillCancel, Day and
	/// GoodTillDate are all <see cref="Messages.TimeInForce.PutInQueue"/>, and the expiry is what
	/// tells them apart - no date gives GoodTillCancel, the
	/// <see cref="Messages.Extensions.Today"/> sentinel gives Day, any other date gives
	/// GoodTillDate (which is also what makes a writer emit the expiry tag alongside). Encoding
	/// the enum alone collapses all three to GoodTillCancel.
	///
	/// A <see langword="null"/> policy is treated as PutInQueue: an absent tag 59 reads as Day, so
	/// a writer has to state something, and "unspecified" is not a thing the wire can carry.
	/// </remarks>
	/// <param name="tif"><see cref="Messages.TimeInForce"/> value.</param>
	/// <param name="tillDate">The order's expiry, when it has one.</param>
	/// <returns><see cref="FixTimeInForce"/> value.</returns>
	public static char ToFix(this Messages.TimeInForce? tif, DateTime? tillDate)
	{
		switch (tif)
		{
			case Messages.TimeInForce.PutInQueue:
			case null:
			{
				if (tillDate == null)
					return FixTimeInForce.GoodTillCancel;
				else if (tillDate.Value.IsToday())
					return FixTimeInForce.Day;
				else
					return FixTimeInForce.GoodTillDate;
			}
			case Messages.TimeInForce.MatchOrCancel:
				return FixTimeInForce.FillOrKill;
			case Messages.TimeInForce.CancelBalance:
				return FixTimeInForce.ImmediateOrCancel;
			default:
				throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue);
		}
	}

	/// <summary>
	/// Convert <see cref="Messages.TimeInForce"/> to <see cref="FixTimeInForce"/> value.
	/// </summary>
	/// <param name="tif"><see cref="Messages.TimeInForce"/> value.</param>
	/// <returns><see cref="FixTimeInForce"/> value.</returns>
	public static char ToFix(this Messages.TimeInForce tif)
	{
		return tif switch
		{
			Messages.TimeInForce.PutInQueue => FixTimeInForce.GoodTillCancel,
			Messages.TimeInForce.MatchOrCancel => FixTimeInForce.FillOrKill,
			Messages.TimeInForce.CancelBalance => FixTimeInForce.ImmediateOrCancel,
			_ => throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue),
		};
	}

	private const string _secTypeCommodity = "COMMODITY";
	private const string _secTypeSpread = "SPREAD";
	private const string _secTypeStrategy = "STRATEGY";
	private const string _secTypeVolatility = "VOLA";
	private const string _cryptoCurrency = "CryptoCurrency";
	private const string _secTypeRepo = "REPO";

	/// <summary>
	/// Convert <see cref="SecurityTypes"/> to <see cref="SecurityType"/> value.
	/// </summary>
	/// <param name="type"><see cref="SecurityTypes"/> value.</param>
	/// <returns><see cref="SecurityType"/> value.</returns>
	public static string ToFix(this SecurityTypes type)
	{
		return type switch
		{
			SecurityTypes.Stock => SecurityType.CommonStock,
			SecurityTypes.Future => SecurityType.Future,
			SecurityTypes.Option => SecurityType.Option,
			SecurityTypes.Index => SecurityType.IndexLinked,
			SecurityTypes.Currency => SecurityType.FXSpot,
			SecurityTypes.CryptoCurrency => _cryptoCurrency,
			SecurityTypes.Bond => SecurityType.BradyBond,
			SecurityTypes.Warrant => SecurityType.Warrant,
			SecurityTypes.Forward => SecurityType.Forward,
			SecurityTypes.Swap => SecurityType.CreditDefaultSwap,
			SecurityTypes.Commodity => _secTypeCommodity,
			SecurityTypes.Cfd => SecurityType.FXForward,
			SecurityTypes.News => SecurityType.Overnight,
			SecurityTypes.Weather => SecurityType.LetterOfCredit,
			SecurityTypes.Fund => SecurityType.MutualFund,
			SecurityTypes.Adr => SecurityType.YankeeCertificateOfDeposit,
			SecurityTypes.Etf => SecurityType.ForeignExchangeContract,
			SecurityTypes.MultiLeg => SecurityType.MultilegInstrument,
			SecurityTypes.Loan => SecurityType.SecuritiesLoan,
			SecurityTypes.Spread => _secTypeSpread,
			SecurityTypes.Gdr => SecurityType.EuroCommercialPaper,
			SecurityTypes.Receipt => SecurityType.DepositNotes,
			SecurityTypes.Indicator => SecurityType.ExtendedCommNote,
			SecurityTypes.Strategy => _secTypeStrategy,
			SecurityTypes.Volatility => _secTypeVolatility,
			SecurityTypes.Repo => _secTypeRepo,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="SecurityType"/> to <see cref="SecurityTypes"/> value.
	/// </summary>
	/// <param name="type"><see cref="SecurityType"/> value.</param>
	/// <returns><see cref="SecurityTypes"/> value.</returns>
	public static SecurityTypes? FromFixType(this string type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		return type switch
		{
			SecurityType.CommonStock or "Equity" => (SecurityTypes?)SecurityTypes.Stock,
			SecurityType.Future => (SecurityTypes?)SecurityTypes.Future,
			SecurityType.Option => (SecurityTypes?)SecurityTypes.Option,
			SecurityType.IndexLinked => (SecurityTypes?)SecurityTypes.Index,
			SecurityType.FXSpot => (SecurityTypes?)SecurityTypes.Currency,
			SecurityType.FXForward => (SecurityTypes?)SecurityTypes.Cfd,
			_cryptoCurrency => (SecurityTypes?)SecurityTypes.CryptoCurrency,
			SecurityType.BradyBond => (SecurityTypes?)SecurityTypes.Bond,
			SecurityType.Warrant => (SecurityTypes?)SecurityTypes.Warrant,
			SecurityType.Forward => (SecurityTypes?)SecurityTypes.Forward,
			SecurityType.CreditDefaultSwap => (SecurityTypes?)SecurityTypes.Swap,
			_secTypeCommodity => (SecurityTypes?)SecurityTypes.Commodity,
			SecurityType.YankeeCertificateOfDeposit => (SecurityTypes?)SecurityTypes.Adr,
			SecurityType.Overnight => (SecurityTypes?)SecurityTypes.News,
			SecurityType.LetterOfCredit => (SecurityTypes?)SecurityTypes.Weather,
			SecurityType.ForeignExchangeContract => (SecurityTypes?)SecurityTypes.Etf,
			SecurityType.MultilegInstrument => (SecurityTypes?)SecurityTypes.MultiLeg,
			SecurityType.SecuritiesLoan => (SecurityTypes?)SecurityTypes.Loan,
			_secTypeSpread => (SecurityTypes?)SecurityTypes.Spread,
			SecurityType.EuroCommercialPaper => (SecurityTypes?)SecurityTypes.Gdr,
			SecurityType.DepositNotes => (SecurityTypes?)SecurityTypes.Receipt,
			SecurityType.ExtendedCommNote => (SecurityTypes?)SecurityTypes.Indicator,
			_secTypeStrategy => (SecurityTypes?)SecurityTypes.Strategy,
			_secTypeVolatility => (SecurityTypes?)SecurityTypes.Volatility,
			_secTypeRepo => (SecurityTypes?)SecurityTypes.Repo,
			_ => type.Iso10962ToSecurityType(),
		};
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="message">A message containing info about the order.</param>
	/// <returns><see cref="SecurityType"/> value.</returns>
	public static string ToSecurityType(this OrderMessage message)
	{
		return message.SecurityType?.ToFix();
	}

	/// <summary>
	/// Convert <see cref="KeyValuePair{TKey, TValue}"/> to <see cref="MDEntry"/>.
	/// </summary>
	/// <param name="change">Change.</param>
	/// <param name="time">Time.</param>
	/// <param name="dateTimeParser">Time parser.</param>
	/// <returns><see cref="MDEntry"/> value.</returns>
	public static MDEntry ToFix(this KeyValuePair<Level1Fields, object> change, DateTime time, FastDateTimeParser dateTimeParser)
	{
		if (change.Value == null)
			throw new ArgumentNullException(nameof(change));

		var fixType = change.Key.ToFix();

		var isSize = false;

		switch (change.Key)
		{
			case Level1Fields.OpenInterest:
			case Level1Fields.BidsVolume:
			case Level1Fields.BidsCount:
			case Level1Fields.AsksVolume:
			case Level1Fields.AsksCount:
			case Level1Fields.VolumeStep:
			case Level1Fields.Multiplier:
			case Level1Fields.LastTradeVolume:
			case Level1Fields.BestBidVolume:
			case Level1Fields.BestAskVolume:
			case Level1Fields.Volume:
			case Level1Fields.TradesCount:
			case Level1Fields.IssueSize:
			case Level1Fields.MinVolume:
			case Level1Fields.MaxVolume:
			case Level1Fields.UnderlyingMinVolume:
				isSize = true;
				break;
			case Level1Fields.LastTradeId:
			case Level1Fields.LastTradeOrigin:
			case Level1Fields.LastTradeUpDown:
			case Level1Fields.IsSystem:
				return new MDEntry
				{
					Type = fixType,
					OtherValue = change.Value.To<string>(),
					Time = time,
				};
			case Level1Fields.LastTradeTime:
			case Level1Fields.BuyBackDate:
			case Level1Fields.CouponDate:
			case Level1Fields.BestBidTime:
			case Level1Fields.BestAskTime:
				return new MDEntry
				{
					Type = fixType,
					OtherValue = dateTimeParser.ToString((DateTime)change.Value),
					Time = time,
				};
			case Level1Fields.LastTradeStringId:
				return new MDEntry
				{
					Type = fixType,
					OtherValue = (string)change.Value,
					Time = time,
				};
		}

		var entry = new MDEntry
		{
			Type = fixType,
			Time = time,
		};

		// some fields has integer values
		var value = change.Value.To<decimal>();

		if (isSize)
			entry.Size = value;
		else
			entry.Price = value;

		return entry;
	}

	/// <summary>
	/// <see cref="Level1ChangeMessage"/>.
	/// </summary>
	public const char Level1 = '*';
	/// <summary>
	/// <see cref="NewsMessage"/>.
	/// </summary>
	public const char News = 'n';
	/// <summary>
	/// <see cref="TimeFrameCandleMessage"/>.
	/// </summary>
	public const char CandleTimeFrame = 'W';
	/// <summary>
	/// <see cref="VolumeCandleMessage"/>.
	/// </summary>
	public const char CandleVolume = 'V';
	/// <summary>
	/// <see cref="TickCandleMessage"/>.
	/// </summary>
	public const char CandleTick = 'i';
	/// <summary>
	/// <see cref="RangeCandleMessage"/>.
	/// </summary>
	public const char CandleRange = 'z';
	/// <summary>
	/// <see cref="RenkoCandleMessage"/>.
	/// </summary>
	public const char CandleRenko = 'R';
	/// <summary>
	/// <see cref="PnFCandleMessage"/>.
	/// </summary>
	public const char CandlePnF = 'x';
	/// <summary>
	/// <see cref="HeikinAshiCandleMessage"/>.
	/// </summary>
	public const char CandleHeikin = '%';
	/// <summary>
	/// <see cref="DataType.OrderLog"/>.
	/// </summary>
	public const char OrderLog = 'I';
	/// <summary>
	/// <see cref="DataType.Transactions"/>.
	/// </summary>
	public const char Transactions = 'Q';

	/// <summary>
	/// <see cref="FixStopOrderTypes.TakeProfit"/>.
	/// </summary>
	public const char TakeProfit = 'T';
	/// <summary>
	/// <see cref="FixStopOrderTypes.TakeProfitTrailing"/>.
	/// </summary>
	public const char TakeProfitTrailing = 'W';
	/// <summary>
	/// <see cref="FixStopOrderTypes.StopLossTrailing"/>.
	/// </summary>
	public const char StopTrailing = 'Z';

	private static readonly PairSet<char, Type> _candleTypes = new()
	{
		{ CandleTimeFrame, typeof(TimeFrameCandleMessage) },
		{ CandleVolume, typeof(VolumeCandleMessage) },
		{ CandleTick, typeof(TickCandleMessage) },
		{ CandleRange, typeof(RangeCandleMessage) },
		{ CandleRenko, typeof(RenkoCandleMessage) },
		{ CandlePnF, typeof(PnFCandleMessage) },
		{ CandleHeikin, typeof(HeikinAshiCandleMessage) },
	};

	/// <summary>
	/// Register new candle type.
	/// </summary>
	/// <param name="code"><see cref="MDEntry"/> value.</param>
	/// <param name="messageType">The type of candle message.</param>
	public static void RegisterCandleType(char code, Type messageType)
		=> _candleTypes.Add(code, messageType ?? throw new ArgumentNullException(nameof(messageType)));

	/// <summary>
	/// Check the specified type is candle.
	/// </summary>
	/// <param name="mdEntryType"><see cref="MDEntry"/> value.</param>
	/// <returns>Check result.</returns>
	public static bool IsCandleEntry(this char mdEntryType) => _candleTypes.ContainsKey(mdEntryType);

	/// <summary>
	/// Convert <see cref="MDEntryType"/> to <see cref="CandleMessage"/> value.
	/// </summary>
	/// <param name="entryType"><see cref="MDEntryType"/> value.</param>
	/// <returns><see cref="CandleMessage"/> value.</returns>
	public static CandleMessage ToCandleMessage(this char entryType)
	{
		if (_candleTypes.TryGetValue(entryType, out var candleType))
			return candleType.CreateInstance<CandleMessage>();

		throw new ArgumentOutOfRangeException(nameof(entryType), entryType, LocalizedStrings.InvalidValue);
	}

	private const char _mmOrderCapacity = 'P';
	private const string _mmOrderRestrictions = "5";

	/// <summary>
	/// Write <see cref="FixTags.OrderCapacity"/> and <see cref="FixTags.OrderRestrictions"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteMarketMakerAsync(this IFixWriter writer, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.OrderCapacity, cancellationToken);
		await writer.WriteAsync(_mmOrderCapacity, cancellationToken);

		await writer.WriteAsync(FixTags.OrderRestrictions, cancellationToken);
		await writer.WriteAsync(_mmOrderRestrictions, cancellationToken);
	}

	/// <summary>
	/// Is the order of market-maker.
	/// </summary>
	/// <param name="orderCapacity"><see cref="FixTags.OrderCapacity"/> value.</param>
	/// <param name="orderRestrictions"><see cref="FixTags.OrderRestrictions"/> value.</param>
	/// <returns>Check result.</returns>
	public static bool IsMarketMaker(char? orderCapacity, string orderRestrictions)
	{
		return orderCapacity == _mmOrderCapacity && orderRestrictions == _mmOrderRestrictions;
	}

	/// <summary>
	/// Convert <see cref="UserRequestType"/> to <see cref="FixUserRequestTypes"/> value.
	/// </summary>
	/// <param name="type"><see cref="UserRequestType"/> value.</param>
	/// <returns><see cref="FixUserRequestTypes"/> value.</returns>
	public static FixUserRequestTypes ToUserRequestType(this UserRequestType type)
	{
		return type switch
		{
			//case UserRequestType.LogOnUser:
			//case UserRequestType.RequestIndividualUserStatus:
			//	return FixUserRequestTypes.Lookup;
			UserRequestType.LogOffUser => FixUserRequestTypes.LogoffForce,
			UserRequestType.ChangePasswordForUser => FixUserRequestTypes.ChangePassword,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="FixUserResponseTypes"/> to <see cref="UserStatus"/> value.
	/// </summary>
	/// <param name="type"><see cref="FixUserResponseTypes"/> value.</param>
	/// <param name="error"></param>
	/// <param name="userName"></param>
	/// <param name="requestType"></param>
	/// <param name="text"></param>
	/// <returns><see cref="UserStatus"/> value.</returns>
	public static UserStatus ToUserStatus(this FixUserResponseTypes type, Exception error, string userName, UserRequestType requestType, out string text)
	{
		text = error?.Message;

		switch (type)
		{
			case FixUserResponseTypes.PasswordChanged:
				if (text.IsEmpty())
					text = LocalizedStrings.PasswordChangedOk;
				return UserStatus.PasswordChanged;
			case FixUserResponseTypes.UserLoggedOff:
				if (text.IsEmpty())
					text = LocalizedStrings.UserLoggedOut;
				return UserStatus.ForcedUserLogoutByExchange;
			case FixUserResponseTypes.UserFound:
				text = string.Empty;
				return UserStatus.LoggedIn;
			case FixUserResponseTypes.UserNotFound:
				if (text.IsEmpty())
					text = LocalizedStrings.ElementNotFoundParams.Put(userName);
				return UserStatus.UserNotRecognised;
			case FixUserResponseTypes.UserBlocked:
				if (text.IsEmpty())
					text = LocalizedStrings.UserBlocked;
				return UserStatus.Other;
			case FixUserResponseTypes.OldPasswordIncorrect:
				if (text.IsEmpty())
					text = LocalizedStrings.PasswordWasNotChangedCauseError;
				return UserStatus.PasswordIncorrect;
			case FixUserResponseTypes.NewPasswordIncorrect:
				if (text.IsEmpty())
					text = LocalizedStrings.PasswordWasNotChangedCauseError;
				return UserStatus.PasswordIncorrect;
			case FixUserResponseTypes.NotSupported:
				if (text.IsEmpty())
					text = LocalizedStrings.UnsupportedType.Put(requestType);
				return UserStatus.Other;
			case FixUserResponseTypes.UnknownError:
				if (text.IsEmpty())
					text = LocalizedStrings.UnknownServerError;
				return UserStatus.Other;
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
		}
	}

	/// <summary>
	/// Convert <see cref="QuoteType"/> to <see cref="SecurityStates"/> value.
	/// </summary>
	/// <param name="type"><see cref="QuoteType"/> value.</param>
	/// <returns><see cref="SecurityStates"/> value.</returns>
	public static SecurityStates? FromQuoteType(this int? type)
	{
		return type switch
		{
			null => null,
			(int)QuoteType.Tradeable or (int)QuoteType.Indicative or (int)QuoteType.Counter => (SecurityStates?)SecurityStates.Trading,
			(int)QuoteType.RestrictedTradeable => (SecurityStates?)SecurityStates.Stoped,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type.Value, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="Urgency"/> to <see cref="NewsPriorities"/> value.
	/// </summary>
	/// <param name="urgency"><see cref="Urgency"/> value.</param>
	/// <returns><see cref="NewsPriorities"/> value.</returns>
	public static NewsPriorities? ToNewsPriority(this char? urgency)
	{
		return urgency switch
		{
			null => null,
			Urgency.Background => (NewsPriorities?)NewsPriorities.Low,
			Urgency.Normal => (NewsPriorities?)NewsPriorities.Regular,
			Urgency.Flash => (NewsPriorities?)NewsPriorities.High,
			_ => null,
		};
	}

	/// <summary>
	/// Write <see cref="FixTags.HandlInst"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="message">Message.</param>
	/// <param name="defaultValue">Default value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteHandlInstAsync(this IFixWriter writer, OrderRegisterMessage message, char defaultValue, CancellationToken cancellationToken)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		await writer.WriteAsync(FixTags.HandlInst, cancellationToken);
		await writer.WriteAsync(message.IsManual == true ? HandlInst.ManualOrder : defaultValue, cancellationToken);
	}

	/// <summary>
	/// Write <see cref="FixTags.Side"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="side">Side.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteSideAsync(this IFixWriter writer, Sides side, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		await writer.WriteAsync(FixTags.Side, cancellationToken);
		await writer.WriteAsync(side.ToFix(), cancellationToken);
	}

	/// <summary>
	/// <see cref="PositionChangeTypes.Commission"/>.
	/// </summary>
	public const string Commission = "COMM";
	/// <summary>
	/// <see cref="PositionChangeTypes.RealizedPnL"/>.
	/// </summary>
	public const string RealizedPnL = "RPNL";
	/// <summary>
	/// <see cref="PositionChangeTypes.UnrealizedPnL"/>.
	/// </summary>
	public const string UnrealizedPnL = "UPNL";
	/// <summary>
	/// <see cref="PositionChangeTypes.Leverage"/>.
	/// </summary>
	public const string Leverage = "LVRG";
	/// <summary>
	/// <see cref="PositionChangeTypes.LiquidationPrice"/>.
	/// </summary>
	public const string LiquidationPrice = "LP";

	/// <summary>
	/// Convert <see cref="SubscriptionRequestType"/> to <see cref="bool"/> value.
	/// </summary>
	/// <param name="type"><see cref="SubscriptionRequestType"/> value.</param>
	/// <returns><see cref="bool "/> value.</returns>
	public static bool IsSubscribe(this char? type)
	{
		//if (type == null)
		//	throw new InvalidOperationException("Request type is missing.");

		return type == null || type.Value.IsSubscribe();
	}

	/// <summary>
	/// Convert <see cref="SubscriptionRequestType"/> to <see cref="bool"/> value.
	/// </summary>
	/// <param name="type"><see cref="SubscriptionRequestType"/> value.</param>
	/// <returns><see cref="bool "/> value.</returns>
	public static bool IsSubscribe(this char type)
	{
		return type == SubscriptionRequestType.Snapshot || type == SubscriptionRequestType.SnapshotPlusUpdates;
	}

	/// <summary>
	/// Get request id.
	/// </summary>
	/// <param name="msg">Subscription.</param>
	/// <returns></returns>
	public static long GetRequestId(this ISubscriptionMessage msg)
	{
		if (msg == null)
			throw new ArgumentNullException(nameof(msg));

		return msg.IsSubscribe ? msg.TransactionId : msg.OriginalTransactionId;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="msg">Subscription.</param>
	/// <returns><see cref="SubscriptionRequestType"/> value.</returns>
	public static char GetSubscriptionType(this ISubscriptionMessage msg)
	{
		if (msg == null)
			throw new ArgumentNullException(nameof(msg));

		return msg.IsSubscribe ? (msg.To == null ? SubscriptionRequestType.SnapshotPlusUpdates : SubscriptionRequestType.Snapshot) : SubscriptionRequestType.DisablePrevious;
	}

	/// <summary>
	/// Read <see cref="OrderCondition"/>.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="tag">Tag.</param>
	/// <param name="getCondition">Handler.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the tag was handled, <see langword="false"/> if the tag is unknown.</returns>
	public static async ValueTask<bool> ReadOrderConditionAsync(this IFixReader reader, FixTags tag, Func<OrderCondition> getCondition, CancellationToken cancellationToken)
	{
		switch (tag)
		{
			case FixTags.StopPx:
				((FixOrderCondition)getCondition()).StopLoss = await reader.ReadDecimalAsync(cancellationToken);
				return true;

			case FixTags.PegOffsetValue:
				((FixOrderCondition)getCondition()).Offset = await reader.ReadDecimalAsync(cancellationToken);
				return true;

			case FixTags.TakeProfit:
				((FixOrderCondition)getCondition()).TakeProfit = await reader.ReadDecimalAsync(cancellationToken);
				return true;

			default:
				return false;
		}
	}

	/// <summary>
	/// Convert <see cref="QuoteCondition"/> to <see cref="QuoteConditions"/> value.
	/// </summary>
	/// <param name="condition"><see cref="QuoteCondition"/> value.</param>
	/// <returns><see cref="QuoteConditions"/> value.</returns>
	public static QuoteConditions ToQuoteCondition(this string condition)
	{
		if (condition.IsEmpty())
			return default;

		// TODO
		return condition[0] switch
		{
			QuoteCondition.Active => QuoteConditions.Active,
			QuoteCondition.Indicative => QuoteConditions.Indicative,
			_ => throw new ArgumentOutOfRangeException(nameof(condition), condition, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="QuoteConditions"/> to <see cref="QuoteCondition"/> value.
	/// </summary>
	/// <param name="condition"><see cref="QuoteConditions"/> value.</param>
	/// <param name="force"></param>
	/// <returns><see cref="QuoteCondition"/> value.</returns>
	public static char? ToNative(this QuoteConditions condition, bool force = false)
	{
		return condition switch
		{
			QuoteConditions.Active => force ? QuoteCondition.Active : null,
			QuoteConditions.Indicative => QuoteCondition.Indicative,
			_ => throw new ArgumentOutOfRangeException(nameof(condition), condition, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="QuoteChangeActions"/> to <see cref="MDUpdateAction"/> value.
	/// </summary>
	/// <param name="action"><see cref="QuoteChangeActions"/> value.</param>
	/// <returns><see cref="MDUpdateAction"/> value.</returns>
	public static char ToNative(this QuoteChangeActions action)
	{
		return action switch
		{
			QuoteChangeActions.New => MDUpdateAction.New,
			QuoteChangeActions.Update => MDUpdateAction.Change,
			QuoteChangeActions.Delete => MDUpdateAction.Delete,
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="MDUpdateAction"/> to <see cref="QuoteChangeActions"/> value.
	/// </summary>
	/// <param name="action"><see cref="MDUpdateAction"/> value.</param>
	/// <returns><see cref="QuoteChangeActions"/> value.</returns>
	public static QuoteChangeActions ToQuoteAction(this char action)
	{
		return action switch
		{
			MDUpdateAction.New => QuoteChangeActions.New,
			MDUpdateAction.Change => QuoteChangeActions.Update,
			MDUpdateAction.Delete or MDUpdateAction.DeleteFrom or MDUpdateAction.DeleteThru or MDUpdateAction.Overlay => QuoteChangeActions.Delete,
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Read time.
	/// </summary>
	/// <param name="reader">Reader.</param>
	/// <param name="parser">Time parser.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Time.</returns>
	public static async ValueTask<DateTime> ReadUtcAsync(this IFixReader reader, FastDateTimeParser parser, CancellationToken cancellationToken)
		=> (await reader.ReadDateTimeAsync(parser, cancellationToken)).UtcKind();

	/// <summary>
	/// Write time.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="dto">Time.</param>
	/// <param name="parser">Time parser.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static ValueTask WriteUtcAsync(this IFixWriter writer, DateTime dto, FastDateTimeParser parser, CancellationToken cancellationToken)
		=> writer.WriteAsync(dto, parser, cancellationToken);

	/// <summary>
	/// Write <see cref="FixTags.TransactTime"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="parser">Time parser.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteTransactTimeAsync(this IFixWriter writer, FastDateTimeParser parser, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.TransactTime, cancellationToken);
		await writer.WriteAsync(DateTime.UtcNow, parser, cancellationToken);
	}

	/// <summary>
	/// Write <see cref="FixTags.ExpireDate"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="regMsg">The message containing the information for the order registration.</param>
	/// <param name="parser">Time parser.</param>
	/// <param name="timeZone">Time zone.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WriteExpiryDateAsync(this IFixWriter writer, OrderRegisterMessage regMsg, FastDateTimeParser parser, TimeZoneInfo timeZone, CancellationToken cancellationToken)
	{
		var dto = regMsg.TillDate;

		if (dto == null)
			return;

		await writer.WriteAsync(FixTags.ExpireDate, cancellationToken);
		await writer.WriteAsync(dto.Value.To(destination: timeZone), parser, cancellationToken);
	}

	/// <summary>
	/// Convert <see cref="PositionEffect"/> to <see cref="OrderPositionEffects"/> value.
	/// </summary>
	/// <param name="effect"><see cref="PositionEffect"/> value.</param>
	/// <returns><see cref="OrderPositionEffects"/> value.</returns>
	public static OrderPositionEffects? ToPositionEffect(this char? effect)
	{
		return effect switch
		{
			null => null,
			PositionEffect.Default => (OrderPositionEffects?)OrderPositionEffects.Default,
			PositionEffect.Close or PositionEffect.CloseNotify => (OrderPositionEffects?)OrderPositionEffects.CloseOnly,
			PositionEffect.Open or PositionEffect.Rolled or PositionEffect.FIFO => (OrderPositionEffects?)OrderPositionEffects.OpenOnly,
			_ => throw new ArgumentOutOfRangeException(nameof(effect), effect, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Write <see cref="FixTags.PositionEffect"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="effect"><see cref="OrderPositionEffects"/> value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async ValueTask WritePositionEffectAsync(this IFixWriter writer, OrderPositionEffects? effect, CancellationToken cancellationToken)
	{
		char fix;

		switch (effect)
		{
			case null:
				return;
			case OrderPositionEffects.Default:
				fix = PositionEffect.Default;
				break;
			case OrderPositionEffects.CloseOnly:
				fix = PositionEffect.Close;
				break;
			case OrderPositionEffects.OpenOnly:
				fix = PositionEffect.Open;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(effect), effect, LocalizedStrings.InvalidValue);
		}

		await writer.WriteAsync(FixTags.PositionEffect, cancellationToken);
		await writer.WriteAsync(fix, cancellationToken);
	}

	/// <summary>
	/// Convert string to dialect type.
	/// </summary>
	/// <param name="dialect">String value.</param>
	/// <param name="logs">Logs.</param>
	/// <returns>Dialect type.</returns>
	public static Type ToDialect(this string dialect, ILogReceiver logs)
	{
		if (logs is null)
			throw new ArgumentNullException(nameof(logs));

		if (dialect.StartsWithIgnoreCase("StockSharp.Fix.Dialects."))
		{
			dialect = dialect
				.Replace(", StockSharp.Fix.Core", ", StockSharp.Fix.Dialects")
				.Replace(", StockSharp.Fix,", ", StockSharp.Fix.Dialects,");

			if (dialect.EndsWithIgnoreCase(", StockSharp.Fix"))
				dialect = dialect[..^", StockSharp.Fix".Length] + ", StockSharp.Fix.Dialects";
		}

		try
		{
			return dialect.To<Type>();
		}
		catch (Exception ex)
		{
			logs.AddErrorLog(ex);

			return null;
		}
	}

	/// <summary>
	/// Convert dialect type to string.
	/// </summary>
	/// <param name="type">Dialect type.</param>
	/// <returns>String value.</returns>
	public static string FromDialect(this Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		return type.To<string>();
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="message"></param>
	/// <param name="symbol"></param>
	/// <param name="board"></param>
	/// <param name="idSource"></param>
	/// <param name="idValue"></param>
	public static void InitSecId(this SecurityMessage message, string symbol, string board, string idSource, string idValue)
	{
		var secId = new SecurityId
		{
			SecurityCode = symbol,
			BoardCode = board,
		};

		switch (idSource)
		{
			case SecurityIDSource.Cusip:
				secId.Cusip = idValue;
				break;

			case SecurityIDSource.Sedol:
				secId.Sedol = idValue;
				break;

			case SecurityIDSource.IsinNumber:
				secId.Isin = idValue;
				break;

			case SecurityIDSource.RicCode:
				secId.Ric = idValue;
				break;

			default:
				secId.Native = idValue;
				break;
		}

		message.SecurityId = secId;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="message"></param>
	/// <param name="entryType"></param>
	/// <param name="price"></param>
	/// <param name="size"></param>
	/// <param name="otherValue"></param>
	/// <param name="dateTimeParser"></param>
	public static void FillLevel1(this Level1ChangeMessage message, char entryType, decimal? price, decimal? size, string otherValue, FastDateTimeParser dateTimeParser)
	{
		var field = entryType.ToLevel1();
		object value;

		switch (field)
		{
			case Level1Fields.OpenInterest:
			case Level1Fields.VolumeStep:
			case Level1Fields.Multiplier:
			case Level1Fields.LastTradeVolume:
			case Level1Fields.BestBidVolume:
			case Level1Fields.BestAskVolume:
			case Level1Fields.BidsVolume:
			case Level1Fields.AsksVolume:
			case Level1Fields.Volume:
			case Level1Fields.IssueSize:
			case Level1Fields.MinVolume:
			case Level1Fields.MaxVolume:
			case Level1Fields.UnderlyingMinVolume:
				value = size.Value;
				break;
			case Level1Fields.BidsCount:
			case Level1Fields.AsksCount:
			case Level1Fields.TradesCount:
				value = (int)size.Value;
				break;
			case Level1Fields.State:
				value = (SecurityStates)price.Value;
				break;
			case Level1Fields.LastTradeId:
				value = otherValue.To<long>();
				break;
			case Level1Fields.LastTradeOrigin:
				value = otherValue.To<Sides>();
				break;
			case Level1Fields.BestBidTime:
			case Level1Fields.BestAskTime:
			case Level1Fields.LastTradeTime:
			case Level1Fields.BuyBackDate:
			case Level1Fields.CouponDate:
				value = dateTimeParser.Parse(otherValue).UtcKind();
				break;
			case Level1Fields.LastTradeUpDown:
			case Level1Fields.IsSystem:
				// TODO 2025-09-03 remove price usage 1 year later
				value = otherValue.To<bool?>() ?? price.To<bool>();
				break;
			case Level1Fields.LastTradeStringId:
				value = otherValue;
				break;
			default:
			{
				value = price.Value;
				break;
			}
		}

		message.Add(field, value);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static bool IsDump(this ILogSource source)
	{
		var level = source.GetLogLevel();

		if (level == LogLevels.Inherit)
		{
			var logManager = LogManager.Instance;

			if (logManager != null)
				level = logManager.Application.LogLevel;
		}

		return level == LogLevels.Verbose;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="cashMargin"></param>
	/// <returns></returns>
	public static MarginModes ToMarginMode(this char cashMargin)
		=> cashMargin == CashMargin.MarginOpen ? MarginModes.Cross : MarginModes.Isolated;
}