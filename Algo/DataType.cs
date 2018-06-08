#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: DataType.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Data type info.
	/// </summary>
	public class DataType : Equatable<DataType>, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataType"/>.
		/// </summary>
		/// <param name="messageType">Message type.</param>
		/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
		/// <returns>Data type info.</returns>
		public static DataType Create(Type messageType, object arg)
		{
			return new DataType
			{
				MessageType = messageType,
				Arg = arg
			};
		}

		private bool _immutable;

		private DataType Immutable()
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
		/// Create data type info for <see cref="TimeFrameCandleMessage"/>.
		/// </summary>
		/// <param name="tf">Candle arg.</param>
		/// <returns>Data type info.</returns>
		public static DataType TimeFrame(TimeSpan tf) => Create(typeof(TimeFrameCandleMessage), tf).Immutable();

		private Type _messageType;

		/// <summary>
		/// Message type.
		/// </summary>
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

		/// <summary>
		/// Serves as a hash function for a particular type. 
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			return _hashCode;
		}

		/// <summary>
		/// Create a copy of <see cref="DataType"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override DataType Clone()
		{
			return new DataType
			{
				MessageType = MessageType,
				Arg = Arg
			};
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
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
			else if (this == Transactions)
				return LocalizedStrings.Transactions;
			else if (this == PositionChanges)
				return LocalizedStrings.Str972;
			else if (this == News)
				return LocalizedStrings.News;
			else if (this == Securities)
				return LocalizedStrings.Securities;
			else if (MessageType?.IsCandleMessage() == true)
			{
				return $"{MessageType.GetDisplayName()}: {Arg}";
			}
			else
				return $"{MessageType}: {Arg}";
		}

		/// <summary>
		/// Determines whether the specified message type is derived from <see cref="CandleMessage"/>.
		/// </summary>
		/// <returns><see langword="true"/> if the specified message type is derived from <see cref="CandleMessage"/>, otherwise, <see langword="false"/>.</returns>
		public bool IsCandles()
		{
			return MessageType?.IsCandleMessage() == true;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			MessageType = storage.GetValue<Type>(nameof(MessageType));

			if (MessageType == typeof(ExecutionMessage))
				Arg = storage.GetValue<ExecutionTypes?>(nameof(Arg));
			else if (MessageType.IsCandleMessage())
				Arg = storage.GetValue(nameof(Arg), Arg);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(MessageType), MessageType.GetTypeName(false));

			if (MessageType == typeof(ExecutionMessage))
				storage.SetValue(nameof(Arg), (ExecutionTypes?)Arg);
			else
				storage.SetValue(nameof(Arg), Arg);
		}
	}
}