namespace StockSharp.OpenECry
{
	using System.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Тип условной заявки OEC.
	/// </summary>
	public enum OpenECryStopType
	{
		/// <summary>
		/// После достижения стоп-цены автоматически выставляется рыночная заявка.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str242Key)]
		StopMarket,

		/// <summary>
		/// После достижения стоп-цены автоматически выставляется лимитная заявка.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1733Key)]
		StopLimit,

		/// <summary>
		/// Стоп-цена автоматически следует за рынком, но только в выгодном для позиции направлении, 
		/// оставаясь на заранее заявленном интервале от рыночной цены. 
		/// В случае, если рынок достигает стоп-цены, автоматически выставляется рыночная заявка.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.TrailingStopLossKey)]
		TrailingStopMarket,

		/// <summary>
		/// Как <see cref="TrailingStopMarket"/>, но при достижении стоп-цены выставляется лимитная заявка.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.TrailingStopLimitKey)]
		TrailingStopLimit
	}

	// OEC trailing stop description: 
	// http://www.openecry.com/cfbb/index.cfm?page=topic&topicID=532
	// http://www.openecry.com/cfbb/index.cfm?page=topic&topicID=225


	/// <summary>
	/// Условие заявок, специфичных для <see cref="OEC"/>.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "OpenECry")]
	public class OpenECryOrderCondition : OrderCondition
	{
		internal enum AssetTypeEnum
		{
			All,
			Equity,
			Future
		}

		private const string _keyStopType = "StopType";
		private const string _keyStopPrice = "StopPrice";
		private const string _keyDelta = "Delta";
		private const string _keyIsPercentDelta = "IsPercentDelta";
		private const string _keyTriggerType = "TriggerType";
		private const string _keyReferencePrice = "ReferencePrice";
		private const string _keyAssetType = "AssetType";

		/// <summary>
		/// Создать <see cref="OpenECryOrderCondition"/>.
		/// </summary>
		public OpenECryOrderCondition()
		{
			AssetType = AssetTypeEnum.Equity;
			StopType = OpenECryStopType.StopLimit;
			StopPrice = 0;
			Delta = 0;
			IsPercentDelta = false;
			TriggerType = Level1Fields.LastTrade;
		}

		///// <summary>
		///// Конструктор для <see cref="OECStopType.StopMarket"/> или <see cref="OECStopType.StopLimit"/> типов.
		///// </summary>
		///// <param name="type">Тип стопа: <see cref="OECStopType.StopMarket"/> или <see cref="OECStopType.StopLimit"/>.</param>
		///// <param name="stopPrice">Стоп-цена. Для типа <see cref="OECStopType.StopMarket"/> используется вместо <see cref="Order.Price"/>.</param>
		//public OECOrderCondition(OECStopType type, decimal stopPrice)
		//{
		//	if (!(type == OECStopType.StopLimit || type == OECStopType.StopMarket))
		//		throw new ArgumentException("Для Trailing типов используйте другой конструктор.");

		//	AssetType = AssetTypeEnum.All;
		//	StopType = type;
		//	StopPrice = stopPrice;
		//}

		///// <summary>
		///// Конструктор для Trailing стопов.
		///// </summary>
		///// <remarks>
		///// <para>
		///// Если тип стопа <paramref name="type"/> равен <see cref="OECStopType.TrailingStopLimit"/>,
		///// то после срабатывания стопа будет выставлена заявка по цене <see cref="Order.Price"/>,
		///// сдвинутой логикой trailing стопа на соответствующее значение.
		///// </para>
		///// <para>
		///// Если тип стопа <paramref name="type"/> равен <see cref="OECStopType.TrailingStopMarket"/>,
		///// то после срабатывания стопа будет выставлена рыночная заявка.
		///// </para>
		///// </remarks>
		///// <param name="type">Тип стопа: <see cref="OECStopType.TrailingStopMarket"/> или <see cref="OECStopType.TrailingStopLimit"/>.</param>
		///// <param name="delta">Trailing стоп следует за рынком в выгодном направлении если разница между рыночной ценой и стопом больше <paramref name="delta"/>.</param>
		///// <param name="isPercentDelta"><see langword="true"/>, если <paramref name="delta"/> выражена в процентах.</param>
		///// <param name="triggerType">Тип срабатывания стопа.</param>
		///// <param name="stopPrice">Начальная стоп-цена, которая двигается логикой trailing-стопа.</param>
		//[CLSCompliant(false)]
		//public OECOrderCondition(OECStopType type, decimal delta, bool isPercentDelta, SecurityChangeTypes triggerType, decimal stopPrice)
		//{
		//	if (!(type == OECStopType.TrailingStopLimit || type == OECStopType.TrailingStopMarket))
		//		throw new ArgumentException("Для не-Trailing стопов используйте другой конструктор.");

