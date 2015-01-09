namespace StockSharp.Hydra.Core
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Инструмент, ассоциированный с <see cref="IHydraTask"/>.
	/// </summary>
	public class HydraTaskSecurity : NotifiableObject
	{
		/// <summary>
		/// Информация по типу данных.
		/// </summary>
		public class TypeInfo : NotifiableObject
		{
			private long _count;

			/// <summary>
			/// Обработанное количество данных.
			/// </summary>
			public long Count
			{
				get { return _count; }
				set
				{
					_count = value;
					NotifyPropertyChanged("Count");
				}
			}

			private DateTime? _lastTime;

			/// <summary>
			/// Временная метка последних обработанных данных.
			/// </summary>
			[Nullable]
			public DateTime? LastTime
			{
				get { return _lastTime; }
				set
				{
					_lastTime = value;
					NotifyPropertyChanged("LastTime");
				}
			}
		}

		/// <summary>
		/// Создать <see cref="HydraTaskSecurity"/>.
		/// </summary>
		public HydraTaskSecurity()
		{
		}

		/// <summary>
		/// Уникальный идентификатор инструмента.
		/// </summary>
		[Identity]
		[Field("Id", ReadOnly = true)]
		public long Id { get; set; }

		/// <summary>
		/// Настройки.
		/// </summary>
		[RelationSingle(IdentityType = typeof(Guid))]
		public HydraTaskSettings Settings { get; set; }

		/// <summary>
		/// Биржевой инструмент.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		public Security Security { get; set; }

		/// <summary>
		/// Типы данных, которые нужно получать для данного инструмента.
		/// </summary>
		[Collection]
		public Type[] MarketDataTypes
		{
			get { return MarketDataTypesSet.Cache; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				MarketDataTypesSet.Clear();
				MarketDataTypesSet.AddRange(value);
			}
		}

		private CandleSeries[] _candleSeries = ArrayHelper<CandleSeries>.EmptyArray;

		/// <summary>
		/// Серии свечек, которые необходимо скачивать для данного инструмента.
		/// </summary>
		[Collection]
		public CandleSeries[] CandleSeries
		{
			get { return _candleSeries; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_candleSeries = value;
			}
		}

		/// <summary>
		/// Хэш-коллекция для быстрой проверки <see cref="MarketDataTypes"/>.
		/// </summary>
		[Ignore]
		public readonly CachedSynchronizedSet<Type> MarketDataTypesSet = new CachedSynchronizedSet<Type>();

		private TypeInfo _tradeInfo = new TypeInfo();

		/// <summary>
		/// Информация о сделках.
		/// </summary>
		[InnerSchema]
		[NameOverride("Count", "TradeCount")]
		[NameOverride("LastTime", "TradeLastTime")]
		public TypeInfo TradeInfo
		{
			get { return _tradeInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_tradeInfo = value;
			}
		}

		private TypeInfo _depthInfo = new TypeInfo();

		/// <summary>
		/// Информация о стаканах.
		/// </summary>
		[InnerSchema]
		[NameOverride("Count", "DepthCount")]
		[NameOverride("LastTime", "DepthLastTime")]
		public TypeInfo DepthInfo
		{
			get { return _depthInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_depthInfo = value;
			}
		}

		private TypeInfo _orderLogInfo = new TypeInfo();

		/// <summary>
		/// Информация о логе заявок.
		/// </summary>
		[InnerSchema]
		[NameOverride("Count", "OrderLogCount")]
		[NameOverride("LastTime", "OrderLogLastTime")]
		public TypeInfo OrderLogInfo
		{
			get { return _orderLogInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_orderLogInfo = value;
			}
		}

		private TypeInfo _level1Info = new TypeInfo();

		/// <summary>
		/// Информация о Level1.
		/// </summary>
		[InnerSchema]
		[NameOverride("Count", "Level1Count")]
		[NameOverride("LastTime", "Level1LastTime")]
		public TypeInfo Level1Info
		{
			get { return _level1Info; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_level1Info = value;
			}
		}

		private TypeInfo _candleInfo = new TypeInfo();

		/// <summary>
		/// Информация о свечах.
		/// </summary>
		[InnerSchema]
		[NameOverride("Count", "CandleCount")]
		[NameOverride("LastTime", "CandleLastTime")]
		public TypeInfo CandleInfo
		{
			get { return _candleInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_candleInfo = value;
			}
		}

		private TypeInfo _executionInfo = new TypeInfo();

		/// <summary>
		/// Информация о логе собственных транзакций.
		/// </summary>
		[InnerSchema]
		[NameOverride("Count", "ExecutionCount")]
		[NameOverride("LastTime", "ExecutionLastTime")]
		public TypeInfo ExecutionInfo
		{
			get { return _executionInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_executionInfo = value;
			}
		}
	}
}