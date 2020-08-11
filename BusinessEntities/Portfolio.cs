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
	public class Portfolio : Position
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
				NotifyChanged();
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
				NotifyChanged();
			}
		}

		/// <summary>
		/// Portfolio associated with the orders received through the orders log.
		/// </summary>
		public static Portfolio AnonymousPortfolio { get; } = new Portfolio
		{
			Name = Extensions.AnonymousPortfolioName,
		};

		/// <summary>
		/// Create virtual portfolio for simulation.
		/// </summary>
		/// <returns>Simulator.</returns>
		public static Portfolio CreateSimulator() => new Portfolio
		{
			Name = Extensions.SimulatorPortfolioName,
			BeginValue = 1000000,
		};

		/// <summary>
		/// To copy the current portfolio fields to the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The portfolio, in which fields should be copied.</param>
		public void CopyTo(Portfolio destination)
		{
			base.CopyTo(destination);

			destination.Name = Name;
			destination.Board = Board;
			//destination.Connector = Connector;
			destination.State = State;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return Name;
		}

		/// <inheritdoc />
		public override Position Clone()
		{
			var clone = new Portfolio();
			CopyTo(clone);
			return clone;
		}
	}
}
