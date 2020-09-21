namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Data type info.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class DataType : Equatable<DataType>, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataType"/>.
		/// </summary>
		/// <param name="messageType">Message type.</param>
		/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
		/// <returns>Data type info.</returns>
		public static DataType Create(Type messageType, object arg)
			=> Create(messageType, arg, false);

		/// <summary>
		/// Initializes a new instance of the <see cref="DataType"/>.
		/// </summary>
		/// <param name="messageType">Message type.</param>
		/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
		/// <param name="isSecurityRequired">Is the data type required security info.</param>
		/// <returns>Data type info.</returns>
		public static DataType Create(Type messageType, object arg, bool isSecurityRequired)
		{
			return new DataType
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

		/// <summary>
		/// Level1.
		/// </summary>
		public static DataType Level1 { get; } = Create(typeof(Level1ChangeMessage), null).Immutable();

		/// <summary>
		/// Market depth.
		/// </summary>
		public static DataType MarketDepth { get; } = Create(typeof(QuoteChangeMessage), null).Immutable();

		/// <summary>
		/// Filtered market depth.
		/// </summary>
		public static DataType FilteredMarketDepth { get; } = Create(typeof(QuoteChangeMessage), ExecutionTypes.Transaction).Immutable();

		/// <summary>
		/// Position changes.
		/// </summary>
		public static DataType PositionChanges { get; } = Create(typeof(PositionChangeMessage), null).Immutable();

		/// <summary>
		/// News.
		/// </summary>
		public static DataType News { get; } = Create(typeof(NewsMessage), null).Immutable();

		/// <summary>
		/// Securities.
		/// </summary>
		public static DataType Securities { get; } = Create(typeof(SecurityMessage), null).Immutable();

		/// <summary>
		/// Ticks.
		/// </summary>
		public static DataType Ticks { get; } = Create(typeof(ExecutionMessage), ExecutionTypes.Tick).Immutable();

		/// <summary>
		/// Order log.
		/// </summary>
		public static DataType OrderLog { get; } = Create(typeof(ExecutionMessage), ExecutionTypes.OrderLog).Immutable();

		/// <summary>
		/// Transactions.
		/// </summary>
		public static DataType Transactions { get; } = Create(typeof(ExecutionMessage), ExecutionTypes.Transaction).Immutable();

		/// <summary>
		/// Board info.
		/// </summary>
		public static DataType Board { get; } = Create(typeof(BoardMessage), null).Immutable();

		/// <summary>
		/// Board state.
		/// </summary>
		public static DataType BoardState { get; } = Create(typeof(BoardStateMessage), null).Immutable();

		/// <summary>
		/// User info.
		/// </summary>
		public static DataType Users { get; } = Create(typeof(UserInfoMessage), null).Immutable();

		/// <summary>
		/// The candle time frames.
		/// </summary>
		public static DataType TimeFrames { get; } = Create(typeof(TimeFrameInfoMessage), null).Immutable();

		/// <summary>
		/// <see cref="TimeFrameCandleMessage"/> data type.
		/// </summary>
		public static DataType CandleTimeFrame { get; } = Create(typeof(TimeFrameCandleMessage), null).Immutable();

		/// <summary>
		/// <see cref="VolumeCandleMessage"/> data type.
		/// </summary>
		public static DataType CandleVolume { get; } = Create(typeof(VolumeCandleMessage), null).Immutable();

		/// <summary>
		/// <see cref="TickCandleMessage"/> data type.
		/// </summary>
		public static DataType CandleTick { get; } = Create(typeof(TickCandleMessage), null).Immutable();

		/// <summary>
		/// <see cref="RangeCandleMessage"/> data type.
		/// </summary>
		public static DataType CandleRange { get; } = Create(typeof(RangeCandleMessage), null).Immutable();

		/// <summary>
		/// <see cref="RenkoCandleMessage"/> data type.
		/// </summary>
		public static DataType CandleRenko { get; } = Create(typeof(RenkoCandleMessage), null).Immutable();
		
		/// <summary>
		/// <see cref="PnFCandleMessage"/> data type.
		/// </summary>
		public static DataType CandlePnF { get; } = Create(typeof(PnFCandleMessage), null).Immutable();
		
		/// <summary>
		/// Adapters.
		/// </summary>
		public static DataType Adapters { get; } = Create(typeof(AdapterResponseMessage), null).Immutable();

		/// <summary>
		/// Portfolio route.
		/// </summary>
		public static DataType PortfolioRoute { get; } = Create(typeof(PortfolioRouteMessage), null).Immutable();

		/// <summary>
		/// Security route.
		/// </summary>
		public static DataType SecurityRoute { get; } = Create(typeof(SecurityRouteMessage), null).Immutable();

		/// <summary>
		/// Security legs.
		/// </summary>
		public static DataType SecurityLegs { get; } = Create(typeof(SecurityLegsInfoMessage), null).Immutable();

		/// <summary>
		/// Security mapping.
		/// </summary>
		public static DataType SecurityMapping { get; } = Create(typeof(SecurityMappingInfoMessage), null).Immutable();

		/// <summary>
		/// <see cref="CommandMessage"/>.
		/// </summary>
		public static DataType Command { get; } = Create(typeof(CommandMessage), null);

		/// <summary>
		/// Create data type info for <see cref="TimeFrameCandleMessage"/>.
		/// </summary>
		/// <param name="tf">Candle arg.</param>
		/// <returns>Data type info.</returns>
		public static DataType TimeFrame(TimeSpan tf) => Create(typeof(TimeFrameCandleMessage), tf).Immutable();

		/// <summary>
		/// Create data type info for <see cref="PortfolioMessage"/>.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		/// <returns>Data type info.</returns>
		public static DataType Portfolio(string portfolioName)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException(nameof(portfolioName));

			return Create(typeof(PortfolioMessage), portfolioName).Immutable();
		}

		private Type _messageType;

		/// <summary>
		/// Message type.
		/// </summary>
		[DataMember]
		public Type MessageType
		{
			get => _messageType;
			set
			{
				if (_immutable)
					throw new InvalidOperationException();

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
			set
			{
				if (_immutable)
					throw new InvalidOperationException();

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
			return new DataType
			{
				MessageType = MessageType,
				Arg = Arg,
				_isSecurityRequired = _isSecurityRequired,
			};
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
				return LocalizedStrings.Str972;
			else if (this == News)
				return LocalizedStrings.News;
			else if (this == Securities)
				return LocalizedStrings.Securities;
			else
			{
				var name = Name;

				if (name.IsEmpty())
					name = $"{MessageType.GetDisplayName()}: {Arg}";

				return name;
			}
		}

		/// <summary>
		/// Determines whether the specified message type is derived from <see cref="CandleMessage"/>.
		/// </summary>
		public bool IsCandles => MessageType?.IsCandleMessage() == true;

		/// <summary>
		/// Determines whether the specified message type is derived from <see cref="PortfolioMessage"/>.
		/// </summary>
		public bool IsPortfolio => MessageType == typeof(PortfolioMessage);

		/// <summary>
		/// Determines whether the specified message type is market-data.
		/// </summary>
		public bool IsMarketData =>
			IsSecurityRequired		||
			this == News			||
			this == Board			||
			this == BoardState		||
			this == SecurityLegs	||
			this == SecurityRoute	||
			this == SecurityMapping	||
			this == TimeFrames;

		private bool _isSecurityRequired;

		/// <summary>
		/// Is the data type required security info.
		/// </summary>
		public bool IsSecurityRequired =>
			_isSecurityRequired			||
			IsCandles					||
			this == MarketDepth			||
			this == FilteredMarketDepth ||
			this == Level1				||
			this == Ticks				||
			this == OrderLog;

		/// <summary>
		/// Is the data type can be used as candles compression source.
		/// </summary>
		public bool IsCandleSource => CandleSources.Contains(this);

		/// <summary>
		/// Possible data types that can be used as candles source.
		/// </summary>
		public static ISet<DataType> CandleSources { get; } = new HashSet<DataType>(new[] { Ticks, Level1, MarketDepth, OrderLog });

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			MessageType = storage.GetValue<Type>(nameof(MessageType));

			if (storage.ContainsKey(nameof(Arg)))
				Arg = storage.GetValue<object>(nameof(Arg));

			if (storage.ContainsKey(nameof(IsSecurityRequired)))
				_isSecurityRequired = storage.GetValue<bool>(nameof(IsSecurityRequired));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(MessageType), MessageType.GetTypeName(false));

			if (Arg != null)
				storage.SetValue(nameof(Arg), Arg);

			if (_isSecurityRequired)
				storage.SetValue(nameof(IsSecurityRequired), true);
		}
	}
}