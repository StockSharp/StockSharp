namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Состояния свечи.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum CandleStates
	{
		/// <summary>
		/// Пустое состояние (свеча отсутствует).
		/// </summary>
		[EnumMember]
		None,

		/// <summary>
		/// Свеча формируется.
		/// </summary>
		[EnumMember]
		Active,

		/// <summary>
		/// Свеча закончена.
		/// </summary>
		[EnumMember]
		Finished,
	}

	/// <summary>
	/// Сообщение, содержащее данные о свече.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public abstract class CandleMessage : Message
	{
		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Время начала свечи.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleOpenTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleOpenTimeKey, true)]
		[MainCategory]
		public DateTimeOffset OpenTime { get; set; }

		/// <summary>
		/// Время максимума свечи.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleHighTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleHighTimeKey, true)]
		[MainCategory]
		public DateTimeOffset HighTime { get; set; }

		/// <summary>
		/// Время минимума свечи.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleLowTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleLowTimeKey, true)]
		[MainCategory]
		public DateTimeOffset LowTime { get; set; }

		/// <summary>
		/// Время окончания свечи.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleCloseTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleCloseTimeKey, true)]
		[MainCategory]
		public DateTimeOffset CloseTime { get; set; }

		/// <summary>
		/// Цена открытия.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str79Key)]
		[DescriptionLoc(LocalizedStrings.Str80Key)]
		[MainCategory]
		public decimal OpenPrice { get; set; }

		/// <summary>
		/// Максимальная цена.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str81Key)]
		[DescriptionLoc(LocalizedStrings.Str82Key)]
		[MainCategory]
		public decimal HighPrice { get; set; }

		/// <summary>
		/// Минимальная цена.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str83Key)]
		[DescriptionLoc(LocalizedStrings.Str84Key)]
		[MainCategory]
		public decimal LowPrice { get; set; }

		/// <summary>
		/// Цена закрытия.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ClosingPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str86Key)]
		[MainCategory]
		public decimal ClosePrice { get; set; }

		/// <summary>
		/// Объем открытия.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? OpenVolume { get; set; }

		/// <summary>
		/// Объем закрытия.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? CloseVolume { get; set; }

		/// <summary>
		/// Максимальный объем.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? HighVolume { get; set; }

		/// <summary>
		/// Минимальный объем.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? LowVolume { get; set; }

		/// <summary>
		/// Относительный объем.
		/// </summary>
		[DataMember]
		public decimal? RelativeVolume { get; set; }

		/// <summary>
		/// Суммарный объем.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.TotalCandleVolumeKey)]
		[MainCategory]
		public decimal TotalVolume { get; set; }

		/// <summary>
		/// Открытый интерес.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OIKey)]
		[DescriptionLoc(LocalizedStrings.OpenInterestKey)]
		[MainCategory]
		public decimal? OpenInterest { get; set; }

		/// <summary>
		/// Количество тиковых сделок.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TicksKey)]
		[DescriptionLoc(LocalizedStrings.TickCountKey)]
		[MainCategory]
		public int? TotalTicks { get; set; }

		/// <summary>
		/// Количество восходящих тиковых сделок.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickUpKey)]
		[DescriptionLoc(LocalizedStrings.TickUpCountKey)]
		[MainCategory]
		public int? UpTicks { get; set; }

		/// <summary>
		/// Количество нисходящих тиковых сделок.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickDownKey)]
		[DescriptionLoc(LocalizedStrings.TickDownCountKey)]
		[MainCategory]
		public int? DownTicks { get; set; }

		/// <summary>
		/// Состояние.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.CandleStateKey)]
		[MainCategory]
		public CandleStates State { get; set; }

		/// <summary>
		/// Идентификатор первоначального сообщения <see cref="MarketDataMessage.TransactionId"/>,
		/// для которого данное сообщение является ответом.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Является ли сообщение последним в запрашиваемом пакете свечек.
		/// </summary>
		[DataMember]
		public bool IsFinished { get; set; }

		/// <summary>
		/// Параметр свечи.
		/// </summary>
		public abstract object Arg { get; set; }

		/// <summary>
		/// Инициализировать <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="type">Тип сообщения.</param>
		protected CandleMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Скопировать параметры.
		/// </summary>
		/// <param name="copy">Копия.</param>
		/// <returns>Копия.</returns>
		protected CandleMessage CopyTo(CandleMessage copy)
		{
			if (copy == null)
				throw new ArgumentNullException("copy");

			copy.LocalTime = LocalTime;
			copy.ClosePrice = ClosePrice;
			copy.CloseTime = CloseTime;
			copy.CloseVolume = CloseVolume;
			copy.HighPrice = HighPrice;
			copy.HighVolume = HighVolume;
			copy.LowPrice = LowPrice;
			copy.LowVolume = LowVolume;
			copy.OpenInterest = OpenInterest;
			copy.OpenPrice = OpenPrice;
			copy.OpenTime = OpenTime;
			copy.OpenVolume = OpenVolume;
			copy.SecurityId = SecurityId;
			copy.TotalVolume = TotalVolume;
			copy.RelativeVolume = RelativeVolume;
			copy.OriginalTransactionId = OriginalTransactionId;
			copy.DownTicks = DownTicks;
			copy.UpTicks = UpTicks;
			copy.TotalTicks = TotalTicks;
			copy.IsFinished = IsFinished;

			return copy;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0},T={1:yyyy/MM/dd HH:mm:ss.fff},O={2},H={3},L={4},C={5},V={6}".Put(Type, OpenTime, OpenPrice, HighPrice, LowPrice, ClosePrice, TotalVolume);
		}
	}

	/// <summary>
	/// Сообщение, содержащее данные о тайм-фрейм свече.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class TimeFrameCandleMessage : CandleMessage
	{
		/// <summary>
		/// Создать <see cref="TimeFrameCandleMessage"/>.
		/// </summary>
		public TimeFrameCandleMessage()
			: base(MessageTypes.CandleTimeFrame)
		{
		}

		/// <summary>
		/// Тайм-фрейм.
		/// </summary>
		[DataMember]
		public TimeSpan TimeFrame { get; set; }

		/// <summary>
		/// Создать копию объекта <see cref="TimeFrameCandleMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return CopyTo(new TimeFrameCandleMessage
			{
				TimeFrame = TimeFrame
			});
		}

		/// <summary>
		/// Параметр свечи.
		/// </summary>
		public override object Arg
		{
			get { return TimeFrame; }
			set { TimeFrame = (TimeSpan)value; }
		}
	}

	/// <summary>
	/// Сообщение, содержащее данные о свече, группируемая по количеству сделок.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class TickCandleMessage : CandleMessage
	{
		/// <summary>
		/// Создать <see cref="TickCandleMessage"/>.
		/// </summary>
		public TickCandleMessage()
			: base(MessageTypes.CandleTick)
		{
		}

		/// <summary>
		/// Максимальное количество сделок, которое может содержать свеча.
		/// </summary>
		[DataMember]
		public int MaxTradeCount { get; set; }

		/// <summary>
		/// Создать копию объекта <see cref="TickCandleMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return CopyTo(new TickCandleMessage
			{
				MaxTradeCount = MaxTradeCount
			});
		}

		/// <summary>
		/// Параметр свечи.
		/// </summary>
		public override object Arg
		{
			get { return MaxTradeCount; }
			set { MaxTradeCount = (int)value; }
		}
	}

	/// <summary>
	/// Сообщение, содержащее данные о свече, группируемая по количеству контрактов.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class VolumeCandleMessage : CandleMessage
	{
		/// <summary>
		/// Создать <see cref="VolumeCandleMessage"/>.
		/// </summary>
		public VolumeCandleMessage()
			: base(MessageTypes.CandleVolume)
		{
		}

		/// <summary>
		/// Максимальное количество контрактов, которое может содержать свеча.
		/// </summary>
		[DataMember]
		public decimal Volume { get; set; }

		/// <summary>
		/// Создать копию объекта <see cref="VolumeCandleMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return CopyTo(new VolumeCandleMessage
			{
				Volume = Volume
			});
		}

		/// <summary>
		/// Параметр свечи.
		/// </summary>
		public override object Arg
		{
			get { return Volume; }
			set { Volume = (decimal)value; }
		}
	}

	/// <summary>
	/// Сообщение, содержащее данные о свече, группируемая по ценовому диапазону.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class RangeCandleMessage : CandleMessage
	{
		/// <summary>
		/// Создать <see cref="RangeCandleMessage"/>.
		/// </summary>
		public RangeCandleMessage()
			: base(MessageTypes.CandleRange)
		{
		}

		/// <summary>
		/// Дельта цены, в рамках которой свеча может содержать сделки.
		/// </summary>
		[DataMember]
		public Unit PriceRange { get; set; }

		/// <summary>
		/// Создать копию объекта <see cref="RangeCandleMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return CopyTo(new RangeCandleMessage
			{
				PriceRange = PriceRange
			});
		}

		/// <summary>
		/// Параметр свечи.
		/// </summary>
		public override object Arg
		{
			get { return PriceRange; }
			set { PriceRange = (Unit)value; }
		}
	}

	/// <summary>
	/// Типы символов.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum PnFTypes
	{
		/// <summary>
		/// Крестики (цена растет).
		/// </summary>
		[EnumMember]
		X,

		/// <summary>
		/// Нолики (цена падает).
		/// </summary>
		[EnumMember]
		O,
	}

	/// <summary>
	/// Значение параметров пункто-цифрового графика (график крестики-нолики).
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class PnFArg : Equatable<PnFArg>
	{
		private Unit _boxSize = new Unit();

		/// <summary>
		/// Изменение цены, при превышении которого регистрируется новый <see cref="PnFTypes.X"/> или <see cref="PnFTypes.O"/>.
		/// </summary>
		[DataMember]
		public Unit BoxSize
		{
			get { return _boxSize; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_boxSize = value;
			}
		}

		/// <summary>
		/// Величина противоположного движения цены, при котором происходит смена <see cref="PnFTypes.X"/> на <see cref="PnFTypes.O"/> (или наоборот).
		/// </summary>
		[DataMember]
		public int ReversalAmount { get; set; }

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "Box = {0} RA = {1}".Put(BoxSize, ReversalAmount);
		}

		/// <summary>
		/// Создать копию объекта <see cref="PnFArg"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override PnFArg Clone()
		{
			return new PnFArg
			{
				BoxSize = BoxSize.Clone(),
				ReversalAmount = ReversalAmount,
			};
		}

		/// <summary>
		/// Сравнить на эквивалентность.
		/// </summary>
		/// <param name="other">Значение параметров пункто-цифрового графика, с которым необходимо сделать сравнение.</param>
		/// <returns><see langword="true"/>, если значения равны. Иначе, <see langword="false"/>.</returns>
		protected override bool OnEquals(PnFArg other)
		{
			return other.BoxSize == BoxSize && other.ReversalAmount == ReversalAmount;
		}

		/// <summary>
		/// Рассчитать хеш-код объекта <see cref="PnFArg"/>.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			return BoxSize.GetHashCode() ^ ReversalAmount.GetHashCode();
		}
	}

	/// <summary>
	/// Сообщение, содержащее данные о свече пункто-цифрового графика (график крестики-нолики).
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class PnFCandleMessage : CandleMessage
	{
		/// <summary>
		/// Создать <see cref="PnFCandleMessage"/>.
		/// </summary>
		public PnFCandleMessage()
			: base(MessageTypes.CandlePnF)
		{
		}

		/// <summary>
		/// Значение параметров.
		/// </summary>
		[DataMember]
		public PnFArg PnFArg { get; set; }

		/// <summary>
		/// Тип символов.
		/// </summary>
		[DataMember]
		public PnFTypes PnFType { get; set; }

		/// <summary>
		/// Создать копию объекта <see cref="PnFCandleMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return CopyTo(new PnFCandleMessage
			{
				PnFArg = PnFArg,
				PnFType = PnFType
			});
		}

		/// <summary>
		/// Параметр свечи.
		/// </summary>
		public override object Arg
		{
			get { return PnFArg; }
			set { PnFArg = (PnFArg)value; }
		}
	}

	/// <summary>
	/// Сообщение, содержащее данные о Рэнко свече.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class RenkoCandleMessage : CandleMessage
	{
		/// <summary>
		/// Создать <see cref="RenkoCandleMessage"/>.
		/// </summary>
		public RenkoCandleMessage()
			: base(MessageTypes.CandleRenko)
		{
		}

		/// <summary>
		/// Изменение цены, при превышении которого регистрируется новая свеча.
		/// </summary>
		[DataMember]
		public Unit BoxSize { get; set; }

		/// <summary>
		/// Создать копию объекта <see cref="RenkoCandleMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return CopyTo(new RenkoCandleMessage
			{
				BoxSize = BoxSize
			});
		}

		/// <summary>
		/// Параметр свечи.
		/// </summary>
		public override object Arg
		{
			get { return BoxSize; }
			set { BoxSize = (Unit)value; }
		}
	}
}