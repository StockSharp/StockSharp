#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: Portfolio.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Portfolio, describing the trading account and the size of its generated commission.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
	[DescriptionLoc(LocalizedStrings.Str541Key)]
	public class Portfolio : BasePosition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Portfolio"/>.
		/// </summary>
		public Portfolio()
		{
		}

		private string _name;

		/// <summary>
		/// Portfolio code name.
		/// </summary>
		[DataMember]
		[Identity]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string Name
		{
			get => _name;
			set
			{
				if (_name == value)
					return;

				_name = value;
				NotifyChanged(nameof(Name));
			}
		}

		private decimal? _leverage;

		/// <summary>
		/// Margin leverage.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.LeverageKey)]
		[DescriptionLoc(LocalizedStrings.Str261Key, true)]
		[MainCategory]
		[Nullable]
		public decimal? Leverage
		{
			get => _leverage;
			set
			{
				if (_leverage == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_leverage = value;
				NotifyChanged(nameof(Leverage));
			}
		}

		//[field: NonSerialized]
		//private IConnector _connector;

		///// <summary>
		///// Connection to the trading system, through which this portfolio has been loaded.
		///// </summary>
		//[Ignore]
		//[XmlIgnore]
		//[Browsable(false)]
		//[Obsolete("The property Connector was obsoleted and is always null.")]
		//public IConnector Connector
		//{
		//	get { return _connector; }
		//	set { _connector = value; }
		//}

		/// <summary>
		/// Exchange board, for which the current portfolio is active.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.Str544Key)]
		[MainCategory]
		public ExchangeBoard Board { get; set; }

		private PortfolioStates? _state;

		/// <summary>
		/// Portfolio state.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str252Key)]
		[MainCategory]
		[Nullable]
		[Browsable(false)]
		public PortfolioStates? State
		{
			get => _state;
			set
			{
				if (_state == value)
					return;

				_state = value;
				NotifyChanged(nameof(State));
			}
		}

		private decimal? _commissionTaker;

		/// <summary>
		/// Commission (taker).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public decimal? CommissionTaker
		{
			get => _commissionTaker;
			set
			{
				_commissionTaker = value;
				NotifyChanged(nameof(CommissionTaker));
			}
		}

		private decimal? _commissionMaker;

		/// <summary>
		/// Commission (maker).
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public decimal? CommissionMaker
		{
			get => _commissionMaker;
			set
			{
				_commissionMaker = value;
				NotifyChanged(nameof(CommissionMaker));
			}
		}

		/// <summary>
		/// Portfolio associated with the orders received through the orders log.
		/// </summary>
		public static Portfolio AnonymousPortfolio { get; } = new Portfolio { Name = LocalizedStrings.Str545 };

		/// <summary>
		/// Create a copy of <see cref="Portfolio"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public Portfolio Clone()
		{
			var clone = new Portfolio();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// To copy the current portfolio fields to the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The portfolio, in which fields should be copied.</param>
		public void CopyTo(Portfolio destination)
		{
			base.CopyTo(destination);

			destination.Name = Name;
			destination.Board = Board;
			destination.Currency = Currency;
			destination.Leverage = Leverage;
			//destination.Connector = Connector;
			destination.State = State;
			destination.CommissionMaker = CommissionMaker;
			destination.CommissionTaker = CommissionTaker;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}
