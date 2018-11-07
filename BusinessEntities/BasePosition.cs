#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: BasePosition.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The base class describes the cash position and the position on the instrument.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public abstract class BasePosition : NotifiableObject, IExtendableEntity
	{
		/// <summary>
		/// Initialize <see cref="BasePosition"/>.
		/// </summary>
		protected BasePosition()
		{
		}

		private decimal? _beginValue;

		/// <summary>
		/// Position size at the beginning of the trading session.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str253Key)]
		[DescriptionLoc(LocalizedStrings.Str424Key)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? BeginValue
		{
			get => _beginValue;
			set
			{
				if (_beginValue == value)
					return;

				_beginValue = value;
				NotifyChanged(nameof(BeginValue));
			}
		}

		private decimal? _currentValue;

		/// <summary>
		/// Current position size.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str254Key)]
		[DescriptionLoc(LocalizedStrings.Str425Key)]
		[StatisticsCategory]
		[Nullable]
		//[Browsable(false)]
		public decimal? CurrentValue
		{
			get => _currentValue;
			set
			{
				if (_currentValue == value)
					return;

				_currentValue = value;
				NotifyChanged(nameof(CurrentValue));
			}
		}

		private decimal? _blockedValue;

		/// <summary>
		/// Position size, registered for active orders.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str255Key)]
		[DescriptionLoc(LocalizedStrings.Str426Key)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? BlockedValue
		{
			get => _blockedValue;
			set
			{
				if (_blockedValue == value)
					return;

				_blockedValue = value;
				NotifyChanged(nameof(BlockedValue));
			}
		}

		[field: NonSerialized]
		private IDictionary<string, object> _extensionInfo;

		/// <summary>
		/// Extended information.
		/// </summary>
		/// <remarks>
		/// Required if additional information is stored in the program. For example, the amount of commission paid.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<string, object> ExtensionInfo
		{
			get => _extensionInfo;
			set
			{
				_extensionInfo = value;
				NotifyChanged(nameof(ExtensionInfo));
			}
		}

		private decimal? _currentPrice;

		/// <summary>
		/// Position price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str256Key)]
		[DescriptionLoc(LocalizedStrings.Str428Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public decimal? CurrentPrice
		{
			get => _currentPrice;
			set
			{
				if (_currentPrice == value)
					return;

				_currentPrice = value;
				NotifyChanged(nameof(CurrentPrice));
			}
		}

		private decimal? _averagePrice;

		/// <summary>
		/// Average price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str257Key)]
		[DescriptionLoc(LocalizedStrings.Str429Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public decimal? AveragePrice
		{
			get => _averagePrice;
			set
			{
				if (_averagePrice == value)
					return;

				_averagePrice = value;
				NotifyChanged(nameof(AveragePrice));
			}
		}

		private decimal? _unrealizedPnL;

		/// <summary>
		/// Unrealized profit.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str258Key)]
		[DescriptionLoc(LocalizedStrings.Str430Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public decimal? UnrealizedPnL
		{
			get => _unrealizedPnL;
			set
			{
				if (_unrealizedPnL == value)
					return;

				_unrealizedPnL = value;
				NotifyChanged(nameof(UnrealizedPnL));
			}
		}

		private decimal? _realizedPnL;

		/// <summary>
		/// Realized profit.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str259Key)]
		[DescriptionLoc(LocalizedStrings.Str431Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public decimal? RealizedPnL
		{
			get => _realizedPnL;
			set
			{
				if (_realizedPnL == value)
					return;

				_realizedPnL = value;
				NotifyChanged(nameof(RealizedPnL));
			}
		}

		private decimal? _variationMargin;

		/// <summary>
		/// Variation margin.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str260Key)]
		[DescriptionLoc(LocalizedStrings.Str432Key)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? VariationMargin
		{
			get => _variationMargin;
			set
			{
				if (_variationMargin == value)
					return;

				_variationMargin = value;
				NotifyChanged(nameof(VariationMargin));
			}
		}

		private decimal? _commission;

		/// <summary>
		/// Total commission.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str433Key)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? Commission
		{
			get => _commission;
			set
			{
				if (_commission == value)
					return;

				_commission = value;
				NotifyChanged(nameof(Commission));
			}
		}

		private decimal? _settlementPrice;

		/// <summary>
		/// Settlement price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str312Key)]
		[DescriptionLoc(LocalizedStrings.SettlementPriceKey)]
		[StatisticsCategory]
		[Nullable]
		[Browsable(false)]
		public decimal? SettlementPrice
		{
			get => _settlementPrice;
			set
			{
				if (_settlementPrice == value)
					return;

				_settlementPrice = value;
				NotifyChanged(nameof(SettlementPrice));
			}
		}

		private DateTimeOffset _lastChangeTime;

		/// <summary>
		/// Time of last position change.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str434Key)]
		[DescriptionLoc(LocalizedStrings.Str435Key)]
		[StatisticsCategory]
		[Browsable(false)]
		public DateTimeOffset LastChangeTime
		{
			get => _lastChangeTime;
			set
			{
				_lastChangeTime = value;
				NotifyChanged(nameof(LastChangeTime));
			}
		}

		private DateTimeOffset _localTime;

		/// <summary>
		/// Local time of the last position change.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str530Key)]
		[DescriptionLoc(LocalizedStrings.Str530Key, true)]
		[StatisticsCategory]
		[Browsable(false)]
		public DateTimeOffset LocalTime
		{
			get => _localTime;
			set
			{
				_localTime = value;
				NotifyChanged(nameof(LocalTime));
			}
		}

		private string _description;

		/// <summary>
		/// Text position description.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DescriptionKey)]
		[DescriptionLoc(LocalizedStrings.Str269Key)]
		[MainCategory]
		public string Description
		{
			get => _description;
			set
			{
				_description = value;
				NotifyChanged(nameof(Description));
			}
		}

		private CurrencyTypes? _currency;

		/// <summary>
		/// Portfolio currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str251Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency
		{
			get => _currency;
			set
			{
				_currency = value;
				NotifyChanged(nameof(Currency));
			}
		}

		private DateTimeOffset? _expirationDate;

		/// <summary>
		/// Expiration date.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.ExpiryDateKey)]
		[MainCategory]
		[Nullable]
		public DateTimeOffset? ExpirationDate
		{
			get => _expirationDate;
			set
			{
				_expirationDate = value;
				NotifyChanged(nameof(ExpirationDate));
			}
		}

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[DataMember]
		[MainCategory]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		/// <summary>
		/// To copy fields of the current position to <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The position in which you should to copy fields.</param>
		public void CopyTo(BasePosition destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			destination.CurrentValue = CurrentValue;
			destination.BeginValue = BeginValue;
			destination.BlockedValue = BlockedValue;
			destination.Commission = Commission;
			destination.VariationMargin = VariationMargin;
			destination.RealizedPnL = RealizedPnL;
			destination.UnrealizedPnL = UnrealizedPnL;
			destination.AveragePrice = AveragePrice;
			destination.CurrentPrice = CurrentPrice;
			destination.SettlementPrice = SettlementPrice;
			destination.Description = Description;
			destination.ExpirationDate = ExpirationDate;
			destination.ClientCode = ClientCode;
			//destination.LastChangeTime = LastChangeTime;
			//destination.LocalTime = LocalTime;
		}
	}
}