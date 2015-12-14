#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Sterling.Sterling
File: SterlingOrderCondition.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Sterling
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Extended order types.
	/// </summary>
	public enum SterlingExtendedOrderTypes
	{
		/// <summary>
		/// Market on close.
		/// </summary>
		MarketOnClose,

		/// <summary>
		/// Market or better.
		/// </summary>
		MarketOrBetter,

		/// <summary>
		/// Market no wait.
		/// </summary>
		MarketNoWait,

		/// <summary>
		/// Limit on close.
		/// </summary>
		LimitOnClose,

		/// <summary>
		/// Stop.
		/// </summary>
		Stop,

		/// <summary>
		/// Stop-limit.
		/// </summary>
		StopLimit,

		/// <summary>
		/// Limit or better.
		/// </summary>
		LimitOrBetter,

		/// <summary>
		/// Limit no wait.
		/// </summary>
		LimitNoWait,

		/// <summary>
		/// Not wait.
		/// </summary>
		NoWait,

		/// <summary>
		/// NYSE.
		/// </summary>
		Nyse,

		/// <summary>
		/// On close.
		/// </summary>
		Close,

		/// <summary>
		/// Pegged.
		/// </summary>
		Pegged,

		/// <summary>
		/// Server stop.
		/// </summary>
		ServerStop,

		/// <summary>
		/// Server stop-limit.
		/// </summary>
		ServerStopLimit,

		/// <summary>
		/// Trailing stop-loss.
		/// </summary>
		TrailingStop,

		/// <summary>
		/// By last price.
		/// </summary>
		Last
	}

	/// <summary>
	/// Execution instructions.
	/// </summary>
	public enum SterlingExecutionInstructions
	{
		/// <summary>
		/// Reservation.
		/// </summary>
		SweepReserve,

		/// <summary>
		/// No preference.
		/// </summary>
		NoPreference
	}

	/// <summary>
	/// <see cref="Sterling"/> order condition.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "Sterling")]
	public class SterlingOrderCondition : OrderCondition
	{
		/// <summary>
		/// Options orders settings.
		/// </summary>
		public class SterlingOptionOrderCondition
		{
			private readonly SterlingOrderCondition _condition;

			internal SterlingOptionOrderCondition(SterlingOrderCondition condition)
			{
				_condition = condition;
			}

			/// <summary>
			/// Open time.
			/// </summary>
			public bool? IsOpen
			{
				get { return (bool?)_condition.Parameters.TryGetValue("OptionIsOpen"); }
				set { _condition.Parameters["OptionIsOpen"] = value; }
			}

			/// <summary>
			/// Settlement date.
			/// </summary>
			public DateTime? Maturity
			{
				get { return (DateTime?)_condition.Parameters.TryGetValue("OptionMaturity"); }
				set { _condition.Parameters["OptionMaturity"] = value; }
			}

			/// <summary>
			/// Option type.
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
			/// Covered option.
			/// </summary>
			public bool? IsCover
			{
				get { return (bool?)_condition.Parameters.TryGetValue("OptionIsCover"); }
				set { _condition.Parameters["OptionIsCover"] = value; }
			}

			/// <summary>
			/// Asset type.
			/// </summary>
			public SecurityTypes? UnderlyingType
			{
				get { return (SecurityTypes?)_condition.Parameters.TryGetValue("OptionUnderlyingType"); }
				set { _condition.Parameters["OptionUnderlyingType"] = value; }
			}

			/// <summary>
			/// Strike price.
			/// </summary>
			public decimal? StrikePrice
			{
				get { return (decimal?)_condition.Parameters.TryGetValue("OptionStrikePrice"); }
				set { _condition.Parameters["OptionStrikePrice"] = value; }
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SterlingOrderCondition"/>.
		/// </summary>
		public SterlingOrderCondition()
		{
			Options = new SterlingOptionOrderCondition(this);
		}

		/// <summary>
		/// Activation price, when reached an order will be placed.
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
		/// Extended type of order.
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
		/// .
		/// </summary>
		public decimal? Discretion { get; set; }

		/// <summary>
		/// Execution instruction.
		/// </summary>
		public SterlingExecutionInstructions ExecutionInstruction { get; set; }

		/// <summary>
		/// Execution brokerage.
		/// </summary>
		public string ExecutionBroker { get; set; }

		/// <summary>
		/// Limit price.
		/// </summary>
		public decimal? ExecutionPriceLimit { get; set; }

		/// <summary>
		/// Peg diff.
		/// </summary>
		public decimal? PegDiff { get; set; }

		/// <summary>
		/// Trailing stop volume.
		/// </summary>
		public decimal? TrailingVolume { get; set; }

		/// <summary>
		/// Trailing stop price step.
		/// </summary>
		public decimal? TrailingIncrement { get; set; }

		/// <summary>
		/// Minimum volume.
		/// </summary>
		public decimal? MinVolume { get; set; }

		/// <summary>
		/// Average price.
		/// </summary>
		public decimal? AveragePriceLimit { get; set; }

		/// <summary>
		/// Duration.
		/// </summary>
		public int? Duration { get; set; }

		/// <summary>
		/// Broker.
		/// </summary>
		public string LocateBroker { get; set; }

		/// <summary>
		/// Volume.
		/// </summary>
		public decimal? LocateVolume { get; set; }

		/// <summary>
		/// Time.
		/// </summary>
		public DateTime? LocateTime { get; set; }

		/// <summary>
		/// Options orders settings.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1529Key)]
		[DescriptionLoc(LocalizedStrings.Str3800Key)]
		[ExpandableObject]
		public SterlingOptionOrderCondition Options { get; private set; }
	}
}