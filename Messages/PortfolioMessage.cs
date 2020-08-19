#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: PortfolioMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Portfolio states.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum PortfolioStates
	{
		/// <summary>
		/// Active.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str248Key)]
		Active,
		
		/// <summary>
		/// Blocked.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str249Key)]
		Blocked,
	}

	/// <summary>
	/// The message contains information about portfolio.
	/// </summary>
	[DataContract]
	[Serializable]
	public class PortfolioMessage : BaseSubscriptionIdMessage<PortfolioMessage>,
	        ISubscriptionMessage, IPortfolioNameMessage
	{
		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Portfolio currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str251Key)]
		[MainCategory]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey, true)]
		[MainCategory]
		public string BoardCode { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[DataMember]
		[MainCategory]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		///// <summary>
		///// Portfolio state.
		///// </summary>
		//[DataMember]
		//[DisplayNameLoc(LocalizedStrings.StateKey)]
		//[DescriptionLoc(LocalizedStrings.Str252Key)]
		//[MainCategory]
		//public PortfolioStates? State { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str343Key)]
		[DescriptionLoc(LocalizedStrings.Str344Key)]
		[MainCategory]
		public DateTimeOffset? From { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str345Key)]
		[DescriptionLoc(LocalizedStrings.Str346Key)]
		[MainCategory]
		public DateTimeOffset? To { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long? Skip { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long? Count { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioMessage"/>.
		/// </summary>
		public PortfolioMessage()
			: base(MessageTypes.Portfolio)
		{
		}

		/// <summary>
		/// Initialize <see cref="PortfolioMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected PortfolioMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.Portfolio(PortfolioName);

		bool ISubscriptionMessage.FilterEnabled
			=>
			!PortfolioName.IsEmpty() || Currency != null ||
			!BoardCode.IsEmpty() || !ClientCode.IsEmpty();

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Name={PortfolioName}";

			if (TransactionId > 0)
				str += $",TransId={TransactionId}";

			if (Currency != default)
				str += $",Curr={Currency.Value}";

			if (!BoardCode.IsEmpty())
				str += $",Board={BoardCode}";

			if (IsSubscribe)
				str += $",IsSubscribe={IsSubscribe}";

			if (From != default)
				str += $",From={From.Value}";

			if (To != default)
				str += $",To={To.Value}";

			if (Skip != default)
				str += $",Skip={Skip.Value}";

			if (Count != default)
				str += $",Count={Count.Value}";

			return str;
		}

		/// <inheritdoc />
		public override void CopyTo(PortfolioMessage destination)
		{
			base.CopyTo(destination);

			destination.PortfolioName = PortfolioName;
			destination.Currency = Currency;
			destination.BoardCode = BoardCode;
			destination.IsSubscribe = IsSubscribe;
			//destination.State = State;
			destination.TransactionId = TransactionId;
			destination.ClientCode = ClientCode;
			destination.From = From;
			destination.To = To;
			destination.Skip = Skip;
			destination.Count = Count;
		}
	}
}