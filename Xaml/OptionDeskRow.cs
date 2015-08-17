namespace StockSharp.Xaml
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Derivatives;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Строчка доски.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str1525Key)]
	class OptionDeskRow : NotifiableObject
	{
		/// <summary>
		/// Информация <see cref="OptionTypes.Call"/> или <see cref="OptionTypes.Put"/> строчки доски.
		/// </summary>
		[ExpandableObject]
		public sealed class OptionDeskRowSide
		{
			private readonly OptionDesk _desk;

			internal OptionDeskRowSide(OptionDesk desk, Security option)
			{
				if (desk == null)
					throw new ArgumentNullException("desk");

				if (option == null)
					throw new ArgumentNullException("option");

				_desk = desk;
				Option = option;
				
				ApplyModel();
			}

			/// <summary>
			/// Цена лучшей покупки.
			/// </summary>
			public decimal? BestBidPrice { get; set; }

			/// <summary>
			/// Цена лучшей покупки.
			/// </summary>
			public decimal? BestAskPrice { get; set; }

			/// <summary>
			/// Объем за сессию.
			/// </summary>
			public decimal? Volume { get; set; }

			/// <summary>
			/// Теоретическая цена.
			/// </summary>
			public decimal? TheorPrice { get; set; }

			/// <summary>
			/// Reserved.
			/// </summary>
			[Browsable(false)]
			public decimal MaxOpenInterest { get; set; }

			/// <summary>
			/// Информация об опционе.
			/// </summary>
			[ReadOnly(true)]
			[DisplayNameLoc(LocalizedStrings.OptionsContractKey)]
			[DescriptionLoc(LocalizedStrings.Str1526Key)]
			public Security Option { get; private set; }

			/// <summary>
			/// Дельта опциона.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.DeltaKey)]
			[DescriptionLoc(LocalizedStrings.OptionDeltaKey)]
			public decimal? Delta { get; set; }

			/// <summary>
			/// Гамма опциона.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.GammaKey)]
			[DescriptionLoc(LocalizedStrings.OptionGammaKey)]
			public decimal? Gamma { get; set; }

			/// <summary>
			/// Тета опциона.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.ThetaKey)]
			[DescriptionLoc(LocalizedStrings.OptionThetaKey)]
			public decimal? Theta { get; set; }

			/// <summary>
			/// Вега опциона.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.VegaKey)]
			[DescriptionLoc(LocalizedStrings.OptionVegaKey)]
			public decimal? Vega { get; set; }

			/// <summary>
			/// Ро опциона.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.RhoKey)]
			[DescriptionLoc(LocalizedStrings.OptionRhoKey)]
			public decimal? Rho { get; set; }

			/// <summary>
			/// Открытый интерес.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.OIKey)]
			[DescriptionLoc(LocalizedStrings.OpenInterestKey)]
			public decimal? OpenInterest { get; set; }

			/// <summary>
			/// Прибыльность опциона.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.Str1527Key)]
			[DescriptionLoc(LocalizedStrings.Str1528Key)]
			[CategoryLoc(LocalizedStrings.Str1529Key)]
			public decimal? PnL { get; set; }

			/// <summary>
			/// Reserved.
			/// </summary>
			[Browsable(false)]
			public decimal MaxVolume { get; set; }

			/// <summary>
			/// Получить строковое представление.
			/// </summary>
			/// <returns>Строковое представление.</returns>
			public override string ToString()
			{
				return string.Empty;
			}

			public BlackScholes Model { get; private set; }

			internal void ApplyModel()
			{
				Model = _desk.UseBlackModel ? new Black(Option, _desk.SecurityProvider, _desk.MarketDataProvider) : new BlackScholes(Option, _desk.SecurityProvider, _desk.MarketDataProvider);
			}
		}

		private readonly OptionDesk _desk;

		internal OptionDeskRow(OptionDesk desk, Security call, Security put)
		{
			if (desk == null)
				throw new ArgumentNullException("desk");

			if (call == null && put == null)
				throw new ArgumentException(LocalizedStrings.Str1530);

			if (call != null)
				Call = new OptionDeskRowSide(desk, call);

			if (put != null)
				Put = new OptionDeskRowSide(desk, put);

			_desk = desk;
			Strike = (call ?? put).Strike;
			UnderlyingAsset = (call ?? put).GetUnderlyingAsset(desk.SecurityProvider);
		}

		/// <summary>
		/// Страйк цена опциона.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.StrikeKey)]
		[DescriptionLoc(LocalizedStrings.OptionStrikePriceKey)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public decimal? Strike { get; private set; }

		/// <summary>
		/// Волатильность (подразумеваемая) опциона.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str293Key)]
		[DescriptionLoc(LocalizedStrings.Str1531Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public decimal? ImpliedVolatility
		{
			get
			{
				var provider = _desk.MarketDataProvider;

				if (provider == null)
					return null;

				return (decimal?)provider.GetSecurityValue((Call ?? Put).Option, Level1Fields.ImpliedVolatility);
			}
		}

		/// <summary>
		/// Волатильность (историческая) опциона.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str299Key)]
		[DescriptionLoc(LocalizedStrings.Str1532Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public decimal? HistoricalVolatility
		{
			get
			{
				var provider = _desk.MarketDataProvider;

				if (provider == null)
					return null;

				return (decimal?)provider.GetSecurityValue((Call ?? Put).Option, Level1Fields.HistoricalVolatility);
			}
		}

		/// <summary>
		/// Прибыльность опциона.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str1527Key)]
		[DescriptionLoc(LocalizedStrings.Str1528Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public decimal? PnL
		{
			get
			{
				decimal? pnl = null;

				if (Call != null)
					pnl = Call.PnL * (Call.OpenInterest ?? 0).Max(Call.Option.VolumeStep ?? 1);

				if (Put != null)
				{
					var putPnL = Put.PnL * (Put.OpenInterest ?? 0).Max(Put.Option.VolumeStep ?? 1);

					if (pnl < putPnL)
						pnl = putPnL;
				}

				return pnl;
			}
		}

		/// <summary>
		/// Reserved.
		/// </summary>
		[Browsable(false)]
		public decimal MaxImpliedVolatility { get; set; }

		/// <summary>
		/// Reserved.
		/// </summary>
		[Browsable(false)]
		public decimal MaxHistoricalVolatility { get; set; }

		/// <summary>
		/// Reserved.
		/// </summary>
		[Browsable(false)]
		public decimal MaxPnL { get; set; }

		/// <summary>
		/// Информация о базовом активе.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.UnderlyingAssetKey)]
		[DescriptionLoc(LocalizedStrings.Str1533Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		[ReadOnly(true)]
		public Security UnderlyingAsset { get; private set; }

		/// <summary>
		/// Параметры <see cref="OptionTypes.Call"/> опциона.
		/// </summary>
		[DisplayName("Call")]
		[DescriptionLoc(LocalizedStrings.Str1534Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public OptionDeskRowSide Call { get; private set; }

		/// <summary>
		/// Параметры <see cref="OptionTypes.Put"/> опциона.
		/// </summary>
		[DisplayName("Put")]
		[DescriptionLoc(LocalizedStrings.Str1535Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public OptionDeskRowSide Put { get; private set; }
	}
}