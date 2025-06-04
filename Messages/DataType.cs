namespace StockSharp.Messages;

/// <summary>
/// Data type info.
/// </summary>
[DataContract]
[Serializable]
public class DataType : Equatable<DataType>, IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DataType"/>.
	/// </summary>
	/// <typeparam name="TMessage">Message type.</typeparam>
	/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
	/// <param name="isSecurityRequired">Is the data type required security info.</param>
	/// <returns>Data type info.</returns>
	public static DataType Create<TMessage>(object arg = default, bool? isSecurityRequired = default)
		=> Create(typeof(TMessage), arg, isSecurityRequired);

	/// <summary>
	/// Initializes a new instance of the <see cref="DataType"/>.
	/// </summary>
	/// <param name="messageType">Message type.</param>
	/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
	/// <param name="isSecurityRequired">Is the data type required security info.</param>
	/// <returns>Data type info.</returns>
	public static DataType Create(Type messageType, object arg, bool? isSecurityRequired = default)
	{
		return new()
		{
			MessageType = messageType,
			Arg = arg,
			_isSecurityRequired = isSecurityRequired,
		};
	}

	private bool _immutable;

	/// <summary>
	/// Make immutable.
	/// </summary>
	/// <returns>Data type info.</returns>
	public DataType Immutable()
	{
		_immutable = true;
		return this;
	}

	private void CheckImmutable()
	{
		if (_immutable)
			throw new InvalidOperationException(LocalizedStrings.CannotBeModified);
	}

	private static DataType CreateImmutable<T>(object arg = default) => Create<T>(arg).Immutable();

	/// <summary>
	/// Level1.
	/// </summary>
	public static DataType Level1 { get; } = CreateImmutable<Level1ChangeMessage>();

	/// <summary>
	/// Market depth.
	/// </summary>
	public static DataType MarketDepth { get; } = CreateImmutable<QuoteChangeMessage>();

	/// <summary>
	/// Filtered market depth.
	/// </summary>
	public static DataType FilteredMarketDepth { get; } = CreateImmutable<QuoteChangeMessage>(ExecutionTypes.Transaction);

	/// <summary>
	/// Position changes.
	/// </summary>
	public static DataType PositionChanges { get; } = CreateImmutable<PositionChangeMessage>();

	/// <summary>
	/// News.
	/// </summary>
	public static DataType News { get; } = CreateImmutable<NewsMessage>();

	/// <summary>
	/// Securities.
	/// </summary>
	public static DataType Securities { get; } = CreateImmutable<SecurityMessage>();

	/// <summary>
	/// Ticks.
	/// </summary>
	public static DataType Ticks { get; } = CreateImmutable<ExecutionMessage>(ExecutionTypes.Tick);

	/// <summary>
	/// Order log.
	/// </summary>
	public static DataType OrderLog { get; } = CreateImmutable<ExecutionMessage>(ExecutionTypes.OrderLog);

	/// <summary>
	/// Transactions.
	/// </summary>
	public static DataType Transactions { get; } = CreateImmutable<ExecutionMessage>(ExecutionTypes.Transaction);

	/// <summary>
	/// Board info.
	/// </summary>
	public static DataType Board { get; } = CreateImmutable<BoardMessage>();

	/// <summary>
	/// Board state.
	/// </summary>
	public static DataType BoardState { get; } = CreateImmutable<BoardStateMessage>();

	/// <summary>
	/// User info.
	/// </summary>
	public static DataType Users { get; } = CreateImmutable<UserInfoMessage>();

	/// <summary>
	/// Data type info.
	/// </summary>
	public static DataType DataTypeInfo { get; } = CreateImmutable<DataTypeInfoMessage>();

	/// <summary>
	/// <see cref="TimeFrameCandleMessage"/> data type.
	/// </summary>
	public static DataType CandleTimeFrame { get; } = CreateImmutable<TimeFrameCandleMessage>();

	/// <summary>
	/// <see cref="VolumeCandleMessage"/> data type.
	/// </summary>
	public static DataType CandleVolume { get; } = CreateImmutable<VolumeCandleMessage>();

	/// <summary>
	/// <see cref="TickCandleMessage"/> data type.
	/// </summary>
	public static DataType CandleTick { get; } = CreateImmutable<TickCandleMessage>();

	/// <summary>
	/// <see cref="RangeCandleMessage"/> data type.
	/// </summary>
	public static DataType CandleRange { get; } = CreateImmutable<RangeCandleMessage>();

	/// <summary>
	/// <see cref="RenkoCandleMessage"/> data type.
	/// </summary>
	public static DataType CandleRenko { get; } = CreateImmutable<RenkoCandleMessage>();

	/// <summary>
	/// <see cref="PnFCandleMessage"/> data type.
	/// </summary>
	public static DataType CandlePnF { get; } = CreateImmutable<PnFCandleMessage>();

	/// <summary>
	/// Security legs.
	/// </summary>
	public static DataType SecurityLegs { get; } = CreateImmutable<SecurityLegsInfoMessage>();

	/// <summary>
	/// <see cref="CommandMessage"/>.
	/// </summary>
	public static DataType Command { get; } = CreateImmutable<CommandMessage>();

	/// <summary>
	/// <see cref="RemoteFileMessage"/>.
	/// </summary>
	public static DataType RemoteFile { get; } = CreateImmutable<RemoteFileMessage>();

	/// <summary>
	/// <see cref="SecurityMappingMessage"/>.
	/// </summary>
	public static DataType SecurityMapping { get; } = CreateImmutable<SecurityMappingMessage>();

	/// <summary>
	/// Create data type info for <see cref="TimeFrameCandleMessage"/>.
	/// </summary>
	/// <param name="arg">Candle arg.</param>
	/// <returns>Data type info.</returns>
	[Obsolete("Use Extensions.TimeFrame method instead.")]
	public static DataType TimeFrame(TimeSpan arg) => arg.TimeFrame();

	private Type _messageType;

	/// <summary>
	/// Message type.
	/// </summary>
	[DataMember]
	public Type MessageType
	{
		get => _messageType;
		private set
		{
			CheckImmutable();

			_messageType = value;
			ReInitHashCode();
		}
	}

	private object _arg;

	/// <summary>
	/// The additional argument, associated with data. For example, candle argument.
	/// </summary>
	[DataMember]
	public object Arg
	{
		get => _arg;
		private set
		{
			CheckImmutable();

			if (value is DataType)
				throw new ArgumentException(value.To<string>(), nameof(value));

			_arg = value;
			ReInitHashCode();
		}
	}

	/// <summary>
	/// Compare <see cref="DataType"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	protected override bool OnEquals(DataType other)
	{
		return MessageType == other.MessageType && (Arg?.Equals(other.Arg) ?? other.Arg == null);
	}

	private int _hashCode;

	private void ReInitHashCode()
	{
		var h1 = MessageType?.GetHashCode() ?? 0;
		var h2 = Arg?.GetHashCode() ?? 0;

		_hashCode = ((h1 << 5) + h1) ^ h2;
	}

	/// <summary>Serves as a hash function for a particular type. </summary>
	/// <returns>A hash code for the current <see cref="T:System.Object" />.</returns>
	public override int GetHashCode() => _hashCode;

	/// <summary>
	/// Create a copy of <see cref="DataType"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override DataType Clone()
	{
		var clone = new DataType
		{
			_messageType = MessageType,
			_arg = Arg,
			_isSecurityRequired = _isSecurityRequired,
			_isCandles = _isCandles,
			_isMarketData = _isMarketData,
			_isNonSecurity = _isNonSecurity,
		};
		clone.ReInitHashCode();
		return clone;
	}

	/// <summary>
	/// Name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Set <see cref="Name"/>.
	/// </summary>
	/// <param name="name">Name.</param>
	/// <returns>Data type info.</returns>
	public DataType SetName(string name)
	{
		Name = name;
		return this;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (this == Ticks)
			return LocalizedStrings.Ticks;
		else if (this == Level1)
			return LocalizedStrings.Level1;
		else if (this == OrderLog)
			return LocalizedStrings.OrderLog;
		else if (this == MarketDepth)
			return LocalizedStrings.MarketDepth;
		else if (this == FilteredMarketDepth)
			return LocalizedStrings.FilteredBook;
		else if (this == Transactions)
			return LocalizedStrings.Transactions;
		else if (this == PositionChanges)
			return LocalizedStrings.Positions;
		else if (this == News)
			return LocalizedStrings.News;
		else if (this == Securities)
			return LocalizedStrings.Securities;
		else
		{
			var name = Name;

			if (name.IsEmpty())
			{
				name = $"{MessageType.GetDisplayName()}";

				if (Arg is not null)
					name += $": {Arg}";
			}

			return name;
		}
	}

	private bool? _isCandles;

	/// <summary>
	/// Determines whether the <see cref="MessageType"/> is derived from <see cref="CandleMessage"/>.
	/// </summary>
	public bool IsCandles => _isCandles ??= MessageType.IsCandleMessage();

	/// <summary>
	/// Determines whether the <see cref="MessageType"/> is <see cref="TimeFrameCandleMessage"/>.
	/// </summary>
	public bool IsTFCandles => MessageType == typeof(TimeFrameCandleMessage);

	private bool? _isMarketData;

	/// <summary>
	/// Determines whether the specified message type is market-data.
	/// </summary>
	public bool IsMarketData => _isMarketData ??=
	(
		IsCandles ||
		MessageType == typeof(QuoteChangeMessage) ||
		MessageType == typeof(Level1ChangeMessage) ||
		(MessageType == typeof(ExecutionMessage) && Arg is ExecutionTypes type && type != ExecutionTypes.Transaction) ||
		MessageType == typeof(NewsMessage) ||
		MessageType == typeof(BoardMessage) ||
		MessageType == typeof(BoardStateMessage) ||
		MessageType == typeof(SecurityLegsInfoMessage) ||
		MessageType == typeof(SecurityMappingMessage) ||
		MessageType == typeof(DataTypeInfoMessage)
	);

	private bool? _isSecurityRequired;

	/// <summary>
	/// Is the data type required security info.
	/// </summary>
	public bool IsSecurityRequired => _isSecurityRequired ??=
	(
		IsCandles ||
		MessageType == typeof(QuoteChangeMessage) ||
		MessageType == typeof(Level1ChangeMessage) ||
		(MessageType == typeof(ExecutionMessage) && Arg is ExecutionTypes t1 && t1 != ExecutionTypes.Transaction)
	);

	private bool? _isNonSecurity;

	/// <summary>
	/// Is the data type never associated with security.
	/// </summary>
	public bool IsNonSecurity => _isNonSecurity ??=
	(
		MessageType == typeof(SecurityMessage) ||
		MessageType == typeof(NewsMessage) ||
		MessageType == typeof(BoardMessage) ||
		MessageType == typeof(BoardStateMessage) ||
		MessageType == typeof(DataTypeInfoMessage)
	);

	/// <summary>
	/// Is the data type can be used as candles compression source.
	/// </summary>
	public bool IsCandleSource => CandleSources.Contains(this);

	/// <summary>
	/// Possible data types that can be used as candles source.
	/// </summary>
	public static ISet<DataType> CandleSources { get; } = new HashSet<DataType>([Ticks, Level1, MarketDepth, OrderLog]);

	/// <summary>
	/// Serialize <see cref="DataType"/> to <see cref="string"/>.
	/// </summary>
	/// <returns>The string representation of <see cref="DataType"/>.</returns>
	public string ToSerializableString()
	{
		var type = MessageType;
		var arg = Arg;

		return $"{type?.GetTypeName(false)}:{(type?.IsCandleMessage() == true ? type.DataTypeArgToString(arg) : $"{arg?.GetType().GetTypeName(false)}:{arg?.ToString()}")}";
	}

	/// <summary>
	/// Deserialize <see cref="DataType"/> from <see cref="string"/>.
	/// </summary>
	/// <param name="value">The string representation of <see cref="DataType"/>.</param>
	/// <returns><see cref="DataType"/></returns>
	public static DataType FromSerializableString(string value)
	{
		if (value.IsEmpty())
			return null;

		var parts = value.SplitByColon(false);

		var msgType = parts[0].To<Type>();

		object arg;

		if (msgType?.IsCandleMessage() == true)
			arg = msgType.ToDataTypeArg(parts[1]);
		else
		{
			var argType = parts[1].To<Type>();
			arg = argType is null ? null : parts[2].To(argType);
		}

		return new()
		{
			MessageType = msgType,
			Arg = arg,
		};
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		MessageType = storage.GetValue<Type>(nameof(MessageType));

		if (storage.ContainsKey(nameof(Arg)))
		{
			var arg = storage.GetValue<object>(nameof(Arg));

			if (arg is SettingsStorage ss)
			{
				var type = ss.GetValue<Type>("type");

				if (type.Is<IPersistable>())
				{
					var instance = type.CreateInstance<IPersistable>();
					instance.Load(ss, "value");

					Arg = instance;
				}
				else
				{
					var value = ss.GetValue<object>("value");

					if (MessageType?.IsCandleMessage() == true && value is string str)
						Arg = MessageType.ToDataTypeArg(str);
					else
						Arg = value.To(type);
				}
			}
			else if (MessageType?.IsCandleMessage() == true && arg is string str)
			{
				Arg = MessageType.ToDataTypeArg(str);
			}
			else
			{
				Arg = arg;
			}
		}

		if (storage.ContainsKey(nameof(IsSecurityRequired)))
			_isSecurityRequired = storage.GetValue<bool>(nameof(IsSecurityRequired));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(MessageType), MessageType?.GetTypeName(false));

		if (Arg != null)
		{
			var ss = new SettingsStorage();
			ss.SetValue("type", Arg.GetType().GetTypeName(false));

			if (Arg is IPersistable per)
				ss.SetValue("value", per.Save());
			else if (MessageType?.IsCandleMessage() == true)
				ss.SetValue("value", MessageType.DataTypeArgToString(Arg));
			else
				ss.SetValue("value", Arg.To<string>());

			storage.SetValue(nameof(Arg), ss);
		}

		if (_isSecurityRequired == true)
			storage.SetValue(nameof(IsSecurityRequired), true);
	}
}