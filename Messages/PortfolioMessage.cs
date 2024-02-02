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
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ActiveKey)]
		Active,
		
		/// <summary>
		/// Blocked.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BlockedKey)]
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
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.NameKey,
			Description = LocalizedStrings.PortfolioNameKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Portfolio currency.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CurrencyKey,
			Description = LocalizedStrings.PortfolioCurrencyKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BoardKey,
			Description = LocalizedStrings.BoardCodeKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string BoardCode { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ClientCodeKey,
			Description = LocalizedStrings.ClientCodeDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string ClientCode { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TransactionKey,
			Description = LocalizedStrings.TransactionIdKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public long TransactionId { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.FromKey,
			Description = LocalizedStrings.StartDateDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public DateTimeOffset? From { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.UntilKey,
			Description = LocalizedStrings.ToDateDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public DateTimeOffset? To { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long? Skip { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long? Count { get; set; }

		/// <inheritdoc />
		[DataMember]
		public FillGapsDays? FillGaps { get; set; }

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
				str += $",Curr={Currency}";

			if (!BoardCode.IsEmpty())
				str += $",Board={BoardCode}";

			if (IsSubscribe)
				str += $",IsSubscribe={IsSubscribe}";

			if (From != default)
				str += $",From={From}";

			if (To != default)
				str += $",To={To}";

			if (Skip != default)
				str += $",Skip={Skip}";

			if (Count != default)
				str += $",Count={Count}";

			if (FillGaps != default)
				str += $",Gaps={FillGaps}";

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
			destination.FillGaps = FillGaps;
		}
	}
}