		//	AssetType = AssetTypeEnum.Equity;
		//	StopType = type;
		//	StopPrice = stopPrice;
		//	Delta = delta;
		//	IsPercentDelta = isPercentDelta;
		//	TriggerType = triggerType;
		//}

		///// <summary>
		///// Конструктор для Trailing стопов для Futures.
		///// </summary>
		///// <remarks>
		///// <para>
		///// Если тип стопа <paramref name="type"/> равен <see cref="OECStopType.TrailingStopLimit"/>, то после срабатывания стопа будет выставлена
		///// заявка по цене <see cref="Order.Price"/>, сдвинутой логикой trailing стопа на соответствующее значение.
		///// </para>
		///// <para>
		///// Если тип стопа <paramref name="type"/> равен <see cref="OECStopType.TrailingStopMarket"/>, то после срабатывания стопа будет выставлена
		///// рыночная заявка.
		///// </para>
		///// </remarks>
		///// <param name="type"><see cref="OECStopType.TrailingStopMarket"/> или <see cref="OECStopType.TrailingStopLimit"/>.</param>
		///// <param name="delta">Trailing стоп следует за рынком в выгодном направлении если разница между рыночной ценой и стопом больше <paramref name="delta"/>.</param>
		///// <param name="referencePrice">Trailing стоп начинает слежение, как только цена достигает <paramref name="referencePrice"/>.</param>
		///// <param name="stopPrice">Начальная стоп-цена, которая двигается логикой trailing-стопа после активации по <paramref name="referencePrice"/>.</param>
		//public OECOrderCondition(OECStopType type, decimal delta, decimal referencePrice, decimal stopPrice)
		//{
		//	if (!(type == OECStopType.TrailingStopLimit || type == OECStopType.TrailingStopMarket))
		//		throw new ArgumentException("Для не-Trailing стопов используйте другой конструктор.");

		//	AssetType = AssetTypeEnum.Future;
		//	StopType = type;
		//	StopPrice = stopPrice;
		//	Delta = delta;
		//	ReferencePrice = referencePrice;
		//}

		/// <summary>
		/// Тип стопа.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2570Key)]
		[DescriptionLoc(LocalizedStrings.Str2571Key)]
		public OpenECryStopType StopType
		{
			get { return (OpenECryStopType)Parameters[_keyStopType]; }
			set { Parameters[_keyStopType] = value; }
		}

		/// <summary>
		/// Стоп-цена.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.StopPriceKey)]
		[DescriptionLoc(LocalizedStrings.StopPriceKey, true)]
		public decimal StopPrice
		{
			get { return (decimal)Parameters[_keyStopPrice]; }
			set { Parameters[_keyStopPrice] = value; }
		}

		/// <summary>
		/// Trailing стоп следует за рынком если изменение цены больше чем Delta.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayName("Trailing Delta")]
		[DescriptionLoc(LocalizedStrings.Str2572Key)]
		public decimal Delta
		{
			get { return (decimal)Parameters[_keyDelta]; }
			set { Parameters[_keyDelta] = value; }
		}

		/// <summary>
		/// <see langword="true"/>, если <see cref="Delta"/> выражена в процентах.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2573Key)]
		[DescriptionLoc(LocalizedStrings.Str2574Key)]
		public bool IsPercentDelta
		{
			get { return (bool)Parameters[_keyIsPercentDelta]; }
			set { Parameters[_keyIsPercentDelta] = value; }
		}

		/// <summary>
		/// Поле срабатывания.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2575Key)]
		[DescriptionLoc(LocalizedStrings.Str2576Key)]
		public Level1Fields TriggerType
		{
			get { return (Level1Fields)Parameters[_keyTriggerType]; }
			set { Parameters[_keyTriggerType] = value; }
		}

		/// <summary>
		/// Trailing стоп начинает слежение, как только цена достигает ReferencePrice.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayName("Trailing ReferencePrice")]
		[DescriptionLoc(LocalizedStrings.Str2577Key)]
		public decimal ReferencePrice
		{
			get { return (decimal)Parameters[_keyReferencePrice]; }
			set { Parameters[_keyReferencePrice] = value; }
		}

		internal AssetTypeEnum AssetType
		{
			get { return (AssetTypeEnum)Parameters[_keyAssetType]; }
			set { Parameters[_keyAssetType] = value; }
		}
	}
}