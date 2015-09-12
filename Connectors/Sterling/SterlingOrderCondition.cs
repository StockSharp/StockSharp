namespace StockSharp.Sterling
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Расширенные типы заявок.
	/// </summary>
	public enum SterlingExtendedOrderTypes
	{
		/// <summary>
		/// Рыночная по закрытию.
		/// </summary>
		MarketOnClose,

		/// <summary>
		/// Рыночная или лучше.
		/// </summary>
		MarketOrBetter,

		/// <summary>
		/// Рыночная без ожидания.
		/// </summary>
		MarketNoWait,

		/// <summary>
		/// Лимитная по закрытию.
		/// </summary>
		LimitOnClose,

		/// <summary>
		/// Стоп.
		/// </summary>
		Stop,

		/// <summary>
		/// Стоп-лимит
		/// </summary>
		StopLimit,

		/// <summary>
		/// Лимитная или лучше
		/// </summary>
		LimitOrBetter,

		/// <summary>
		/// Лимитная без ожидания.
		/// </summary>
		LimitNoWait,

		/// <summary>
		/// Без ожидания
		/// </summary>
		NoWait,

		/// <summary>
		/// NYSE.
		/// </summary>
		Nyse,

		/// <summary>
		/// По закрытию.
		/// </summary>
		Close,

		/// <summary>
		/// Привязанная.
		/// </summary>
		Pegged,

		/// <summary>
		/// Серверный стоп.
		/// </summary>
		ServerStop,

		/// <summary>
		/// Серверный стоп-лимит.
		/// </summary>
		ServerStopLimit,

		/// <summary>
		/// Скользящий стоп.
		/// </summary>
		TrailingStop,

		/// <summary>
		/// По последней цене.
		/// </summary>
		Last
	}

	/// <summary>
	/// Инструкции исполнения.
	/// </summary>
	public enum SterlingExecutionInstructions
	{
		/// <summary>
		/// С резервированием.
		/// </summary>
		SweepReserve,

		/// <summary>
		/// Без преференций.
		/// </summary>
		NoPreference
	}

	/// <summary>
	/// Условие заявок, специфичных для <see cref="Sterling"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "Sterling")]
	public class SterlingOrderCondition : OrderCondition
	{
		/// <summary>
		/// Настройки для заявок на опционы.
		/// </summary>
		public class SterlingOptionOrderCondition
		{
			private readonly SterlingOrderCondition _condition;

			internal SterlingOptionOrderCondition(SterlingOrderCondition condition)
			{
				_condition = condition;
			}

			/// <summary>
			/// Открытие.
			/// </summary>
			public bool? IsOpen
			{
				get { return (bool?)_condition.Parameters.TryGetValue("OptionIsOpen"); }
				set { _condition.Parameters["OptionIsOpen"] = value; }
			}

			/// <summary>
			/// Дата поставки.
			/// </summary>
			public DateTime? Maturity
			{
				get { return (DateTime?)_condition.Parameters.TryGetValue("OptionMaturity"); }
				set { _condition.Parameters["OptionMaturity"] = value; }
			}

			/// <summary>
			/// Тип опциона.
			/// </summary>
			public OptionTypes? Type
			{
				get { return (OptionTypes?)_condition.Parameters.TryGetValue("OptionType"); }
				set { _condition.Parameters["OptionType"] = value; }
			}

			/// <summary>
			/// Код базового актива.
			/// </summary>
			public string UnderlyingCode
			{
				get { return (string)_condition.Parameters.TryGetValue("OptionUnderlyingCode"); }
				set { _condition.Parameters["OptionUnderlyingCode"] = value; }
			}

			/// <summary>
			/// Покрытый опцион.
			/// </summary>
			public bool? IsCover
			{
				get { return (bool?)_condition.Parameters.TryGetValue("OptionIsCover"); }
				set { _condition.Parameters["OptionIsCover"] = value; }
			}

			/// <summary>
			/// Тип базового актива.
			/// </summary>
			public SecurityTypes? UnderlyingType
			{
				get { return (SecurityTypes?)_condition.Parameters.TryGetValue("OptionUnderlyingType"); }
				set { _condition.Parameters["OptionUnderlyingType"] = value; }
			}

			/// <summary>
			/// Страйк-цена.
			/// </summary>
			public decimal? StrikePrice
			{
				get { return (decimal?)_condition.Parameters.TryGetValue("OptionStrikePrice"); }
				set { _condition.Parameters["OptionStrikePrice"] = value; }
			}
		}

		/// <summary>
		/// Создать <see cref="SterlingOrderCondition"/>.
		/// </summary>
		public SterlingOrderCondition()
		{
			Options = new SterlingOptionOrderCondition(this);
		}

		/// <summary>
		/// Цена активации, при достижении которой будет выставлена заявка.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str242Key)]
		[DisplayNameLoc(LocalizedStrings.Str2455Key)]
		[DescriptionLoc(LocalizedStrings.Str3460Key)]
		public decimal? StopPrice 
		{
			get { return (decimal?)Parameters.TryGetValue("StopPrice"); }
			set { Parameters["StopPrice"] = value; }
		}

		/// <summary>
		/// Расширенный тип заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2368Key)]
		[DescriptionLoc(LocalizedStrings.Str2369Key)]
		public SterlingExtendedOrderTypes? ExtendedOrderType
		{
			get { return (SterlingExtendedOrderTypes?)Parameters.TryGetValue("ExtendedOrderType"); }
			set { Parameters["ExtendedOrderType ="] = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public decimal? Discretion { get; set; }

		/// <summary>
		/// Инструкция исполнения.
		/// </summary>
		public SterlingExecutionInstructions ExecutionInstruction { get; set; }

		/// <summary>
		/// Исполняющий брокер.
		/// </summary>
		public string ExecutionBroker { get; set; }

		/// <summary>
		/// Лимит цены исполнения.
		/// </summary>
		public decimal? ExecutionPriceLimit { get; set; }

		/// <summary>
		/// Peg diff.
		/// </summary>
		public decimal? PegDiff { get; set; }

		/// <summary>
		/// Объем скользящего стопа.
		/// </summary>
		public decimal? TrailingVolume { get; set; }

		/// <summary>
		/// Шаг увеличения цены скользящего стопа.
		/// </summary>
		public decimal? TrailingIncrement { get; set; }

		/// <summary>
		/// Минимальный объем.
		/// </summary>
		public decimal? MinVolume { get; set; }

		/// <summary>
		/// Средняя цена исполнения.
		/// </summary>
		public decimal? AveragePriceLimit { get; set; }

		/// <summary>
		/// Продолжительность.
		/// </summary>
		public int? Duration { get; set; }

		/// <summary>
		/// Брокер.
		/// </summary>
		public string LocateBroker { get; set; }

		/// <summary>
		/// Объем.
		/// </summary>
		public decimal? LocateVolume { get; set; }

		/// <summary>
		/// Время.
		/// </summary>
		public DateTime? LocateTime { get; set; }

		/// <summary>
		/// Настройки для заявок на опционы.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1529Key)]
		[DescriptionLoc(LocalizedStrings.Str3800Key)]
		[ExpandableObject]
		public SterlingOptionOrderCondition Options { get; private set; }
	}
}