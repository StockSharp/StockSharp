namespace StockSharp.Algo.Candles
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Algo.Candles.VolumePriceStatistics;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Базовый класс для свечи (содержит основные параметры).
	/// </summary>
	[DataContract]
	[Serializable]
	[KnownType(typeof(TickCandle))]
	[KnownType(typeof(VolumeCandle))]
	[KnownType(typeof(RangeCandle))]
	[KnownType(typeof(TimeFrameCandle))]
	[KnownType(typeof(PnFCandle))]
	[KnownType(typeof(RenkoCandle))]
	public abstract class Candle
	{
		/// <summary>
		/// Инструмент.
		/// </summary>
		[DataMember]
		public Security Security { get; set; }

		private DateTimeOffset _openTime;

		/// <summary>
		/// Время начала свечи.
		/// </summary>
		[DataMember]
		public DateTimeOffset OpenTime
		{
			get { return _openTime; }
			set
			{
				ThrowIfFinished();
				_openTime = value;
			}
		}

		private DateTimeOffset _closeTime;

		/// <summary>
		/// Время окончания свечи.
		/// </summary>
		[DataMember]
		public DateTimeOffset CloseTime
		{
			get { return _closeTime; }
			set
			{
				ThrowIfFinished();
				_closeTime = value;
			}
		}

		private DateTimeOffset _highTime;

		/// <summary>
		/// Время с максимальной ценой в свече.
		/// </summary>
		[DataMember]
		public DateTimeOffset HighTime
		{
			get { return _highTime; }
			set
			{
				ThrowIfFinished();
				_highTime = value;
			}
		}

		private DateTimeOffset _lowTime;

		/// <summary>
		/// Время с минимальной ценой в свече.
		/// </summary>
		[DataMember]
		public DateTimeOffset LowTime
		{
			get { return _lowTime; }
			set
			{
				ThrowIfFinished();
				_lowTime = value;
			}
		}

		private decimal _openPrice;

		/// <summary>
		/// Цена открытия.
		/// </summary>
		[DataMember]
		public decimal OpenPrice
		{
			get { return _openPrice; }
			set
			{
				ThrowIfFinished();
				_openPrice = value;
			}
		}

		private decimal _closePrice;

		/// <summary>
		/// Цена закрытия.
		/// </summary>
		[DataMember]
		public decimal ClosePrice
		{
			get { return _closePrice; }
			set
			{
				ThrowIfFinished();
				_closePrice = value;
			}
		}

		private decimal _highPrice;

		/// <summary>
		/// Максимальная цена.
		/// </summary>
		[DataMember]
		public decimal HighPrice
		{
			get { return _highPrice; }
			set
			{
				ThrowIfFinished();
				_highPrice = value;
			}
		}

		private decimal _lowPrice;

		/// <summary>
		/// Минимальная цена.
		/// </summary>
		[DataMember]
		public decimal LowPrice
		{
			get { return _lowPrice; }
			set
			{
				ThrowIfFinished();
				_lowPrice = value;
			}
		}

		private decimal _totalPrice;

		/// <summary>
		/// Суммарный оборот по сделкам.
		/// </summary>
		[DataMember]
		public decimal TotalPrice
		{
			get { return _totalPrice; }
			set
			{
				ThrowIfFinished();
				_totalPrice = value;
			}
		}

		private decimal _openVolume;

		/// <summary>
		/// Объем открытия.
		/// </summary>
		[DataMember]
		public decimal OpenVolume
		{
			get { return _openVolume; }
			set
			{
				ThrowIfFinished();
				_openVolume = value;
			}
		}

		private decimal _closeVolume;

		/// <summary>
		/// Объем закрытия.
		/// </summary>
		[DataMember]
		public decimal CloseVolume
		{
			get { return _closeVolume; }
			set
			{
				ThrowIfFinished();
				_closeVolume = value;
			}
		}

		private decimal _highVolume;

		/// <summary>
		/// Максимальный объем.
		/// </summary>
		[DataMember]
		public decimal HighVolume
		{
			get { return _highVolume; }
			set
			{
				ThrowIfFinished();
				_highVolume = value;
			}
		}

		private decimal _lowVolume;

		/// <summary>
		/// Минимальный объем.
		/// </summary>
		[DataMember]
		public decimal LowVolume
		{
			get { return _lowVolume; }
			set
			{
				ThrowIfFinished();
				_lowVolume = value;
			}
		}

		private decimal _totalVolume;

		/// <summary>
		/// Суммарный объем.
		/// </summary>
		[DataMember]
		public decimal TotalVolume
		{
			get { return _totalVolume; }
			set
			{
				ThrowIfFinished();
				_totalVolume = value;
			}
		}

		private decimal _relativeVolume;

		/// <summary>
		/// Относительный объем.
		/// </summary>
		[DataMember]
		public decimal RelativeVolume
		{
			get { return _relativeVolume; }
			set
			{
				ThrowIfFinished();
				_relativeVolume = value;
			}
		}

		[field: NonSerialized]
		private CandleSeries _series;

		/// <summary>
		/// Серия свечек.
		/// </summary>
		public CandleSeries Series
		{
			get { return _series; }
			set { _series = value; }
		}

		[field: NonSerialized]
		private ICandleManagerSource _source;

		/// <summary>
		/// Источник, из которого была получена свеча.
		/// </summary>
		public ICandleManagerSource Source
		{
			get { return _source; }
			set { _source = value; }
		}

		/// <summary>
		/// Параметр свечи.
		/// </summary>
		public abstract object Arg { get; set; }

		/// <summary>
		/// Состояние.
		/// </summary>
		[DataMember]
		public CandleStates State { get; set; }

		/// <summary>
		/// Количество тиковых сделок.
		/// </summary>
		[DataMember]
		public int TotalTicks { get; set; }

		/// <summary>
		/// Количество восходящих тиковых сделок.
		/// </summary>
		[DataMember]
		public int UpTicks { get; set; }

		/// <summary>
		/// Количество нисходящих тиковых сделок.
		/// </summary>
		[DataMember]
		public int DownTicks { get; set; }

		[field: NonSerialized]
		private readonly VolumeProfile _volumeProfileInfo = new VolumeProfile();

		/// <summary>
		/// Профиль объема.
		/// </summary>
		public VolumeProfile VolumeProfileInfo
		{
			get { return _volumeProfileInfo; }
		}

		/// <summary>
		/// Открытый интерес.
		/// </summary>
		[DataMember]
		public decimal? OpenInterest { get; set; }

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0:HH:mm:ss} {1} (O:{2}, H:{3}, L:{4}, C:{5}, V:{6})"
				.Put(OpenTime, Series == null ? GetType().Name + "_" + Security + "_" + Arg : Series.ToString(),
						OpenPrice, HighPrice, LowPrice, ClosePrice, TotalVolume);
		}

		private void ThrowIfFinished()
		{
			if (State == CandleStates.Finished)
				throw new InvalidOperationException(LocalizedStrings.Str649);
		}
	}

	/// <summary>
	/// Свеча, группируемая по тайм-фрейму.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayName("Time Frame")]
	public class TimeFrameCandle : Candle
	{
		/// <summary>
		/// Тайм-фрейм.
		/// </summary>
		[DataMember]
		public TimeSpan TimeFrame { get; set; }

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
	/// Свеча, группируемая по количеству сделок.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayName("Tick")]
	public class TickCandle : Candle
	{
		/// <summary>
		/// Максимальное количество сделок, которое может содержать свеча.
		/// </summary>
		[DataMember]
		public int MaxTradeCount { get; set; }

		/// <summary>
		/// Текущее количество сделок, которое содержит свеча.
		/// </summary>
		[DataMember]
		public int CurrentTradeCount { get; set; }

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
	/// Свеча, группируемая по количеству контрактов.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayName("Volume")]
	public class VolumeCandle : Candle
	{
		/// <summary>
		/// Максимальное количество контрактов, которое может содержать свеча.
		/// </summary>
		[DataMember]
		public decimal Volume { get; set; }

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
	/// Свеча, группируемая по ценовому диапазону.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayName("Range")]
	public class RangeCandle : Candle
	{
		/// <summary>
		/// Дельта цены, в рамках которой свеча может содержать сделки.
		/// </summary>
		[DataMember]
		public Unit PriceRange { get; set; }

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
	/// Свеча пункто-цифрового графика (график крестики-нолики).
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayName("X&0")]
	public class PnFCandle : Candle
	{
		/// <summary>
		/// Значение параметров.
		/// </summary>
		[DataMember]
		public PnFArg PnFArg { get; set; }

		/// <summary>
		/// Тип символов.
		/// </summary>
		[DataMember]
		public PnFTypes Type { get; set; }

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
	/// Свеча Рэнко графика.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayName("Renko")]
	public class RenkoCandle : Candle
	{
		/// <summary>
		/// Изменение цены, при превышении которого регистрируется новая свеча.
		/// </summary>
		[DataMember]
		public Unit BoxSize { get; set; }

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
