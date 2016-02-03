#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: OptionDeskRow.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// The board row.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str1525Key)]
	class OptionDeskRow : NotifiableObject
	{
		/// <summary>
		/// Information <see cref="OptionTypes.Call"/> or <see cref="OptionTypes.Put"/> of board row.
		/// </summary>
		[ExpandableObject]
		public sealed class OptionDeskRowSide
		{
			private readonly OptionDesk _desk;

			internal OptionDeskRowSide(OptionDesk desk, Security option)
			{
				if (desk == null)
					throw new ArgumentNullException(nameof(desk));

				if (option == null)
					throw new ArgumentNullException(nameof(option));

				_desk = desk;
				Option = option;
				
				ApplyModel();
			}

			/// <summary>
			/// The best buy price.
			/// </summary>
			public decimal? BestBidPrice { get; set; }

			/// <summary>
			/// The best buy price.
			/// </summary>
			public decimal? BestAskPrice { get; set; }

			/// <summary>
			/// Volume per session.
			/// </summary>
			public decimal? Volume { get; set; }

			/// <summary>
			/// Theoretical price.
			/// </summary>
			public decimal? TheorPrice { get; set; }

			/// <summary>
			/// Reserved.
			/// </summary>
			[Browsable(false)]
			public decimal MaxOpenInterest { get; set; }

			/// <summary>
			/// Information about the option.
			/// </summary>
			[ReadOnly(true)]
			[DisplayNameLoc(LocalizedStrings.OptionsContractKey)]
			[DescriptionLoc(LocalizedStrings.Str1526Key)]
			public Security Option { get; }

			/// <summary>
			/// Option delta.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.DeltaKey)]
			[DescriptionLoc(LocalizedStrings.OptionDeltaKey)]
			public decimal? Delta { get; set; }

			/// <summary>
			/// Option gamma.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.GammaKey)]
			[DescriptionLoc(LocalizedStrings.OptionGammaKey)]
			public decimal? Gamma { get; set; }

			/// <summary>
			/// Option theta.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.ThetaKey)]
			[DescriptionLoc(LocalizedStrings.OptionThetaKey)]
			public decimal? Theta { get; set; }

			/// <summary>
			/// Option vega.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.VegaKey)]
			[DescriptionLoc(LocalizedStrings.OptionVegaKey)]
			public decimal? Vega { get; set; }

			/// <summary>
			/// Option rho.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.RhoKey)]
			[DescriptionLoc(LocalizedStrings.OptionRhoKey)]
			public decimal? Rho { get; set; }

			/// <summary>
			/// Open interest.
			/// </summary>
			[DisplayNameLoc(LocalizedStrings.OIKey)]
			[DescriptionLoc(LocalizedStrings.OpenInterestKey)]
			public decimal? OpenInterest { get; set; }

			/// <summary>
			/// Profitability of an option contract.
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
			/// Returns a string that represents the current object.
			/// </summary>
			/// <returns>A string that represents the current object.</returns>
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
				throw new ArgumentNullException(nameof(desk));

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
		/// Option strike price.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.StrikeKey)]
		[DescriptionLoc(LocalizedStrings.OptionStrikePriceKey)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public decimal? Strike { get; private set; }

		/// <summary>
		/// Option volatility (implied).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str293Key)]
		[DescriptionLoc(LocalizedStrings.Str1531Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public decimal? ImpliedVolatility => (decimal?)_desk.MarketDataProvider?.GetSecurityValue((Call ?? Put).Option, Level1Fields.ImpliedVolatility);

		/// <summary>
		/// Option volatility (historic).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str299Key)]
		[DescriptionLoc(LocalizedStrings.Str1532Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public decimal? HistoricalVolatility => (decimal?)_desk.MarketDataProvider?.GetSecurityValue((Call ?? Put).Option, Level1Fields.HistoricalVolatility);

		/// <summary>
		/// Profitability of an option contract.
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
		/// Information about the underlying asset.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.UnderlyingAssetKey)]
		[DescriptionLoc(LocalizedStrings.Str1533Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		[ReadOnly(true)]
		public Security UnderlyingAsset { get; private set; }

		/// <summary>
		/// Option parameters <see cref="OptionTypes.Call"/>.
		/// </summary>
		[DisplayName("Call")]
		[DescriptionLoc(LocalizedStrings.Str1534Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public OptionDeskRowSide Call { get; }

		/// <summary>
		/// Option parameters <see cref="OptionTypes.Put"/>.
		/// </summary>
		[DisplayName("Put")]
		[DescriptionLoc(LocalizedStrings.Str1535Key)]
		[CategoryLoc(LocalizedStrings.Str1529Key)]
		public OptionDeskRowSide Put { get; }
	}
}