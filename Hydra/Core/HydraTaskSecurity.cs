#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: HydraTaskSecurity.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Algo;
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
					NotifyPropertyChanged(nameof(Count));
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
					NotifyPropertyChanged(nameof(LastTime));
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
		public DataType[] DataTypes
		{
			get { return DataTypesSet.Cache; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (value.Any(t => t.MessageType == null))
					throw new ArgumentException(nameof(value));

				DataTypesSet.Clear();
				DataTypesSet.AddRange(value);
			}
		}

		//private CandleSeries[] _candleSeries = ArrayHelper.Empty<CandleSeries>();

		///// <summary>
		///// Серии свечек, которые необходимо скачивать для данного инструмента.
		///// </summary>
		//[Collection]
		//public CandleSeries[] CandleSeries
		//{
		//	get { return _candleSeries; }
		//	set
		//	{
		//		if (value == null)
		//			throw new ArgumentNullException(nameof(value));

		//		_candleSeries = value;
		//	}
		//}

		/// <summary>
		/// Хэш-коллекция для быстрой проверки <see cref="DataTypes"/>.
		/// </summary>
		[Ignore]
		public readonly CachedSynchronizedSet<DataType> DataTypesSet = new CachedSynchronizedSet<DataType>();

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
					throw new ArgumentNullException(nameof(value));

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
					throw new ArgumentNullException(nameof(value));

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
					throw new ArgumentNullException(nameof(value));

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
					throw new ArgumentNullException(nameof(value));

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
					throw new ArgumentNullException(nameof(value));

				_candleInfo = value;
			}
		}

		private TypeInfo _transactionInfo = new TypeInfo();

		/// <summary>
		/// Информация о логе собственных транзакций.
		/// </summary>
		[InnerSchema]
		[NameOverride("Count", "ExecutionCount")]
		[NameOverride("LastTime", "ExecutionLastTime")]
		public TypeInfo TransactionInfo
		{
			get { return _transactionInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_transactionInfo = value;
			}
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			var s = Security;
			return s?.ToString() ?? string.Empty;
		}
	}
}